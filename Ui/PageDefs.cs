using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>One page in the sidebar. Edit this list to rename, reorder or move pages.</summary>
public sealed record PageDef(string Key, string Title, string Subtitle, string Glyph, string Section);

public static class PageDefs
{
    public const string Debloat = "DEBLOAT";
    public const string Personalize = "PERSONALIZE";
    public const string Optimizations = "OPTIMIZATIONS";

    // Order here is the order in the sidebar and on the Optimizations hub.
    // Glyphs are Segoe Fluent Icons / Segoe MDL2 Assets code points.
    public static readonly List<PageDef> All = new()
    {
        new("debloat", "Debloat", "Tweaks, advanced tweaks and toggles in one place. Safe changes first; the advanced ones explain themselves before you turn them on.", "\uE74D", Debloat),
        new("winapps", "Debloat apps", "Remove the apps Windows came with. Each one is labelled, and the ones Windows needs are locked.", "\uE7B8", Debloat),
        new("apps", "Apps", "Install game launchers, the runtimes games need, and everyday apps.", "\uE71D", Debloat),

        // Optimizations hub, in this order.
        new("gaming", "Gaming & Performance", "Proven, low-risk changes for a snappier PC, matched to your CPU and graphics card.", "\uE7FC", Optimizations),
        new("privacy", "Privacy & Security", "Telemetry-adjacent settings, Windows AI, app permissions and security options.", "\uE72E", Optimizations),
        new("notifications", "Notifications", "Choose which Windows notifications and system nags you see.", "\uE7E7", Optimizations),
        new("power", "Power", "Power plan and sleep behaviour, with separate plugged-in and on-battery values.", "\uE7E8", Optimizations),
        new("sound", "Sound", "System sounds, startup sound and audio ducking.", "\uE767", Optimizations),
        new("updates", "Update", "Take control of when and how Windows updates, without losing security patches.", "\uE895", Optimizations),
        new("customize", "Customization", "Look and feel: sounds, notification extras, lock screen and accessibility shortcuts.", "\uE771", Optimizations),
    };

    /// <summary>Every Optimizations area on one page: the areas are listed on the left, gaming first, updates last.</summary>
    public static readonly PageDef OptimizationsPage = new("optimizations", "Optimizations",
        "Every optimization in one place. Pick an area on the left; the settings picked for this PC are marked Recommended.", "\uE945", Optimizations);

    /// <summary>The order of the areas in that list.</summary>
    public static readonly string[] AreaOrder = { "gaming", "privacy", "power", "sound", "notifications", "customize", "updates" };

    public static PageDef Get(string key) => key == OptimizationsPage.Key ? OptimizationsPage : All.First(p => p.Key == key);

    static readonly HashSet<string> DebloatSections = TrapShack.Tweaks.Catalog.DebloatLayout.Select(x => x.Section).ToHashSet();

    /// <summary>Which page a tweak is shown on.</summary>
    public static string KeyOf(Tweak t)
    {
        var g = t.Group ?? "";
        var power = t.Id.Contains("power", StringComparison.OrdinalIgnoreCase) || t.Title.Contains("power plan", StringComparison.OrdinalIgnoreCase)
                    || t.Title.Contains("hibernat", StringComparison.OrdinalIgnoreCase) || t.Title.Contains("sleep", StringComparison.OrdinalIgnoreCase);
        return t.Category switch
        {
            Category.Debloat => DebloatSections.Contains(g) ? "debloat" : "privacy",
            Category.Customize => g == "Notifications" ? "notifications" : g == "Sound" ? "sound" : g == "Power" ? "power" : "customize",
            Category.Privacy => "privacy",
            Category.Performance => power ? "power" : "gaming",
            Category.Gaming => "gaming",
            Category.Updates => "updates",
            _ => "gaming",
        };
    }
}
