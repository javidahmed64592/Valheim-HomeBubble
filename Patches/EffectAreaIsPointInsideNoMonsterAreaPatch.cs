using HarmonyLib;

using HomeBubble.Bubble;

using UnityEngine;

namespace HomeBubble.Patches;

/// <summary>Monsters treat a bubble like a trader's safe zone.</summary>
[HarmonyPatch(typeof(EffectArea), nameof(EffectArea.IsPointInsideNoMonsterArea))]
internal static class EffectArea_IsPointInsideNoMonsterArea_Patch
{
    private static void Postfix(Vector3 p, ref EffectArea __result)
    {
        if (__result == null)
        {
            __result = HomeBubbleZone.FindInside(p);
        }
    }
}
