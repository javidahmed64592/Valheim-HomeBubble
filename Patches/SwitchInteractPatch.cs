using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>Shift + E on the generator switches the bubble on or off instead of adding bones.</summary>
[HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
internal static class Switch_Interact_Patch
{
    private static bool Prefix(Switch __instance, bool hold, bool alt, ref bool __result)
    {
        if (!alt)
        {
            return true;
        }

        HomeBubbleZone bubble = __instance.GetComponentInParent<HomeBubbleZone>();
        if (bubble == null)
        {
            return true;
        }

        // Like opening a chest: someone else's ward keeps you from switching the bubble off.
        if (!hold && PrivateArea.CheckAccess(bubble.transform.position))
        {
            bubble.Toggle();
        }
        __result = false;
        return false;
    }
}
