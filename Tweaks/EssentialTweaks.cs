using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

/// <summary>
/// Debloat page, sections "Safe for everyone" and "Read each card first". Where TrapShack already has the same tweak on another
/// card (telemetry, ads, Copilot, OneDrive, Edge, background apps, visual effects, IPv4) it is not repeated here.
/// </summary>
public static partial class Catalog
{
    const string Essential = "Safe for everyone";
    const string AdvancedGroup = "Read each card first";

    static IEnumerable<Tweak> Essentials()
    {
        yield return Mk("essential.wifi-sense", Category.Debloat, Essential,
            "Turn off Wi-Fi Sense",
            "Stops Windows sharing and auto-connecting to Wi-Fi hotspots.",
            Always,
            L(LM(@"SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting", "Value", 0, 1),
              LM(@"SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowAutoConnectToWiFiSenseHotspots", "Value", 0, 1)),
            detail: "Wi-Fi Sense was removed from current Windows builds, so this is mostly housekeeping. Harmless to switch on or off.");

        yield return Mk("essential.storage-sense", Category.Debloat, Essential,
            "Turn off Storage Sense",
            "Windows stops deleting temporary files and emptying the Recycle Bin on its own.",
            Always,
            L(CU(@"Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy", "01", 0, 1),
              CU(@"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseGlobal", 0),
              LM(@"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseGlobal", 0)),
            detail: "Storage Sense cleans up automatically when disk space is low. Turn this off only if you prefer to manage temporary files yourself.");

        yield return Mk("essential.hibernation", Category.Debloat, Essential,
            "Turn off hibernation",
            "Deletes hiberfile.sys (often several GB) and removes Hibernate and Fast Startup.",
            s => s.IsLaptop
                ? Verdict.Warn("Laptops rely on hibernation to save battery and survive running flat. Leave this off unless you are short on disk space.")
                : s.SystemDriveIsSsd == false ? Verdict.Opt("Fast Startup helps on a hard disk. You will lose it.") : Verdict.Opt(),
            L(LM(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowHibernateOption", 0, 1),
              new CustomAction(
                "Run powercfg /hibernate off (restore runs powercfg /hibernate on)",
                isApplied: () => RegUtil.ReadInt(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled") == 0,
                apply: () => RunPowerCfg("/hibernate off"),
                revert: _ => RunPowerCfg("/hibernate on"))),
            detail: "Frees disk space equal to roughly 40% of your RAM. Fast Startup depends on hibernation, so Windows will do a full boot every time.",
            restart: true);

        yield return Mk("essential.end-task", Category.Debloat, Essential,
            "Add \"End task\" to the taskbar right-click menu",
            "Right-click a running app on the taskbar and close it even if it froze.",
            s => s.Build >= 22621 ? Verdict.Rec() : Verdict.No("Needs Windows 11 version 22H2 or newer."),
            L(CU(Adv + @"\TaskbarDeveloperSettings", "TaskbarEndTask", 1, 0)),
            detail: "Uses the setting Microsoft added under Settings > System > For developers. Safe on any PC.");

    }

    static IEnumerable<Tweak> AdvancedDebloat()
    {
        yield return Mk("advanced.disable-ipv6", Category.Debloat, AdvancedGroup,
            "Disable IPv6",
            "Turns IPv6 off completely. Microsoft recommends \"Prefer IPv4\" (Network page) instead.",
            s => Verdict.Warn("Some Windows features and apps, including Xbox networking and Remote Assistance, use IPv6. Try \"Prefer IPv4\" first."),
            L(LM(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", 255)),
            detail: "Sets DisabledComponents to 0xFF. Do not combine with \"Prefer IPv4\": they use the same setting, so turn only one on. Takes effect after a restart.",
            risk: Risk.Advanced, restart: true);

        yield return Mk("advanced.teredo", Category.Debloat, AdvancedGroup,
            "Disable Teredo tunnelling",
            "Switches off Teredo, an IPv6-over-IPv4 tunnel.",
            s => Verdict.Warn("Xbox and some peer-to-peer games use Teredo for party chat and matchmaking. Leave this off if you play those."),
            L(LM(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", 1, 0),
              new CustomAction(
                  "Run netsh interface teredo set state disabled (restore sets it back to default)",
                  isApplied: () => RegUtil.ReadInt(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents") == 1,
                  apply: () => RunNetsh("interface teredo set state disabled"),
                  revert: _ => RunNetsh("interface teredo set state default"))),
            detail: "Sets the IPv6 tunnelling switch and runs netsh to turn Teredo off. Restore sets Teredo back to its default. Takes effect after a restart.",
            risk: Risk.Advanced, restart: true);

        yield return Mk("advanced.notification-center", Category.Debloat, AdvancedGroup,
            "Disable the notification centre and toasts",
            "No more pop-up notifications or notification/calendar flyout.",
            s => Verdict.Warn("You will no longer see any app notifications, including calendar alerts and security warnings."),
            L(CU(@"Software\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", 1),
              CU(@"Software\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled", 0, 1)),
            detail: "Policy plus the toast switch. Switch off to get notifications back.",
            risk: Risk.Advanced, signOut: true);

        yield return Mk("advanced.hide-gallery", Category.Debloat, AdvancedGroup,
            "Hide Gallery from File Explorer",
            "Removes the Gallery entry from the File Explorer sidebar.",
            s => s.Build >= 22621 ? Verdict.Opt() : Verdict.No("Gallery only exists in Windows 11 22H2 and newer."),
            L(CU(@"Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}", "System.IsPinnedToNameSpaceTree", 0)),
            detail: "Unpins Gallery for your account. The Home entry is left alone, and \"Open File Explorer to This PC\" is on the Customization page.",
            signOut: true);

        yield return Mk("advanced.utc-clock", Category.Debloat, AdvancedGroup,
            "Use UTC for the hardware clock (dual boot)",
            "Stops Windows and Linux fighting over the system time on a dual-boot PC.",
            s => Verdict.Warn("Only for dual-booting with Linux. On a Windows-only PC this makes the clock wrong until it resyncs."),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation",
                "RealTimeIsUniversal", 1, RegistryValueKind.DWord, null)),
            detail: "Tells Windows the hardware clock is in UTC, the way Linux expects. Restore deletes the setting.",
            risk: Risk.Advanced, restart: true);
    }

    static void RunNetsh(string args)
    {
        var r = Shell.Run("netsh.exe", args, 60_000);
        if (!r.Ok) throw new InvalidOperationException($"netsh {args} failed: {(r.Output + r.Error).Trim()}");
    }

    static void RunPowerCfg(string args)
    {
        var r = Shell.Run("powercfg.exe", args, 60_000);
        if (!r.Ok) throw new InvalidOperationException($"powercfg {args} failed: {(r.Output + r.Error).Trim()}");
    }
}
