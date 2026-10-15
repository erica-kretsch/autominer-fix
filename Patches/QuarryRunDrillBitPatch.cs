using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using UnityEngine;

namespace AutominerFix.Patches
{
    /// <summary>
    /// Stops the Autominer from losing the stop at the corner where it descends a layer.
    ///
    /// <c>RunDrillBit</c> runs from <c>Update</c> (every rendered frame); the gantry only moves
    /// from <c>FixedUpdate</c> (50 Hz). At a layer change <c>HandleOutsideBounds</c> orders the
    /// head down one voxel and sends the carriage's target back inside the pit. On a frame with
    /// no <c>FixedUpdate</c> since the row step, the carriage has not drifted, so it is already
    /// "at" its target and the method carries on regardless of the head still being a voxel
    /// higher: it advances the carriage's target one column and calls <c>HandleMining</c>, which
    /// is a no-op because the head position hasn't changed. By the time the head has descended
    /// the carriage has a new target, so the corner is never mined at the new depth. The corner
    /// alternates front and rear with each layer, and the more frames per fixed step the more
    /// often it happens.
    ///
    /// The arrival test is the second <c>DrillCarriage.localPosition == _drillEndSegmentX</c>
    /// in the method (the first is the ore-delivery check). It becomes
    /// <c>... == _drillEndSegmentX &amp;&amp; DrillHead.localPosition == _drillEndSegmentY</c>.
    /// </summary>
    [HarmonyPatch(typeof(Quarry), "RunDrillBit")]
    internal static class QuarryRunDrillBitPatch
    {
        private static readonly FieldInfo EndSegmentX = AccessTools.Field(typeof(Quarry), "_drillEndSegmentX");

        private static readonly MethodInfo Vector3Equality = AccessTools.Method(
            typeof(Vector3),
            "op_Equality",
            new[] { typeof(Vector3), typeof(Vector3) }
        );

        // Both carriage-versus-target comparisons in RunDrillBit: the ore-delivery check,
        // then the one that gates mining.
        private const int ExpectedMatches = 2;

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            var matches = new List<int>();
            for (var i = 1; i < code.Count; i++)
            {
                if (code[i].Calls(Vector3Equality) && code[i - 1].LoadsField(EndSegmentX))
                {
                    matches.Add(i);
                }
            }

            if (matches.Count != ExpectedMatches)
            {
                // The game changed RunDrillBit. Leave vanilla behaviour rather than guess.
                AutominerFixPlugin.Log.LogWarning(
                    $"Quarry.RunDrillBit: expected {ExpectedMatches} carriage-target comparisons, "
                        + $"found {matches.Count}; layer-change fix not applied."
                );
                return code;
            }

            // The stack holds the comparison's bool; and it with the head test.
            code.InsertRange(
                matches[matches.Count - 1] + 1,
                new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.Call(typeof(QuarryRunDrillBitPatch), nameof(HeadAtTarget)),
                    new CodeInstruction(OpCodes.And),
                }
            );

            AutominerFixPlugin.Log.LogInfo("Quarry.RunDrillBit: layer-change fix applied.");
            return code;
        }

        // Called from the patched IL; the head is "at" its target when it has finished descending.
        internal static bool HeadAtTarget(Quarry quarry)
        {
            return quarry.DrillHead.localPosition == quarry._drillEndSegmentY;
        }
    }
}
