using System.Collections.Generic;

using HomeBubble.Bubble;

using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

using UnityEngine;

namespace HomeBubble.Prefabs;

internal static class BubblePrefab
{
    public const string PrefabName = "HB_HomeBubble";
    private const string VanillaGenerator = "piece_shieldgenerator";
    private const string TraderLocation = "Vendor_BlackForest";
    public const string ForceFieldName = "HB_ForceField";
    private const string PoweredHumName = "sfx_shieldgenerator_powered_loop";

    private static GameObject _prefab;

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
        shield.m_maxFuel = BubbleConfig.MaxFuel.Value;
        shield.m_name = "$piece_homebubble"; // the hover text, in place of "$piece_shieldgenerator"
        RemovePoweredHum(clone);

        // Stand-in the no-monster patches hand back to the AI so monsters have something to flee from.
        GameObject marker = new("HB_Area");
        marker.transform.SetParent(clone.transform, false);
        SphereCollider collider = marker.AddComponent<SphereCollider>();
        collider.radius = 0.1f;
        collider.isTrigger = true;
        EffectArea area = marker.AddComponent<EffectArea>();
        area.m_type = EffectArea.Type.None;

        _ = clone.AddComponent<HomeBubbleZone>();
        _prefab = clone;

        PieceConfig config = new()
        {
            Name = "$piece_homebubble",
            Description = "$piece_homebubble_description",
            PieceTable = "Hammer",
            Category = "Defence",
            CraftingStation = "",
            Requirements =
            [
                new RequirementConfig { Item = "Iron", Amount = 5, Recover = true },
                new RequirementConfig { Item = "Copper", Amount = 5, Recover = true },
                new RequirementConfig { Item = "DragonTear", Amount = 2, Recover = true }
            ]
        };

        _ = PieceManager.Instance.AddPiece(new CustomPiece(clone, false, config));
        HomeBubblePlugin.Log.LogInfo("Registered " + PrefabName);
    }

    /// <summary>
    /// Gives the bubble the shimmering force field that surrounds the trader's camp in place of the shield
    /// generator's dome. The size is set at runtime, so only the mesh and material are borrowed.
    /// </summary>
    public static void AttachForceField()
    {
        ZoneManager.OnVanillaLocationsAvailable -= AttachForceField;
        if (_prefab == null)
        {
            return;
        }

        try
        {
            ZoneSystem.ZoneLocation location = ZoneManager.Instance.GetZoneLocation(TraderLocation);
            GameObject camp = (location != null ? location.m_prefab.Asset : null)
                ?? PrefabManager.Instance.GetPrefab(TraderLocation);
            Transform source = camp != null ? camp.transform.Find("ForceField") : null;
            if (source == null)
            {
                HomeBubblePlugin.Log.LogWarning(camp == null
                    ? "The trader's camp prefab was not found; the bubble keeps the shield generator's dome."
                    : "The trader's camp has no ForceField object; the bubble keeps the shield generator's dome.");
                return;
            }

            GameObject field = Object.Instantiate(source.gameObject, _prefab.transform);
            field.name = ForceFieldName;
            field.transform.localPosition = Vector3.zero;
            field.transform.localRotation = Quaternion.identity;
            field.transform.localScale = Vector3.one;

            BoostVisibility(field);

            // The camp's copy carries its own fixed-size no-monster area; ours is sized by the bubble.
            foreach (EffectArea area in field.GetComponentsInChildren<EffectArea>(true))
            {
                Object.DestroyImmediate(area.gameObject);
            }
        }
        catch (System.Exception e)
        {
            HomeBubblePlugin.Log.LogError("Failed to attach the trader's force field: " + e);
        }
    }

    /// <summary>
    /// Removes the generator's powered hum, so a fuelled bubble is quiet; the low-fuel hum still plays when bones
    /// run low. Vanilla registers a looping sound again every time it restarts and only forgets it when the object
    /// is destroyed. A generator fuelled up from empty restarts its hum, so it ends up muted as if it were a copy
    /// of itself, while one loaded with fuel already in it hums at full volume. Without the hum, both are the same.
    /// </summary>
    private static void RemovePoweredHum(GameObject clone)
    {
        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == PoweredHumName)
            {
                Object.DestroyImmediate(child.gameObject);
                return;
            }
        }
        HomeBubblePlugin.Log.LogWarning("No " + PoweredHumName + " found on the shield generator; the bubble keeps its hum.");
    }

    /// <summary>
    /// The renderers of the bubble's dome (the force field, its shell, and the generator's own dome), so the
    /// Hammer's highlight can leave them out and only light up the generator.
    /// </summary>
    public static void RemoveDomeRenderers(Transform root, List<Renderer> renderers)
    {
        Transform field = root.Find(ForceFieldName);
        ShieldGenerator shield = root.GetComponentInChildren<ShieldGenerator>(true);
        Transform dome = shield != null && shield.m_shieldDome != null ? shield.m_shieldDome.transform : null;
        _ = renderers.RemoveAll(renderer => renderer != null
            && ((field != null && renderer.transform.IsChildOf(field)) || (dome != null && renderer.transform.IsChildOf(dome))));
    }

    internal static Color ParseTint()
    {
        return ColorUtility.TryParseHtmlString(BubbleConfig.Tint.Value, out Color tint)
            ? tint
            : new Color(0.3f, 0.72f, 1f);
    }

    /// <summary>
    /// The trader's field is a distortion effect with a black tint, which is faint on its own. The bubble gets
    /// its own copy of the material with stronger refraction and a tint, plus a thin translucent shell so the
    /// surface is easy to see from far away and from inside.
    /// </summary>
    private static void BoostVisibility(GameObject field)
    {
        Color tint = ParseTint();

        MeshRenderer renderer = field.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = new(renderer.sharedMaterials[i]);
                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", new Color(tint.r, tint.g, tint.b, 1f));
                }
                if (material.HasProperty("_RefractionIntensity"))
                {
                    material.SetFloat("_RefractionIntensity", material.GetFloat("_RefractionIntensity") * BubbleConfig.Visibility.Value);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }

        // The shell: the same sphere with a plain see-through colour, drawn both inside and out.
        Shader shellShader = Shader.Find("Sprites/Default");
        MeshFilter mesh = field.GetComponent<MeshFilter>();
        float opacity = BubbleConfig.TintOpacity.Value;
        if (shellShader != null && mesh != null && opacity > 0f)
        {
            GameObject shell = new("HB_Shell");
            shell.transform.SetParent(field.transform, false);
            shell.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            MeshRenderer shellRenderer = shell.AddComponent<MeshRenderer>();
            shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shellRenderer.receiveShadows = false;
            shellRenderer.sharedMaterial = new Material(shellShader) { color = new Color(tint.r, tint.g, tint.b, opacity) };
        }
    }
}
