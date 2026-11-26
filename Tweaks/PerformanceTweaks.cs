using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

public static partial class Catalog
{
    const string Responsiveness = "Responsiveness";
    const string GamingGroup = "Gaming";
    const string Connection = "Connection";

    static IEnumerable<Tweak> Performance()
    {
        const string visualKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
        const string desktopKey = @"Control Panel\Desktop";
        const string advancedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

        yield return Mk("perf.visual-effects", Category.Performance, Responsiveness,
            "Visual effects: best performance",
            "Turns off animations, shadows and transparency. Text stays smooth (ClearType is kept on).",
            // the Optimizations page has each visual effect as its own switch, recommended one by one; this turns all of them off
            Sometimes,
            L(CU(visualKey, "VisualFXSetting", 3, 0),
              new RegistryAction(RegistryHive.CurrentUser, desktopKey, "UserPreferencesMask",
                  new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary,
                  new byte[] { 0x9E, 0x3E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 }),
              CUs(@"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", "1"),
              CUs(desktopKey, "DragFullWindows", "0", "1"),
              CUs(desktopKey, "MenuShowDelay", "0", "400"),
              CUs(@"Control Panel\Keyboard", "KeyboardDelay", "0", "1"),
              CU(advancedKey, "TaskbarMn", 0, 1),
              CU(advancedKey, "TaskbarAnimations", 0, 1),
              CU(advancedKey, "ListviewAlphaSelect", 0, 1),
              CU(advancedKey, "ListviewShadow", 0, 1),
              CU(@"Software\Microsoft\Windows\DWM", "EnableAeroPeek", 0, 1),
              CU(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, 1)),
            detail: "Sets the Visual Effects mode to Custom with animations, shadows and transparency turned off, the same settings the published display tweak uses. Font smoothing stays on so text stays readable. Also shortens the menu delay and hides the Chat icon.",
            signOut: true);

        yield return Mk("perf.background-apps", Category.Performance, Responsiveness,
            "Stop apps running in the background",
            "Store apps can't keep running, syncing or polling when you aren't using them.",
            s => s.IsLaptop
                ? Verdict.Rec("Also saves battery on a laptop.")
                : Verdict.Rec(),
            L(LM(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2),
              CU(@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1, 0)),
            detail: "Applies the \"Let Windows apps run in the background: force deny\" policy. Apps that rely on background activity " +
                    "(some mail, chat and alarm apps) will only notify you while they are open. Classic desktop programs are not affected.");

        yield return Mk("perf.search-indexing", Category.Performance, Responsiveness,
            "Disable Windows Search indexing",
            "Stops the background indexer. Search still works, but searches files directly.",
            s => s.SystemDriveIsSsd switch
            {
                true => Verdict.Rec("On an SSD, searching without an index stays fast and you save CPU and disk writes."),
                false => Verdict.Warn("On a hard disk, the index keeps Start-menu and Explorer search fast. Leave this off unless you never search."),
                null => Verdict.Opt(),
            },
            L(new ServiceAction("WSearch", ServiceAction.Disabled, "delayed-auto")),
            detail: "Stops and disables the Windows Search service. Outlook and other apps that use the Windows index for search will be slower. Restore sets the service back to its original startup type.");

        yield return Mk("perf.svchost-split", Category.Performance, Responsiveness,
            "Stop splitting svchost.exe",
            "Keeps Windows services together in fewer svchost processes instead of one process per service.",
            s => s.RamGb >= 8
                ? Verdict.Rec($"You have {s.RamGb:0.#} GB of RAM, plenty for this.")
                : s.RamGb >= 4
                    ? Verdict.Warn("With under 8 GB of RAM, splitting services actually saves memory. Leave this off.")
                    : Verdict.No("Needs at least 4 GB of RAM."),
            L(new RegistryAction(
                RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB",
                () => (int)Math.Min((ulong)int.MaxValue, SystemInfo.Current.RamKb + 1_048_576UL),
                RegistryValueKind.DWord, 3670016,   // Windows' own value: 0x380000 KB (3.5 GB)
                matcher: v => v is int i && i >= 0 && (ulong)i >= SystemInfo.Current.RamKb)),
            detail: "Windows splits services into separate processes only when RAM is above SvcHostSplitThresholdInKB (3.5 GB by default). " +
                    "Setting it to just above your installed RAM merges them again, which means a tidier Task Manager. " +
                    "It does not make games faster. Takes effect after a restart.",
            restart: true);
    }

