using TrapShack.Core;

namespace TrapShack.Tweaks;

/// <summary>
/// The Debloat page: one page with four sections, in this order. A tweak listed here is shown on the Debloat page
/// under that section, and nowhere else. Anything not listed stays on its Optimizations / Customization / Apps page.
/// </summary>
public static partial class Catalog
{
    // "@Name" places every option of that drop-down (see DebloatPlus.cs) at that spot.
    public static readonly (string Section, string Glyph, string[] Ids)[] DebloatLayout =
    {
        ("Privacy & suggested content", "\uE72E", new[]
        {
            "extra.telemetry", "extra.activity", "rd.suggestions", "rd.lockscreen-tips", "extra.location", "rd.find-my-device",
            "extra.tweaksconsumerfeatures", "clean.lockscreen", "clean.oosu",
        }),
        ("Windows AI", "\uE794", new[]
        {
            "ai.copilot", "ai.copilot-app", "ai.recall-policy", "ai.recall-feature", "rd.click-to-do", "rd.ai-service",
            "ai.notepad", "rd.paint-ai", "ai.hide-settings-page", "rd.chrome-ai",
        }),
        ("Start menu & search", "\uE721", new[]
        {
            "rd.bing-search", "extra.togglebingsearch", "clean.store-search", "clean.start-recommended", "rd.start-phone-link",
            "rd.start-all-apps", "extra.revert-start-menu", "extra.togglehidesettingshome", "@All apps view in Start",
        }),
        ("Taskbar", "\uE75B", new[]
        {
            "clean.widgets", "rd.hide-chat", "custom.taskview", "custom.taskbar-left", "essential.end-task", "rd.last-active-click",
            "extra.togglebatterypercentage", "@Taskbar search", "@Combine taskbar buttons", "@Combine buttons on other screens", "@Show taskbar apps on",
        }),
        ("File Explorer", "\uEC50", new[]
        {
            "custom.file-ext", "custom.hidden-files", "essential.hide-home", "advanced.hide-gallery", "rd.hide-onedrive",
            "rd.this-pc-folders", "custom.classic-menu", "extra.explorer-autodiscovery", "@Drive letters",
        }),
        ("System", "\uE770", new[]
        {
            "extra.services", "essential.hibernation", "rd.fast-startup", "rd.standby-network", "extra.togglestandbyfix", "extra.toggles3sleep",
            "custom.mouse-accel", "custom.sticky-keys", "custom.numlock", "essential.long-paths", "custom.detailed-bsod", "custom.verbose-logon",
            "perf.svchost-split", "perf.background-apps", "perf.search-indexing", "advanced.notification-center", "advanced.utc-clock",
            "clean.bitlocker", "essential.storage-sense", "clean.reserved-storage", "extra.tweakswpbt", "extra.tweaksdisablewarningforunsignedrdp",
            "extra.restore-point",
        }),
        ("Appearance", "\uE771", new[]
        {
            "custom.dark-theme", "perf.visual-effects", "extra.togglescrollbars", "extra.toggleloginblur", "@Desktop spotlight",
        }),
        ("Multitasking", "\uE7C4", new[]
        {
            "custom.snap-window", "rd.snap-assist", "rd.snap-layouts", "@Browser tabs in Alt+Tab",
        }),
        ("Windows Update", "\uE895", new[]
        {
            "updates.delivery-optimization", "extra.tweakspreventdevicemetadatafromnetwork",
        }),
        ("Gaming", "\uE7FC", new[]
        {
            "opt.gaming-game-mode", "rd.gamebar-integration", "more.overlay-compat", "more.overlay-full", "more.overlay-minfps", "gaming.ultimate-power",
        }),
        ("Browsers & apps", "\uE774", new[]
        {
            "browsers.edge-debloat", "rd.edge-ads", "browsers.edge-remove", "browsers.brave-debloat", "clean.new-outlook", "debloat.onedrive",
            "clean.razer-block", "clean.logi-block", "clean.adobe-block",
        }),
        ("Network", "\uE701", new[]
        {
            "network.prefer-ipv4", "advanced.teredo", "advanced.disable-ipv6",
        }),
        ("Clean up", "\uE74D", new[]
        {
            "extra.disk-cleanup", "extra.delete-temp",
        }),
    };

    /// <summary>Settings that a drop-down on the Debloat page now covers; they are left out of the app.</summary>
    static readonly HashSet<string> Superseded = new() { "custom.taskbar-search" };

    /// <summary>Moves the listed tweaks onto the Debloat page, in the order above.</summary>
    static List<Tweak> ApplyDebloatLayout(List<Tweak> all)
    {
        all = all.Where(t => !Superseded.Contains(t.Id)).ToList();
        var byId = all.ToDictionary(t => t.Id);
        var placed = new List<Tweak>();
        foreach (var (section, _, ids) in DebloatLayout)
            foreach (var id in ids)
            {
                var hits = id.StartsWith('@')
                    ? all.Where(t => t.ChoiceGroup == id[1..]).ToList()
                    : byId.TryGetValue(id, out var one) ? new List<Tweak> { one } : new List<Tweak>();
                foreach (var t in hits)
                {
                    t.Category = Category.Debloat;
                    t.Group = section;
                    placed.Add(t);
                }
            }
        var placedIds = placed.Select(t => t.Id).ToHashSet();
        return placed.Concat(all.Where(t => !placedIds.Contains(t.Id))).ToList();
    }
}
