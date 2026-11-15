using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>
/// A switched-off bubble looks and sounds dead, whatever fuel it has, and the sounds the generator has just
/// started (such as the low-fuel hum) are turned down to the configured volume.
/// </summary>
[HarmonyPatch(typeof(ShieldGenerator), "UpdateShield")]
internal static class ShieldGenerator_UpdateShield_Patch
{
    private static void Postfix(ShieldGenerator __instance)
    {
        HomeBubbleZone bubble = __instance.GetComponentInParent<HomeBubbleZone>();
        if (bubble != null)
        {
            bubble.ApplyGeneratorState();
            bubble.ApplyVolume();
        }
    }
}
