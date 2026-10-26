using System.Collections.Generic;

using UnityEngine;

namespace HomeBubble;

/// <summary>
/// The behaviour layered on top of the cloned shield generator: it keeps the monsters out like a trader's
/// safe zone, ties the bubble to one set of crafting stations, and sizes the dome from their upgrades.
/// </summary>
internal sealed class HomeBubbleZone : MonoBehaviour
{
    private const string RadiusKey = "HB_radius";
    /// <summary>
    /// bitmask of the stations that are gone
    /// </summary>
    private const string BrokenKey = "HB_broken";
    /// <summary>
    /// checks are 1s apart; rides out a station&#39;s data still arriving
    /// </summary>
    private const int MissChecksBeforeCollapse = 5;
    private const float CollapsedRadius = 0.2f;
    private const string DrainTimeKey = "HB_lastDrain";
    private const float DrainInterval = 10f;
    private const string OffKey = "HB_off";
    /// <summary>
    /// fraction of the fuel range the look catches up per second
    /// </summary>
    private const float FadeSpeed = 0.5f;
    /// <summary>
    /// how much of its strength the field keeps on the last bone
    /// </summary>
    private const float LowFuelOpacity = 0.25f;
    private static readonly Color LowFuelColor = new(1f, 0.15f, 0.1f);
    /// <summary>
    /// matches vanilla&#39;s EffectArea &quot;close to&quot; margin
    /// </summary>
    private const float NoMonsterCloseMargin = 15f;

    private static readonly string[] StationKeys = ["HB_station0", "HB_station1", "HB_station2", "HB_station3"];

    private static readonly (string Prefab, string Label)[] RequiredStations =
    [
        ("piece_workbench", "Workbench"),
        ("forge", "Forge"),
        ("piece_stonecutter", "Stonecutter"),
        ("piece_artisanstation", "Artisan Table"),
    ];

    private static readonly Dictionary<string, int> MaxUpgrades = [];

    public static readonly List<HomeBubbleZone> Instances = [];

    private ZNetView _nview;
    private ShieldGenerator _shield;
    private EffectArea _area;
    private Transform _field;
    private float _fieldUnitRadius = 0.5f;
    private bool _collapsed;
    /// <summary>
    /// forces an update on the first frame
    /// </summary>
    private float _timer = 10f;
    private float _drainTimer;
    private readonly int[] _missChecks = new int[4];

    /// <summary>
    /// Each bubble gets its own copy of the field's materials so its look can follow its own fuel.
    /// </summary>
    private Material[] _fieldMaterials = [];
    private Color[] _fieldColors;
    private float[] _fieldRefraction;
    private Material _shellMaterial;
    private Color _shellColor;
    private float _shownFuel = -1f;
    private float _appliedFuel = -1f;
    /// <summary>
    /// the generator&#39;s own low-fuel hum threshold
    /// </summary>
    private float _lowFuelLoopStart;

    private void Start()
    {
        if (Player.IsPlacementGhost(gameObject))
        {
            enabled = false;
            return;
        }

        _nview = GetComponent<ZNetView>();
        _shield = GetComponentInChildren<ShieldGenerator>(true);
        _area = GetComponentInChildren<EffectArea>(true);
        if (_nview == null || !_nview.IsValid() || _shield == null)
        {
            enabled = false;
            return;
        }

        _field = transform.Find(BubblePrefab.ForceFieldName);
        MeshFilter mesh = _field ? _field.GetComponent<MeshFilter>() : null;
        if (mesh != null && mesh.sharedMesh != null)
        {
            _fieldUnitRadius = Mathf.Max(0.01f, mesh.sharedMesh.bounds.extents.x);
        }

        _lowFuelLoopStart = _shield.m_shieldLowLoopFuelStart;
        SetUpFieldLook();

        Instances.Add(this);
        if (_nview.IsOwner() && _nview.GetZDO().GetZDOID(StationKeys[0]) == ZDOID.None)
        {
            ClaimStations();
        }
    }

    private void OnDestroy()
    {
        _ = Instances.Remove(this);
        foreach (Material material in _fieldMaterials)
        {
            Destroy(material);
        }
        if (_shellMaterial != null)
        {
            Destroy(_shellMaterial);
        }
    }

