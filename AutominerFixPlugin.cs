using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AutominerFix
{
    // LaunchPad calls Awake on the splash screen, before any world exists. Only patch here;
    // nothing may touch game state until a world is running.
    [BepInPlugin(ThisModInfo.ModID, ThisModInfo.AssemblyName, ThisModInfo.Version)]
    public class AutominerFixPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            try
            {
                new Harmony(ThisModInfo.ModID).PatchAll(typeof(AutominerFixPlugin).Assembly);
                Log.LogInfo($"Autominer Fix {ThisModInfo.Version} initialized");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error during Autominer Fix init: {ex}");
            }
        }
    }
}
