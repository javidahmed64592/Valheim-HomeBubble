using System.Collections.Generic;

using HarmonyLib;

using HomeBubble.Bubble;
using HomeBubble.Prefabs;

using UnityEngine;

namespace HomeBubble.Patches;

/// <summary>
/// The Hammer highlights a piece by tinting every renderer under it, which for a bubble includes the dome and
/// lights the whole sphere up bright blue. The list of renderers is gathered here once per piece, so leaving the
/// dome out keeps the highlight on the generator only.
/// </summary>
[HarmonyPatch(typeof(MaterialMan.PropertyContainer), nameof(MaterialMan.PropertyContainer.RefreshRenderers))]
internal static class MaterialMan_PropertyContainer_RefreshRenderers_Patch
{
    private static void Postfix(GameObject go, List<Renderer> ___m_assignedRenderers)
    {
        if (go != null && go.TryGetComponent(out HomeBubbleZone _))
        {
            BubblePrefab.RemoveDomeRenderers(go.transform, ___m_assignedRenderers);
        }
    }
}
