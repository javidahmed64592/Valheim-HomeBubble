using HarmonyLib;

using HomeBubble.Bubble;

using UnityEngine;

namespace HomeBubble.Patches;

/// <summary>Replaces the generic "missing station" message with one that says what the bubble lacks.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
internal static class Player_TryPlacePiece_Patch
{
    private static void Postfix(Player __instance, bool __result)
    {
        GameObject ghost = __instance.m_placementGhost;
        if (__result || ghost == null || ghost.GetComponent<HomeBubbleZone>() == null
            || __instance.m_placementStatus != Player.PlacementStatus.ExtensionMissingStation)
        {
            return;
        }

        if (!HomeBubbleZone.TryFindStations(ghost.transform.position, out _, out string problem))
        {
            __instance.Message(MessageHud.MessageType.Center, problem);
        }
    }
}