    private void SetUpFieldLook()
    {
        if (_field == null)
        {
            return;
        }

        MeshRenderer renderer = _field.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            _fieldMaterials = renderer.materials; // instances, so other bubbles are unaffected
            _fieldColors = new Color[_fieldMaterials.Length];
            _fieldRefraction = new float[_fieldMaterials.Length];
            for (int i = 0; i < _fieldMaterials.Length; i++)
            {
                Material material = _fieldMaterials[i];
                _fieldColors[i] = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                _fieldRefraction[i] = material.HasProperty("_RefractionIntensity") ? material.GetFloat("_RefractionIntensity") : 0f;
            }
        }

        Transform shell = _field.Find("HB_Shell");
        MeshRenderer shellRenderer = shell ? shell.GetComponent<MeshRenderer>() : null;
        if (shellRenderer != null)
        {
            _shellMaterial = shellRenderer.material;
            _shellColor = _shellMaterial.color;
        }
    }

    /// <summary>Redder and fainter as the fuel runs down; full strength with a full generator.</summary>
    private void ApplyFuelLook(float fuel)
    {
        Color low = LowFuelColor;
        float strength = Mathf.Lerp(LowFuelOpacity, 1f, fuel);
        for (int i = 0; i < _fieldMaterials.Length; i++)
        {
            Material material = _fieldMaterials[i];
            if (material.HasProperty("_Color"))
            {
                Color color = Color.Lerp(low, _fieldColors[i], fuel);
                color.a = _fieldColors[i].a;
                material.SetColor("_Color", color);
            }
            if (material.HasProperty("_RefractionIntensity"))
            {
                material.SetFloat("_RefractionIntensity", _fieldRefraction[i] * strength);
            }
        }
        if (_shellMaterial != null)
        {
            Color color = Color.Lerp(low, _shellColor, fuel);
            color.a = _shellColor.a * strength;
            _shellMaterial.color = color;
        }
    }

    public bool IsOff { get; private set; }

    /// <summary>
    /// sounds and lights switched off with the bubble
    /// </summary>
    private readonly List<Behaviour> _silenced = [];
    private readonly List<ParticleSystem> _stopped = [];

    /// <summary>
    /// Makes the generator look and sound dead while the bubble is switched off. The generator re-lights itself
    /// from its fuel several times a second, so this runs after each of those updates.
    /// </summary>
    public void ApplyGeneratorState()
    {
        if (_shield == null)
        {
            return;
        }

        // The low-fuel hum is a looping effect the generator starts and stops itself. A zero threshold stops
        // it starting, and asking it to stop removes any already playing.
        _shield.m_shieldLowLoopFuelStart = IsOff ? 0f : _lowFuelLoopStart;
        if (IsOff)
        {
            Character.SetupContinuousEffect(_shield.transform, _shield.transform.position, false,
                _shield.m_shieldLowLoop, ref _shield.m_lowLoopInstances);
            _shield.m_enabledObject?.SetActive(false);
            _shield.m_disabledObject?.SetActive(true);
            foreach (Behaviour behaviour in GetComponentsInChildren<Behaviour>(true))
            {
                if ((behaviour is Light || behaviour is AudioSource) && behaviour.enabled)
                {
                    behaviour.enabled = false;
                    _silenced.Add(behaviour);
                }
            }
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particles.isPlaying)
                {
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    _stopped.Add(particles);
                }
            }
        }
        else if (_silenced.Count > 0 || _stopped.Count > 0)
        {
            foreach (Behaviour behaviour in _silenced)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }
            foreach (ParticleSystem particles in _stopped)
            {
                if (particles != null)
                {
                    particles.Play(true);
                }
            }
            _silenced.Clear();
            _stopped.Clear();
            _shield.Invoke("UpdateShield", 0f); // light back up from the fuel straight away
        }
    }

    /// <summary>Switches the bubble on or off. Anyone can ask; the owner of the generator's data keeps the setting.</summary>
    public void Toggle()
    {
        if (_nview == null || !_nview.IsValid())
        {
            return;
        }
        _nview.ClaimOwnership();
        ZDO zdo = _nview.GetZDO();
        bool off = !zdo.GetBool(OffKey, false);
        zdo.Set(OffKey, off);
        IsOff = off;
        _timer = 10f; // apply straight away
        ApplyGeneratorState();
    }

    /// <summary>True if this shield generator is one of ours, so the vanilla dome effect should be skipped for it.</summary>
    public static bool UsesForceField(ShieldGenerator shield)
    {
        HomeBubbleZone bubble = shield.GetComponentInParent<HomeBubbleZone>();
        return bubble != null && bubble._field != null;
    }

    private void LateUpdate()
    {
        if (_field == null || _shield == null || _shield.m_shieldDome == null)
        {
            return;
        }

        // The generator already eases its radius up and down, so the force field simply follows it.
        float radius = _shield.m_radius;
        bool visible = radius > 0.1f && !_collapsed && !IsOff;
        if (_field.gameObject.activeSelf != visible)
        {
            _field.gameObject.SetActive(visible);
        }
        if (visible)
        {
            float fuel = _shield.GetFuelRatio();
            _shownFuel = _shownFuel < 0f ? fuel : Mathf.MoveTowards(_shownFuel, fuel, FadeSpeed * Time.deltaTime);
            if (!Mathf.Approximately(_shownFuel, _appliedFuel))
            {
                _appliedFuel = _shownFuel;
                ApplyFuelLook(_shownFuel);
            }

            _field.position = _shield.m_shieldDome.transform.position;
            _field.localScale = Vector3.one * (radius / _fieldUnitRadius);
        }
    }

    private void Update()
    {
        _drainTimer += Time.deltaTime;
        _timer += Time.deltaTime;
        if (_timer < 1f || !_nview.IsValid())
        {
            return;
        }
        _timer = 0f;

        ZDO zdo = _nview.GetZDO();
        if (_nview.IsOwner())
        {
            CheckStationsStillStand(zdo);
        }

        IsOff = zdo.GetBool(OffKey, false);
        _collapsed = IsCollapsed(out _);
        if (_collapsed || IsOff)
        {
            // Collapsed or switched off: the dome shrinks away and monsters are no longer kept off (see
            // Protects), and no bones burn. The shield keeps a
            // sliver of radius because a zero radius with fuel left makes it replay its start-up sound
            // every few ticks.
            _shield.m_minShieldRadius = CollapsedRadius;
            _shield.m_maxShieldRadius = CollapsedRadius;
            return;
        }

        if (_nview.IsOwner())
        {
            float? radius = ComputeRadius(zdo);
            if (radius.HasValue)
            {
                zdo.Set(RadiusKey, radius.Value);
            }
            if (_drainTimer >= DrainInterval)
            {
                _drainTimer = 0f;
                DrainFuel(zdo);
            }
        }

        // The shield sizes itself from fuel between min and max; pinning both makes fuel only turn it on or off.
        float size = zdo.GetFloat(RadiusKey, BubbleConfig.BaseRadius.Value);
        _shield.m_minShieldRadius = size;
        _shield.m_maxShieldRadius = size;
    }

    /// <summary>True while any of the bubble's stations is gone; says which ones.</summary>
    public bool IsCollapsed(out string stationLabels)
    {
        int mask = _nview != null && _nview.IsValid() ? _nview.GetZDO().GetInt(BrokenKey, 0) : 0;
        List<string> labels = [];
        for (int i = 0; i < RequiredStations.Length; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                labels.Add(RequiredStations[i].Label);
            }
        }
        stationLabels = string.Join(" and ", labels);
        return mask != 0;
    }

    /// <summary>
    /// A claimed station whose object no longer exists anywhere (not merely unloaded) has been destroyed,
    /// which brings the bubble down. Building a replacement in range brings it back.
    /// </summary>
    private void CheckStationsStillStand(ZDO zdo)
    {
        int mask = zdo.GetInt(BrokenKey, 0);
        int newMask = mask;
        for (int i = 0; i < StationKeys.Length; i++)
        {
            ZDOID id = zdo.GetZDOID(StationKeys[i]);
            if (id == ZDOID.None)
            {
                continue;
            }

            bool gone = ZNetScene.instance.FindInstance(id) == null && ZDOMan.instance.GetZDO(id) == null;
            _missChecks[i] = gone ? _missChecks[i] + 1 : 0;
            if (_missChecks[i] >= MissChecksBeforeCollapse)
            {
                newMask |= 1 << i;
            }
        }

        // Look for a rebuilt station to stand in for each one that is gone.
        if (newMask != 0)
        {
            HashSet<ZDOID> claimed = ClaimedStations();
            for (int i = 0; i < StationKeys.Length; i++)
            {
                if ((newMask & (1 << i)) == 0)
                {
                    continue;
                }
                CraftingStation replacement = FindUnclaimedStation(i, transform.position, claimed);
                if (replacement != null)
                {
                    zdo.Set(StationKeys[i], replacement.GetComponent<ZNetView>().GetZDO().m_uid);
                    newMask &= ~(1 << i);
                    _missChecks[i] = 0;
                }
            }
        }

        if (newMask != mask)
        {
            zdo.Set(BrokenKey, newMask);
        }
    }

    /// <summary>Burns bones over time. Uses world time so a bubble keeps burning while nobody is near it.</summary>
    private void DrainFuel(ZDO zdo)
    {
        long now = ZNet.instance.GetTime().Ticks;
        long last = zdo.GetLong(DrainTimeKey, 0L);
        zdo.Set(DrainTimeKey, now);
        float minutesPerBone = BubbleConfig.MinutesPerBone.Value;
        if (last <= 0 || minutesPerBone <= 0f)
        {
            return;
        }

        float fuel = _shield.GetFuel();
        if (fuel <= 0f)
        {
            return;
        }
        double elapsedMinutes = new System.TimeSpan(now - last).TotalMinutes;
        _shield.SetFuel(Mathf.Max(0f, fuel - (float)(elapsedMinutes / minutesPerBone)));
    }

    private void ClaimStations()
    {
        if (!TryFindStations(transform.position, out CraftingStation[] stations, out _))
        {
            return;
        }
        ZDO zdo = _nview.GetZDO();
        for (int i = 0; i < stations.Length; i++)
        {
            zdo.Set(StationKeys[i], stations[i].GetComponent<ZNetView>().GetZDO().m_uid);
        }
    }

    /// <summary>The radius from the claimed stations' upgrades, or null if any of them is not loaded.</summary>
    private static float? ComputeRadius(ZDO zdo)
    {
        int upgrades = 0;
        int possible = 0;
        for (int i = 0; i < StationKeys.Length; i++)
        {
            ZDOID id = zdo.GetZDOID(StationKeys[i]);
            GameObject go = id == ZDOID.None ? null : ZNetScene.instance.FindInstance(id);
            CraftingStation station = go ? go.GetComponent<CraftingStation>() : null;
            if (station == null)
            {
                return null;
            }
            upgrades += CountUpgrades(station);
            possible += CountAvailableUpgrades(station);
        }

        // Fully upgraded is always the biggest the bubble gets; every upgrade adds an equal share.
        float progress = possible > 0 ? Mathf.Clamp01((float)upgrades / possible) : 0f;
        return (float)Mathf.Lerp(BubbleConfig.BaseRadius.Value, BubbleConfig.FullRadius.Value, progress);
    }

    /// <summary>The kinds of upgrade built next to the station (vanilla counts a repeated kind once).</summary>
    private static int CountUpgrades(CraftingStation station)
    {
        HashSet<string> kinds = [];
        foreach (StationExtension extension in StationExtension.m_allExtensions)
        {
            if (extension != null && extension.m_craftingStation != null
                && extension.m_craftingStation.m_name == station.m_name
                && Vector3.Distance(extension.transform.position, station.transform.position) < extension.m_maxStationDistance)
            {
                _ = kinds.Add(Utils.GetPrefabName(extension.gameObject));
            }
        }
        return kinds.Count;
    }

    /// <summary>How many kinds of upgrade exist for the station, read from the game's prefabs so modded ones count too.</summary>
    private static int CountAvailableUpgrades(CraftingStation station)
    {
        if (!MaxUpgrades.TryGetValue(station.m_name, out int count))
        {
            HashSet<string> kinds = [];
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                StationExtension extension = prefab ? prefab.GetComponent<StationExtension>() : null;
                if (extension != null && extension.m_craftingStation != null
                    && extension.m_craftingStation.m_name == station.m_name)
                {
                    _ = kinds.Add(prefab.name);
                }
            }
            count = kinds.Count;
            MaxUpgrades[station.m_name] = count;
        }
        return count;
    }

    private bool Protects(Vector3 point, float margin)
    {
        if (_collapsed || IsOff || _shield == null || _shield.m_shieldDome == null)
        {
            return false;
        }
        float radius = _shield.m_radius; // follows the dome as it grows, shrinks or switches off
        return radius > 0.1f && Vector3.Distance(_shield.m_shieldDome.transform.position, point) < radius + margin;
    }

    /// <summary>The bubble keeping monsters off this point, if any. Mirrors EffectArea.IsPointInsideNoMonsterArea.</summary>
    public static EffectArea FindInside(Vector3 point)
    {
        return Find(point, 0f);
    }

    /// <summary>Mirrors EffectArea.IsPointCloseToNoMonsterArea.</summary>
    public static EffectArea FindClose(Vector3 point)
    {
        return Find(point, NoMonsterCloseMargin);
    }

    private static EffectArea Find(Vector3 point, float margin)
    {
        foreach (HomeBubbleZone bubble in Instances)
        {
            if (bubble != null && bubble.Protects(point, margin))
            {
                return bubble._area;
            }
        }
        return null;
    }

    /// <summary>
    /// Stations spoken for by bubbles. A collapsed bubble keeps its surviving stations reserved so it can
    /// come back when the missing one is rebuilt; deconstruct it to free them.
    /// </summary>
    private static HashSet<ZDOID> ClaimedStations()
    {
        HashSet<ZDOID> claimed = [];
        foreach (HomeBubbleZone bubble in Instances)
        {
            if (bubble == null || !bubble._nview.IsValid())
            {
                continue;
            }
            foreach (string key in StationKeys)
            {
                _ = claimed.Add(bubble._nview.GetZDO().GetZDOID(key));
            }
        }
        return claimed;
    }

    private static CraftingStation FindUnclaimedStation(int index, Vector3 point, HashSet<ZDOID> claimed)
    {
        return FindUnclaimedStation(index, point, claimed, out _);
    }

    /// <summary>
    /// The station of the required type that serves the point and is not claimed. Stations in plain build range are
    /// preferred, closest first. Failing that, whatever station the game says lets you build there is used, so
    /// anything that widens build range (such as BenchLink's linked stations) counts too.
    /// </summary>
    private static CraftingStation FindUnclaimedStation(int index, Vector3 point, HashSet<ZDOID> claimed, out bool anyInRange)
    {
        anyInRange = false;
        CraftingStation best = null;
        float bestDistance = float.MaxValue;
        string stationName = null;
        foreach (CraftingStation station in CraftingStation.m_allStations)
        {
            if (station == null || Utils.GetPrefabName(station.gameObject) != RequiredStations[index].Prefab)
            {
                continue;
            }
            stationName = station.m_name;
            Vector3 flat = point;
            flat.y = station.transform.position.y; // build range ignores height, like vanilla
            float distance = Vector3.Distance(station.transform.position, flat);
            if (distance >= station.GetStationBuildRange())
            {
                continue;
            }
            anyInRange = true;
            if (IsFree(station, claimed) && distance < bestDistance)
            {
                bestDistance = distance;
                best = station;
            }
        }

        if (best != null || stationName == null)
        {
            return best;
        }

        // Nothing in plain range is free: ask the game, which also knows about any extended build range.
        CraftingStation buildable = CraftingStation.HaveBuildStationInRange(stationName, point);
        if (buildable == null)
        {
            return null;
        }
        anyInRange = true;
        return IsFree(buildable, claimed) ? buildable : null;
    }

    private static bool IsFree(CraftingStation station, HashSet<ZDOID> claimed)
    {
        ZNetView view = station.GetComponent<ZNetView>();
        return view != null && view.IsValid() && !claimed.Contains(view.GetZDO().m_uid);
    }

    /// <summary>
    /// Picks, for each required station type, the closest one in build range of the point that no other
    /// bubble has claimed. Fails with a message naming whatever is missing.
    /// </summary>
    public static bool TryFindStations(Vector3 point, out CraftingStation[] found, out string problem)
    {
        HashSet<ZDOID> claimed = ClaimedStations();
        found = new CraftingStation[RequiredStations.Length];
        List<string> missing = [];
        List<string> claimedLabels = [];
        for (int i = 0; i < RequiredStations.Length; i++)
        {
            found[i] = FindUnclaimedStation(i, point, claimed, out bool anyInRange);
            if (found[i] == null)
            {
                (anyInRange ? claimedLabels : missing).Add(RequiredStations[i].Label);
            }
        }

        if (missing.Count == 0 && claimedLabels.Count == 0)
        {
            problem = null;
            return true;
        }

        problem = missing.Count > 0
            ? "Home Bubble needs these in range: " + string.Join(", ", missing)
            : "Already covered by another Home Bubble: " + string.Join(", ", claimedLabels);
        return false;
    }
}
