using System.Collections.Generic;
using UnityEngine;

namespace HomeBubble
{
    /// <summary>
    /// The behaviour layered on top of the cloned shield generator: it keeps the monsters out like a trader's
    /// safe zone, ties the bubble to one set of crafting stations, and sizes the dome from their upgrades.
    /// </summary>
    internal class HomeBubbleZone : MonoBehaviour
    {
        private const string RadiusKey = "HB_radius";
        private const string BrokenKey = "HB_broken"; // bitmask of the stations that are gone
        private const int MissChecksBeforeCollapse = 5; // checks are 1s apart; rides out a station's data still arriving
        private const string DrainTimeKey = "HB_lastDrain";
        private const float DrainInterval = 10f;
        private const float NoMonsterCloseMargin = 15f; // matches vanilla's EffectArea "close to" margin

        private static readonly string[] StationKeys = { "HB_station0", "HB_station1", "HB_station2", "HB_station3" };

        private static readonly (string Prefab, string Label)[] RequiredStations =
        {
            ("piece_workbench", "Workbench"),
            ("forge", "Forge"),
            ("piece_stonecutter", "Stonecutter"),
            ("piece_artisanstation", "Artisan Table"),
        };

        private static readonly Dictionary<string, int> MaxUpgrades = new Dictionary<string, int>();

        public static readonly List<HomeBubbleZone> Instances = new List<HomeBubbleZone>();

        private ZNetView _nview;
        private ShieldGenerator _shield;
        private EffectArea _area;
        private float _timer = 10f; // forces an update on the first frame
        private float _drainTimer;
        private readonly int[] _missChecks = new int[4];

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

            Instances.Add(this);
            if (_nview.IsOwner() && _nview.GetZDO().GetZDOID(StationKeys[0]) == ZDOID.None)
            {
                ClaimStations();
            }
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
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

            if (IsCollapsed(out _))
            {
                // No radius means no protection: the dome shrinks away and monsters are no longer kept off.
                _shield.m_minShieldRadius = 0f;
                _shield.m_maxShieldRadius = 0f;
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
            var labels = new List<string>();
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
        private float? ComputeRadius(ZDO zdo)
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
            float radius = Mathf.Lerp(BubbleConfig.BaseRadius.Value, BubbleConfig.FullRadius.Value, progress);
            return radius;
        }

        /// <summary>The kinds of upgrade built next to the station (vanilla counts a repeated kind once).</summary>
        private static int CountUpgrades(CraftingStation station)
        {
            var kinds = new HashSet<string>();
            foreach (StationExtension extension in StationExtension.m_allExtensions)
            {
                if (extension != null && extension.m_craftingStation != null
                    && extension.m_craftingStation.m_name == station.m_name
                    && Vector3.Distance(extension.transform.position, station.transform.position) < extension.m_maxStationDistance)
                {
                    kinds.Add(Utils.GetPrefabName(extension.gameObject));
                }
            }
            return kinds.Count;
        }

        /// <summary>How many kinds of upgrade exist for the station, read from the game's prefabs so modded ones count too.</summary>
        private static int CountAvailableUpgrades(CraftingStation station)
        {
            if (!MaxUpgrades.TryGetValue(station.m_name, out int count))
            {
                var kinds = new HashSet<string>();
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    StationExtension extension = prefab ? prefab.GetComponent<StationExtension>() : null;
                    if (extension != null && extension.m_craftingStation != null
                        && extension.m_craftingStation.m_name == station.m_name)
                    {
                        kinds.Add(prefab.name);
                    }
                }
                count = kinds.Count;
                MaxUpgrades[station.m_name] = count;
            }
            return count;
        }

        private bool Protects(Vector3 point, float margin)
        {
            if (_shield == null || _shield.m_shieldDome == null)
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
            var claimed = new HashSet<ZDOID>();
            foreach (HomeBubbleZone bubble in Instances)
            {
                if (bubble == null || !bubble._nview.IsValid())
                {
                    continue;
                }
                foreach (string key in StationKeys)
                {
                    claimed.Add(bubble._nview.GetZDO().GetZDOID(key));
                }
            }
            return claimed;
        }

        private static CraftingStation FindUnclaimedStation(int index, Vector3 point, HashSet<ZDOID> claimed)
        {
            return FindUnclaimedStation(index, point, claimed, out _);
        }

        /// <summary>The closest station of the required type in build range of the point that is not claimed.</summary>
        private static CraftingStation FindUnclaimedStation(int index, Vector3 point, HashSet<ZDOID> claimed, out bool anyInRange)
        {
            anyInRange = false;
            CraftingStation best = null;
            float bestDistance = float.MaxValue;
            foreach (CraftingStation station in CraftingStation.m_allStations)
            {
                if (station == null || Utils.GetPrefabName(station.gameObject) != RequiredStations[index].Prefab)
                {
                    continue;
                }
                Vector3 flat = point;
                flat.y = station.transform.position.y; // build range ignores height, like vanilla
                float distance = Vector3.Distance(station.transform.position, flat);
                if (distance >= station.GetStationBuildRange())
                {
                    continue;
                }
                anyInRange = true;
                ZNetView view = station.GetComponent<ZNetView>();
                if (view == null || !view.IsValid() || claimed.Contains(view.GetZDO().m_uid))
                {
                    continue;
                }
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = station;
                }
            }
            return best;
        }

        /// <summary>
        /// Picks, for each required station type, the closest one in build range of the point that no other
        /// bubble has claimed. Fails with a message naming whatever is missing.
        /// </summary>
        public static bool TryFindStations(Vector3 point, out CraftingStation[] found, out string problem)
        {
            HashSet<ZDOID> claimed = ClaimedStations();
            found = new CraftingStation[RequiredStations.Length];
            var missing = new List<string>();
            var claimedLabels = new List<string>();
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
}
