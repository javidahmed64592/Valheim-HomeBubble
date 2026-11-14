using HarmonyLib;

using HomeBubble.Bubble;

namespace HomeBubble.Patches;

/// <summary>A bubble that has lost a station cannot be topped up; say why instead.</summary>
[HarmonyPatch(typeof(ShieldGenerator), "OnAddFuel")]
internal static class ShieldGenerator_OnAddFuel_Patch
{
    private static bool Prefix(ShieldGenerator __instance, Humanoid user, ref bool __result)
    {
        HomeBubbleZone bubble = __instance.GetComponentInParent<HomeBubbleZone>();
        if (bubble == null || !bubble.IsCollapsed(out string station))
        {
            return true;
        }

        user.Message(MessageHud.MessageType.Center, CollapseText.For(station));
        __result = false;
        return false;
    }
}
