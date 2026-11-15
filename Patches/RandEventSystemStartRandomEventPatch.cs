using HarmonyLib;

using HomeBubble.Raids;

namespace HomeBubble.Patches;

/// <summary>
/// Vanilla's regular raid roll. Watches which raids a bubble kept from starting, so that one of them can
/// happen later if no raid started at all.
/// </summary>
[HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.StartRandomEvent))]
internal static class RandEventSystem_StartRandomEvent_Patch
{
    private static void Prefix(RandEventSystem __instance, out RandomEvent __state)
    {
        __state = __instance.m_randomEvent;
        RaidDeferral.BeginAttempt();
    }

    private static void Postfix(RandEventSystem __instance, RandomEvent __state)
    {
        RaidDeferral.EndAttempt(__instance.m_randomEvent != __state);
    }
}
