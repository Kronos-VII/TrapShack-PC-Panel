using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

public static partial class Catalog
{
    const string Apps = "Bundled apps";
    const string Clutter = "OneDrive";

    static IEnumerable<Tweak> Debloat()
    {
        // ---------------------------------------------------------------- Apps
        // ---------------------------------------------------------------- Ads & clutter
        yield return Mk("debloat.onedrive", Category.Debloat, Clutter,
            "Uninstall OneDrive",
            "Removes the OneDrive app. Your files in the cloud are not deleted.",
            s => s.DocumentsInOneDrive
                ? Verdict.Warn("Your Documents folder is synced with OneDrive. Move it back to a local folder first or those files will no longer be on this PC.")
                : Verdict.Opt(),
            L(new CustomAction(
                  "Uninstall OneDrive (restore reinstalls it with Windows' own installer)",
                  isApplied: () => !OneDriveOps.IsInstalled(),
                  apply: OneDriveOps.Remove,
                  revert: _ => OneDriveOps.Reinstall())),
            detail: "Uninstalls OneDrive with the installer that ships with Windows. Files already synced stay in your OneDrive folder " +
                    "and online. Make sure nothing in there exists only on this PC before you continue.",
            risk: Risk.Advanced,
            confirm: "OneDrive will be uninstalled from this PC. Files stored in the cloud stay safe online, but anything that exists only " +
                     "in your OneDrive folder should be copied somewhere else first.");
    }
}
