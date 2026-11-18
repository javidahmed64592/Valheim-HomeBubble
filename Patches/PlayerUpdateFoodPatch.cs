using HarmonyLib;

using HomeBubble.Comfort;

namespace HomeBubble.Patches;

/// <summary>
/// Holds the food timers inside a bubble. Vanilla's once-a-second food tick still runs, since it is also what
/// sets max health, stamina and eitr from your food (after death, for one), but each food gets back the second
/// the tick is about to take from it. Food still heals you over time as usual.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
internal static class Player_UpdateFood_Patch
{
    /// <summary>Seconds of food time vanilla takes on each tick.</summary>
    private const float TickSeconds = 1f;

    private static void Prefix(Player __instance, float dt, bool forceUpdate)
    {
        if (forceUpdate || __instance.m_foodUpdateTimer + (dt * Game.m_foodRate) < TickSeconds
            || !TimerPause.IsPaused(__instance))
        {
            return;
        }

        foreach (Player.Food food in __instance.m_foods)
        {
            food.m_time += TickSeconds;
        }
    }
}
