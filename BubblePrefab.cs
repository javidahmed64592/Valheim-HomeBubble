using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HomeBubble
{
    internal static class BubblePrefab
    {
        public const string PrefabName = "HB_HomeBubble";
        private const string VanillaGenerator = "piece_shieldgenerator";

        public static void Register()
        {
            GameObject source = PrefabManager.Instance.GetPrefab(VanillaGenerator);
            if (source == null)
            {
                HomeBubblePlugin.Log.LogError("Vanilla prefab not found: " + VanillaGenerator);
                return;
            }

            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source);

            // The vanilla generator's strike attack would wipe the bubble's fuel; this is a defensive piece only.
            ShieldGenerator shield = clone.GetComponentInChildren<ShieldGenerator>(true);
            shield.m_enableAttack = false;

            // Stand-in the no-monster patches hand back to the AI so monsters have something to flee from.
            var marker = new GameObject("HB_Area");
            marker.transform.SetParent(clone.transform, false);
            var collider = marker.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            collider.isTrigger = true;
            EffectArea area = marker.AddComponent<EffectArea>();
            area.m_type = EffectArea.Type.None;

            clone.AddComponent<HomeBubbleZone>();

            var config = new PieceConfig
            {
                Name = "Home Bubble",
                Description = "A protective bubble for your base. Monsters keep out, but you can still build inside. Needs a Workbench, Forge, Stonecutter and Artisan Table nearby, and grows as they are upgraded. Fuelled by bones.",
                PieceTable = "Hammer",
                Category = "Misc",
                CraftingStation = "",
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
                }
            };

            PieceManager.Instance.AddPiece(new CustomPiece(clone, false, config));
            HomeBubblePlugin.Log.LogInfo("Registered " + PrefabName);
        }
    }
}
