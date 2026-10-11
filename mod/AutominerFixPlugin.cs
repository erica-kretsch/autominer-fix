using System;
using BepInEx;
using HarmonyLib;

namespace AutominerFix
{
    // LaunchPad calls Awake on the splash screen, before any world exists. Only patch here;
    // nothing may touch game state until a world is running.
    [BepInPlugin(ThisModInfo.ModID, ThisModInfo.AssemblyName, ThisModInfo.Version)]
    public class AutominerFixPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            try
            {
                L.SetLogger(this.Logger);
                new Harmony(ThisModInfo.ModID).PatchAll(typeof(AutominerFixPlugin).Assembly);
                L.Info($"Autominer Fix initialized ({ThisModInfo.Info})");
            }
            catch (Exception ex)
            {
                L.Error($"Error during Autominer Fix init: {ex}");
            }
        }
    }
}
