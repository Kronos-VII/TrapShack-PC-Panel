using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

/// <summary>Optimization settings that are personal preferences or situational. None is applied by "Apply recommended".</summary>
public static partial class Catalog
{
    const string DiagPol = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection";

    static IEnumerable<Tweak> MoreOptions()
    {
        yield return Mk("more.search-highlights", Category.Privacy, TrackingFeedback,
            "Turn off search highlights",
            "Removes the daily illustrations and trending items from the search box.",
            Always,
            L(CU(@"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0, null)),
            detail: "Same as Settings > Privacy & security > Search permissions > More settings > Show search highlights.");

        yield return Mk("more.scoobe-suggestions", Category.Customize, "Notifications",
            "Stop \"finish setting up your device\" suggestions",
            "Hides the repeated prompts to get the most out of Windows.",
            Always,
            L(CU(@"Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement", "ScoobeSystemSettingEnabled", 0, 1)),
            detail: "Same as Settings > System > Notifications > Suggest ways to get the most out of Windows.");

        yield return Mk("more.onedrive-backup-block", Category.Privacy, TrackingFeedback,
            "Stop OneDrive asking to back up your folders",
            "Blocks the prompts to move Desktop, Documents and Pictures into OneDrive.",
            Always,
            L(LM(@"SOFTWARE\Policies\Microsoft\OneDrive", "KFMBlockOptIn", 1, null),
              CU(@"SOFTWARE\Policies\Microsoft\OneDrive", "KFMBlockOptIn", 1, null)),
            detail: "Does not remove OneDrive or touch folders that are already backed up.");

        yield return Mk("more.mouse-hover-instant", Category.Customize, "Mouse & keyboard",
            "Make hover previews instant",
            "Taskbar and window previews appear as soon as you point at them.",
            Always,
            L(CUs(@"Control Panel\Mouse", "MouseHoverTime", "1", "400")),
            detail: "Sets the hover delay to 1 ms instead of the default 400 ms. Takes effect after you sign out and in.",
            signOut: true);

        yield return Mk("more.overlay-compat", Category.Gaming, "Gaming",
            "Turn off Multiplane Overlay",
            "A fix for flicker, black screens or stutter on some graphics cards.",
            _ => Verdict.Opt("Only worth trying if you see display flicker. It is not a general speed-up."),
            L(LM(@"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5, null)),
            detail: "Sets OverlayTestMode to 5, the milder way of disabling the overlay planes. Applied immediately.");

        yield return Mk("more.overlay-full", Category.Gaming, "Gaming",
            "Turn off hardware overlays completely",
            "The stronger version of the overlay fix, for problems that remain after the milder one.",
            _ => Verdict.Opt("Use only if the milder overlay fix did not help. It can increase power use slightly."),
            L(LM(@"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5, null),
              LM(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "DisableOverlays", 1, null)),
            detail: "Sets OverlayTestMode to 5 and DisableOverlays to 1. Restart for it to take full effect.",
            restart: true, risk: Risk.Advanced);

        yield return Mk("more.overlay-minfps", Category.Gaming, "Gaming",
            "Remove the overlay minimum frame rate",
            "Lets Windows use overlays even at low frame rates.",
            _ => Verdict.Opt("A niche display setting. Leave it off unless you are troubleshooting."),
            L(LM(@"SOFTWARE\Microsoft\Windows\Dwm", "OverlayMinFPS", 0, null)),
            detail: "Sets OverlayMinFPS to 0.");
    }
}
