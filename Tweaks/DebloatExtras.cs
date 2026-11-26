using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

// GENERATED from published tweak definitions: the registry-only entries that were not already in the catalog.
// Values and originals are copied as published. Toggles are inverted so the card always shows the "tweaked" state.
public static partial class Catalog
{
    static IEnumerable<Tweak> DebloatExtras()
    {
        yield return Mk("extra.tweakswpbt", Category.Debloat, AdvancedGroup,
            "Block firmware programs at startup (WPBT)",
            "Stops the PC's firmware running its maker's programs (anti-theft tools, driver installers) every time Windows starts.",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager", "DisableWpbtExecution", 1, RegistryValueKind.DWord, null)),
            detail: "If enabled, WPBT allows your computer vendor to execute programs at boot time, such as anti-theft software, software drivers, as well as force install software without user consent. Poses potential security risk.");

        yield return Mk("extra.tweaksconsumerfeatures", Category.Debloat, Essential,
            "Turn off Windows consumer features",
            "Stops promoted app installs and reduces app suggestions from Microsoft Store content.",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord, null)),
            detail: "Stops promoted app installs and reduces app suggestions from Microsoft Store content.");

        yield return Mk("extra.tweaksdisablewarningforunsignedrdp", Category.Customize, "Remote Desktop",
            "Turn off warnings for unsigned Remote Desktop files",
            "No more warning when you open a Remote Desktop (.rdp) file that isn't signed.",
            _ => Verdict.Warn("Those warnings protect against booby-trapped .rdp files sent by email. Only for people who open their own .rdp files."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows NT\\Terminal Services\\Client", "RedirectionWarningDialogVersion", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Microsoft\\Terminal Server Client", "RdpLaunchConsentAccepted", 1, RegistryValueKind.DWord, null)),
            detail: "Disables warnings shown when launching unsigned RDP files introduced with the latest Windows 10 and 11 updates.");

        yield return Mk("extra.tweakspreventdevicemetadatafromnetwork", Category.Privacy, "Tracking",
            "Stop device companion apps installing",
            "Plugging in a device (a monitor, a mouse) no longer downloads its maker's app and icons from the internet.",
            Sometimes,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Device Metadata", "PreventDeviceMetadataFromNetwork", 1, RegistryValueKind.DWord, null)),
            detail: "Prevents additional software from being installed when plugging in devices (e.g. Ads when plugging in a monitor). Poses potential security risk.");

        yield return Mk("extra.togglebatterypercentage", Category.Customize, "Taskbar",
            "Show the battery percentage in the taskbar",
            "Shows numeric battery percentage next to the battery icon in the system tray.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "IsBatteryPercentageEnabled", 1, RegistryValueKind.DWord, null)),
            detail: "Shows numeric battery percentage next to the battery icon in the system tray.");

        yield return Mk("extra.togglescrollbars", Category.Customize, "Appearance",
            "Always show scrollbars",
            "Scrollbars stay visible instead of hiding when you're not using them.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Accessibility", "DynamicScrollbars", 0, RegistryValueKind.DWord, 1)),
            detail: "If enabled, scrollbars will always be visible. If disabled, Windows will automatically hide scrollbars when not in use.");

        yield return Mk("extra.togglebingsearch", Category.Customize, "Search",
            "Turn off Bing results in Start search",
            "Searching from Start only shows what's on this PC, not Bing web results.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "BingSearchEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Sets BingSearchEnabled to 0 for your account. Switch it off to bring web results back.");

        yield return Mk("extra.toggleloginblur", Category.Customize, "Lock screen",
            "Turn off the sign-in screen blur",
            "The sign-in screen shows your lock screen picture sharp instead of blurred.",
            Sometimes,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "DisableAcrylicBackgroundOnLogon", 1, RegistryValueKind.DWord, null)),
            detail: "Sets the DisableAcrylicBackgroundOnLogon policy. Switching it off removes the policy, which is Windows' default (blurred).");

        yield return Mk("extra.togglehidesettingshome", Category.Customize, "Settings app",
            "Hide the Settings Home page",
            "Settings opens straight on System instead of the Home page with its suggestions.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "SettingsPageVisibility", "hide:home", RegistryValueKind.String, null)),
            detail: "Sets SettingsPageVisibility to hide:home. Switching it off removes the value, which is Windows' default.");

        yield return Mk("extra.toggles3sleep", Category.Customize, "Power",
            "Use classic S3 sleep instead of Modern Standby",
            "Sleep fully powers down the processor instead of staying half-awake, so laptops don't drain or heat up in a bag.",
            _ => Verdict.Warn("Only works if the PC's firmware supports S3. If it doesn't, Sleep disappears from the power menu until you switch this off again."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power", "PlatformAoAcOverride", 0, RegistryValueKind.DWord, null)),
            detail: "Sets PlatformAoAcOverride to 0. Needs a restart. Switching it off removes the value and Modern Standby comes back.", restart: true);

        yield return Mk("extra.togglestandbyfix", Category.Customize, "Power",
            "No network during Modern Standby (plugged in)",
            "While asleep on Modern Standby, the PC stops using the network when plugged in.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Power\\PowerSettings\\f15576e8-98b7-4186-b944-eafa664402d9", "ACSettingIndex", 0, RegistryValueKind.DWord, null)),
            detail: "Sets the plugged-in value of the \"network connectivity in standby\" power policy to off. Switching it off removes the policy.");
    }
}
