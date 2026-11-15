using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>
/// <para>
/// Vanilla only keeps monsters out of a safe zone in two cases: one with no target flees when it comes within
/// 15 metres, and one with a target flees only when that target is inside. A monster chasing something outside the
/// bubble, or one that picks up a target while wandering, could walk straight in.
/// </para>
/// <para>
/// After the monster's own AI has run, one inside a bubble or within a few metres of its edge drops its target and
/// flees from the generator. With no target, vanilla's own rule keeps it fleeing until it is 15 metres clear. Like
/// vanilla's safe zones, this leaves tamed creatures and the Dvergr (including worker mods' Dvergr) alone.
/// </para>
/// </summary>
[HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
internal static class MonsterAI_UpdateAI_Patch
{
    private static void Postfix(MonsterAI __instance, float dt, bool __result)
    {
        Character character = __instance.m_character;
        if (!__result || character == null || character.IsTamed() || __instance.IsSleeping()
            || character.GetFaction() is Character.Faction.Players or Character.Faction.Dverger)
        {
            return;
        }

        EffectArea bubble = HomeBubbleZone.FindKeepingOut(character.transform.position);
        if (bubble == null)
        {
            return;
        }

        __instance.m_targetCreature = null;
        __instance.m_targetStatic = null;
        _ = __instance.Flee(dt, bubble.transform.position);
    }
}
