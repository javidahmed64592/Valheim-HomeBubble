using System.Collections.Generic;

using HarmonyLib;

using HomeBubble.Core;
using HomeBubble.Raids;

using UnityEngine;

namespace HomeBubble.Patches;

/// <summary>
/// Vanilla starts a raid on one of the players it could happen to, so leaving out the players inside a bubble
/// that is up means no raid starts on them. If that leaves nowhere for the raid, it is noted so it can happen
/// later instead.
/// </summary>
[HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.GetValidEventPoints))]
[Feature(RaidDeferral.Feature)]
internal static class RandEventSystem_GetValidEventPoints_Patch
{
    private static void Postfix(RandomEvent ev, List<Vector3> __result)
    {
        if (!BubbleConfig.BlockRaids.Value || __result.Count == 0)
        {
            return;
        }
        int removed = __result.RemoveAll(BubbleRegistry.IsInsideBubble);
        if (removed > 0 && __result.Count == 0)
        {
            RaidDeferral.NoteBlocked(ev);
        }
    }
}
