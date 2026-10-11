using Assets.Scripts.Objects.Electrical;
using HarmonyLib;

namespace AutominerFix.Patches
{
    /// <summary>
    /// Makes the first sweep start in a direction that stays inside the pit.
    ///
    /// The carriage starts at the +X edge of the pit but <c>_forwardDirection</c> starts false,
    /// which steps it +X, straight out of bounds. <c>HandleOutsideBounds</c> pulls it back and
    /// moves on to the next row, so the first row of the first layer is mined only at its start
    /// column and the rest of that row is never touched. Starting the sweep towards -X when the
    /// first step would leave the pit gives that row its full width.
    ///
    /// A save restores <c>_forwardDirection</c> after this runs, so a half-dug pit continues as
    /// it was.
    /// </summary>
    [HarmonyPatch(typeof(Quarry), "SetupDrillHead")]
    internal static class QuarrySetupDrillHeadPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Quarry __instance)
        {
            var origin = __instance.QuarryDigPosition;
            if (origin == null)
            {
                return;
            }

            var upperEdge = origin.localPosition.x + __instance._halfQuarrySize.x;
            var firstStep = __instance.DefaultDrillCarriagePosition.x + __instance.MovementAmount.x;
            __instance._forwardDirection = firstStep > upperEdge;
        }
    }
}
