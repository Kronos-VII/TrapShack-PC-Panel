using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

public static partial class Catalog
{
    const string Tracking = "Telemetry & tracking";
    const string WindowsAi = "Windows AI";

    const string WindowsAiKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI";

    static IEnumerable<Tweak> Privacy()
    {
        // ---------------------------------------------------------------- Telemetry & tracking
        yield return Mk("privacy.telemetry-tasks", Category.Privacy, Tracking,
            "Disable telemetry scheduled tasks",
            "Turns off the Compatibility Appraiser, Customer Experience Improvement and feedback collection tasks.",
            Always,
            L(new ScheduledTaskAction(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"),
              new ScheduledTaskAction(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater"),
              new ScheduledTaskAction(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"),
              new ScheduledTaskAction(@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"),
              new ScheduledTaskAction(@"\Microsoft\Windows\Feedback\Siuf\DmClient"),
              new ScheduledTaskAction(@"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload")),
            detail: "A standard set of telemetry tasks. Only tasks that exist on your PC are touched, and each one is put back exactly as it was when you restore.");

        // ---------------------------------------------------------------- Windows AI
        yield return Mk("ai.copilot", Category.Privacy, WindowsAi,
            "Disable Windows Copilot",
            "Turns Copilot off and removes its taskbar button.",
            s => s.Build >= 22621 ? Verdict.Rec() : Verdict.No("Copilot isn't part of this Windows build."),
            L(CU(@"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
              LM(@"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
              CU(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0, 1)),
            detail: "Applies Microsoft's \"Turn off Windows Copilot\" policy and hides the taskbar button. " +
                    "Microsoft has marked this policy as deprecated on newer builds; the button removal and the app removal below cover those.",
            signOut: true);

        yield return Mk("ai.recall-policy", Category.Privacy, WindowsAi,
            "Block Recall snapshots & Click to Do",
            "Stops Windows saving screenshots of your activity for Recall and turns Click to Do off.",
            s => !s.Is24H2OrNewer
                ? Verdict.No("Recall only exists in Windows 11 24H2 and newer.")
                : s.IsHome
                    ? Verdict.Rec("On Windows Home these policies may be ignored. The \"Disable Recall feature\" switch below is the sure way.")
                    : Verdict.Rec(),
            L(LM(WindowsAiKey, "AllowRecallEnablement", 0),
              LM(WindowsAiKey, "DisableAIDataAnalysis", 1),
              LM(WindowsAiKey, "DisableClickToDo", 1)),
            detail: "Uses Microsoft's documented WindowsAI policies: AllowRecallEnablement=0, DisableAIDataAnalysis=1 and DisableClickToDo=1.",
            restart: true);

        yield return Mk("ai.recall-feature", Category.Privacy, WindowsAi,
            "Disable the Recall Windows feature",
            "Switches the Recall component itself off. Only shown as available on PCs that have it.",
            s => s.RecallFeatureState == null
                ? Verdict.No("Recall isn't present on this PC.")
                : Verdict.Opt("Recall is on this PC but has to be set up by you, so most people never see it."),
            L(new CustomAction(
                "Windows optional feature \"Recall\"  →  disabled",
                isApplied: () => string.Equals(SystemInfo.Current.RecallFeatureState, "Disabled", StringComparison.OrdinalIgnoreCase),
                apply: () =>
                {
                    var r = Shell.PowerShell("Disable-WindowsOptionalFeature -Online -FeatureName Recall -NoRestart -ErrorAction Stop | Out-Null");
                    if (!r.Ok) throw new InvalidOperationException("Windows could not disable Recall: " + (r.Error + r.Output).Trim());
                    SystemInfo.Current.RecallFeatureState = "Disabled";
                },
                revert: _ =>
                {
                    var r = Shell.PowerShell("Enable-WindowsOptionalFeature -Online -FeatureName Recall -NoRestart -ErrorAction Stop | Out-Null");
                    if (!r.Ok) throw new InvalidOperationException("Windows could not re-enable Recall: " + (r.Error + r.Output).Trim());
                    SystemInfo.Current.RecallFeatureState = "Enabled";
                },
                applicable: () => SystemInfo.Current.RecallFeatureState != null)),
            risk: Risk.Advanced,
            restart: true);

        yield return Mk("ai.notepad", Category.Privacy, WindowsAi,
            "Turn off AI in Notepad",
            "Disables the AI features built into Notepad.",
            s => s.Build >= 22621 ? Verdict.Opt() : Verdict.No("Notepad's AI features are part of Windows 11."),
            L(LM(@"SOFTWARE\Policies\WindowsNotepad", "DisableAIFeatures", 1)),
            detail: "Applies the Notepad \"DisableAIFeatures\" policy. Restore removes it.");

        yield return Mk("ai.hide-settings-page", Category.Privacy, WindowsAi,
            "Hide the AI components page in Settings",
            "Hides the \"AI components\" page in the Settings app.",
            s => s.Build >= 22621 ? Verdict.Opt() : Verdict.No("The AI components page is part of Windows 11."),
            L(LMs(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility", "hide:aicomponents")),
            detail: "Sets the machine-wide Settings page visibility policy to hide:aicomponents. Restore removes the policy.");

        yield return Mk("ai.copilot-app", Category.Privacy, WindowsAi,
            "Remove the Copilot app",
            "Uninstalls the standalone Copilot app.",
            s => s.Build >= 22621 ? Verdict.Opt() : Verdict.No("The Copilot app isn't part of this Windows build."),
            L(new AppxAction("Microsoft.Copilot", "Microsoft.Windows.Ai.Copilot.Provider")),
            detail: "Removes the Copilot app package. Reinstall it from the Microsoft Store if you change your mind.",
            store: true);
    }
}
