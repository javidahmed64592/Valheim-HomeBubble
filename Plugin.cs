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
        public const string PluginVersion = "0.2.0";

        internal static BepInEx.Logging.ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            BubbleConfig.Bind(Config);

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefabs;
            ZoneManager.OnVanillaLocationsAvailable += BubblePrefab.AttachForceField;
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
        public static ConfigEntry<float> Visibility;
        public static ConfigEntry<string> Tint;
        public static ConfigEntry<float> TintOpacity;

        public static void Bind(ConfigFile config)
        {
            BaseRadius = config.Bind("Bubble", "BaseRadius", 24f,
                new ConfigDescription("Radius (metres) of a bubble whose crafting stations have no upgrades.",
                    new AcceptableValueRange<float>(5f, 200f)));
            FullRadius = config.Bind("Bubble", "FullRadius", 48f,
                new ConfigDescription("Radius (metres) once every upgrade is built on the bubble's four crafting stations.",
                    new AcceptableValueRange<float>(5f, 200f)));
            Visibility = config.Bind("Look", "Distortion", 4f,
                new ConfigDescription("How strongly the bubble warps what you see through it.",
                    new AcceptableValueRange<float>(0.5f, 20f)));
            Tint = config.Bind("Look", "Tint", "#4DB8FF",
                "Colour of the bubble as a hex code, e.g. #4DB8FF.");
            TintOpacity = config.Bind("Look", "TintOpacity", 0.06f,
                new ConfigDescription("How solid the bubble's coloured surface is. 0 turns it off.",
                    new AcceptableValueRange<float>(0f, 0.6f)));
            MinutesPerBone = config.Bind("Bubble", "MinutesPerBone", 30f,
                new ConfigDescription("Real-time minutes one bone keeps the bubble up. 0 means bones are only used up by projectile hits.",
                    new AcceptableValueRange<float>(0f, 600f)));
        }
    }
}
