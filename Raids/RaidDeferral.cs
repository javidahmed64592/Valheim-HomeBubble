using System.Collections.Generic;

using UnityEngine;

namespace HomeBubble.Raids;

/// <summary>
/// Raids that a bubble held off, started later once a player is clear of the bubbles. Runs on the server,
/// where raids are chosen. Only one raid is kept back at a time (the latest), and it isn't saved, so it is
/// forgotten when the world is closed.
/// </summary>
internal static class RaidDeferral
{
    private const float CheckInterval = 5f;

    /// <summary>The raid a bubble held off, waiting to start.</summary>
    private static RandomEvent _owed;

    /// <summary>Raids that only failed to start because everyone they could pick was in a bubble.</summary>
    private static readonly List<RandomEvent> BlockedThisAttempt = [];
    private static bool _attempting;

    /// <summary>When each player (by peer id, 0 for the host) was last within the defer distance of a bubble.</summary>
    private static readonly Dictionary<long, float> NearBubbleUntil = [];
    private static readonly List<RandEventSystem.PlayerEventData> Single = [];
    private static float _nextCheck;

    public static void BeginAttempt()
    {
        BlockedThisAttempt.Clear();
        _attempting = true;
    }

    /// <summary>Noted while vanilla looks for places to start each raid: this one had nowhere outside a bubble.</summary>
    public static void NoteBlocked(RandomEvent raid)
    {
        if (_attempting && !BlockedThisAttempt.Contains(raid))
        {
            BlockedThisAttempt.Add(raid);
        }
    }

    /// <summary>
    /// After vanilla's attempt to start a raid: if none started only because of bubbles, one of the raids it could
    /// have picked waits until a player is clear of them.
    /// </summary>
    public static void EndAttempt(bool started)
    {
        _attempting = false;
        if (!started && BlockedThisAttempt.Count > 0 && BubbleConfig.DeferRaids.Value)
        {
            _owed = BlockedThisAttempt[Random.Range(0, BlockedThisAttempt.Count)];
            NearBubbleUntil.Clear();
            HomeBubblePlugin.Log.LogInfo("A Home Bubble held off the raid " + _owed.m_name + "; it starts once a player is clear of the bubbles");
        }
        BlockedThisAttempt.Clear();
    }

    /// <summary>
    /// Every few seconds on the server: starts the held-off raid on a player who has been at least
    /// <c>DeferDistance</c> beyond every bubble for <c>DeferMinutes</c>, where vanilla would allow that raid.
    /// Waits while another raid is on.
    /// </summary>
    public static void Tick(RandEventSystem events)
    {
        if (Time.time < _nextCheck)
        {
            return;
        }
        _nextCheck = Time.time + CheckInterval;

        if (_owed == null || !BubbleConfig.BlockRaids.Value || !BubbleConfig.DeferRaids.Value)
        {
            _owed = null;
            return;
        }
        if (events.m_randomEvent != null)
        {
            return;
        }

        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null
            && TryStartOn(events, 0L, ZNet.instance.m_serverSyncedPlayerData, ZNet.instance.GetReferencePosition()))
        {
            return;
        }
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            if (peer.IsReady() && TryStartOn(events, peer.m_uid, peer.m_serverSyncedPlayerData, peer.m_refPos))
            {
                return;
            }
        }
    }

    private static bool TryStartOn(RandEventSystem events, long player, Dictionary<string, string> data, Vector3 position)
    {
        if (!RandEventSystem.RetrievePlayerEventData(data, position, out RandEventSystem.PlayerEventData eventData))
        {
            return false;
        }

        float now = Time.time;
        if (BubbleRegistry.DistanceBeyondNearestEdge(position) < BubbleConfig.DeferDistance.Value)
        {
            NearBubbleUntil[player] = now;
            return false;
        }
        if (!NearBubbleUntil.TryGetValue(player, out float since))
        {
            NearBubbleUntil[player] = now; // first seen clear: the wait starts now
            return false;
        }
        if (now - since < BubbleConfig.DeferMinutes.Value * 60f)
        {
            return false;
        }

        // The same checks vanilla makes when it picks a place for a raid: biome, a base nearby if the raid needs
        // one, the bosses this player has beaten, and the world's raid settings.
        Single.Clear();
        Single.Add(eventData);
        bool allowed = _owed.m_enabled && events.HaveGlobalKeys(_owed, Single) && events.GetValidEventPoints(_owed, Single).Count > 0;
        Single.Clear();
        if (!allowed)
        {
            return false;
        }

        HomeBubblePlugin.Log.LogInfo("Starting the raid " + _owed.m_name + " that a Home Bubble held off");
        events.SetRandomEvent(_owed, position);
        _owed = null;
        NearBubbleUntil.Clear();
        return true;
    }
}
