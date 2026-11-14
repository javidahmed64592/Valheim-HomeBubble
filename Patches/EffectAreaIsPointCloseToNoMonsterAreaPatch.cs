using HarmonyLib;

using HomeBubble.Bubble;

using UnityEngine;

namespace HomeBubble.Patches;

[HarmonyPatch(typeof(EffectArea), nameof(EffectArea.IsPointCloseToNoMonsterArea))]
internal static class EffectArea_IsPointCloseToNoMonsterArea_Patch
{
    private static void Postfix(Vector3 p, ref EffectArea __result)
    {
        if (__result == null)
        {
            __result = HomeBubbleZone.FindClose(p);
        }
    }
}
