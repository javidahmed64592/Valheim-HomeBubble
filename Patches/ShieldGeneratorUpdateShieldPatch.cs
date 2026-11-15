using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>A switched-off bubble looks and sounds dead, whatever fuel it has.</summary>
[HarmonyPatch(typeof(ShieldGenerator), "UpdateShield")]
internal static class ShieldGenerator_UpdateShield_Patch
{
    private static void Postfix(ShieldGenerator __instance)
    {
        __instance.GetComponentInParent<HomeBubbleZone>()?.ApplyGeneratorState();
    }
}
