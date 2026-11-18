using HarmonyLib;

using HomeBubble.Comfort;

using UnityEngine;

namespace HomeBubble.Patches;

/// <summary>Holds the Rested timer inside a bubble by taking back the time vanilla just added.</summary>
[HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.UpdateStatusEffect))]
internal static class SE_Rested_UpdateStatusEffect_Patch
{
    private static void Postfix(SE_Rested __instance, float dt)
    {
        if (TimerPause.IsPaused(__instance.m_character as Player))
        {
            __instance.m_time = Mathf.Max(0f, __instance.m_time - dt);
        }
    }
}
