using HarmonyLib;

using HomeBubble.Core;
using HomeBubble.Raids;

namespace HomeBubble.Patches;

/// <summary>On the server, keeps the list of bubbles up to date and starts a held-off raid when it can.</summary>
[HarmonyPatch(typeof(RandEventSystem), "FixedUpdate")]
[Feature(RaidDeferral.Feature)]
internal static class RandEventSystem_FixedUpdate_Patch
{
    private static void Postfix(RandEventSystem __instance)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return;
        }
        BubbleRegistry.Tick();
        RaidDeferral.Tick(__instance);
    }
}
