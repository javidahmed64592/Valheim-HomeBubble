using System.Collections.Generic;

using HomeBubble.Bubble;
using HomeBubble.Prefabs;

using UnityEngine;

namespace HomeBubble.Raids;

/// <summary>
/// The server's list of every Home Bubble in the world, read from the saved world data rather than loaded
/// objects, since a dedicated server loads nothing near its players. It is rebuilt a little each physics step
/// (the game's own iterative search) and restarted a few seconds after each pass finishes.
/// </summary>
internal static class BubbleRegistry
{
    private const float RescanDelay = 10f;

    private static List<ZDO> _bubbles = [];
    private static List<ZDO> _scan = [];
    private static int _scanIndex;
    private static float _nextScan;

    /// <summary>Advances the search. Called on the server from <c>RandEventSystem.FixedUpdate</c>.</summary>
    public static void Tick()
    {
        if (ZDOMan.instance == null || Time.time < _nextScan)
        {
            return;
        }
        if (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(BubblePrefab.PrefabName, _scan, ref _scanIndex))
        {
            return;
        }

        (_bubbles, _scan) = (_scan, _bubbles);
        _scan.Clear();
        _scanIndex = 0;
        _nextScan = Time.time + RescanDelay;
    }

    /// <summary>
    /// How far the point is beyond the edge of the nearest bubble that is up: negative inside one, and
    /// infinity when no bubble is up.
    /// </summary>
    public static float DistanceBeyondNearestEdge(Vector3 point)
    {
        float nearest = float.PositiveInfinity;
        foreach (ZDO zdo in _bubbles)
        {
            if (zdo == null || !zdo.IsValid())
            {
                continue;
            }
            if (HomeBubbleZone.DistanceBeyondEdge(zdo, point) is float beyond && beyond < nearest)
            {
                nearest = beyond;
            }
        }
        return nearest;
    }

    public static bool IsInsideBubble(Vector3 point)
    {
        return DistanceBeyondNearestEdge(point) < 0f;
    }
}
