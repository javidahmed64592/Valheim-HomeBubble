using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>Ending the Hammer's highlight also clears the generator's glow, so the generator puts it back.</summary>
[HarmonyPatch(typeof(WearNTear), "ResetHighlight")]
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
