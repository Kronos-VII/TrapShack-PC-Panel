using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

/// <summary>
/// Extra privacy, performance and gaming switches for the Optimizations pages. Only settings with a
/// documented effect are here. They are added to the matching pages after the core tweaks.
/// </summary>
public static partial class Catalog
{
    const string TrackingFeedback = "Tracking & feedback";
    const string SystemGroup = "System";
    const string GameExperience = "Game experience";

    static IEnumerable<Tweak> MorePrivacy()
    {
        yield return Mk("privacy.language-list", Category.Privacy, TrackingFeedback,
            "Stop websites reading your language list",
            "Websites can no longer use your installed languages to tailor content.",
            Always,
            L(CU(@"Control Panel\International\User Profile", "HttpAcceptLanguageOptOut", 1)),
            detail: "Same as Settings > Privacy & security > General > Let websites show me locally relevant content by accessing my language list.");

        yield return Mk("privacy.error-reporting", Category.Privacy, TrackingFeedback,
            "Turn off Windows Error Reporting",
            "Crash reports are no longer sent to Microsoft.",
            s => s.DomainJoined ? Verdict.Warn("Company IT teams often collect these reports. Ask before switching this off.") : Verdict.Opt(),
            L(LM(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, 0),
              CU(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, 0)),
            detail: "You can still see crash details locally in Reliability Monitor and Event Viewer.");

        yield return Mk("privacy.cortana", Category.Privacy, TrackingFeedback,
            "Turn off Cortana",
            "Policy switch that disables the legacy Cortana assistant.",
            Always,
            L(LM(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0),
              CU(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0)),
            detail: "Cortana is already retired on current Windows 11 builds, so this mainly matters on Windows 10.");
    }

    static IEnumerable<Tweak> MorePerformance()
    {
        yield return Mk("perf.startup-delay", Category.Performance, SystemGroup,
            "Remove the startup-apps delay",
            "Programs in your Startup list begin loading straight away after sign-in.",
            s => s.SystemDriveIsSsd == false
                ? Verdict.Warn("On a hard disk this makes the desktop feel slower while everything loads at once.")
                : Verdict.Opt("Good on an SSD: your startup apps load sooner."),
            L(CU(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0)),
            detail: "Windows normally waits a few seconds before starting your startup apps. Switching this off removes the setting so Windows uses its default again.");

        yield return Mk("perf.power-throttling", Category.Performance, SystemGroup,
            "Turn off Intel power throttling",
            "Stops Windows slowing background programs to save power.",
            s => s.IsVirtualMachine ? Verdict.No("Not applicable in a virtual machine.")
               : !s.IsIntelCpu ? Verdict.No("Power throttling is an Intel feature. It does nothing on your CPU.")
               : s.IsLaptop ? Verdict.Warn("On a laptop this costs battery life.")
               : Verdict.Opt("Harmless on a desktop, and background tools such as game launchers and capture software keep full speed."),
            L(LM(@"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1)),
            detail: "Sets Microsoft's PowerThrottlingOff policy. Takes effect after a restart.",
            restart: true);
    }

    static IEnumerable<Tweak> MoreGaming()
    {
        yield return Mk("gaming.windowed-opt", Category.Gaming, GameExperience,
            "Optimizations for windowed games",
            "Windowed and borderless games use the modern flip presentation model for lower latency.",
            s => s.Build >= 22621 ? Verdict.Rec("Microsoft's own Windows 11 feature, free and safe.") : Verdict.No("Needs Windows 11 version 22H2 or newer."),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings",
                "SwapEffectUpgradeEnable=1;", RegistryValueKind.String, null,
                matcher: v => v is string t && t.Contains("SwapEffectUpgradeEnable=1", StringComparison.OrdinalIgnoreCase))),
            detail: "The same switch as Settings > System > Display > Graphics > Default graphics settings > Optimizations for windowed games. " +
                    "Restore puts back whatever value was there before.");

        yield return Mk("gaming.xbox-button", Category.Gaming, GameExperience,
            "Stop the controller Xbox button opening Game Bar",
            "Pressing the guide button on a controller no longer pops up Game Bar over your game.",
            Sometimes,
            L(CU(@"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0, 1)),
            detail: "Game Bar and its keyboard shortcut still work. Only the controller button shortcut is switched off.");
    }
}
