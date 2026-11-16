using System.Globalization;

using HarmonyLib;

using HomeBubble.Bubble;

using UnityEngine;

namespace HomeBubble.Patches;

[HarmonyPatch(typeof(ShieldGenerator), "OnHoverAddFuel")]
internal static class ShieldGenerator_OnHoverAddFuel_Patch
{
    private static void Postfix(ShieldGenerator __instance, ref string __result)
    {
        HomeBubbleZone bubble = __instance.GetComponentInParent<HomeBubbleZone>();
        if (bubble == null)
        {
            return;
        }

        if (bubble.IsCollapsed(out string station))
        {
            __result = Localization.instance.Localize("$piece_homebubble") + "\n<color=red>" + CollapseText.For(station) + "</color>";
            return;
        }

        string state = bubble.IsOff ? "$homebubble_turn_on" : "$homebubble_turn_off";
        string text = "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] " + state;
        if (bubble.IsOff)
        {
            text += "\n<color=red>$homebubble_switched_off</color>";
        }

        if (BubbleConfig.ResistPerUpgrade.Value > 0f)
        {
            int upgrades = bubble.Upgrades;
            string percent = Mathf.RoundToInt(BubbleConfig.Resistance(upgrades) * 100f).ToString(CultureInfo.InvariantCulture);
            text += "\n" + Localization.instance.Localize("$homebubble_resistance", percent,
                upgrades.ToString(CultureInfo.InvariantCulture));
        }
        __result += Localization.instance.Localize(text);
    }
}
