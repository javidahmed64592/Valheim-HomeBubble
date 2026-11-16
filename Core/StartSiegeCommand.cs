using System.Collections.Generic;
using System.Globalization;

using HomeBubble.Bubble;

using Jotunn.Entities;

using UnityEngine;

namespace HomeBubble.Core;

/// <summary>
/// Testing aid: spawns a hostile group from a biome, from the Mountains (1) to the Deep North (5), already hunting
/// the player. Stand inside a bubble and they start attacking it at once; stand outside and they chase you, so
/// run inside to start the siege. Needs devcommands.
/// </summary>
internal sealed class StartSiegeCommand : ConsoleCommand
{
    /// <summary>Metres beyond the bubble's edge (or in front of the player, outside one) that the group appears.</summary>
    private const float SpawnDistance = 15f;
    private const float SpawnSpread = 4f;

    /// <summary>
    /// Puts the whole group on one side: monsters of different factions otherwise fight each other. Characters in
    /// the same group are never enemies.
    /// </summary>
    private const string GroupName = "HomeBubbleSiegeTest";

    private static readonly (string Biome, (string Prefab, int Count)[] Group)[] Tiers =
    [
        ("Mountains", [("Wolf", 3), ("Ulv", 2), ("Fenring", 1), ("Fenring_Cultist", 1), ("StoneGolem", 1)]),
        ("Plains", [("Goblin", 3), ("GoblinArcher", 2), ("GoblinBrute", 1), ("GoblinShaman", 1)]),
        ("Mistlands", [("Seeker", 3), ("Tick", 2), ("SeekerBrute", 1)]),
        ("Ashlands", [("Charred_Melee", 3), ("Charred_Archer", 2), ("Charred_Mage", 1), ("Asksvin", 2), ("Morgen_NonSleeping", 1)]),
        ("Deep North", [("Greydwarf_Frozen", 3), ("Skeleton_DeepNorth", 2), ("JotunWarrior", 2), ("JotunWitch", 1), ("TrollFrost", 1)]),
    ];

    public override string Name => "startsiege";

    public override string Help => "[1-5] Spawns a group of monsters hunting you to test Home Bubble sieges: "
        + "1 Mountains, 2 Plains, 3 Mistlands, 4 Ashlands, 5 Deep North. In a bubble they attack it at once.";

    public override bool IsCheat => true;

    public override List<string> CommandOptionList()
    {
        List<string> options = [];
        for (int i = 1; i <= Tiers.Length; i++)
        {
            options.Add(i.ToString(CultureInfo.InvariantCulture));
        }

        return options;
    }

    public override void Run(string[] args)
    {
        Run(args, Console.instance);
    }

    public override void Run(string[] args, Terminal context)
    {
        Player player = Player.m_localPlayer;
        if (player == null || ZNetScene.instance == null)
        {
            context.AddString("startsiege: no player in the world.");
            return;
        }

        if (args.Length < 1 || !int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tier)
            || tier < 1 || tier > Tiers.Length)
        {
            context.AddString("Usage: startsiege " + Help);
            return;
        }

        (string biome, (string Prefab, int Count)[] group) = Tiers[tier - 1];
        Vector3 origin = player.transform.position;
        HomeBubbleZone bubble = HomeBubbleZone.FindBubbleInside(origin);
        Vector3 centre = SpawnCentre(player, bubble);

        int spawned = 0;
        List<string> missing = [];
        foreach ((string prefabName, int count) in group)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (prefab == null)
            {
                missing.Add(prefabName);
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                Spawn(prefab, centre, origin, player, bubble);
                spawned++;
            }
        }

        string where = bubble != null ? " outside the bubble; they attack it now." : " in front of you; run into a bubble to start the siege.";
        context.AddString("startsiege: spawned " + spawned + " " + biome + " monsters" + where);
        if (missing.Count > 0)
        {
            context.AddString("startsiege: these prefabs were not found: " + string.Join(", ", missing));
        }
    }

    /// <summary>Just outside the bubble on the player's side of it, or ahead of the player when not in one.</summary>
    private static Vector3 SpawnCentre(Player player, HomeBubbleZone bubble)
    {
        Vector3 origin = player.transform.position;
        if (bubble == null)
        {
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            return origin + (forward.normalized * SpawnDistance);
        }

        Vector3 outward = origin - bubble.Centre;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f)
        {
            outward = player.transform.forward;
            outward.y = 0f;
        }

        return bubble.Centre + (outward.normalized * (bubble.Radius + SpawnDistance));
    }

    private static void Spawn(GameObject prefab, Vector3 centre, Vector3 origin, Player player, HomeBubbleZone bubble)
    {
        Vector2 offset = Random.insideUnitCircle * SpawnSpread;
        Vector3 position = new(centre.x + offset.x, 0f, centre.z + offset.y);
        position.y = ZoneSystem.instance.GetGroundHeight(position) + 0.5f;

        Vector3 facing = origin - position;
        facing.y = 0f;
        Quaternion rotation = facing.sqrMagnitude > 0.01f ? Quaternion.LookRotation(facing) : Quaternion.identity;
        GameObject spawned = Object.Instantiate(prefab, position, rotation);

        Character character = spawned.GetComponent<Character>();
        if (character != null)
        {
            character.m_group = GroupName;
        }

        MonsterAI ai = spawned.GetComponent<MonsterAI>();
        if (ai == null)
        {
            return;
        }

        ai.SetAlerted(true);
        ai.m_targetCreature = player;
        if (bubble != null)
        {
            BubbleSiege.PrimeChase(ai, player);
        }
    }
}
