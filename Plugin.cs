using System;

using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using HomeBubble.Bubble;
using HomeBubble.Core;
using HomeBubble.Prefabs;

using Jotunn.Managers;
using Jotunn.Utils;

namespace HomeBubble;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class HomeBubblePlugin : BaseUnityPlugin
{
    public const string PluginGUID = "javidahmed64592.homebubble";
    public const string PluginName = "Home Bubble";
    public const string PluginVersion = "0.6.0";

    internal static BepInEx.Logging.ManualLogSource Log { get; private set; }

    private Harmony _harmony;

    private void Awake()
    {
        Log = Logger;
        BubbleConfig.Bind(Config);
        Translations.Load();

        _harmony = new Harmony(PluginGUID);
        _harmony.PatchAll();

        PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefabs;
        ZoneManager.OnVanillaLocationsAvailable += BubblePrefab.AttachForceField;

        CommandManager.Instance.AddConsoleCommand(new StartSiegeCommand());
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Unity message")]
    private void Update()
    {
        BubbleMap.Tick();
    }

    private void OnDestroy()
    {
        BubbleMap.Destroy();
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
    public static ConfigEntry<int> MaxFuel;
    public static ConfigEntry<float> Visibility;
    public static ConfigEntry<string> Tint;
    public static ConfigEntry<float> TintOpacity;
    public static ConfigEntry<bool> BlockRaids;
    public static ConfigEntry<bool> DeferRaids;
    public static ConfigEntry<float> DeferMinutes;
    public static ConfigEntry<float> DeferDistance;
    public static ConfigEntry<bool> AttackBubble;
    public static ConfigEntry<float> SiegeSeconds;
    public static ConfigEntry<float> DamagePerBone;
    public static ConfigEntry<float> ResistPerUpgrade;
    public static ConfigEntry<bool> MapCoverage;
    public static ConfigEntry<float> Volume;
    public static ConfigEntry<int> LowFuelWarning;

    public static void Bind(ConfigFile config)
    {
        BaseRadius = config.Bind("Bubble", "BaseRadius", 24f,
            new ConfigDescription("Radius (metres) of a bubble whose crafting stations have no upgrades. (Synced with server)",
                new AcceptableValueRange<float>(5f, 200f), Synced()));
        FullRadius = config.Bind("Bubble", "FullRadius", 48f,
            new ConfigDescription("Radius (metres) once every upgrade is built on the bubble's crafting stations. (Synced with server)",
                new AcceptableValueRange<float>(5f, 200f), Synced()));
        MinutesPerBone = config.Bind("Bubble", "MinutesPerBone", 30f,
            new ConfigDescription("Real-time minutes one bone keeps the bubble up. 0 means bones are only used up by monsters hitting the bubble. (Synced with server)",
                new AcceptableValueRange<float>(0f, 600f), Synced()));
        MaxFuel = config.Bind("Bubble", "MaxFuel", 50,
            new ConfigDescription("The most bones the generator can hold. (Synced with server)",
                new AcceptableValueRange<int>(1, 1000), Synced()));

        Visibility = config.Bind("Look", "Distortion", 4f,
            new ConfigDescription("How strongly the bubble warps what you see through it.",
                new AcceptableValueRange<float>(0.5f, 20f)));
        Tint = config.Bind("Look", "Tint", "#4DB8FF",
            "Colour of the bubble as a hex code, e.g. #4DB8FF.");
        TintOpacity = config.Bind("Look", "TintOpacity", 0.06f,
            new ConfigDescription("How solid the bubble's coloured surface is. 0 turns it off.",
                new AcceptableValueRange<float>(0f, 0.6f)));

        BlockRaids = config.Bind("Raids", "BlockRaids", true,
            new ConfigDescription("Raids don't start on players inside a bubble that is up. (Synced with server)",
                null, Synced()));
        DeferRaids = config.Bind("Raids", "DeferRaids", true,
            new ConfigDescription("A raid that a bubble kept from starting happens later instead, once a player has been far enough from every bubble for long enough. Only used with BlockRaids. (Synced with server)",
                null, Synced()));
        DeferMinutes = config.Bind("Raids", "DeferMinutes", 3f,
            new ConfigDescription("Minutes a player must stay clear of every bubble before a held-off raid starts on them. (Synced with server)",
                new AcceptableValueRange<float>(0f, 120f), Synced()));
        DeferDistance = config.Bind("Raids", "DeferDistance", 30f,
            new ConfigDescription("How many metres beyond the edge of every bubble a player must be for a held-off raid to start on them. (Synced with server)",
                new AcceptableValueRange<float>(0f, 500f), Synced()));

        AttackBubble = config.Bind("Monsters", "AttackBubble", true,
            new ConfigDescription("A monster fighting someone who runs into a bubble attacks the bubble for a while instead of fleeing. Its hits use up bones, like projectiles hitting the shield. Monsters that find players already inside still keep away. (Synced with server)",
                null, Synced()));
        SiegeSeconds = config.Bind("Monsters", "SiegeSeconds", 30f,
            new ConfigDescription("How many seconds a monster attacks the bubble before it loses interest and wanders off. Hitting it from inside starts the time over. (Synced with server)",
                new AcceptableValueRange<float>(5f, 300f), Synced()));
        DamagePerBone = config.Bind("Monsters", "DamagePerBone", 100f,
            new ConfigDescription("How much damage the bubble takes from monster hits and projectiles to use up one bone. Hits count the monster's star level and the world's difficulty. Lower drains the bubble faster; the vanilla shield generator's rate is 500. (Synced with server)",
                new AcceptableValueRange<float>(10f, 2000f), Synced()));
        ResistPerUpgrade = config.Bind("Monsters", "ResistPerUpgrade", 1f,
            new ConfigDescription("How much less damage (percent) the bubble takes for each upgrade built on its stations, so a fully upgraded bubble with about 20 upgrades takes about 20% less. 0 turns it off. (Synced with server)",
                new AcceptableValueRange<float>(0f, 5f), Synced()));

        MapCoverage = config.Bind("Look", "MapCoverage", true,
            "Shade the area each bubble covers on the minimap and the large map, in the bubble's tint.");

        Volume = config.Bind("Sound", "Volume", 0.4f,
            new ConfigDescription("How loud the bubble generator's sounds are, such as its low-fuel hum. 1 is the vanilla shield generator's volume, 0 is silent.",
                new AcceptableValueRange<float>(0f, 1f)));

        LowFuelWarning = config.Bind("Warnings", "LowFuelWarning", 5,
            new ConfigDescription("Warn you when you are in or near a bubble with fewer bones than this. 0 turns the warning off.",
                new AcceptableValueRange<int>(0, 1000)));
    }

    /// <summary>Bones used up per point of damage the bubble takes.</summary>
    public static float FuelPerDamage => 1f / DamagePerBone.Value;

    /// <summary>The share of damage a bubble with this many station upgrades shrugs off, capped at 90%.</summary>
    public static float Resistance(int upgrades)
    {
        return UnityEngine.Mathf.Clamp(upgrades * ResistPerUpgrade.Value / 100f, 0f, 0.9f);
    }

    /// <summary>Settings that use the server's value when the server has the mod.</summary>
    private static ConfigurationManagerAttributes Synced()
    {
        return new ConfigurationManagerAttributes { IsAdminOnly = true };
    }
}
