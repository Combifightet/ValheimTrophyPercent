using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace ValheimTrophyPercent
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    [BepInProcess("valheim.exe")]
    public class ValheimMod : BaseUnityPlugin
    {
        // Define your mod credentials as constants
        const string pluginGUID = "Combifightet.TrophyPercent";
        const string pluginName = "Valheim Trophy Percent";
        const string pluginVersion = "1.0.0";

        // Create the Harmony instance using your GUID
        private readonly Harmony HarmonyInstance = new Harmony(pluginGUID);

        // Create a logger so you can print to the BepInEx console
        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public void Awake()
        {
            // Print a startup message to the console
            logger.LogInfo("Trophy Percent Mod is loading...");

            // Tell Harmony to look through this entire DLL and apply any [HarmonyPatch] it finds
            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            logger.LogInfo("Trophy Percent Mod loaded successfully!");
        }
    }
}