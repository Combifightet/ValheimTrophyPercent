using BepInEx;
using BepInEx.Configuration;
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
        const string pluginVersion = "1.2.0";

        // Create the Harmony instance using your GUID
        private readonly Harmony HarmonyInstance = new Harmony(pluginGUID);

        // Create a logger so you can print to the BepInEx console
        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        // Singleton so our Manager can access the config
        public static ValheimMod Instance;
        public ConfigEntry<string> PinnedTrophiesConfig;

        public void Awake()
        {
            Instance = this;
            logger.LogInfo("Trophy Percent Mod is loading...");

            // Bind our config file to store a string of pinned trophies
            // Added a formatting example so users know how to manually edit this file
            PinnedTrophiesConfig = Config.Bind("General", "PinnedTrophies", "", "Comma-separated list of pinned trophy prefab names. Example: TrophyBoar,TrophyNeck,TrophyDeer");
            TrophyManager.LoadConfig(PinnedTrophiesConfig.Value);

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            logger.LogInfo("Trophy Percent Mod loaded successfully!");
        }

        public void SavePinnedConfig()
        {
            // Join the HashSet into a string and save it to the config file
            PinnedTrophiesConfig.Value = string.Join(",", TrophyManager.PinnedTrophies);
            Config.Save();
        }
    }
}