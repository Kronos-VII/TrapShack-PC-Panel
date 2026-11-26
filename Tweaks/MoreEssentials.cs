using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

/// <summary>
/// Additional essential, advanced and optimization switches. Every value was checked against published tweak lists. The restore
/// value is always what Windows ships with, and TrapShack also backs up what was on your PC before it changes anything.
/// </summary>
public static partial class Catalog
{
    const string MmcssKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";

    static IEnumerable<Tweak> MoreEssentials()
    {
        yield return Mk("essential.remote-assist", Category.Debloat, Essential,
            "Turn off Remote Assistance",
            "Blocks the old \"invite someone to control this PC\" feature.",
            s => s.DomainJoined ? Verdict.Warn("Company IT sometimes uses Remote Assistance. Ask first.") : Verdict.Opt(),
            L(LM(@"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 0, 1)),
            detail: "Same as System Properties > Remote > Allow Remote Assistance connections off. Quick Assist (the modern tool) is a separate app and is not affected.");

        yield return Mk("essential.long-paths", Category.Debloat, Essential,
            "Allow file paths longer than 260 characters",
            "Fixes \"path too long\" errors in developer tools and some games and mods.",
            Sometimes,
            L(LM(@"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1, 0)),
            detail: "Microsoft's documented long-path switch. Programs must opt in to benefit, so nothing existing changes.");

        yield return Mk("essential.hide-home", Category.Debloat, Essential,
            "Hide Home from File Explorer",
            "Removes the Home entry from the File Explorer sidebar.",
            s => s.Build >= 22621 ? Verdict.Opt("Turn on \"Open File Explorer to This PC\" on the Customization page too, so Explorer doesn't open on the missing Home page.") : Verdict.No("Home only exists in Windows 11 22H2 and newer."),
            L(CU(@"Software\Classes\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}", "System.IsPinnedToNameSpaceTree", 0),
              CU(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1)),
            detail: "Unpins Home for your account only and makes File Explorer open to This PC so it doesn't open on the missing Home page. Restore deletes both settings.",
            signOut: true);

    }

    static IEnumerable<Tweak> MoreSettings()
    {
        // ------------------------------------------------------------------ Performance
        yield return Mk("perf.foreground-boost", Category.Performance, SystemGroup,
            "Prioritise the program you are using",
            "Gives the foreground app longer, higher-priority time slices than background services.",
            Sometimes,
            L(LM(@"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38, 2)),
            detail: "Sets Win32PrioritySeparation to 0x26 (38), a commonly recommended value. The gain is small and mostly noticeable on busy PCs. Windows' own default is 2.");

        yield return Mk("perf.sysmain", Category.Performance, SystemGroup,
            "Turn off SysMain (Superfetch)",
            "Stops Windows pre-loading apps into memory.",
            s => s.SystemDriveIsSsd switch
            {
                false => Verdict.Warn("On a hard disk SysMain makes app launches faster. Leave it on."),
                true => Verdict.Rec("On an SSD the benefit of SysMain is small, so this saves background disk reads."),
                _ => Verdict.Opt(),
            },
            L(new ServiceAction("SysMain", ServiceAction.Disabled, "auto")),
            detail: "Stops and disables the SysMain service. Restore sets it back to Automatic, or to the exact startup type it had.");

        yield return Mk("perf.wallpaper-quality", Category.Performance, SystemGroup,
            "Full-quality desktop wallpaper",
            "Stops Windows compressing your wallpaper (JPEG quality 100).",
            Sometimes,
            L(CU(@"Control Panel\Desktop", "JPEGImportQuality", 100)),
            detail: "Only matters if your wallpaper looks slightly soft or blocky. Restore deletes the setting.");

        // ------------------------------------------------------------------ Gaming
        yield return Mk("gaming.mmcss", Category.Gaming, GameExperience,
            "Game task scheduling (Multimedia Class Scheduler)",
            "Tells Windows to give games more CPU priority than background work.",
            s => s.IsVirtualMachine ? Verdict.No("Not meaningful in a virtual machine.") : Verdict.Opt("Often recommended, but results are small. Test it rather than assuming."),
            L(LM(MmcssKey, "SystemResponsiveness", 10, 20),
              LM(MmcssKey + @"\Tasks\Games", "Priority", 6, 2),
              LMs(MmcssKey + @"\Tasks\Games", "Scheduling Category", "High", "Medium")),
            detail: "Sets SystemResponsiveness 10, and the Games task to Priority 6 and Scheduling Category High. Windows' defaults are 20, 2 and Medium. " +
                    "Expect a small difference at most, and only when other programs are using the CPU while you play.");

        yield return Mk("gaming.game-bar-tips", Category.Gaming, GameExperience,
            "Hide Game Bar start-up tips",
            "No more \"Press Win+G to open Game Bar\" pop-ups.",
            Always,
            L(CU(@"Software\Microsoft\GameBar", "ShowStartupPanel", 0, 1)),
            detail: "Only hides the tip. Game Bar itself still works.");

        // ------------------------------------------------------------------ Privacy
        yield return Mk("privacy.search-history", Category.Privacy, TrackingFeedback,
            "Stop Windows keeping search history and cloud search",
            "Search no longer stores what you looked for on this PC or reaches into your Microsoft account.",
            Always,
            L(CU(@"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDeviceSearchHistoryEnabled", 0, 1),
              CU(@"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsMSACloudSearchEnabled", 0, 1),
              CU(@"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsAADCloudSearchEnabled", 0, 1)),
            detail: "Same as Settings > Privacy & security > Search permissions: Search history on this device off, and Cloud content search off.");

        yield return Mk("privacy.workplace-join", Category.Privacy, TrackingFeedback,
            "Block \"add your work account\" prompts",
            "Stops Windows asking to join a work or school directory.",
            s => s.DomainJoined ? Verdict.Warn("This PC is joined to a domain. Leave this alone.") : Verdict.Rec(),
            L(LM(@"SOFTWARE\Policies\Microsoft\Windows\WorkplaceJoin", "BlockAADWorkplaceJoin", 1),
              CU(@"SOFTWARE\Policies\Microsoft\Windows\WorkplaceJoin", "BlockAADWorkplaceJoin", 1)),
            detail: "Do not use if you sign in to Microsoft 365 apps with a work account and need device registration.");

        // ------------------------------------------------------------------ Windows Update
        yield return Mk("updates.no-auto-restart", Category.Updates, Behaviour,
            "No automatic restarts while you're signed in",
            "Windows waits for you to restart instead of rebooting after updates.",
            Always,
            L(LM(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1),
              CU(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1)),
            detail: "Updates still install. Restart when it suits you. Do restart regularly so security patches finish applying.");

        yield return Mk("updates.store-auto", Category.Updates, Behaviour,
            "Don't auto-update Microsoft Store apps",
            "Store apps only update when you tell the Store to.",
            Always,
            L(LM(@"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2),
              CU(@"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2)),
            detail: "Store apps will fall behind unless you update them by hand, which can leave known bugs unfixed.");
    }
}
