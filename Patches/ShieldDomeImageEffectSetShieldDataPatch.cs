using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>Bubbles draw the trader's force field instead of the shield generator's screen-space dome.</summary>
[HarmonyPatch(typeof(ShieldDomeImageEffect), nameof(ShieldDomeImageEffect.SetShieldData))]
internal static class ShieldDomeImageEffect_SetShieldData_Patch
{
    private static bool Prefix(ShieldGenerator shield)
    {
        return !HomeBubbleZone.UsesForceField(shield);
    }
}