    static IEnumerable<Tweak> Gaming()
    {
        yield return Mk("gaming.game-dvr", Category.Gaming, GamingGroup,
            "Turn off background game recording",
            "Stops Windows constantly recording in the background for \"record that\". The Game Bar overlay still works.",
            s => s.IsAmdCpu && s.HasX3D
                ? Verdict.Rec("Only background recording stops. Game Bar itself, which your Ryzen X3D chip uses to recognise games, keeps working.")
                : Verdict.Rec(),
            L(CU(@"System\GameConfigStore", "GameDVR_Enabled", 0, 1),
              CU(@"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, 1),
              LM(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, null)),
            detail: "Background capture costs a little performance in every game. Manual recording with the Game Bar is not touched. If you use \"record the last 30 seconds\", leave this off.");

        yield return Mk("gaming.hags", Category.Gaming, GamingGroup,
            "Hardware-accelerated GPU scheduling",
            "Lets the graphics card manage its own memory scheduling, which cuts CPU overhead and is needed for frame generation.",
            s =>
            {
                if (s.IsVirtualMachine) return Verdict.No("Not available inside a virtual machine.");
                if (s.HasNvidiaRtx40OrNewer)
                    return Verdict.Rec("Many games need this on to offer DLSS Frame Generation on your RTX card.");
                if (s.HasNvidiaHagsCapable)
                    return Verdict.Opt("Supported on your NVIDIA card. Results vary by game, so it's worth testing rather than assuming.");
                if (s.HasAmdDiscreteGpu && s.HasAmdHagsCapable)
                    return Verdict.Opt("Supported on your Radeon card with a current driver. Results vary by game.");
                if (s.HasIntelArcGpu)
                    return Verdict.Opt("Supported on Intel Arc with a current driver. Results vary by game.");
                return Verdict.No("Your graphics hardware isn't known to support this safely.");
            },
            L(new RegistryAction(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                "HwSchMode", 2, RegistryValueKind.DWord, 1)),
            detail: "The same switch as Settings > System > Display > Graphics > Default graphics settings. " +
                    "TrapShack only offers it for GPUs that support it and needs a current graphics driver. Takes effect after a restart.",
            restart: true);

        yield return Mk("gaming.ultimate-power", Category.Gaming, GamingGroup,
            "Ultimate Performance power plan",
            "Keeps the CPU ready instead of ramping up on demand.",
            s => s.IsVirtualMachine
                ? Verdict.No("Power plans don't apply inside a virtual machine.")
                : s.IsLaptop
                    ? Verdict.Warn("On a laptop this drains the battery quickly and runs hotter.")
                    : s.IsAmdCpu && s.HasX3D
                        ? Verdict.Warn("AMD says Ryzen X3D chips should stay on a Balanced plan so the 3D V-Cache driver can place games correctly. Leave this off.")
                        : s.IsAmdCpu
                            ? Verdict.Opt("AMD's chipset driver already installs a tuned Ryzen plan, so the gain here is small.")
                            : s.IsIntelCpu
                                ? Verdict.Rec("Safe on a desktop Intel CPU that's plugged into the wall.")
                                : Verdict.Opt(),
            L(new CustomAction(
                "Create and activate the Ultimate Performance power plan (restore returns to your previous plan)",
                isApplied: PowerPlanOps.IsUltimateActive,
                apply: PowerPlanOps.Enable,
                revert: PowerPlanOps.Restore,
                capture: PowerPlanOps.ActiveGuid)),
            detail: "Uses Windows' hidden Ultimate Performance plan, which many tuning guides also use. Your current plan is remembered and put back when you switch this off.");
    }

    static IEnumerable<Tweak> Network()
    {
        yield return Mk("network.prefer-ipv4", Category.Network, Connection,
            "Prefer IPv4 over IPv6",
            "Windows tries IPv4 first. IPv6 stays installed and working.",
            s => Verdict.Opt("Harmless, and useful if some sites or games connect slowly."),
            L(LM(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", 0x20)),
            detail: "Sets DisabledComponents to 0x20, which is Microsoft's documented value for \"prefer IPv4 over IPv6\". " +
                    "It does NOT turn IPv6 off, which Microsoft recommends against. Takes effect after a restart.",
            restart: true);
    }
}
