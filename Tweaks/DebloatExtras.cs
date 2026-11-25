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
            "Windows Platform Binary Table (WPBT) - Disable",
            "If enabled, WPBT allows your computer vendor to execute programs at boot time, such as anti-theft software, software drivers, as well as force…",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager", "DisableWpbtExecution", 1, RegistryValueKind.DWord, null)),
            detail: "If enabled, WPBT allows your computer vendor to execute programs at boot time, such as anti-theft software, software drivers, as well as force install software without user consent. Poses potential security risk.");

        yield return Mk("extra.tweaksconsumerfeatures", Category.Debloat, Essential,
            "ConsumerFeatures - Disable",
            "Stops promoted app installs and reduces app suggestions from Microsoft Store content.",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures", 1, RegistryValueKind.DWord, null)),
            detail: "Stops promoted app installs and reduces app suggestions from Microsoft Store content.");

        yield return Mk("extra.tweaksdisablewarningforunsignedrdp", Category.Customize, "Remote Desktop",
            "RDP Unsigned File Warnings - Disable",
            "Disables warnings shown when launching unsigned RDP files introduced with the latest Windows 10 and 11 updates.",
            Sometimes,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows NT\\Terminal Services\\Client", "RedirectionWarningDialogVersion", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Microsoft\\Terminal Server Client", "RdpLaunchConsentAccepted", 1, RegistryValueKind.DWord, null)),
            detail: "Disables warnings shown when launching unsigned RDP files introduced with the latest Windows 10 and 11 updates.");

        yield return Mk("extra.tweakspreventdevicemetadatafromnetwork", Category.Privacy, "Tracking",
            "Prevent Device Companion Apps",
            "Prevents additional software from being installed when plugging in devices (e.g. Ads when plugging in a monitor). Poses potential security risk.",
            Sometimes,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Device Metadata", "PreventDeviceMetadataFromNetwork", 1, RegistryValueKind.DWord, null)),
            detail: "Prevents additional software from being installed when plugging in devices (e.g. Ads when plugging in a monitor). Poses potential security risk.");

        yield return Mk("extra.togglebatterypercentage", Category.Customize, "Taskbar",
            "System Tray Battery Percentage",
            "Shows numeric battery percentage next to the battery icon in the system tray.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "IsBatteryPercentageEnabled", 1, RegistryValueKind.DWord, null)),
            detail: "Shows numeric battery percentage next to the battery icon in the system tray.");

        yield return Mk("extra.togglescrollbars", Category.Customize, "Appearance",
            "Scrollbars Always Visible",
            "If enabled, scrollbars will always be visible. If disabled, Windows will automatically hide scrollbars when not in use.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Accessibility", "DynamicScrollbars", 0, RegistryValueKind.DWord, 1)),
            detail: "If enabled, scrollbars will always be visible. If disabled, Windows will automatically hide scrollbars when not in use.");

        yield return Mk("extra.togglebingsearch", Category.Customize, "Search",
            "Start Menu Bing Search",
            "Toggles Bing web search results in Windows Search.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "BingSearchEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Toggles Bing web search results in Windows Search.");

        yield return Mk("extra.toggleloginblur", Category.Customize, "Lock screen",
            "Logon Screen Acrylic Blur",
            "Toggles the acrylic blur effect on login screen background.",
            Sometimes,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "DisableAcrylicBackgroundOnLogon", 1, RegistryValueKind.DWord, 0)),
            detail: "Toggles the acrylic blur effect on login screen background.");

        yield return Mk("extra.togglehidesettingshome", Category.Customize, "Settings app",
            "Settings Home Page",
            "Toggles the Home Page in the Windows Settings app.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "SettingsPageVisibility", "hide:home", RegistryValueKind.String, "show:home")),
            detail: "Toggles the Home Page in the Windows Settings app.");

        yield return Mk("extra.toggles3sleep", Category.Customize, "Power",
            "S3 Sleep",
            "Toggles between Modern Standby and S3 Sleep, which cuts off power to the CPU while continuing to refresh the memory.",
            Sometimes,
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power", "PlatformAoAcOverride", 0, RegistryValueKind.DWord, null)),
            detail: "Toggles between Modern Standby and S3 Sleep, which cuts off power to the CPU while continuing to refresh the memory.");

        yield return Mk("extra.togglestandbyfix", Category.Customize, "Power",
            "S0 Sleep Network Connectivity",
            "Toggles network connectivity during S0 Sleep which is low power idle in modern laptops.",
            Sometimes,
            L(new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Power\\PowerSettings\\f15576e8-98b7-4186-b944-eafa664402d9", "ACSettingIndex", 0, RegistryValueKind.DWord, 1)),
            detail: "Toggles network connectivity during S0 Sleep which is low power idle in modern laptops.");
    }
}
