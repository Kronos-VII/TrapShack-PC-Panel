using System.IO;
using System.Text.RegularExpressions;
using TrapShack.Core;

namespace TrapShack.Tweaks;

/// <summary>Multi-step operations that are more than a registry value. Kept here so the catalog stays readable.</summary>

public static class OneDriveOps
{
    static IEnumerable<string> Executables()
    {
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive", "OneDrive.exe");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft OneDrive", "OneDrive.exe");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft OneDrive", "OneDrive.exe");
    }

    public static bool IsInstalled() => Executables().Any(File.Exists);

    /// <summary>The installer Windows keeps on disk; it also works as the uninstaller.</summary>
    static string SetupExe()
    {
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var candidates = new[]
        {
            Path.Combine(win, "SysWOW64", "OneDriveSetup.exe"),
            Path.Combine(win, "System32", "OneDriveSetup.exe"),
        };
        return candidates.FirstOrDefault(File.Exists)
               ?? throw new FileNotFoundException("Windows' OneDrive setup file was not found on this PC.");
    }

    public static void Remove()
    {
        Shell.KillProcess("OneDrive.exe");
        var r = Shell.Run(SetupExe(), "/uninstall", 300_000);
        Log.Info($"OneDrive uninstall exit code {r.ExitCode}");
        if (IsInstalled()) throw new InvalidOperationException("OneDrive is still installed after the uninstaller ran.");
    }

    public static void Reinstall()
    {
        var r = Shell.Run(SetupExe(), "/silent", 300_000);
        Log.Info($"OneDrive install exit code {r.ExitCode}");
    }
}

public static class EdgeOps
{
    static string EdgeRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application");

    public static bool IsInstalled() => File.Exists(Path.Combine(EdgeRoot, "msedge.exe"));

    static Version? ParseVersion(string dir) => Version.TryParse(Path.GetFileName(dir), out var v) ? v : null;

    public static void Remove()
    {
        if (!Directory.Exists(EdgeRoot)) return;

        // Edge's own installer is the supported way to remove it: Application\<version>\Installer\setup.exe
        var setup = Directory.GetDirectories(EdgeRoot)
            .Select(d => (Dir: d, Ver: ParseVersion(d)))
            .Where(x => x.Ver != null)
            .OrderByDescending(x => x.Ver)
            .Select(x => Path.Combine(x.Dir, "Installer", "setup.exe"))
            .FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Edge's installer was not found, so it cannot be removed automatically.");

        Shell.KillProcess("msedge.exe");
        Shell.KillProcess("msedgewebview2.exe");   // WebView2 apps restart it themselves; the runtime stays installed.

        var r = Shell.Run(setup, "--uninstall --system-level --verbose-logging --force-uninstall", 300_000);
        Log.Info($"Edge uninstall exit code {r.ExitCode}");

        if (IsInstalled())
            throw new InvalidOperationException(
                "Windows blocked the removal on this build, so Edge was left in place. Nothing was broken.");
    }

    public static void Reinstall()
    {
        var r = Shell.Run("winget.exe",
            "install --id Microsoft.Edge --exact --silent --accept-package-agreements --accept-source-agreements", 600_000);
        if (!r.Ok)
            throw new InvalidOperationException(
                "Could not reinstall Edge automatically. Download it from microsoft.com/edge.");
    }
}

public static class PowerPlanOps
{
    // Microsoft's template GUID for the Ultimate Performance plan.
    const string UltimateTemplate = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    // Fixed GUID for TrapShack's copy, so detection never depends on the plan's (localized) name.
    public const string TrapShackPlan = "e3be0001-ce5c-4f6d-9a2e-5d4f1b7a0001";
    const string Balanced = "381b4222-f694-41f0-9685-ff5bb260df2e";

    static readonly Regex GuidRx = new(@"[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}");

    public static string? ActiveGuid()
    {
        var m = GuidRx.Match(Shell.Run("powercfg.exe", "/getactivescheme", 15_000).Output);
        return m.Success ? m.Value.ToLowerInvariant() : null;
    }

    public static bool IsUltimateActive()
    {
        var g = ActiveGuid();
        return g == UltimateTemplate || g == TrapShackPlan;
    }

    public static void Enable()
    {
        var list = Shell.Run("powercfg.exe", "/list", 15_000).Output.ToLowerInvariant();
        if (!list.Contains(TrapShackPlan))
        {
            var dup = Shell.Run("powercfg.exe", $"-duplicatescheme {UltimateTemplate} {TrapShackPlan}", 30_000);
            if (!dup.Ok)
                throw new InvalidOperationException("This PC does not offer the Ultimate Performance power plan.");
        }
        var set = Shell.Run("powercfg.exe", $"/setactive {TrapShackPlan}", 30_000);
        if (!set.Ok) throw new InvalidOperationException("Could not switch power plan: " + (set.Output + set.Error).Trim());
    }

    public static void Restore(string? originalGuid)
    {
        var target = string.IsNullOrEmpty(originalGuid) || originalGuid == TrapShackPlan ? Balanced : originalGuid;
        Shell.Run("powercfg.exe", $"/setactive {target}", 30_000);
        Shell.Run("powercfg.exe", $"/delete {TrapShackPlan}", 30_000);
    }
}
