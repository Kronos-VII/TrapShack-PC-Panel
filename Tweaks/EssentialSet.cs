using Microsoft.Win32;
using TrapShack.Core;

namespace TrapShack.Tweaks;

// GENERATED from the published essential tweak definitions: registry values, services and the matching restore values are copied as published.
public static partial class Catalog
{
    const string ExplorerBags = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags";
    const string ExplorerBagMru = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU";
    const string ExplorerAllFolders = ExplorerBags + @"\AllFolders\Shell";

    static void ClearExplorerBags()
    {
        Registry.CurrentUser.DeleteSubKeyTree(ExplorerBags, false);
        Registry.CurrentUser.DeleteSubKeyTree(ExplorerBagMru, false);
    }

    /// <summary>Asked through PowerShell, so the answer is kept for a few minutes (see SlowCheck).</summary>
    static int? DefenderSampleConsent() => SlowCheck.Get<int?>("defender.samples", ReadDefenderSampleConsent);

    static int? ReadDefenderSampleConsent()
    {
        try
        {
            var r = Shell.PowerShell("(Get-MpPreference -ErrorAction Stop).SubmitSamplesConsent", 30_000);
            return r.ExitCode == 0 && int.TryParse(r.Output.Trim(), out var n) ? n : null;
        }
        catch { return null; }
    }

    /// <summary>Marks a card as a one-time action ("Run now") instead of a switch.</summary>
    static Tweak Once(Tweak t) { t.OneShot = true; return t; }

