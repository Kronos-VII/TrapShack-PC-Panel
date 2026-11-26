using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

/// <summary>
/// Customization page: the look-and-feel switches (common Windows UI preferences). Every card is a preference, so only the clearly harmless ones count as "recommended".
/// Each switch is the "changed" state; switching it off restores what the PC had before.
/// </summary>
public static partial class Catalog
{
    const string Appearance = "Appearance";
    const string TaskbarGroup = "Taskbar & clock";
    const string ExplorerGroup = "File Explorer";
    const string InputGroup = "Mouse & keyboard";
    const string BehaviourGroup = "Windows behaviour";

    const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    static IEnumerable<Tweak> Customize()
    {
        // ---------------------------------------------------------------- Appearance
        yield return Mk("custom.dark-theme", Category.Customize, Appearance,
            "Dark theme for Windows and apps",
            "Switches Windows, Settings and supporting apps to dark mode.",
            Sometimes,
            L(CU(Personalize, "AppsUseLightTheme", 0, 1), CU(Personalize, "SystemUsesLightTheme", 0, 1)),
            detail: "Sets both the app mode and the Windows mode to dark. Switch off to go back to light.");

        yield return Mk("custom.classic-menu", Category.Customize, Appearance,
            "Classic right-click menu",
            "Brings back the full Windows 10 style context menu instead of \"Show more options\".",
            s => W11(s, Verdict.Opt()),
            L(new CustomAction(
                "Create the empty InprocServer32 key under HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2} (restore deletes it)",
                isApplied: () => RegUtil.KeyExists(RegistryHive.CurrentUser, ClassicMenuKey + @"\InprocServer32"),
                apply: () =>
                {
                    using var b = RegUtil.Base(RegistryHive.CurrentUser);
                    using var k = b.CreateSubKey(ClassicMenuKey + @"\InprocServer32", true);
                    k.SetValue("", "", RegistryValueKind.String);
                },
                revert: _ =>
                {
                    using var b = RegUtil.Base(RegistryHive.CurrentUser);
                    b.DeleteSubKeyTree(ClassicMenuKey, false);
                })),
            detail: "The well-known per-user switch for the old menu. Switching it off deletes that key again, so Windows 11's new menu returns.",
            signOut: true);

        // ---------------------------------------------------------------- Taskbar
        yield return Mk("custom.taskbar-left", Category.Customize, TaskbarGroup,
            "Align the taskbar to the left",
            "Start button and icons sit on the left like Windows 10.",
            s => W11(s, Verdict.Rec()),
            L(CU(Adv, "TaskbarAl", 0, 1)),
            detail: "Off = centred (the Windows 11 default).");

        yield return Mk("custom.taskbar-search", Category.Customize, TaskbarGroup,
            "Hide the search box on the taskbar",
            "Search is still one keypress away (Win + S).",
            Sometimes,
            L(CU(@"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0, 2)),
            detail: "Hides the search box or button. Switch off to bring it back as a search box.");

        yield return Mk("custom.taskview", Category.Customize, TaskbarGroup,
            "Hide the Task View button",
            "Win + Tab still works.",
            Sometimes,
            L(CU(Adv, "ShowTaskViewButton", 0, 1)),
            detail: "Only hides the button.");

        // ---------------------------------------------------------------- File Explorer
        yield return Mk("custom.file-ext", Category.Customize, ExplorerGroup,
            "Show file extensions",
            "See .exe, .pdf, .jpg. Helps you spot disguised files.",
            Always,
            L(CU(Adv, "HideFileExt", 0, 1)),
            detail: "Security-minded and used by every tech guide: it stops \"invoice.pdf.exe\" passing as a document.");

        yield return Mk("custom.hidden-files", Category.Customize, ExplorerGroup,
            "Show hidden files and folders",
            "Hidden items appear (faded) in File Explorer.",
            Sometimes,
            L(CU(Adv, "Hidden", 1, 2)),
            detail: "Protected operating system files stay hidden.");

        // ---------------------------------------------------------------- Mouse & keyboard
        yield return Mk("custom.mouse-accel", Category.Customize, InputGroup,
            "Turn off mouse acceleration",
            "Pointer moves the same distance for the same hand movement (\"Enhance pointer precision\" off).",
            Sometimes,
            L(CUs(@"Control Panel\Mouse", "MouseSpeed", "0", "1"),
              CUs(@"Control Panel\Mouse", "MouseThreshold1", "0", "6"),
              CUs(@"Control Panel\Mouse", "MouseThreshold2", "0", "10")),
            detail: "Preferred by most gamers because aim becomes predictable. Takes effect after you sign out and back in.",
            signOut: true);

        yield return Mk("custom.numlock", Category.Customize, InputGroup,
            "Turn Num Lock on at startup",
            "Numeric keypad works as soon as you reach the sign-in screen.",
            Sometimes,
            L(CUs(@"Control Panel\Keyboard", "InitialKeyboardIndicators", "2", "0"),
              new RegistryAction(RegistryHive.Users, @".DEFAULT\Control Panel\Keyboard", "InitialKeyboardIndicators",
                  "2", RegistryValueKind.String, "2147483648")),
            detail: "Sets the keyboard start state for your account and for the sign-in screen. Fast Startup can override it on some PCs.");

        yield return Mk("custom.sticky-keys", Category.Customize, InputGroup,
            "Stop the Sticky Keys pop-up",
            "Pressing Shift five times no longer interrupts games.",
            Always,
            // 506 = Windows' 510 with only the shortcut bit cleared, so Sticky Keys itself keeps its settings
            L(new RegistryAction(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "506", RegistryValueKind.String, "510",
                // "on" means the shortcut bit (4) is clear, whatever the other Sticky Keys options are
                matcher: v => v is string f && int.TryParse(f, out var n) && (n & 4) == 0)),
            detail: "Turns off the shortcut only. If you rely on Sticky Keys, leave this off; you can still enable it in Settings > Accessibility.");

        yield return Mk("custom.snap-window", Category.Customize, InputGroup,
            "Turn off window snapping",
            "Dragging a window to the screen edge no longer resizes it.",
            Sometimes,
            L(CUs(@"Control Panel\Desktop", "WindowArrangementActive", "0", "1")),
            detail: "Disables snap entirely. Use the next two cards instead if you only dislike the pop-ups.",
            signOut: true);

        // ---------------------------------------------------------------- Behaviour
        yield return Mk("custom.detailed-bsod", Category.Customize, BehaviourGroup,
            "Detailed blue screens",
            "Blue screens show the stop code and technical details instead of the smiley face.",
            Always,
            L(LM(@"SYSTEM\CurrentControlSet\Control\CrashControl", "DisplayParameters", 1, 0),
              LM(@"SYSTEM\CurrentControlSet\Control\CrashControl", "DisableEmoticon", 1, 0)),
            detail: "Makes crashes much easier to diagnose. It does not change how often they happen.");

        yield return Mk("custom.verbose-logon", Category.Customize, BehaviourGroup,
            "Verbose messages during sign-in",
            "Shows what Windows is doing while it starts and signs you in.",
            Sometimes,
            L(LM(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", 1, 0)),
            detail: "Useful for spotting what makes sign-in slow.");

        yield return Mk("custom.startup-sound", Category.Customize, BehaviourGroup,
            "Turn off the Windows startup sound",
            "Silent boot.",
            Sometimes,
            L(LM(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\BootAnimation", "DisableStartupSound", 1, 0),
              LM(@"SOFTWARE\Microsoft\Windows\CurrentVersion\EditionOverrides", "UserSetting_DisableStartupSound", 1, 0)),
            detail: "Takes effect at the next restart.",
            restart: true);
    }

    const string ClassicMenuKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
}
