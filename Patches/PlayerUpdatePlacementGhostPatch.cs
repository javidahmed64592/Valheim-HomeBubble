using HarmonyLib;

using HomeBubble.Bubble;

using UnityEngine;

namespace HomeBubble.Patches;

/// <summary>A bubble can only be placed in range of an unclaimed workbench, forge, stonecutter and artisan table.</summary>
[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
internal static class Player_UpdatePlacementGhost_Patch
{
    private static void Postfix(Player __instance)
    {
        GameObject ghost = __instance.m_placementGhost;
        if (ghost == null || ghost.GetComponent<HomeBubbleZone>() == null
            || __instance.m_placementStatus != Player.PlacementStatus.Valid)
        {
            return;
        }

        if (!HomeBubbleZone.TryFindStations(ghost.transform.position, out _, out _))
        {
            __instance.m_placementStatus = Player.PlacementStatus.ExtensionMissingStation;
            __instance.SetPlacementGhostValid(false);
        }
    }
}
