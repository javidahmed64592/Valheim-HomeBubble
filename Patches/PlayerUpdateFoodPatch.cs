using HarmonyLib;

using HomeBubble.Comfort;

namespace HomeBubble.Patches;

/// <summary>
/// Holds the food timers inside a bubble by taking back the time vanilla is about to add, so food still heals
/// you over time as usual.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
internal static class Player_UpdateFood_Patch
{
    private static void Prefix(Player __instance, float dt, bool forceUpdate)
    {
        if (!forceUpdate && TimerPause.IsPaused(__instance))
        {
            __instance.m_foodUpdateTimer -= dt * Game.m_foodRate;
        }
    }
}
