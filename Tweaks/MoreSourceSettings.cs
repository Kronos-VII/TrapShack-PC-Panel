using TrapShack.Core;

namespace TrapShack.Tweaks;

/// <summary>Settings from the optimisation source that need more than plain registry values.</summary>
public static partial class Catalog
{
    static IEnumerable<Tweak> MoreSourceSettings()
    {
        // The source's recommended value is "on". Restore points are what TrapShack's own safety net relies on.
        yield return Mk("opt.system-restore-protection", Category.Performance, "Responsiveness",
            "Keep System Restore switched on for the system drive",
            "Windows can make restore points, so a bad driver or update can be undone.",
            s => s.IsVirtualMachine ? Verdict.Opt("Virtual machines usually use snapshots instead.") : Verdict.Rec(),
            L(new CustomAction(
                "Run Enable-ComputerRestore on the system drive (restore runs Disable-ComputerRestore if it was off before)",
                isApplied: RestorePoints.IsEnabled,
                apply: () =>
                {
                    var r = Shell.PowerShell("Enable-ComputerRestore -Drive \"$env:SystemDrive\\\" -ErrorAction Stop", 120_000);
                    if (!r.Ok) throw new InvalidOperationException((r.Output + r.Error).Trim());
                },
                revert: saved =>
                {
                    if (saved != "off") return;   // it was already on before: leave it on
                    Shell.PowerShell("Disable-ComputerRestore -Drive \"$env:SystemDrive\\\"", 120_000);
                },
                capture: () => RestorePoints.IsEnabled() ? "on" : "off")),
            detail: "Turns on System Protection for the Windows drive, the same as System Properties > System Protection > Configure > Turn on. " +
                    "Restore points then work, including the ones TrapShack makes before big changes.");
    }
}
