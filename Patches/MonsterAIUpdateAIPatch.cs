using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>
/// <para>
/// Vanilla only keeps monsters out of a safe zone in two cases: one with no target flees when it comes within
/// 15 metres, and one with a target flees only when that target is inside. A monster chasing something outside the
/// bubble could follow it in.
/// </para>
/// <para>
/// After the monster's own AI has run, one that is inside a bubble is moved back out. If its target is still
/// outside, it keeps the target and steps just past the edge, so it can carry on fighting from there; outside the
/// bubble it is left to vanilla and may attack right up to the edge. If its target is inside too, or it has none,
/// it flees from the middle of the bubble. Like vanilla's safe zones, this leaves tamed creatures and the Dvergr
/// (including worker mods' Dvergr) alone.
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

        HomeBubbleZone bubble = HomeBubbleZone.FindBubbleInside(character.transform.position);
        if (bubble == null)
        {
            return;
        }

        Character target = __instance.m_targetCreature;
        _ = target != null && HomeBubbleZone.FindBubbleInside(target.transform.position) == null
            ? __instance.MoveTo(dt, bubble.ExitPoint(character.transform.position), 0.5f, true)
            : __instance.Flee(dt, bubble.Centre);
    }
}
