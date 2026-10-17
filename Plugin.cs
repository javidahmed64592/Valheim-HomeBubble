using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using System;

namespace HomeBubble
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class HomeBubblePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "javidahmed64592.homebubble";
        public const string PluginName = "Home Bubble";
        public const string PluginVersion = "0.1.0";

        internal static BepInEx.Logging.ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            BubbleConfig.Bind(Config);

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefabs;
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void RegisterPrefabs()
        {
            // The event fires on every menu start; only build once.
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefabs;

            try
            {
                BubblePrefab.Register();
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to register the Home Bubble prefab: " + e);
            }
        }
    }

    internal static class BubbleConfig
    {
        public static ConfigEntry<float> BaseRadius;
        public static ConfigEntry<float> FullRadius;
        public static ConfigEntry<float> MinutesPerBone;

        public static void Bind(ConfigFile config)
        {
            // A Valheim zone is 64m across, so 16m is half a zone and 32m is a whole one.
            BaseRadius = config.Bind("Bubble", "BaseRadius", 16f,
                new ConfigDescription("Radius (metres) of a bubble whose crafting stations have no upgrades. 16 covers about half a zone.",
                    new AcceptableValueRange<float>(5f, 200f)));
            FullRadius = config.Bind("Bubble", "FullRadius", 32f,
                new ConfigDescription("Radius (metres) once every upgrade is built on the bubble's four crafting stations. 32 covers a whole zone. The growth is shared out evenly between the upgrades.",
                    new AcceptableValueRange<float>(5f, 200f)));
            MinutesPerBone = config.Bind("Bubble", "MinutesPerBone", 30f,
                new ConfigDescription("Real-time minutes one bone keeps the bubble up. 0 means bones are only used up by projectile hits.",
                    new AcceptableValueRange<float>(0f, 600f)));
        }
    }
}
