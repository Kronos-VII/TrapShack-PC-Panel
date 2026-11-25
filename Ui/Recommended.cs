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
        "ai.recall-policy", "ai.copilot", "perf.visual-effects", "perf.background-apps", "essential.storage-sense", "extra.activity",
    };

    public static string ApplyLabel(string? section) => "Apply recommended settings";
}
