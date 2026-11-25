using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

public static partial class Catalog
{
    const string Behaviour = "Update behaviour";
    const string DangerZone = "Danger zone";
    const string EdgeGroup = "Microsoft Edge";
    const string BraveGroup = "Brave";

    const string WuKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

    static IEnumerable<Tweak> Updates()
    {
        yield return Mk("updates.delay-features", Category.Updates, Behaviour,
            "Security updates only (delay feature updates)",
            "Installs security fixes after a week, and holds back big feature upgrades for a year.",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions", 2, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions", 2, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "BranchReadinessLevel", 20, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "DeferFeatureUpdates", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "DeferFeatureUpdatesPeriodInDays", 365, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "DeferQualityUpdates", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "DeferQualityUpdatesPeriodInDays", 7, RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoUpdate", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAUShutdownOption", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AlwaysAutoRebootAtScheduledTime", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AutoInstallMinorUpdates", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "UseWUServer", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoUpdate", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAUShutdownOption", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AlwaysAutoRebootAtScheduledTime", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AutoInstallMinorUpdates", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "UseWUServer", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseFeatureUpdatesStartTime", RegistryValueKind.String, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseFeatureUpdatesEndTime", RegistryValueKind.String, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseQualityUpdatesStartTime", RegistryValueKind.String, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseQualityUpdatesEndTime", RegistryValueKind.String, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseUpdatesStartTime", RegistryValueKind.String, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseUpdatesExpiryTime", RegistryValueKind.String, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PausedQualityDate", RegistryValueKind.String, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PausedFeatureDate", RegistryValueKind.String, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "FlightSettingsMaxPauseDays", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PausedFeatureStatus", RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PausedQualityStatus", RegistryValueKind.DWord, null)),
            detail: "Delays feature updates by a year and quality updates by a week, and sets Windows to ask before downloading updates. Security updates keep coming. " +
                    "It also clears any update pause or disable policy that was set, so it undoes \"Disable Windows Update completely\". If your Windows version is close to end of support, update manually first.");

        yield return Mk("updates.delivery-optimization", Category.Updates, Behaviour,
            "Don't share updates with other PCs",
            "Turns off peer-to-peer update sharing so your connection isn't used to upload Windows updates to strangers.",
            Always,
            L(LM(@"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 99),
              CU(@"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 99)),
            detail: "Sets Delivery Optimization to its Disabled mode (99). Updates are still downloaded from Microsoft as usual.");

        yield return Mk("updates.disable-all", Category.Updates, DangerZone,
            "Disable Windows Update completely",
            "Windows never checks for or installs updates.",
            s => Verdict.Warn("You'll miss security patches, including Defender definitions. Only for offline or special-purpose PCs."),
            L(LM(WuKey + @"\AU", "NoAutoUpdate", 1),
              new ServiceAction("wuauserv")),
            detail: "Applies the \"Configure Automatic Updates: disabled\" policy and disables the Windows Update service. " +
                    "Switch it off again any time to go back to normal updating.",
            risk: Risk.Advanced,
            confirm: "With Windows Update disabled your PC won't receive security patches, which puts it at risk. Continue only if you understand that.");
    }

    static IEnumerable<Tweak> Browsers()
    {
        const string edge = @"SOFTWARE\Policies\Microsoft\Edge";
        const string brave = @"SOFTWARE\Policies\BraveSoftware\Brave";

        yield return Mk("browsers.edge-debloat", Category.Browsers, EdgeGroup,
            "Debloat Microsoft Edge",
            "Turns off the Copilot sidebar, shopping assistant, Rewards, Collections, reporting and first-run nags.",
            s => s.EdgeInstalled
                ? Verdict.Rec("Edge will show \"managed by your organization\". That is how Edge displays browser policies.")
                : Verdict.No("Edge isn't installed."),
            L(LM(@"SOFTWARE\Policies\Microsoft\EdgeUpdate", "CreateDesktopShortcutDefault", 0),
              LM(edge, "PersonalizationReportingEnabled", 0),
              LMs(edge + @"\ExtensionInstallBlocklist", "1", "ofefcgjbeghpigppfmkologfjadafddi"),
              LM(edge, "ShowRecommendationsEnabled", 0),
              LM(edge, "HideFirstRunExperience", 1),
              LM(edge, "UserFeedbackAllowed", 0),
              LM(edge, "ConfigureDoNotTrack", 1),
              LM(edge, "AlternateErrorPagesEnabled", 0),
              LM(edge, "EdgeCollectionsEnabled", 0),
              LM(edge, "EdgeShoppingAssistantEnabled", 0),
              LM(edge, "MicrosoftEdgeInsiderPromotionEnabled", 0),
              LM(edge, "ShowMicrosoftRewards", 0),
              LM(edge, "WebWidgetAllowed", 0),
              LM(edge, "DiagnosticData", 0),
              LM(edge, "EdgeAssetDeliveryServiceEnabled", 0),
              LM(edge, "WalletDonationEnabled", 0),
              LM(edge, "DefaultBrowserSettingsCampaignEnabled", 0),
              LM(edge, "HubsSidebarEnabled", 0)),
            detail: "Every setting here is an official Microsoft Edge group policy, using the same list as the published Edge debloat plus the Copilot sidebar switch. Nothing is deleted and Edge keeps working normally. Takes effect the next time Edge starts.");

        yield return Mk("browsers.edge-remove", Category.Browsers, EdgeGroup,
            "Uninstall Microsoft Edge",
            "Removes the Edge browser. The WebView2 runtime that many apps rely on is kept.",
            s => Verdict.Warn("Windows still opens some links and help pages in Edge. Only do this if you have another browser installed."),
            L(new CustomAction(
                  "Uninstall Microsoft Edge with its own installer (restore reinstalls it with winget)",
                  isApplied: () => !EdgeOps.IsInstalled(),
                  apply: EdgeOps.Remove,
                  revert: _ => EdgeOps.Reinstall())),
            detail: "Runs Edge's own uninstaller. Some Windows builds block the removal; if that happens TrapShack tells you and nothing is changed. " +
                    "Restoring reinstalls Edge from Microsoft.",
            risk: Risk.Advanced,
            confirm: "Edge will be uninstalled. Make sure you have another browser installed first. Some Windows builds refuse this, in which case nothing changes.");

        yield return Mk("browsers.brave-debloat", Category.Browsers, BraveGroup,
            "Debloat Brave",
            "Turns off Rewards, Wallet, VPN, Leo AI chat, News, Talk, Tor windows and the usage and metrics reporting.",
            s => s.BraveInstalled ? Verdict.Rec() : Verdict.No("Brave isn't installed."),
            L(LM(brave, "BraveRewardsDisabled", 1),
              LM(brave, "BraveWalletDisabled", 1),
              LM(brave, "BraveVPNDisabled", 1),
              LM(brave, "BraveAIChatEnabled", 0),
              LM(brave, "BraveNewsDisabled", 1),
              LM(brave, "BraveTalkDisabled", 1),
              LM(brave, "BraveStatsPingEnabled", 0),
              LM(brave, "TorDisabled", 1),
              LM(brave, "BraveP3AEnabled", 0),
              LM(brave, "UrlKeyedAnonymizedDataCollectionEnabled", 0),
              LM(brave, "SafeBrowsingExtendedReportingEnabled", 0),
              LM(brave, "MetricsReportingEnabled", 0)),
            detail: "These are Brave's official group policies, the supported way to switch these features off. Takes effect the next time Brave starts.");
    }
}
