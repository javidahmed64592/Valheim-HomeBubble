using HarmonyLib;

using HomeBubble.Bubble;
using HomeBubble.Core;

namespace HomeBubble.Patches;

/// <summary>Ending the Hammer's highlight also clears the generator's glow, so the generator puts it back.</summary>
[HarmonyPatch(typeof(WearNTear), "ResetHighlight")]
[Feature(MaterialMan_PropertyContainer_RefreshRenderers_Patch.Feature)]
internal static class WearNTear_ResetHighlight_Patch
{
    private static void Postfix(WearNTear __instance)
    {
        if (__instance.TryGetComponent(out HomeBubbleZone bubble))
        {
            bubble.RestoreGlow();
        }
    }
}