    static IEnumerable<Tweak> EssentialSet()
    {
        yield return Mk("extra.activity", Category.Debloat, Essential,
            "Turn off activity history",
            "Stops Windows from publishing or uploading what you do, while keeping clipboard history.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "PublishUserActivities", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableActivityFeed", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "PublishUserActivities", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "UploadUserActivities", 0, RegistryValueKind.DWord, null)),
            detail: "Sets the activity feed policies so Windows no longer publishes or uploads your activity. Clipboard history is left alone. Restore removes the policies.");

        yield return Mk("extra.location", Category.Debloat, Essential,
            "Turn off location tracking",
            "Denies location access system-wide and stops the location service.",
            Sometimes,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location", "Value", "Deny", RegistryValueKind.String, "Allow"),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Sensor\\Overrides\\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}", "SensorPermissionState", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\Maps", "AutoUpdateEnabled", 0, RegistryValueKind.DWord, 1),
              new ServiceAction("lfsvc", "disabled", "demand")),
            detail: "Sets location consent to Deny, turns off the location sensor and map auto-updates, and disables the Geolocation service. Apps that need your location (maps, weather) will stop finding you. Restore puts it back.");

        yield return Mk("extra.services", Category.Debloat, Essential,
            "Adjust background services",
            "Disables a few services you rarely need and sets two to Manual.",
            Sometimes,
            L(new ServiceAction("CscService", "disabled", "demand"),
              new ServiceAction("MapsBroker", "demand", "auto"),
              new ServiceAction("StorSvc", "demand", "auto"),
              new ServiceAction("SharedAccess", "disabled", "auto")),
            detail: "Disables Offline Files and Internet Connection Sharing (needed for sharing your connection as a hotspot); sets Maps and Storage services to Manual. Each service goes back to its original startup type when you restore.");

        yield return Mk("extra.telemetry", Category.Debloat, Essential,
            "Turn off telemetry",
            "Stops diagnostic data collection and related tracking services.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy", "HasAccepted", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Input\\TIPC", "Enabled", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\InputPersonalization", "RestrictImplicitInkCollection", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\InputPersonalization", "RestrictImplicitTextCollection", 1, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\InputPersonalization\\TrainedDataStore", "HarvestContacts", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Personalization\\Settings", "AcceptedPrivacyPolicy", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord, null),
              new RegistryDeleteAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Siuf\\Rules", "PeriodInNanoSeconds", RegistryValueKind.QWord),
              new ServiceAction("diagtrack", "disabled", "auto"),
              new ServiceAction("WerSvc", "disabled", "demand"),
              new CustomAction(
                "Windows Defender  →  automatic sample submission turned off (restore sets it back)",
                isApplied: () => DefenderSampleConsent() == 2,
                apply: () => { SlowCheck.Forget("defender.samples"); Shell.PowerShell("Set-MpPreference -SubmitSamplesConsent 2 -ErrorAction Stop", 60_000); },
                revert: saved => { SlowCheck.Forget("defender.samples"); Shell.PowerShell($"Set-MpPreference -SubmitSamplesConsent {(int.TryParse(saved, out var n) ? n : 1)} -ErrorAction Stop", 60_000); },
                capture: () => DefenderSampleConsent()?.ToString(),
                applicable: () => DefenderSampleConsent() != null)),
            detail: "Sets the diagnostic data policy to its lowest level, turns off tailored experiences, advertising ID, feedback prompts, handwriting and typing data collection and Defender automatic sample submission, and disables the telemetry and error reporting services. Windows Update keeps working. Restore removes each setting.");

        yield return Mk("extra.explorer-autodiscovery", Category.Debloat, Essential,
            "Stop File Explorer guessing folder types",
            "Stops Explorer analysing folder contents, which can slow browsing.",
            Sometimes,
            L(new CustomAction(
                "File Explorer  →  folder view types reset and set to the generic view (restore clears saved folder views)",
                isApplied: () => RegUtil.Read(RegistryHive.CurrentUser, ExplorerAllFolders, "FolderType") is string s && s == "NotSpecified",
                apply: () =>
                {
                    ClearExplorerBags();
                    using var k = Registry.CurrentUser.CreateSubKey(ExplorerAllFolders);
                    k.SetValue("FolderType", "NotSpecified", RegistryValueKind.String);
                },
                revert: _ => ClearExplorerBags())),
            detail: "Resets Explorer's saved folder views and sets every folder to the generic view. Warning: this also turns off Explorer's automatic grouping, and your per-folder view settings are cleared. Sign out and back in to finish.", signOut: true);

        yield return Once(Mk("extra.disk-cleanup", Category.Debloat, Essential,
            "Run Disk Cleanup",
            "One-time cleanup of drive C: and old Windows Update files.",
            Sometimes,
            L(new CustomAction(
                "Disk Cleanup on C: and Windows Update component cleanup (one-time, cannot be undone)",
                isApplied: () => false,
                apply: () => Shell.PowerShell("cleanmgr.exe /d C: /VERYLOWDISK; Dism.exe /online /Cleanup-Image /StartComponentCleanup /ResetBase", 3_600_000))),
            detail: "Runs Disk Cleanup on C: and removes superseded Windows Update components. This cannot be undone and takes several minutes. Afterwards you can no longer uninstall the Windows updates installed so far.",
            confirm: "This runs Disk Cleanup and removes old Windows Update files. It can take several minutes and cannot be undone: you will no longer be able to uninstall updates installed so far."));

        yield return Once(Mk("extra.delete-temp", Category.Debloat, Essential,
            "Delete temporary files",
            "One-time removal of everything in your Temp folders.",
            Sometimes,
            L(new CustomAction(
                "Delete the contents of the user and Windows Temp folders (one-time)",
                isApplied: () => false,
                apply: () => Shell.PowerShell("Remove-Item -Path \"$Env:Temp\\*\" -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item -Path \"$Env:SystemRoot\\Temp\\*\" -Recurse -Force -ErrorAction SilentlyContinue", 600_000))),
            detail: "Deletes the contents of your user Temp folder and the Windows Temp folder. Files that are in use are skipped. This cannot be undone.",
            confirm: "This deletes the contents of your Temp folders. Files in use are skipped. It cannot be undone."));
    }
}
