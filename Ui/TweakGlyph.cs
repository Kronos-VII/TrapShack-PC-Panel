namespace TrapShack.Ui;

/// <summary>
/// Picks an icon for a setting from what it is about (its id, title and group), so cards aren't all the same gear.
/// First match wins, so the more specific words come first.
/// </summary>
public static class TweakGlyph
{
    static readonly (string[] Words, string Glyph)[] Map =
    {
        (new[] { "copilot", "recall", " ai ", "ai.", "windows ai", "click to do", "office ai", "edge ai" }, ""),
        (new[] { "vbs", "virtualization based", "memory integrity", "hvci", "defender", "smartscreen", "uac", "security" }, ""),
        (new[] { "telemetry", "diagnostic", "feedback", "ceip", "error report", "compatibility appraiser", "customer experience" }, ""),
        (new[] { "location", "find my device", "find-my" }, ""),
        (new[] { "camera" }, ""),
        (new[] { "microphone", "speech", "voice", "narrator", "dictation" }, ""),
        (new[] { "inking", "typing", "keyboard", "sticky keys", "filter keys", "toggle keys" }, ""),
        (new[] { "mouse", "pointer", "cursor" }, ""),
        (new[] { "xbox", "game", "gaming", "fullscreen", "overlay", "mmcss", "hags", "gpu", "nvidia", "radeon" }, ""),
        (new[] { "sound", "audio", "volume", "spatial" }, ""),
        (new[] { "wi-fi", "wifi", "hotspot" }, ""),
        (new[] { "network", "dns", "ipv4", "ipv6", "teredo", "nagle", "tcp", "netbios", "llmnr" }, ""),
        (new[] { "edge", "browser", "brave", "chrome", "firefox" }, ""),
        (new[] { "update", "delivery optimization", "driver" }, ""),
        (new[] { " store", "microsoft store", "app install", "winget" }, ""),
        (new[] { "onedrive", "cloud", "backup" }, ""),
        (new[] { "notification", "toast", "focus assist", "do not disturb", "badge" }, ""),
        (new[] { "advert", " ads", "promotional", "subscribed content", "suggest", "tips", "consumer feature", "spotlight", "recommend" }, ""),
        (new[] { "search", "bing", "cortana", "index" }, ""),
        (new[] { "lock screen", "lockscreen", "sign-in", "account", "workplace", "password" }, ""),
        (new[] { "clipboard" }, ""),
        (new[] { "power", "sleep", "hibernat", "battery", "usb selective", "throttl", "core park", "fast startup", "energy" }, ""),
        (new[] { "explorer", "file ext", "folder", "hidden files", "this pc", "gallery", "home page" }, ""),
        (new[] { "taskbar", "widgets", "task view", "chat", "teams" }, ""),
        (new[] { "start menu", "start-", "all apps" }, ""),
        (new[] { "visual", "animation", "transparen", "wallpaper", "dark mode", "theme", "colour", "color", "wallpaper" }, ""),
        (new[] { "display", "screen", "refresh", "hdr", "scaling" }, ""),
        (new[] { "clean", "temp", "disk", "storage", "reserved" }, ""),
        (new[] { "scheduled task", "tasks", "startup delay", "startup" }, ""),
        (new[] { "performance", "boost", "priority", "foreground", "latency", "timer", "responsive", "speed" }, ""),
        (new[] { "privacy", "permission", "access", "activity", "history", "tracking" }, ""),
    };

    /// <summary>An icon for a whole group: from its name when that says what it is, else the icon most of its settings use.</summary>
    public static string ForGroup(string group, IReadOnlyCollection<TweakVM> items)
    {
        var text = " " + group.ToLowerInvariant() + " ";
        foreach (var (words, glyph) in Map)
            if (words.Any(w => text.Contains(w))) return glyph;
        return items.Count == 0 ? "\uE713" : items.GroupBy(v => v.RowGlyph).OrderByDescending(g => g.Count()).First().Key;
    }

    public static string For(TrapShack.Core.Tweak t)
    {
        var text = " " + (t.Id + " " + t.Title + " " + t.Group).ToLowerInvariant() + " ";
        foreach (var (words, glyph) in Map)
            if (words.Any(w => text.Contains(w))) return glyph;
        return t.OneShot ? "" : "";
    }
}
