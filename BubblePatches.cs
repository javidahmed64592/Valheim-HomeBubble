using HarmonyLib;
using UnityEngine;

namespace HomeBubble
{
    /// <summary>Monsters treat a bubble like a trader's safe zone.</summary>
    [HarmonyPatch(typeof(EffectArea), nameof(EffectArea.IsPointInsideNoMonsterArea))]
    internal static class InsideNoMonsterPatch
    {
        private static void Postfix(Vector3 p, ref EffectArea __result)
        {
            if (__result == null)
            {
                __result = HomeBubbleZone.FindInside(p);
            }
        }
    }

    [HarmonyPatch(typeof(EffectArea), nameof(EffectArea.IsPointCloseToNoMonsterArea))]
    internal static class CloseToNoMonsterPatch
    {
        private static void Postfix(Vector3 p, ref EffectArea __result)
        {
            if (__result == null)
            {
                __result = HomeBubbleZone.FindClose(p);
            }
        }
    }

    /// <summary>Bubbles draw the trader's force field instead of the shield generator's screen-space dome.</summary>
    [HarmonyPatch(typeof(ShieldDomeImageEffect), nameof(ShieldDomeImageEffect.SetShieldData))]
    internal static class HideVanillaDomePatch
    {
        private static bool Prefix(ShieldGenerator shield)
        {
            return !HomeBubbleZone.UsesForceField(shield);
        }
    }

    /// <summary>A bubble can only be placed in range of an unclaimed workbench, forge, stonecutter and artisan table.</summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    internal static class PlacementGhostPatch
    {
        private static void Postfix(Player __instance)
        {
            GameObject ghost = __instance.m_placementGhost;
            if (ghost == null || ghost.GetComponent<HomeBubbleZone>() == null
                || __instance.m_placementStatus != Player.PlacementStatus.Valid)
            {
                return;
            }

            if (!HomeBubbleZone.TryFindStations(ghost.transform.position, out _, out _))
            {
                __instance.m_placementStatus = Player.PlacementStatus.ExtensionMissingStation;
                __instance.SetPlacementGhostValid(false);
            }
        }
    }

    /// <summary>Replaces the generic "missing station" message with one that says what the bubble lacks.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    internal static class PlacementMessagePatch
    {
        private static void Postfix(Player __instance, bool __result)
        {
            GameObject ghost = __instance.m_placementGhost;
            if (__result || ghost == null || ghost.GetComponent<HomeBubbleZone>() == null
                || __instance.m_placementStatus != Player.PlacementStatus.ExtensionMissingStation)
            {
                return;
            }

            if (!HomeBubbleZone.TryFindStations(ghost.transform.position, out _, out string problem))
            {
                __instance.Message(MessageHud.MessageType.Center, problem);
            }
        }
    }

    /// <summary>A bubble that has lost a station cannot be topped up; say why instead.</summary>
    [HarmonyPatch(typeof(ShieldGenerator), "OnAddFuel")]
    internal static class AddFuelPatch
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

    [HarmonyPatch(typeof(ShieldGenerator), "OnHoverAddFuel")]
    internal static class HoverFuelPatch
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
                __result = "Home Bubble\n<color=red>" + CollapseText.For(station) + "</color>";
                return;
            }

            string state = bubble.IsOff ? "Turn on" : "Turn off";
            __result += "\n[<color=yellow><b>Shift + E</b></color>] " + state;
            if (bubble.IsOff)
            {
                __result += "\n<color=red>Switched off</color>";
            }
        }
    }

    /// <summary>A switched-off bubble looks and sounds dead, whatever fuel it has.</summary>
    [HarmonyPatch(typeof(ShieldGenerator), "UpdateShield")]
    internal static class GeneratorOffPatch
    {
        private static void Postfix(ShieldGenerator __instance)
        {
            __instance.GetComponentInParent<HomeBubbleZone>()?.ApplyGeneratorState();
        }
    }

    /// <summary>Shift + E on the generator switches the bubble on or off instead of adding bones.</summary>
    [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
    internal static class ToggleBubblePatch
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

            if (!hold)
            {
                bubble.Toggle();
            }
            __result = false;
            return false;
        }
    }

    internal static class CollapseText
    {
        public static string For(string station)
        {
            return "The bubble is down because its " + station + " was destroyed. Rebuild it nearby to bring the bubble back.";
        }
    }
}
