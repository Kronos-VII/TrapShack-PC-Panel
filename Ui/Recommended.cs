namespace TrapShack.Ui;

/// <summary>
/// What "Apply recommended" means on each part of the app.
///
/// DEBLOAT pages: the standard set below is what "Apply recommended settings" applies. Add or remove tweak ids to change it.
/// OPTIMIZATIONS pages: tweaks whose id starts with "opt." and the ids in OptimizationRecommended count as recommended
/// (they carry the published recommended values); everything else is shown as Optional.
/// </summary>
public static class Recommended
{
    public static readonly HashSet<string> Standard = new()
    {
        "extra.activity",
        "extra.tweaksconsumerfeatures",
        "extra.explorer-autodiscovery",
        "extra.tweakswpbt",
        "extra.location",
        "extra.services",
        "extra.telemetry",
        "updates.delivery-optimization",
        "extra.disk-cleanup",
        "extra.delete-temp",
        "essential.end-task",
        // the safe-for-everyone defaults of the second debloat source
        "rd.suggestions",
        "rd.lockscreen-tips",
        "rd.edge-ads",
        "rd.edge-diagnostics",
        "rd.bing-search",
        "clean.store-search",
        "ai.copilot",
        "ai.recall-policy",
        "rd.click-to-do",
        "rd.ai-service",
        "clean.widgets",
        "rd.hide-chat",
        "custom.file-ext",
        "rd.standby-network",
    };

    /// <summary>
    /// Optimizations tweaks that are not named "opt.*" but whose values are the published recommended values for that setting.
    /// They count as recommended on the Optimizations pages just like the "opt.*" ones.
    /// </summary>
    public static readonly HashSet<string> OptimizationRecommended = new()
    {
        "essential.remote-assist", "perf.foreground-boost", "perf.wallpaper-quality", "gaming.mmcss", "gaming.game-bar-tips",
        "privacy.search-history", "privacy.workplace-join", "updates.no-auto-restart", "updates.store-auto",
        "more.search-highlights", "more.scoobe-suggestions", "more.onedrive-backup-block", "more.mouse-hover-instant",
        "essential.wifi-sense", "privacy.language-list", "privacy.error-reporting", "privacy.cortana", "perf.startup-delay",
        "gaming.xbox-button", "custom.startup-sound", "updates.delay-features", "updates.delivery-optimization",
        "ai.recall-policy", "ai.copilot", "perf.background-apps", "essential.storage-sense", "extra.activity",
        // gaming picks of the same source: power throttling off (only offered on Intel desktops) and mouse acceleration off
        "perf.power-throttling", "custom.mouse-accel",
        // hibernation off (desktops; laptops keep their warning)
        "essential.hibernation",
    };

    /// <summary>
    /// Settings whose own verdict already checks the hardware, and only says Recommended where it helps (for example
    /// GPU scheduling on RTX 40/50 cards). That verdict is kept as it is instead of being turned into Optional.
    /// </summary>
    public static readonly HashSet<string> HardwareChecked = new()
    {
        "gaming.hags", "gaming.windowed-opt", "gaming.game-dvr",
    };

    /// <summary>
    /// Customization picks the optimisation source marks as a personal preference (taskbar look, Explorer layout and
    /// so on). They are Recommended on the Customization page, and its Apply recommended applies them, but the Home
    /// page's Apply recommended leaves them out so it never changes how someone's desktop looks.
    /// </summary>
    public static readonly HashSet<string> PersonalChoice = new()
    {
        "custom.taskbar-left",
        "cz.start-menu-layout.more-pins",
        "cz.devices-dynamic-lighting-ambient",
        "cz.devices-dynamic-lighting-foreground-app",
        "cz.devices-default-printer-management",
        "cz.explorer-customization-icon-thumbnails",
        "cz.explorer-customization-hide-merge-conflicts",
        "cz.explorer-customization-preview-handlers",
        "cz.explorer-customization-sharing-wizard",
        "cz.explorer-customization-thispc-folder-desktop-win10",
        "cz.explorer-customization-thispc-folder-documents-win10",
        "cz.explorer-customization-thispc-folder-downloads-win10",
        "cz.explorer-customization-thispc-folder-music-win10",
        "cz.explorer-customization-thispc-folder-pictures-win10",
        "cz.explorer-customization-thispc-folder-videos-win10",
        "cz.explorer-customization-nav-expand-current",
        "cz.taskbar-extended-hover-time.instant",
        "cz.taskbar-system-tray-icons",
        "cz.taskbar-small",
    };

    public static string ApplyLabel(string? section) => "Apply recommended settings";
}
