namespace HomeBubble.Bubble;

internal static class CollapseText
{
    public static string For(string station)
    {
        return Localization.instance.Localize("$homebubble_collapsed", Localization.instance.Localize(station));
    }
}
