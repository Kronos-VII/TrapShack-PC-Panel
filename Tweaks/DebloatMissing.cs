using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

public static partial class Catalog
{
    /// <summary>The remaining Debloat-page items that were not covered elsewhere.</summary>
    static IEnumerable<Tweak> DebloatMissing()
    {
        yield return Once(Mk("extra.restore-point", Category.Debloat, "Tweaks",
            "Create a restore point",
            "Saves a Windows restore point you can roll back to. TrapShack also makes one before every apply.",
            Sometimes,
            L(new CustomAction(
                "Create a Windows restore point named TrapShack - manual",
                isApplied: () => false,
                apply: () =>
                {
                    var r = RestorePoint.Create("TrapShack - manual");
                    if (!r.Ok) throw new InvalidOperationException(r.Message);
                })),
            detail: "Creates a system restore point now. Windows allows one restore point every 24 hours by default, so TrapShack lifts that limit for the moment it creates this one."));

        yield return Mk("extra.revert-start-menu", Category.Debloat, "Tweaks",
            "Bring back the previous Start menu layout",
            "Turns the new Start menu layout off again. Only works on Windows 11 builds that still ship the old layout.",
            s => W11(s, Verdict.Opt("On newer Windows 11 versions this setting is ignored.")),
            L(new RegistryAction(RegistryHive.LocalMachine, @"SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8\3036241548", "EnabledState", 1, RegistryValueKind.DWord, null)),
            detail: "Adds one Feature Management override value. On Windows 11 25H2 and newer this has no effect. Restore removes the value.",
            restart: true);
    }
}
