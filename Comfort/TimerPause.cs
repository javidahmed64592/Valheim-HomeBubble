using HomeBubble.Bubble;

namespace HomeBubble.Comfort;

/// <summary>
/// Food and the Rested effect don't wear off while a player is inside a bubble that is up, so players can get
/// ready for an adventure at home and leave in their own time.
/// </summary>
internal static class TimerPause
{
    /// <summary>The food and Rested patches. Without it, both wear off inside a bubble as in vanilla.</summary>
    public const string Feature = "Pause timers";

    /// <summary>Whether this player's food and Rested timers are held where they are.</summary>
    public static bool IsPaused(Player player)
    {
        return BubbleConfig.PauseTimers.Value && player != null
            && HomeBubbleZone.FindBubbleInside(player.transform.position) != null;
    }
}
