using Microsoft.Win32;
using TrapShack.Core;

namespace TrapShack.Tweaks;

// The rest of the optimisation source's Optimizations and Customization settings, in TrapShack's style. Each switch
// turns on the change from the Windows default; Recommended follows that source's recommended value for the setting.
// GENERATED from the source's own definitions (registry paths, values and Windows defaults). Titles are hand written.
public static partial class Catalog
{
    static IEnumerable<Tweak> SourceSettings()
    {
        yield return Mk("opt.updates-other-products", Category.Updates, "Update behaviour",
            "Get updates for other Microsoft products",
            "Office and other Microsoft software update together with Windows.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"Software\Microsoft\WindowsUpdate\UX\Settings", @"AllowMUUpdateService", 1, RegistryValueKind.DWord, 0)),
            detail: "Office and other Microsoft software update together with Windows. About this setting: Get Microsoft Office and other updates together with Windows updates.");

        yield return Mk("opt.updates-notification-level", Category.Updates, "Update behaviour",
            "Hide all Windows Update notifications",
            "No pop-ups about available updates or update progress, including restart warnings.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", @"SetUpdateNotificationLevel", 2, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", @"SetUpdateNotificationLevel", 2, RegistryValueKind.DWord, null)),
            detail: "No pop-ups about available updates or update progress, including restart warnings. About this setting: Show or hide notifications about available updates and update progress.");

        yield return Mk("opt.updates-restart-notification", Category.Updates, "Update behaviour",
            "Tell me when a restart is needed for updates",
            "Shows a notification when your PC needs a restart to finish updating.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"Software\Microsoft\WindowsUpdate\UX\Settings", @"RestartNotificationsAllowed2", 1, RegistryValueKind.DWord, 0)),
            detail: "Shows a notification when your PC needs a restart to finish updating. About this setting: Show notification when your device requires a restart to finish updating.");

        yield return Mk("opt.gaming-performance-explorer-search", Category.Performance, "Responsiveness",
            "Search the whole drive, not just indexed places",
            "More complete search results, but searching is much slower.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Search\Preferences", @"WholeFileSystem", 1, RegistryValueKind.DWord, 0)),
            detail: "More complete search results, but searching is much slower. About this setting: Search your entire file system instead of only indexed locations. This provides more complete results but is significantly slower than indexed search and increases disk activity.");

        yield return Mk("opt.gaming-performance-explorer-menu-show-delay", Category.Performance, "Responsiveness",
            "Open menus instantly",
            "Removes the pause before sub-menus open (400 ms by default).",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"MenuShowDelay", @"0", RegistryValueKind.String, @"400")),
            detail: "Removes the pause before sub-menus open (400 ms by default). About this setting: Add a brief delay before displaying menus (400ms - Windows default), or show them instantly (0ms) for faster navigation.");

        yield return Mk("opt.gaming-fullscreen-optimizations", Category.Gaming, "Graphics",
            "Turn off fullscreen optimizations",
            "Can fix stutter in some older fullscreen games. Most games run best with it on.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"System\GameConfigStore", @"GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord, 0)),
            detail: "Can fix stutter in some older fullscreen games. Most games run best with it on. About this setting: Allow Windows to optimize games running in fullscreen mode. Disabling can fix performance issues or stuttering in some older games that don't work well with borderless fullscreen optimization.");

        yield return Mk("opt.gaming-performance-desktop-composition", Category.Gaming, "Graphics",
            "Turn off desktop composition effects",
            "Only for very old hardware: removes window effects for a tiny speed-up.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\DWM", @"CompositionPolicy", 0, RegistryValueKind.DWord, null)),
            detail: "Only for very old hardware: removes window effects for a tiny speed-up. About this setting: Enable visual effects managed by the Desktop Window Manager. Disabling may provide minor performance gains on older hardware but will break Aero effects.");

        yield return Mk("opt.gaming-network-throttling", Category.Performance, "Network",
            "Turn off network throttling for media",
            "Removes Windows' packet limit while media plays. The default is better for most connections.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", @"NetworkThrottlingIndex", -1, RegistryValueKind.DWord, 10)),
            detail: "Removes Windows' packet limit while media plays. The default is better for most connections. About this setting: Controls network packet rate limiting for multimedia applications. Keeping throttling enabled (default: 10 packets/ms) is recommended as it provides better DPC latency for gaming than disabling it entirely.");

        yield return Choice("Visual effects", "Let Windows choose (default)", Mk("opt.visual-effects-mode.let-windows-choose-default", Category.Performance, "Visual effects",
            "Visual effects: Let Windows choose (default)",
            "Which visual effects set Windows uses. Custom keeps the effects you choose below.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", @"VisualFXSetting", 0, RegistryValueKind.DWord, 0)),
            detail: "Which visual effects set Windows uses. Custom keeps the effects you choose below. About this setting: Choose how Windows displays visual effects.", restart: true));

        yield return Choice("Visual effects", "Best appearance", Mk("opt.visual-effects-mode.best-appearance", Category.Performance, "Visual effects",
            "Visual effects: Best appearance",
            "Which visual effects set Windows uses. Custom keeps the effects you choose below.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", @"VisualFXSetting", 1, RegistryValueKind.DWord, 0)),
            detail: "Which visual effects set Windows uses. Custom keeps the effects you choose below. About this setting: Choose how Windows displays visual effects.", restart: true));

        yield return Choice("Visual effects", "Best performance", Mk("opt.visual-effects-mode.best-performance", Category.Performance, "Visual effects",
            "Visual effects: Best performance",
            "Which visual effects set Windows uses. Custom keeps the effects you choose below.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", @"VisualFXSetting", 2, RegistryValueKind.DWord, 0)),
            detail: "Which visual effects set Windows uses. Custom keeps the effects you choose below. About this setting: Choose how Windows displays visual effects.", restart: true));

        yield return Choice("Visual effects", "Custom", Mk("opt.visual-effects-mode.custom", Category.Performance, "Visual effects",
            "Visual effects: Custom",
            "Which visual effects set Windows uses. Custom keeps the effects you choose below.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", @"VisualFXSetting", 3, RegistryValueKind.DWord, 0)),
            detail: "Which visual effects set Windows uses. Custom keeps the effects you choose below. About this setting: Choose how Windows displays visual effects.", restart: true));

        yield return Mk("opt.ui-effects", Category.Performance, "Visual effects",
            "Turn off animations inside windows",
            "Buttons and controls appear without animating.",
            s => Verdict.Rec(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"UserPreferencesMask", 4, 0x02, false, true)),
            detail: "Buttons and controls appear without animating. About this setting: Enables animation effects for controls and UI elements.", restart: true);

        yield return Mk("opt.window-animation", Category.Performance, "Visual effects",
            "Turn off minimise and maximise animations",
            "Windows open, close and minimise instantly.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Control Panel\Desktop\WindowMetrics", @"MinAnimate", @"0", RegistryValueKind.String, @"1")),
            detail: "Windows open, close and minimise instantly. About this setting: Shows smooth animation when windows are minimized or maximized.", restart: true);

        yield return Mk("opt.taskbar-animations", Category.Performance, "Visual effects",
            "Turn off taskbar animations",
            "Taskbar buttons appear and change without animating.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarAnimations", 0, RegistryValueKind.DWord, 1)),
            detail: "Taskbar buttons appear and change without animating. About this setting: Controls taskbar animation effects for opening, closing, and switching windows.", restart: true);

        yield return Mk("opt.enable-peek", Category.Performance, "Visual effects",
            "Turn off Peek",
            "Hovering the Show desktop corner no longer previews the desktop.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\DWM", @"EnableAeroPeek", 0, RegistryValueKind.DWord, 1)),
            detail: "Hovering the Show desktop corner no longer previews the desktop. About this setting: Allows peeking at desktop when hovering over Show Desktop button.", restart: true);

        yield return Mk("opt.menu-animation", Category.Performance, "Visual effects",
            "Turn off menu fade and slide",
            "Menus appear instantly.",
            s => Verdict.Rec(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"UserPreferencesMask", 0, 0x02, false, true)),
            detail: "Menus appear instantly. About this setting: Animates menus when they appear using fade or slide effects.", restart: true);

        yield return Mk("opt.fade-tooltip", Category.Performance, "Visual effects",
            "Turn off tooltip fade and slide",
            "Tooltips appear instantly.",
            s => Verdict.Rec(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"UserPreferencesMask", 1, 0x08, false, true)),
            detail: "Tooltips appear instantly. About this setting: Animates tooltips when they appear using fade or slide effects.", restart: true);

        yield return Mk("opt.fade-menu-items", Category.Performance, "Visual effects",
            "Turn off fading menu items after a click",
            "Menus close straight away after you click.",
            s => Verdict.Rec(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"UserPreferencesMask", 1, 0x04, false, true)),
            detail: "Menus close straight away after you click. About this setting: Fades menu items after selection before closing the menu.", restart: true);

        yield return Mk("opt.taskbar-thumbnails", Category.Performance, "Visual effects",
            "Keep taskbar thumbnail previews saved",
            "Saves window previews so they show faster when you hover the taskbar.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\DWM", @"AlwaysHibernateThumbnails", 1, RegistryValueKind.DWord, 0)),
            detail: "Saves window previews so they show faster when you hover the taskbar. About this setting: Saves thumbnail previews of taskbar windows for faster display.", restart: true);

        yield return Mk("opt.mouse-shadow", Category.Performance, "Visual effects",
            "Turn off the mouse pointer shadow",
            "No shadow under the cursor.",
            s => Verdict.Rec(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"UserPreferencesMask", 1, 0x20, false, true)),
            detail: "No shadow under the cursor. About this setting: Displays shadow effect underneath the mouse cursor.", restart: true);

        yield return Mk("opt.window-shadows", Category.Performance, "Visual effects",
            "Turn off shadows under windows",
            "Windows are drawn without drop shadows.",
            s => Verdict.Rec(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"UserPreferencesMask", 2, 0x04, false, true)),
            detail: "Windows are drawn without drop shadows. About this setting: Displays shadow effects underneath windows.", restart: true);

        yield return Mk("opt.show-thumbnails", Category.Performance, "Visual effects",
            "Show icons instead of thumbnails",
            "Files show their type icon instead of a picture preview.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"IconsOnly", 1, RegistryValueKind.DWord, 0)),
            detail: "Files show their type icon instead of a picture preview. About this setting: Displays image and document previews instead of generic file icons.", restart: true);

        yield return Mk("opt.translucent-selection", Category.Performance, "Visual effects",
            "Turn off the see-through selection box",
            "Dragging to select shows a plain outline.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ListviewAlphaSelect", 0, RegistryValueKind.DWord, 1)),
            detail: "Dragging to select shows a plain outline. About this setting: Display a semi-transparent selection box when dragging to select multiple files or items.", restart: true);

        yield return Mk("opt.drag-full-windows", Category.Performance, "Visual effects",
            "Show only an outline while dragging windows",
            "Windows don't redraw their contents while you move them.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"DragFullWindows", @"0", RegistryValueKind.String, @"1")),
            detail: "Windows don't redraw their contents while you move them. About this setting: Displays window contents when dragging instead of just an outline.", restart: true);

        yield return Mk("opt.combo-box-animation", Category.Performance, "Visual effects",
            "Turn off drop-down list animation",
            "Drop-down lists open instantly.",
            s => Verdict.Rec(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"UserPreferencesMask", 0, 0x04, false, true)),
            detail: "Drop-down lists open instantly. About this setting: Animates combo boxes when they open with a sliding effect.", restart: true);

        yield return Mk("opt.smooth-scroll-listboxes", Category.Performance, "Visual effects",
            "Turn off smooth scrolling in lists",
            "Lists jump straight to the new position.",
            s => Verdict.Rec(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Control Panel\Desktop", @"UserPreferencesMask", 0, 0x08, false, true)),
            detail: "Lists jump straight to the new position. About this setting: Enables smooth scrolling in list boxes instead of jumping.", restart: true);

        yield return Mk("opt.drop-shadows", Category.Performance, "Visual effects",
            "Turn off drop shadows on desktop icon labels",
            "Icon names on the desktop are drawn without a shadow.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ListviewShadow", 0, RegistryValueKind.DWord, 1)),
            detail: "Icon names on the desktop are drawn without a shadow. About this setting: Add shadow effects behind desktop icon text to improve readability against backgrounds.", restart: true);

        yield return Mk("opt.accessibility-highcontrast-hotkey", Category.Customize, "Accessibility shortcuts",
            "Turn off the High Contrast shortcut (Left Alt+Left Shift+Print Screen)",
            "Stops that key combination switching High Contrast on by accident.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Control Panel\Accessibility\HighContrast", @"Flags", @"4194", RegistryValueKind.String, @"126")),
            detail: "Stops that key combination switching High Contrast on by accident. About this setting: Enable the keyboard shortcut to activate High Contrast mode by pressing Left Alt + Left Shift + Print Screen.");

        yield return Mk("opt.security-developer-mode", Category.Privacy, "Security",
            "Turn on Developer Mode",
            "Lets apps be installed from any source, including loose files.",
            s => Verdict.Warn("Lets apps install from any source, including unsigned files. Only for app developers."),
            L(new RegistryAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\AppModelUnlock", @"AllowDevelopmentWithoutDevLicense", 1, RegistryValueKind.DWord, 0)),
            detail: "Lets apps be installed from any source, including loose files. About this setting: Allows the installation of apps from any source, including loose files.");

        yield return Mk("opt.privacy-camera-access", Category.Privacy, "App permissions",
            "Block all apps from using the camera",
            "No app can use your camera until you switch this off again.",
            s => Verdict.Warn("Video calls and webcam apps stop working until you switch this off."),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam", @"Value", @"Deny", RegistryValueKind.String, @"Allow")),
            detail: "No app can use your camera until you switch this off again. About this setting: Allow apps to have camera access.");

        yield return Mk("opt.privacy-microphone-access", Category.Privacy, "App permissions",
            "Block all apps from using the microphone",
            "No app can use your microphone, including Discord and games, until you switch this off.",
            s => Verdict.Warn("Voice chat in Discord and games stops working until you switch this off."),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone", @"Value", @"Deny", RegistryValueKind.String, @"Allow")),
            detail: "No app can use your microphone, including Discord and games, until you switch this off. About this setting: Allow apps to have microphone access.");

        yield return Mk("opt.notifications-app-location-request", Category.Customize, "Notifications",
            "Don't notify when apps ask for your location",
            "Apps can no longer pop up a location request notification.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", @"ShowGlobalPrompts", 0, RegistryValueKind.DWord, 1)),
            detail: "Apps can no longer pop up a location request notification. About this setting: Show notifications when apps attempt to access your location information.");

        yield return Mk("opt.notifications-windows-security", Category.Customize, "Notifications",
            "Turn off Windows Security notifications",
            "No notifications about threats, scans or protection status.",
            s => Verdict.Warn("You would no longer hear about threats Windows Security finds."),
            L(new RegistryAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows Defender Security Center\Notifications", @"DisableNotifications", 1, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows Defender Security Center\Notifications", @"DisableNotifications", 1, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows Defender Security Center\Notifications", @"DisableNotifications", 1, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows Defender Security Center\Notifications", @"DisableEnhancedNotifications", 1, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows Defender Security Center\Notifications", @"DisableEnhancedNotifications", 1, RegistryValueKind.DWord, 0)),
            detail: "No notifications about threats, scans or protection status. About this setting: Show all notifications from Windows Security about threats, scans, and protection status.");

        yield return Mk("opt.notifications-security-maintenance", Category.Customize, "Notifications",
            "Turn off Security and Maintenance notifications",
            "No notifications from Security and Maintenance.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.SecurityAndMaintenance", @"Enabled", 0, RegistryValueKind.DWord, 1)),
            detail: "No notifications from Security and Maintenance. About this setting: Show notifications from the Security and Maintenance Action Center.");

        yield return Choice("Start layout", "Default", Mk("cz.start-menu-layout.default", Category.Customize, "Start menu",
            "Start layout: Default",
            "More pinned apps or more recommendations in Start.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : s.Build < 22000 ? Verdict.No("Needs a newer version of Windows.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"Start_Layout", 0, RegistryValueKind.DWord, 0)),
            detail: "More pinned apps or more recommendations in Start. About this setting: Choose whether the Start Menu shows more pinned apps, more recommendations, or a balanced default layout."));

        yield return Choice("Start layout", "More pins", Mk("cz.start-menu-layout.more-pins", Category.Customize, "Start menu",
            "Start layout: More pins",
            "More pinned apps or more recommendations in Start.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : s.Build < 22000 ? Verdict.No("Needs a newer version of Windows.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"Start_Layout", 1, RegistryValueKind.DWord, 0)),
            detail: "More pinned apps or more recommendations in Start. About this setting: Choose whether the Start Menu shows more pinned apps, more recommendations, or a balanced default layout."));

        yield return Choice("Start layout", "More recommendations", Mk("cz.start-menu-layout.more-recommendations", Category.Customize, "Start menu",
            "Start layout: More recommendations",
            "More pinned apps or more recommendations in Start.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : s.Build < 22000 ? Verdict.No("Needs a newer version of Windows.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"Start_Layout", 2, RegistryValueKind.DWord, 0)),
            detail: "More pinned apps or more recommendations in Start. About this setting: Choose whether the Start Menu shows more pinned apps, more recommendations, or a balanced default layout."));

        yield return Mk("cz.start-show-recently-added-apps", Category.Customize, "Start menu",
            "Hide recently added apps in Start",
            "New installs no longer appear at the top of All apps.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Start", @"ShowRecentList", 0, RegistryValueKind.DWord, 1)),
            detail: "New installs no longer appear at the top of All apps. About this setting: Display a list of recently installed applications at the top of the All Apps list.");

        yield return Mk("cz.start-show-frequent-list", Category.Customize, "Start menu",
            "Hide most used apps in Start",
            "Your most launched apps no longer appear at the top of All apps.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Start", @"ShowFrequentList", 0, RegistryValueKind.DWord, 1)),
            detail: "Your most launched apps no longer appear at the top of All apps. About this setting: Display your frequently launched applications at the top of the All Apps list for quick access.");

        yield return Mk("cz.start-show-recommended-files", Category.Customize, "Start menu",
            "Hide recent files in Start",
            "Recently opened files no longer appear in Start's Recommended section.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"Start_TrackDocs", 0, RegistryValueKind.DWord, 1)),
            detail: "Recently opened files no longer appear in Start's Recommended section. About this setting: Display your recently opened documents and files in the Start Menu's Recommended section for quick access.");

        yield return Choice("Shortcut names", "Add \" - Shortcut\" (default)", Mk("cz.explorer-customization-shortcut-suffix.add-shortcut-default", Category.Customize, "Desktop",
            "Shortcut names: Add \" - Shortcut\" (default)",
            "Whether new shortcuts get \" - Shortcut\" added to their name.",
            s => Verdict.Opt(),
            L(new RegistryDeleteAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", @"link", RegistryValueKind.Binary, null)),
            detail: "Whether new shortcuts get \" - Shortcut\" added to their name. About this setting: Controls whether Windows appends '- Shortcut' text to newly created shortcut file names.", restart: true));

        yield return Choice("Shortcut names", "Plain name", Mk("cz.explorer-customization-shortcut-suffix.plain-name", Category.Customize, "Desktop",
            "Shortcut names: Plain name",
            "Whether new shortcuts get \" - Shortcut\" added to their name.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", @"link", new byte[] { 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary, null)),
            detail: "Whether new shortcuts get \" - Shortcut\" added to their name. About this setting: Controls whether Windows appends '- Shortcut' text to newly created shortcut file names.", restart: true));

        yield return Mk("cz.explorer-customization-desktop-icon-this-pc", Category.Customize, "Desktop",
            "Show This PC on the desktop",
            "Adds the This PC icon to the desktop.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", @"{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu", @"{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0, RegistryValueKind.DWord, 1)),
            detail: "Adds the This PC icon to the desktop. About this setting: Displays the This PC (Computer) icon on the desktop.", restart: true);

        yield return Mk("cz.explorer-customization-desktop-icon-recycle-bin", Category.Customize, "Desktop",
            "Hide the Recycle Bin from the desktop",
            "Removes the Recycle Bin icon from the desktop.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", @"{645FF040-5081-101B-9F08-00AA002F954E}", 1, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu", @"{645FF040-5081-101B-9F08-00AA002F954E}", 1, RegistryValueKind.DWord, 0)),
            detail: "Removes the Recycle Bin icon from the desktop. About this setting: Displays the Recycle Bin icon on the desktop.", restart: true);

        yield return Mk("cz.explorer-customization-desktop-icon-users-files", Category.Customize, "Desktop",
            "Show your user folder on the desktop",
            "Adds your profile folder icon to the desktop.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", @"{59031A47-3F72-44A7-89C5-5595FE6B30EE}", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu", @"{59031A47-3F72-44A7-89C5-5595FE6B30EE}", 0, RegistryValueKind.DWord, 1)),
            detail: "Adds your profile folder icon to the desktop. About this setting: Displays the current user's profile folder icon on the desktop.", restart: true);

        yield return Mk("cz.explorer-customization-desktop-icon-control-panel", Category.Customize, "Desktop",
            "Show Control Panel on the desktop",
            "Adds the Control Panel icon to the desktop.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", @"{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu", @"{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", 0, RegistryValueKind.DWord, 1)),
            detail: "Adds the Control Panel icon to the desktop. About this setting: Displays the Control Panel icon on the desktop.", restart: true);

        yield return Mk("cz.explorer-customization-desktop-icon-network", Category.Customize, "Desktop",
            "Show Network on the desktop",
            "Adds the Network icon to the desktop.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", @"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu", @"{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", 0, RegistryValueKind.DWord, 1)),
            detail: "Adds the Network icon to the desktop. About this setting: Displays the Network icon on the desktop.", restart: true);

        yield return Mk("cz.explorer-context-menu-windows-terminal", Category.Customize, "Context menu",
            "Remove \"Open in Terminal\" from the right-click menu",
            "Hides Windows Terminal from the folder right-click menu.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", @"{9F156763-7844-4DC4-B2B1-901F640F5155}", @"", RegistryValueKind.String, null)),
            detail: "Hides Windows Terminal from the folder right-click menu. About this setting: Displays the Windows Terminal option when right-clicking folders and backgrounds in File Explorer.");

        yield return Mk("cz.devices-dynamic-lighting-ambient", Category.Customize, "Devices",
            "Turn off Dynamic Lighting",
            "Windows stops controlling the RGB lighting on your devices, so your own lighting software is in charge.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Lighting", @"AmbientLightingEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Windows stops controlling the RGB lighting on your devices, so your own lighting software is in charge. About this setting: Allow Windows Dynamic Lighting to control ambient RGB effects on compatible devices.");

        yield return Mk("cz.devices-dynamic-lighting-foreground-app", Category.Customize, "Devices",
            "Stop foreground apps controlling lighting",
            "Apps in front no longer take over your RGB lighting.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Lighting", @"ControlledByForegroundApp", 0, RegistryValueKind.DWord, 1)),
            detail: "Apps in front no longer take over your RGB lighting. About this setting: Allow compatible apps to control device lighting effects.");

        yield return Mk("cz.devices-default-printer-management", Category.Customize, "Devices",
            "Stop Windows changing your default printer",
            "Your default printer stays the one you picked.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows NT\CurrentVersion\Windows", @"LegacyDefaultPrinterMode", 1, RegistryValueKind.DWord, 0)),
            detail: "Your default printer stays the one you picked. About this setting: Let Windows automatically set your default printer based on your location or last used printer.");

        yield return Choice("Opening folders", "Same window (default)", Mk("cz.explorer-customization-browse-folders.same-window-default", Category.Customize, "File Explorer",
            "Opening folders: Same window (default)",
            "Whether a folder opens in the same window or a new one.",
            s => Verdict.Opt(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState", @"Settings", 4, 0x20, false, false)),
            detail: "Whether a folder opens in the same window or a new one. About this setting: Choose whether each folder opens in the same window or in its own window.", restart: true));

        yield return Choice("Opening folders", "New window each time", Mk("cz.explorer-customization-browse-folders.new-window-each-time", Category.Customize, "File Explorer",
            "Opening folders: New window each time",
            "Whether a folder opens in the same window or a new one.",
            s => Verdict.Opt(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState", @"Settings", 4, 0x20, true, false)),
            detail: "Whether a folder opens in the same window or a new one. About this setting: Choose whether each folder opens in the same window or in its own window.", restart: true));

        yield return Choice("Opening items", "Double-click (default)", Mk("cz.explorer-customization-click-items.double-click-default", Category.Customize, "File Explorer",
            "Opening items: Double-click (default)",
            "Double-click or single-click to open files and folders.",
            s => Verdict.Opt(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShellState", 4, 0x20, true, false),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"IconUnderline", 3, RegistryValueKind.DWord, 3)),
            detail: "Double-click or single-click to open files and folders. About this setting: Choose whether to open files and folders with a single click (like web links) or double-click (traditional).", restart: true));

        yield return Choice("Opening items", "Single-click, always underlined", Mk("cz.explorer-customization-click-items.single-click-always-underlined", Category.Customize, "File Explorer",
            "Opening items: Single-click, always underlined",
            "Double-click or single-click to open files and folders.",
            s => Verdict.Opt(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShellState", 4, 0x20, false, false),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"IconUnderline", 3, RegistryValueKind.DWord, 3)),
            detail: "Double-click or single-click to open files and folders. About this setting: Choose whether to open files and folders with a single click (like web links) or double-click (traditional).", restart: true));

        yield return Choice("Opening items", "Single-click, underlined on hover", Mk("cz.explorer-customization-click-items.single-click-underlined-on-hover", Category.Customize, "File Explorer",
            "Opening items: Single-click, underlined on hover",
            "Double-click or single-click to open files and folders.",
            s => Verdict.Opt(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShellState", 4, 0x20, false, false),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"IconUnderline", 2, RegistryValueKind.DWord, 3)),
            detail: "Double-click or single-click to open files and folders. About this setting: Choose whether to open files and folders with a single click (like web links) or double-click (traditional).", restart: true));

        yield return Mk("cz.explorer-customization-show-recent-files", Category.Customize, "File Explorer",
            "Hide recent files in File Explorer",
            "Quick Access stops listing recently used files.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShowRecent", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShowRecommendations", 0, RegistryValueKind.DWord, 1)),
            detail: "Quick Access stops listing recently used files. About this setting: Displays recently accessed files and recommendations in Quick Access.");

        yield return Mk("cz.explorer-customization-show-frequent-folders", Category.Customize, "File Explorer",
            "Hide frequent folders in File Explorer",
            "Quick Access stops listing your most used folders.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShowFrequent", 0, RegistryValueKind.DWord, 1)),
            detail: "Quick Access stops listing your most used folders. About this setting: Displays your most accessed folders in Quick Access section.");

        yield return Mk("cz.explorer-customization-show-office-files", Category.Customize, "File Explorer",
            "Hide Office.com files in File Explorer",
            "Quick Access stops showing cloud files from your Office account.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShowCloudFilesInQuickAccess", 0, RegistryValueKind.DWord, null)),
            detail: "Quick Access stops showing cloud files from your Office account. About this setting: Displays cloud files from your Office.com account in Quick Access.");

        yield return Mk("cz.explorer-customization-show-thumbnails", Category.Customize, "File Explorer",
            "Always show icons, never thumbnails",
            "File Explorer shows type icons instead of picture previews.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"IconsOnly", 1, RegistryValueKind.DWord, 0)),
            detail: "File Explorer shows type icons instead of picture previews. About this setting: Displays generic file icons instead of image/document previews.");

        yield return Mk("cz.explorer-customization-show-menus", Category.Customize, "File Explorer",
            "Always show menus",
            "Shows the File / Edit menu bar in windows that have one.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"AlwaysShowMenus", 1, RegistryValueKind.DWord, 0)),
            detail: "Shows the File / Edit menu bar in windows that have one. About this setting: Shows the Menu bar (File, Edit etc.) on all windows that support it.");

        yield return Mk("cz.explorer-customization-item-space", Category.Customize, "File Explorer",
            "Use compact view in File Explorer",
            "Less space between files and folders, so more fit on screen.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"UseCompactMode", 1, RegistryValueKind.DWord, 0)),
            detail: "Less space between files and folders, so more fit on screen. About this setting: Reduces vertical spacing between files and folders for denser view.");

        yield return Mk("cz.explorer-customization-icon-thumbnails", Category.Customize, "File Explorer",
            "Show the file icon on thumbnails",
            "A small type icon in the corner of each picture preview.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ShowTypeOverlay", 1, RegistryValueKind.DWord, 0)),
            detail: "A small type icon in the corner of each picture preview. About this setting: Shows file type icon overlay on bottom-right corner of thumbnail previews.");

        yield return Choice("Icon cache size", "Windows default", Mk("cz.explorer-customization-icon-cache-size.windows-default", Category.Customize, "File Explorer",
            "Icon cache size: Windows default",
            "How many icons File Explorer keeps ready, so big folders load faster.",
            s => Verdict.Opt(),
            L(new RegistryDeleteAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", @"MaxCachedIcons", RegistryValueKind.String, null)),
            detail: "How many icons File Explorer keeps ready, so big folders load faster. About this setting: Sets the maximum number of icons Explorer keeps cached, so it reloads them from disk less often when browsing folders with many files.", restart: true));

        yield return Choice("Icon cache size", "Large (4,096 icons)", Mk("cz.explorer-customization-icon-cache-size.large-4-096-icons", Category.Customize, "File Explorer",
            "Icon cache size: Large (4,096 icons)",
            "How many icons File Explorer keeps ready, so big folders load faster.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", @"MaxCachedIcons", @"4096", RegistryValueKind.String, null)),
            detail: "How many icons File Explorer keeps ready, so big folders load faster. About this setting: Sets the maximum number of icons Explorer keeps cached, so it reloads them from disk less often when browsing folders with many files.", restart: true));

        yield return Choice("Icon cache size", "Very large (8,192 icons)", Mk("cz.explorer-customization-icon-cache-size.very-large-8-192-icons", Category.Customize, "File Explorer",
            "Icon cache size: Very large (8,192 icons)",
            "How many icons File Explorer keeps ready, so big folders load faster.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", @"MaxCachedIcons", @"8192", RegistryValueKind.String, null)),
            detail: "How many icons File Explorer keeps ready, so big folders load faster. About this setting: Sets the maximum number of icons Explorer keeps cached, so it reloads them from disk less often when browsing folders with many files.", restart: true));

        yield return Mk("cz.explorer-customization-thumbnail-cache-cleanup", Category.Customize, "File Explorer",
            "Keep the thumbnail cache",
            "Windows maintenance stops deleting cached thumbnails, so folders don't rebuild them.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache", @"Autorun", 0, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache", @"Autorun", 0, RegistryValueKind.DWord, 3)),
            detail: "Windows maintenance stops deleting cached thumbnails, so folders don't rebuild them. About this setting: Lets Windows clear the thumbnail cache during automatic disk maintenance. Turn this off to keep cached thumbnails so Explorer does not have to regenerate them when you reopen folders.");

        yield return Mk("cz.explorer-customization-folder-tips", Category.Customize, "File Explorer",
            "Hide folder size in folder tooltips",
            "Hovering a folder no longer shows its size and file count.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"FolderContentsInfoTip", 0, RegistryValueKind.DWord, null)),
            detail: "Hovering a folder no longer shows its size and file count. About this setting: Shows total size and file count when hovering over folders.");

        yield return Mk("cz.explorer-customization-full-path", Category.Customize, "File Explorer",
            "Don't show the full path in the title bar",
            "Explorer windows show only the folder name in the title bar.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState", @"FullPath", 0, RegistryValueKind.DWord, 1)),
            detail: "Explorer windows show only the folder name in the title bar. About this setting: Shows complete directory path in window title instead of folder name only.");

        yield return Mk("cz.explorer-customization-hide-empty-drives", Category.Customize, "File Explorer",
            "Show empty drives",
            "Card readers and disc drives with nothing in them stay visible.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"HideDrivesWithNoMedia", 0, RegistryValueKind.DWord, null)),
            detail: "Card readers and disc drives with nothing in them stay visible. About this setting: Hides drives with no media inserted like empty card readers or optical drives.");

        yield return Mk("cz.explorer-customization-show-lnk-extension", Category.Customize, "File Explorer",
            "Show the .lnk extension on shortcuts",
            "Makes shortcuts disguised as folders or documents easy to spot (with file extensions shown).",
            s => Verdict.Opt(),
            L(new RegistryDeleteAction(RegistryHive.ClassesRoot, @"lnkfile", @"NeverShowExt", RegistryValueKind.String, @"")),
            detail: "Makes shortcuts disguised as folders or documents easy to spot (with file extensions shown). About this setting: Shows the .lnk extension on shortcut files when file extensions are enabled. Helps spot malicious shortcuts disguised as folders or documents. Once enabled, some Start Menu names may keep showing .lnk even after turning this off.", restart: true);

        yield return Mk("cz.explorer-customization-hide-merge-conflicts", Category.Customize, "File Explorer",
            "Ask before merging folders with the same name",
            "Copying a folder onto one with the same name asks first instead of merging straight away.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"HideMergeConflicts", 0, RegistryValueKind.DWord, null)),
            detail: "Copying a folder onto one with the same name asks first instead of merging straight away. About this setting: Automatically merges folders with same name without confirmation dialog.");

        yield return Mk("cz.explorer-customization-hide-protected-files", Category.Customize, "File Explorer",
            "Show protected Windows system files",
            "Shows files Windows hides to protect itself. Don't delete them.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ShowSuperHidden", 1, RegistryValueKind.DWord, 0)),
            detail: "Shows files Windows hides to protect itself. Don't delete them. About this setting: Displays system files marked with the SuperHidden attribute.");

        yield return Mk("cz.explorer-customization-separate-process", Category.Customize, "File Explorer",
            "Open folder windows in a separate process",
            "One crashed Explorer window can't close the others.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"SeparateProcess", 1, RegistryValueKind.DWord, 0)),
            detail: "One crashed Explorer window can't close the others. About this setting: Runs each Explorer window in its own process to prevent crashes affecting all windows.");

        yield return Mk("cz.explorer-customization-persist-browsers", Category.Customize, "File Explorer",
            "Reopen folder windows when you sign in",
            "Explorer windows open at shutdown come back.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"PersistBrowsers", 1, RegistryValueKind.DWord, 0)),
            detail: "Explorer windows open at shutdown come back. About this setting: Reopens Explorer windows that were open when you last shut down or logged off.");

        yield return Mk("cz.explorer-customization-show-drive-letters", Category.Customize, "File Explorer",
            "Hide drive letters",
            "Drives show their names without C:, D: and so on.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ShowDriveLettersFirst", 2, RegistryValueKind.DWord, null)),
            detail: "Drives show their names without C:, D: and so on. About this setting: Displays drive letters (C:, D:) before drive names in This PC.");

        yield return Mk("cz.explorer-customization-compressed-color", Category.Customize, "File Explorer",
            "Don't colour encrypted or compressed files",
            "Encrypted (green) and compressed (blue) files use normal colours.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ShowEncryptCompressedColor", 0, RegistryValueKind.DWord, 1)),
            detail: "Encrypted (green) and compressed (blue) files use normal colours. About this setting: Displays encrypted files in green and compressed files in blue.");

        yield return Mk("cz.explorer-customization-popup-descriptions", Category.Customize, "File Explorer",
            "Turn off pop-up descriptions",
            "Hovering files and folders no longer shows a details tooltip.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ShowInfoTip", 0, RegistryValueKind.DWord, 1)),
            detail: "Hovering files and folders no longer shows a details tooltip. About this setting: Displays tooltip with item details when hovering over files and folders.");

        yield return Mk("cz.explorer-customization-preview-handlers", Category.Customize, "File Explorer",
            "Turn off file previews in the preview pane",
            "The preview pane stops opening file contents.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ShowPreviewHandlers", 0, RegistryValueKind.DWord, null)),
            detail: "The preview pane stops opening file contents. About this setting: Enables file content preview when selecting files in Explorer.");

        yield return Mk("cz.explorer-customization-status-bar", Category.Customize, "File Explorer",
            "Hide the status bar in File Explorer",
            "Removes the item count and size bar at the bottom.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ShowStatusBar", 0, RegistryValueKind.DWord, 1)),
            detail: "Removes the item count and size bar at the bottom. About this setting: Displays bar at bottom showing item count and selected file sizes.");

        yield return Mk("cz.explorer-customization-checkbox-select", Category.Customize, "File Explorer",
            "Use check boxes to select files",
            "Adds tick boxes for selecting several items.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"AutoCheckSelect", 1, RegistryValueKind.DWord, 0)),
            detail: "Adds tick boxes for selecting several items. About this setting: Adds checkboxes next to items for easier multi-selection.");

        yield return Mk("cz.explorer-customization-sharing-wizard", Category.Customize, "File Explorer",
            "Use advanced sharing settings",
            "Sharing opens the full permissions dialog instead of the simple wizard.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"SharingWizardOn", 0, RegistryValueKind.DWord, null)),
            detail: "Sharing opens the full permissions dialog instead of the simple wizard. About this setting: Shows simplified sharing dialog instead of advanced security permissions.");

        yield return Choice("Typing in a folder", "Jump to the matching item (default)", Mk("cz.explorer-customization-typing-behavior.jump-to-the-matching-item-default", Category.Customize, "File Explorer",
            "Typing in a folder: Jump to the matching item (default)",
            "What happens when you start typing in a File Explorer folder.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TypeAhead", 0, RegistryValueKind.DWord, 0)),
            detail: "What happens when you start typing in a File Explorer folder. About this setting: Chooses whether typing selects matching items or searches automatically."));

        yield return Choice("Typing in a folder", "Type into the search box", Mk("cz.explorer-customization-typing-behavior.type-into-the-search-box", Category.Customize, "File Explorer",
            "Typing in a folder: Type into the search box",
            "What happens when you start typing in a File Explorer folder.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TypeAhead", 1, RegistryValueKind.DWord, 0)),
            detail: "What happens when you start typing in a File Explorer folder. About this setting: Chooses whether typing selects matching items or searches automatically."));

        yield return Mk("cz.explorer-customization-thispc-folder-desktop-win10", Category.Customize, "This PC folders",
            "Hide Desktop from This PC",
            "Removes the Desktop folder from This PC.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryKeyAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", false),
              new RegistryKeyAction(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", false)),
            detail: "Removes the Desktop folder from This PC. About this setting: Displays the Desktop folder under This PC in File Explorer.", restart: true);

        yield return Mk("cz.explorer-customization-thispc-folder-documents-win10", Category.Customize, "This PC folders",
            "Hide Documents from This PC",
            "Removes the Documents folder from This PC.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryKeyAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{D3162B92-9365-467A-956B-92703ACA08AF}", false),
              new RegistryKeyAction(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{D3162B92-9365-467A-956B-92703ACA08AF}", false)),
            detail: "Removes the Documents folder from This PC. About this setting: Displays the Documents folder under This PC in File Explorer.", restart: true);

        yield return Mk("cz.explorer-customization-thispc-folder-downloads-win10", Category.Customize, "This PC folders",
            "Hide Downloads from This PC",
            "Removes the Downloads folder from This PC.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryKeyAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088E3905-0323-4B02-9826-5D99428E115F}", false),
              new RegistryKeyAction(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088E3905-0323-4B02-9826-5D99428E115F}", false)),
            detail: "Removes the Downloads folder from This PC. About this setting: Displays the Downloads folder under This PC in File Explorer.", restart: true);

        yield return Mk("cz.explorer-customization-thispc-folder-music-win10", Category.Customize, "This PC folders",
            "Hide Music from This PC",
            "Removes the Music folder from This PC.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryKeyAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3DFDF296-DBEC-4FB4-81D1-6A3438BCF4DE}", false),
              new RegistryKeyAction(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3DFDF296-DBEC-4FB4-81D1-6A3438BCF4DE}", false)),
            detail: "Removes the Music folder from This PC. About this setting: Displays the Music folder under This PC in File Explorer.", restart: true);

        yield return Mk("cz.explorer-customization-thispc-folder-pictures-win10", Category.Customize, "This PC folders",
            "Hide Pictures from This PC",
            "Removes the Pictures folder from This PC.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryKeyAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24AD3AD4-A569-4530-98E1-AB02F9417AA8}", false),
              new RegistryKeyAction(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24AD3AD4-A569-4530-98E1-AB02F9417AA8}", false)),
            detail: "Removes the Pictures folder from This PC. About this setting: Displays the Pictures folder under This PC in File Explorer.", restart: true);

        yield return Mk("cz.explorer-customization-thispc-folder-videos-win10", Category.Customize, "This PC folders",
            "Hide Videos from This PC",
            "Removes the Videos folder from This PC.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryKeyAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{F86FA3AB-70D2-4FC7-9C99-FCBF05467F3A}", false),
              new RegistryKeyAction(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{F86FA3AB-70D2-4FC7-9C99-FCBF05467F3A}", false)),
            detail: "Removes the Videos folder from This PC. About this setting: Displays the Videos folder under This PC in File Explorer.", restart: true);

        yield return Mk("cz.explorer-customization-3d-objects", Category.Customize, "This PC folders",
            "Hide 3D Objects from This PC",
            "Removes the 3D Objects folder.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryKeyAction(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}", false),
              new RegistryKeyAction(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}", false)),
            detail: "Removes the 3D Objects folder. About this setting: Display the 3D Objects folder alongside Documents, Pictures, and other default folders.");

        yield return Mk("cz.explorer-customization-show-availability-status", Category.Customize, "Navigation pane",
            "Always show OneDrive sync status",
            "Shows cloud status icons for every file in the navigation pane.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"NavPaneShowAllCloudStates", 1, RegistryValueKind.DWord, 0)),
            detail: "Shows cloud status icons for every file in the navigation pane. About this setting: Shows cloud sync status icons for OneDrive files in navigation pane.");

        yield return Mk("cz.explorer-customization-nav-expand-current", Category.Customize, "Navigation pane",
            "Don't expand to the open folder",
            "The navigation pane stops following the folder you're in.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"NavPaneExpandToCurrentFolder", 0, RegistryValueKind.DWord, 1)),
            detail: "The navigation pane stops following the folder you're in. About this setting: Automatically expands navigation tree to highlight current folder location.");

        yield return Mk("cz.explorer-customization-nav-show-all-folders", Category.Customize, "Navigation pane",
            "Show all folders in the navigation pane",
            "Shows every folder, including Control Panel and Recycle Bin.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"NavPaneShowAllFolders", 1, RegistryValueKind.DWord, 0)),
            detail: "Shows every folder, including Control Panel and Recycle Bin. About this setting: Shows all folders in the navigation pane.", restart: true);

        yield return Mk("cz.explorer-customization-nav-saf-desktop", Category.Customize, "Navigation pane",
            "Hide Desktop from the navigation pane",
            "Removes the Desktop shortcut on the left of File Explorer.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\NonEnum", @"{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", @"HiddenByDefault", 1, RegistryValueKind.DWord, null)),
            detail: "Removes the Desktop shortcut on the left of File Explorer. About this setting: Shows the Desktop folder in the navigation pane.");

        yield return Mk("cz.explorer-customization-nav-saf-documents", Category.Customize, "Navigation pane",
            "Hide Documents from the navigation pane",
            "Removes the Documents shortcut on the left of File Explorer.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\NonEnum", @"{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}", @"HiddenByDefault", 1, RegistryValueKind.DWord, null)),
            detail: "Removes the Documents shortcut on the left of File Explorer. About this setting: Shows the Documents folder in the navigation pane.");

        yield return Mk("cz.explorer-customization-nav-saf-downloads", Category.Customize, "Navigation pane",
            "Hide Downloads from the navigation pane",
            "Removes the Downloads shortcut on the left of File Explorer.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\NonEnum", @"{374DE290-123F-4565-9164-39C4925E467B}", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{374DE290-123F-4565-9164-39C4925E467B}", @"HiddenByDefault", 1, RegistryValueKind.DWord, null)),
            detail: "Removes the Downloads shortcut on the left of File Explorer. About this setting: Shows the Downloads folder in the navigation pane.");

        yield return Mk("cz.explorer-customization-nav-saf-music", Category.Customize, "Navigation pane",
            "Hide Music from the navigation pane",
            "Removes the Music shortcut on the left of File Explorer.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\NonEnum", @"{1CF1260C-4DD0-4ebb-811F-33C572699FDE}", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{1CF1260C-4DD0-4ebb-811F-33C572699FDE}", @"HiddenByDefault", 1, RegistryValueKind.DWord, null)),
            detail: "Removes the Music shortcut on the left of File Explorer. About this setting: Shows the Music folder in the navigation pane.");

        yield return Mk("cz.explorer-customization-nav-saf-pictures", Category.Customize, "Navigation pane",
            "Hide Pictures from the navigation pane",
            "Removes the Pictures shortcut on the left of File Explorer.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\NonEnum", @"{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}", @"HiddenByDefault", 1, RegistryValueKind.DWord, null)),
            detail: "Removes the Pictures shortcut on the left of File Explorer. About this setting: Shows the Pictures folder in the navigation pane.");

        yield return Mk("cz.explorer-customization-nav-saf-videos", Category.Customize, "Navigation pane",
            "Hide Videos from the navigation pane",
            "Removes the Videos shortcut on the left of File Explorer.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\NonEnum", @"{A0953C92-50DC-43bf-BE83-3742FED03C9C}", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{A0953C92-50DC-43bf-BE83-3742FED03C9C}", @"HiddenByDefault", 1, RegistryValueKind.DWord, null)),
            detail: "Removes the Videos shortcut on the left of File Explorer. About this setting: Shows the Videos folder in the navigation pane.");

        yield return Mk("cz.explorer-customization-nav-show-libraries", Category.Customize, "Navigation pane",
            "Show Libraries in the navigation pane",
            "Pins Libraries on the left of File Explorer.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{031E4825-7B94-4dc3-B131-E946B44C8DD5}", @"System.IsPinnedToNameSpaceTree", 1, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\NonEnum", @"{031E4825-7B94-4dc3-B131-E946B44C8DD5}", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{031E4825-7B94-4dc3-B131-E946B44C8DD5}", @"HiddenByDefault", 0, RegistryValueKind.DWord, 1)),
            detail: "Pins Libraries on the left of File Explorer. About this setting: Pins the Libraries folder as a top-level item in the navigation pane. Has no effect when Show All Folders is enabled, as Libraries becomes part of the folder tree instead.", restart: true);

        yield return Mk("cz.explorer-customization-netplwiz-auto-login", Category.Customize, "Network",
            "Show the automatic sign-in option in User Accounts",
            "Brings back the \"Users must enter a user name and password\" tick box in netplwiz.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device", @"DevicePasswordLessBuildVersion", 0, RegistryValueKind.DWord, 2)),
            detail: "Brings back the \"Users must enter a user name and password\" tick box in netplwiz. About this setting: Shows the classic 'Users must enter a user name and password to use this computer' checkbox in the User Accounts (netplwiz) window, allowing you to configure automatic logon through the standard Windows UI.");

        yield return Mk("cz.explorer-autoplay", Category.Customize, "Devices",
            "Turn off AutoPlay",
            "Nothing opens or runs by itself when you plug in a USB drive, disc or SD card.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers", @"DisableAutoplay", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", @"NoDriveTypeAutoRun", 255, RegistryValueKind.DWord, null)),
            detail: "Nothing opens or runs by itself when you plug in a USB drive, disc or SD card. About this setting: Allow Windows to automatically open a dialog or run programs when you insert a USB drive, DVD, or SD card.");

        yield return Mk("cz.taskbar-auto-hide", Category.Customize, "Taskbar & clock",
            "Automatically hide the taskbar",
            "The taskbar slides away until you point at the bottom of the screen.",
            s => Verdict.Opt(),
            L(new RegistryBitAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3", @"Settings", 8, 0x01, true, false)),
            detail: "The taskbar slides away until you point at the bottom of the screen. About this setting: Automatically hides the taskbar when not in use. Hover at the bottom of the screen to reveal it.", restart: true);

        yield return Choice("Taskbar preview delay", "Instant", Mk("cz.taskbar-extended-hover-time.instant", Category.Customize, "Taskbar & clock",
            "Taskbar preview delay: Instant",
            "How long you hover a taskbar button before its preview appears.",
            s => Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ExtendedUIHoverTime", 1, RegistryValueKind.DWord, 400)),
            detail: "How long you hover a taskbar button before its preview appears. About this setting: Controls how long you must hover at the screen edge before the auto-hidden taskbar appears (in milliseconds). Lower values make the taskbar appear faster when using auto-hide. Default is 400ms.", restart: true));

        yield return Choice("Taskbar preview delay", "10 ms", Mk("cz.taskbar-extended-hover-time.10-ms", Category.Customize, "Taskbar & clock",
            "Taskbar preview delay: 10 ms",
            "How long you hover a taskbar button before its preview appears.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ExtendedUIHoverTime", 10, RegistryValueKind.DWord, 400)),
            detail: "How long you hover a taskbar button before its preview appears. About this setting: Controls how long you must hover at the screen edge before the auto-hidden taskbar appears (in milliseconds). Lower values make the taskbar appear faster when using auto-hide. Default is 400ms.", restart: true));

        yield return Choice("Taskbar preview delay", "50 ms", Mk("cz.taskbar-extended-hover-time.50-ms", Category.Customize, "Taskbar & clock",
            "Taskbar preview delay: 50 ms",
            "How long you hover a taskbar button before its preview appears.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ExtendedUIHoverTime", 50, RegistryValueKind.DWord, 400)),
            detail: "How long you hover a taskbar button before its preview appears. About this setting: Controls how long you must hover at the screen edge before the auto-hidden taskbar appears (in milliseconds). Lower values make the taskbar appear faster when using auto-hide. Default is 400ms.", restart: true));

        yield return Choice("Taskbar preview delay", "100 ms", Mk("cz.taskbar-extended-hover-time.100-ms", Category.Customize, "Taskbar & clock",
            "Taskbar preview delay: 100 ms",
            "How long you hover a taskbar button before its preview appears.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ExtendedUIHoverTime", 100, RegistryValueKind.DWord, 400)),
            detail: "How long you hover a taskbar button before its preview appears. About this setting: Controls how long you must hover at the screen edge before the auto-hidden taskbar appears (in milliseconds). Lower values make the taskbar appear faster when using auto-hide. Default is 400ms.", restart: true));

        yield return Choice("Taskbar preview delay", "200 ms", Mk("cz.taskbar-extended-hover-time.200-ms", Category.Customize, "Taskbar & clock",
            "Taskbar preview delay: 200 ms",
            "How long you hover a taskbar button before its preview appears.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ExtendedUIHoverTime", 200, RegistryValueKind.DWord, 400)),
            detail: "How long you hover a taskbar button before its preview appears. About this setting: Controls how long you must hover at the screen edge before the auto-hidden taskbar appears (in milliseconds). Lower values make the taskbar appear faster when using auto-hide. Default is 400ms.", restart: true));

        yield return Choice("Taskbar preview delay", "400 ms (default)", Mk("cz.taskbar-extended-hover-time.400-ms-default", Category.Customize, "Taskbar & clock",
            "Taskbar preview delay: 400 ms (default)",
            "How long you hover a taskbar button before its preview appears.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ExtendedUIHoverTime", 400, RegistryValueKind.DWord, 400)),
            detail: "How long you hover a taskbar button before its preview appears. About this setting: Controls how long you must hover at the screen edge before the auto-hidden taskbar appears (in milliseconds). Lower values make the taskbar appear faster when using auto-hide. Default is 400ms.", restart: true));

        yield return Mk("cz.taskbar-badges", Category.Customize, "Taskbar & clock",
            "Hide badges on taskbar apps",
            "No unread counters on taskbar icons.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarBadges", 0, RegistryValueKind.DWord, 1)),
            detail: "No unread counters on taskbar icons. About this setting: Show notification badge counters on taskbar app icons to indicate unread messages or alerts.");

        yield return Mk("cz.taskbar-flashing", Category.Customize, "Taskbar & clock",
            "Stop taskbar apps flashing",
            "Taskbar icons no longer flash for attention.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarFlashing", 0, RegistryValueKind.DWord, 1)),
            detail: "Taskbar icons no longer flash for attention. About this setting: Allow taskbar app icons to flash when they require your attention.");

        yield return Mk("cz.taskbar-multi-display", Category.Customize, "Taskbar & clock",
            "Show the taskbar on one display only",
            "Other monitors no longer get a taskbar.",
            s => Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MMTaskbarEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Other monitors no longer get a taskbar. About this setting: Show the taskbar on all connected monitors when using a multi-display setup.", restart: true);

        yield return Mk("cz.taskbar-share-window", Category.Customize, "Taskbar & clock",
            "Turn off \"Share this window\" on the taskbar",
            "Removes the share button that appears on taskbar previews during calls.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarSn", 0, RegistryValueKind.DWord, 1)),
            detail: "Removes the share button that appears on taskbar previews during calls. About this setting: Enable sharing any open window directly from the taskbar during a call.");

        yield return Mk("cz.taskbar-show-desktop", Category.Customize, "Taskbar & clock",
            "Turn off Show desktop in the taskbar corner",
            "Clicking the far corner no longer minimises everything.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarSd", 0, RegistryValueKind.DWord, 1)),
            detail: "Clicking the far corner no longer minimises everything. About this setting: Click the far corner of the taskbar to quickly show the desktop by minimizing all open windows.");

        yield return Choice("Taskbar button size", "Always small", Mk("cz.taskbar-button-size.always-small", Category.Customize, "Taskbar & clock",
            "Taskbar button size: Always small",
            "When the taskbar uses smaller buttons.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : s.Build < 26100 ? Verdict.No("Needs a newer version of Windows.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"IconSizePreference", 0, RegistryValueKind.DWord, 2)),
            detail: "When the taskbar uses smaller buttons. About this setting: Control the size of taskbar buttons. This setting may not persist on all Windows 11 builds.", restart: true));

        yield return Choice("Taskbar button size", "Small when full (default)", Mk("cz.taskbar-button-size.small-when-full-default", Category.Customize, "Taskbar & clock",
            "Taskbar button size: Small when full (default)",
            "When the taskbar uses smaller buttons.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : s.Build < 26100 ? Verdict.No("Needs a newer version of Windows.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"IconSizePreference", 2, RegistryValueKind.DWord, 2)),
            detail: "When the taskbar uses smaller buttons. About this setting: Control the size of taskbar buttons. This setting may not persist on all Windows 11 builds.", restart: true));

        yield return Choice("Taskbar button size", "Never small", Mk("cz.taskbar-button-size.never-small", Category.Customize, "Taskbar & clock",
            "Taskbar button size: Never small",
            "When the taskbar uses smaller buttons.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : s.Build < 26100 ? Verdict.No("Needs a newer version of Windows.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"IconSizePreference", 1, RegistryValueKind.DWord, 2)),
            detail: "When the taskbar uses smaller buttons. About this setting: Control the size of taskbar buttons. This setting may not persist on all Windows 11 builds.", restart: true));

        yield return Mk("cz.taskbar-system-tray-icons", Category.Customize, "Taskbar & clock",
            "Always show all system tray icons",
            "Every tray icon sits on the taskbar instead of the overflow menu.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"EnableAutoTray", 0, RegistryValueKind.DWord, 1)),
            detail: "Every tray icon sits on the taskbar instead of the overflow menu. About this setting: Show all system tray icons directly on the taskbar instead of hiding them in the overflow menu. To control individual icon visibility, go to Taskbar Settings and select which icons appear on the taskbar.", restart: true);

        yield return Mk("cz.taskbar-copilot-companion", Category.Customize, "Taskbar & clock",
            "Hide the Copilot companion button",
            "Removes the newer Copilot button from the taskbar.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarCompanion", 0, RegistryValueKind.DWord, 1)),
            detail: "Removes the newer Copilot button from the taskbar. About this setting: Show or hide the newer Copilot companion button on the taskbar.", restart: true);

        yield return Mk("cz.taskbar-copilot-pwa-pin", Category.Customize, "Taskbar & clock",
            "Unpin the Copilot app from the taskbar",
            "Removes the Copilot web app pin.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"CopilotPWAPin", 0, RegistryValueKind.DWord, 1)),
            detail: "Removes the Copilot web app pin. About this setting: Show or hide the Copilot PWA pin on the taskbar.", restart: true);

        yield return Mk("cz.taskbar-recall-pin", Category.Customize, "Taskbar & clock",
            "Unpin Recall from the taskbar",
            "Removes the Recall pin.",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"RecallPin", 0, RegistryValueKind.DWord, 1)),
            detail: "Removes the Recall pin. About this setting: Show or hide the Recall pin on the taskbar.", restart: true);

        yield return Mk("cz.taskbar-widgets", Category.Customize, "Taskbar & clock",
            "Hide the Widgets button",
            "Removes news, weather and widgets from the taskbar (by policy).",
            s => !s.IsWindows11 ? Verdict.No("This setting only exists on Windows 11.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Dsh", @"AllowNewsAndInterests", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Dsh", @"AllowNewsAndInterests", 0, RegistryValueKind.DWord, 1)),
            detail: "Removes news, weather and widgets from the taskbar (by policy). About this setting: Show the Widgets button that displays personalized news, weather, calendar, and other information.", restart: true);

        yield return Mk("cz.taskbar-news-and-interests", Category.Customize, "Taskbar & clock",
            "Hide News and Interests",
            "Removes the news and weather widget from the taskbar.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Windows Feeds", @"EnableFeeds", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\Windows Feeds", @"EnableFeeds", 0, RegistryValueKind.DWord, 1)),
            detail: "Removes the news and weather widget from the taskbar. About this setting: Show the News and Interests widget that displays headlines, weather, stocks, and other personalized content.", restart: true);

        yield return Choice("Taskbar transparency", "Windows default", Mk("cz.taskbar-transparent.windows-default", Category.Customize, "Taskbar & clock",
            "Taskbar transparency: Windows default",
            "How see-through the taskbar is.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Opt(),
            L(new RegistryDeleteAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarAcrylicOpacity", RegistryValueKind.DWord, null)),
            detail: "How see-through the taskbar is. About this setting: Controls the transparency level of the taskbar. Transparency effects need to be on in Windows for this to show.", restart: true));

        yield return Choice("Taskbar transparency", "Transparent", Mk("cz.taskbar-transparent.transparent", Category.Customize, "Taskbar & clock",
            "Taskbar transparency: Transparent",
            "How see-through the taskbar is.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarAcrylicOpacity", 0, RegistryValueKind.DWord, null)),
            detail: "How see-through the taskbar is. About this setting: Controls the transparency level of the taskbar. Transparency effects need to be on in Windows for this to show.", restart: true));

        yield return Choice("Taskbar transparency", "Solid", Mk("cz.taskbar-transparent.solid", Category.Customize, "Taskbar & clock",
            "Taskbar transparency: Solid",
            "How see-through the taskbar is.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Opt(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarAcrylicOpacity", 255, RegistryValueKind.DWord, null)),
            detail: "How see-through the taskbar is. About this setting: Controls the transparency level of the taskbar. Transparency effects need to be on in Windows for this to show.", restart: true));

        yield return Mk("cz.taskbar-small", Category.Customize, "Taskbar & clock",
            "Use small taskbar buttons",
            "A thinner taskbar with smaller icons.",
            s => s.IsWindows11 ? Verdict.No("Only on Windows 10.") : Verdict.Rec(),
            L(new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarSmallIcons", 1, RegistryValueKind.DWord, 0)),
            detail: "A thinner taskbar with smaller icons. About this setting: Reduce the height of the taskbar by using smaller icons, giving you more screen space.", restart: true);

    }
}
