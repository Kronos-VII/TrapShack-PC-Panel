using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

// GENERATED from the published power-plan definitions: every setting with its recommended and default value,
// separately for plugged in (AC) and on battery (DC). Each setting is recommended for both plugged in and on battery; laptops are shown a note.
public static partial class Catalog
{
    static IEnumerable<Tweak> PowerSettings()
    {
        yield return Mk("opt.power-display-timeout", Category.Performance, "Display",
            "Turn off the display",
            "Plugged in: Never. On battery: 5 minutes.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Turn off the display", "7516b95f-f776-4464-8c53-06167f40cc99", "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e", 0, 300, 300, 180)),
            detail: "Specifies the period of inactivity before Windows turns off the display. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-harddisk-timeout", Category.Performance, "Hard Disk",
            "Turn off hard disk after",
            "Plugged in: Never. On battery: 600 s.",
            Always,
            L(new PowerCfgAction("Turn off hard disk after", "0012ee47-9041-4b5d-9b77-535fba8b1442", "6738e2c4-e8a5-4a42-b16a-e040e769756e", 0, 600, 1200, 600)),
            detail: "Specifies the period of inactivity before Windows turns off the hard disk. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-internet-explorer-javascript-timer", Category.Performance, "Internet Explorer",
            "JavaScript Timer Frequency",
            "Plugged in: Maximum Performance. On battery: Maximum Performance.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("JavaScript Timer Frequency", "02f815b5-a5cf-4c84-bf20-649d1f75d3d8", "4c793e7d-a264-42e1-87d3-7a0d2f523ccd", 0, 0, 0, 1)),
            detail: "Specifies the frequency of JavaScript timers. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-desktop-slideshow", Category.Performance, "Desktop Background Settings",
            "Desktop Background Slide Show",
            "Plugged in: Available. On battery: Paused.",
            Always,
            L(new PowerCfgAction("Desktop Background Slide Show", "0d7dbae2-4294-402a-ba8e-26777e8488cd", "309dce9b-bef4-4119-9921-a851fb12f0f4", 1, 1, 0, 1)),
            detail: "Allow or prevent Windows from rotating through multiple wallpaper images. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-sleep-timeout", Category.Performance, "Sleep",
            "Put the computer to sleep",
            "Plugged in: Never. On battery: 15 minutes.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Put the computer to sleep", "238c9fa8-0aad-41ed-83f4-97be242c8f20", "29f6c1db-86da-48c5-9fdb-f2b67b1f44da", 0, 900, 900, 600)),
            detail: "Specifies the period of inactivity before Windows puts the computer to sleep. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-wake-timers", Category.Performance, "Sleep",
            "Allow wake timers",
            "Plugged in: Disable. On battery: Disable.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Allow wake timers", "238c9fa8-0aad-41ed-83f4-97be242c8f20", "bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d", 0, 0, 1, 1)),
            detail: "Allow scheduled tasks and applications to wake your computer from sleep. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-hibernate-timeout", Category.Performance, "Sleep",
            "Hibernate after",
            "Plugged in: 0. On battery: 0.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Hibernate after", "238c9fa8-0aad-41ed-83f4-97be242c8f20", "9d7815a6-7ee4-497e-8888-515a05f02364", 0, 0, 0, 10800)),
            detail: "Specifies the period of inactivity before Windows hibernates the computer. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-hybrid-sleep", Category.Performance, "Sleep",
            "Allow hybrid sleep",
            "Plugged in: 0. On battery: 0.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Allow hybrid sleep", "238c9fa8-0aad-41ed-83f4-97be242c8f20", "94ac6d29-73ce-41a6-809f-6363ba21b47e", 0, 0, 1, 1)),
            detail: "Combines sleep and hibernate by saving your session to disk while staying in low-power mode for faster wake. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-usb-hub-selective-suspend-timeout", Category.Performance, "USB settings",
            "USB Hub Selective Suspend Timeout",
            "Plugged in: 0 ms. On battery: 1000 ms.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("USB Hub Selective Suspend Timeout", "2a737441-1930-4402-8d77-b2bebba308a3", "0853a681-27c8-4100-a2fd-82013e970683", 0, 1000, 50, 50)),
            detail: "Set how long USB hubs wait idle before powering down to save energy. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-usb-selective-suspend", Category.Performance, "USB settings",
            "USB selective suspend setting",
            "Plugged in: Disabled. On battery: Enabled.",
            Always,
            L(new PowerCfgAction("USB selective suspend setting", "2a737441-1930-4402-8d77-b2bebba308a3", "48e6b7a6-50f5-4782-a5d4-53bb8f07e226", 0, 1, 1, 1)),
            detail: "Allow Windows to power down individual USB ports when devices are idle to save energy. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-usb3-link-power-management", Category.Performance, "USB settings",
            "USB 3 Link Power Management",
            "Plugged in: Off. On battery: Moderate power savings.",
            Always,
            L(new PowerCfgAction("USB 3 Link Power Management", "2a737441-1930-4402-8d77-b2bebba308a3", "d4e98f31-5ffe-4ce1-be31-1b38b384c009", 0, 2, 2, 2)),
            detail: "Control how aggressively USB 3.0 ports enter low-power states when devices are idle. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-intel-graphics-power-plan", Category.Performance, "Intel(R) Graphics Settings",
            "Intel(R) Graphics Power Plan",
            "Plugged in: Maximum Performance. On battery: Balanced.",
            Always,
            L(new PowerCfgAction("Intel(R) Graphics Power Plan", "44f3beca-a7c0-460e-9df2-bb8b99e0cba6", "3619c3f2-afb2-4afc-b0e9-e7fef372de36", 2, 1, 1, 1)),
            detail: "Balance Intel integrated graphics performance with power consumption and battery life. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-button-action", Category.Performance, "Power Buttons and Lid",
            "Power button action",
            "Plugged in: Do nothing. On battery: Do nothing.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Power button action", "4f971e89-eebd-4455-a8de-9e59040e7347", "7648efa3-dd9c-4e3e-b566-50f929386280", 0, 0, 3, 3)),
            detail: "Choose what happens when you press the physical power button on your computer. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-sleep-button-action", Category.Performance, "Power Buttons and Lid",
            "Sleep button action",
            "Plugged in: Do nothing. On battery: Do nothing.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Sleep button action", "4f971e89-eebd-4455-a8de-9e59040e7347", "96996bc0-ad50-47ec-923b-6f41874dd9eb", 0, 0, 1, 1)),
            detail: "Choose what happens when you press the dedicated sleep button on your keyboard or computer. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-pci-link-state-power-management", Category.Performance, "PCI Express",
            "Link State Power Management",
            "Plugged in: Off. On battery: Maximum power savings.",
            Always,
            L(new PowerCfgAction("Link State Power Management", "501a4d13-42af-4429-9fd1-a8218c268e20", "ee12f906-d277-404b-b6da-e5fa1a576df5", 0, 2, 1, 2)),
            detail: "Control power savings for PCIe devices like graphics cards, SSDs, and expansion cards. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-processor-min-state", Category.Performance, "Processor Power Management",
            "Minimum processor state",
            "Plugged in: 100%. On battery: 5%.",
            Always,
            L(new PowerCfgAction("Minimum processor state", "54533251-82be-4824-96c1-47b60b740d00", "893dee8e-2bef-41e0-89c6-b55d0929964c", 100, 5, 0, 5)),
            detail: "Set the lowest CPU speed allowed as a percentage of maximum frequency. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-system-cooling-policy", Category.Performance, "Processor Power Management",
            "System cooling policy",
            "Plugged in: Active. On battery: Active.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("System cooling policy", "54533251-82be-4824-96c1-47b60b740d00", "94d3a615-a899-4ac5-ae2b-e4d8f634367f", 1, 1, 1, 0)),
            detail: "Choose whether to slow down the processor first (passive) or speed up fans first (active) when hot. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-processor-performance-boost-mode", Category.Performance, "Processor Power Management",
            "Processor performance boost mode",
            "Plugged in: Aggressive. On battery: Enabled.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Processor performance boost mode", "54533251-82be-4824-96c1-47b60b740d00", "be337238-0d82-4146-a960-4f3749d470c7", 2, 1, 2, 2)),
            detail: "Control how aggressively your CPU boosts above base frequency for demanding tasks. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-processor-performance-decrease-policy", Category.Performance, "Processor Power Management",
            "Processor Performance Decrease Policy",
            "Plugged in: Single. On battery: Rocket.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Processor Performance Decrease Policy", "54533251-82be-4824-96c1-47b60b740d00", "40fbefc7-2e9d-4d25-a185-0cfd8574bac6", 1, 2, 1, 0)),
            detail: "Control how quickly CPU reduces speed when workload decreases (for legacy non-HWP processors). Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-processor-core-parking-min-cores", Category.Performance, "Processor Power Management",
            "CPU Core Parking Minimum Cores",
            "Plugged in: 0%. On battery: 0%.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("CPU Core Parking Minimum Cores", "54533251-82be-4824-96c1-47b60b740d00", "0cc5b647-c1df-4637-891a-dec35c318583", 0, 0, 100, 10)),
            detail: "Set the minimum percentage of CPU cores that must remain active and responsive. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-processor-energy-performance-preference", Category.Performance, "Processor Power Management",
            "Processor Energy Performance Preference",
            "Plugged in: 0%. On battery: 50%.",
            Always,
            L(new PowerCfgAction("Processor Energy Performance Preference", "54533251-82be-4824-96c1-47b60b740d00", "36687f9e-e3a5-4dbf-b1dc-15eb381c6863", 0, 50, 25, 50)),
            detail: "Balance power efficiency and performance for modern CPUs with HWP (0 = max performance, 100 = max efficiency). Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-processor-performance-increase-threshold", Category.Performance, "Processor Power Management",
            "Processor Performance Increase Threshold",
            "Plugged in: 10%. On battery: 30%.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Processor Performance Increase Threshold", "54533251-82be-4824-96c1-47b60b740d00", "06cadf0e-64ed-448a-8927-ce7bf90eb35d", 10, 30, 30, 90)),
            detail: "Set CPU usage percentage that triggers speed increase (lower = more responsive, for legacy non-HWP CPUs). Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-processor-performance-decrease-threshold", Category.Performance, "Processor Power Management",
            "Processor Performance Decrease Threshold",
            "Plugged in: 8%. On battery: 20%.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Processor Performance Decrease Threshold", "54533251-82be-4824-96c1-47b60b740d00", "12a0ab44-fe28-4fa9-b3bd-4b64f44960a6", 8, 20, 10, 30)),
            detail: "Set CPU usage percentage that triggers speed reduction (lower = maintains performance longer, for legacy non-HWP CPUs). Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-multimedia-when-sharing-media", Category.Performance, "Multimedia Settings",
            "When Sharing Media",
            "Plugged in: Prevent idling to sleep. On battery: Prevent idling to sleep.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("When Sharing Media", "9596fb26-9850-41fd-ac3e-f7c3c00afd4b", "03680956-93bc-4294-bba6-4e0f09bb717f", 1, 1, 1, 0)),
            detail: "Control whether your PC can sleep while streaming media to other devices on your network. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-multimedia-video-playback-quality-bias", Category.Performance, "Multimedia Settings",
            "Video Playback Quality Bias",
            "Plugged in: Video playback performance bias. On battery: Video playback performance bias.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Video Playback Quality Bias", "9596fb26-9850-41fd-ac3e-f7c3c00afd4b", "10778347-1370-4ee0-8bbd-33bdacaade49", 1, 1, 1, 0)),
            detail: "Prioritize smooth video playback over battery life when watching videos. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-multimedia-when-playing-video", Category.Performance, "Multimedia Settings",
            "When Playing Video",
            "Plugged in: Optimize video quality. On battery: Optimize video quality.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("When Playing Video", "9596fb26-9850-41fd-ac3e-f7c3c00afd4b", "34c7b99f-9a6d-4b3c-8dc7-b6693b78cef4", 0, 0, 0, 1)),
            detail: "Balance video quality and power consumption during video playback. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-amd-power-slider-overlay", Category.Performance, "AMD Power Slider",
            "Overlay",
            "Plugged in: Best Performance. On battery: Better Battery.",
            s => s.IsLaptop ? Verdict.Rec("Sets separate plugged-in and on-battery values, so it also changes how the laptop behaves on battery.") : Verdict.Rec(),
            L(new PowerCfgAction("Overlay", "c763b4ec-0e50-4b6b-9bed-2b92a6ee884e", "7ec1751b-60ed-4588-afb5-9819d3d77d90", 3, 1, 2, 2)),
            detail: "Balance AMD laptop performance and battery life with quick power mode selection. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-ati-powerplay-setting", Category.Performance, "ATI PowerPlay",
            "ATI PowerPlay Setting",
            "Plugged in: Maximum Performance. On battery: Balanced.",
            Always,
            L(new PowerCfgAction("ATI PowerPlay Setting", "f693fb01-e858-4f00-b20f-f30e12ac06d6", "191f65b5-d45c-4a4f-8aae-1ab8bfd980e6", 2, 1, 1, 1)),
            detail: "Control power management for older AMD Radeon graphics cards. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-switchable-graphics-gpu-preference", Category.Performance, "Switchable Graphics",
            "GPU Preference",
            "Plugged in: Maximize Performance. On battery: Optimize Power Savings.",
            Always,
            L(new PowerCfgAction("GPU Preference", "e276e160-7cb0-43c6-b20b-73f5dce39954", "a1662ab2-9d34-4e53-ba8b-2639b9e20857", 2, 1, 1, 1)),
            detail: "Choose between integrated GPU for battery life or dedicated GPU for performance in hybrid graphics laptops. Changes the active power plan. Restore puts back your previous values. Part of the recommended set.");
        yield return Mk("opt.power-fast-startup", Category.Performance, "Sleep",
            "Turn off Fast Startup",
            "Shut down fully instead of hibernating, so Windows starts from scratch each time.",
            Always,
            L(LM(@"SYSTEM\ControlSet001\Control\Session Manager\Power", "HiberbootEnabled", 0, null)),
            detail: "Shut down fully instead of hibernating, so Windows starts from scratch each time. Part of the recommended set.", restart: true);
        yield return Mk("opt.power-start-lock-option", Category.Performance, "Start Menu",
            "Hide Lock in the Start power menu",
            "Removes the Lock entry from the power button menu.",
            Always,
            L(LM(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowLockOption", 0, null)),
            detail: "Removes the Lock entry from the power button menu. Part of the recommended set.");
        yield return Mk("opt.power-start-sleep-option", Category.Performance, "Start Menu",
            "Hide Sleep in the Start power menu",
            "Removes the Sleep entry from the power button menu.",
            Always,
            L(LM(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowSleepOption", 0, null)),
            detail: "Removes the Sleep entry from the power button menu. Part of the recommended set.");
    }
}
