using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>Fighting back from inside keeps a besieging monster at the wall.</summary>
[HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.OnDamaged))]
internal static class MonsterAI_OnDamaged_Patch
{
    private static void Postfix(MonsterAI __instance, Character attacker)
    {
        BubbleSiege.OnDamaged(__instance, attacker);
    }
}
