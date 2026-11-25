using System.Text.RegularExpressions;

namespace TrapShack.Core;

/// <summary>
/// Scheduled tasks and Appx packages are slow to query one by one, so they are read in bulk once
/// (and again after we change something).
/// </summary>
public static class Caches
{
    static readonly object Gate = new();
    static Dictionary<string, string> _tasks = new(StringComparer.OrdinalIgnoreCase);
    static List<string> _appx = new();
    static Dictionary<string, string> _appxHome = new(StringComparer.OrdinalIgnoreCase);

    public static bool TasksValid { get; private set; }
    public static bool AppxValid { get; private set; }

    public static void Refresh() => Parallel.Invoke(RefreshTasks, RefreshAppx);

    public static void RefreshTasks()
    {
        try
        {
            var r = Shell.PowerShell("Get-ScheduledTask | ForEach-Object { $_.TaskPath + $_.TaskName + '|' + $_.State }");
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in r.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                var i = line.LastIndexOf('|');
                if (i > 0) map[line[..i]] = line[(i + 1)..];
            }
            lock (Gate) { _tasks = map; TasksValid = r.Ok && map.Count > 0; }
        }
        catch (Exception ex) { Log.Error("Task scan failed: " + ex.Message); lock (Gate) TasksValid = false; }
    }

    public static void RefreshAppx()
    {
        try
        {
            var r = Shell.PowerShell(
                "Get-AppxPackage -AllUsers | ForEach-Object { 'I|' + $_.Name + '|' + $_.InstallLocation }\n" +
                "Get-AppxProvisionedPackage -Online | ForEach-Object { 'P|' + $_.DisplayName }");
            var lines = r.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 2 && l[1] == '|')
                .Select(l => l[2..])
                .ToList();
            // installed packages come as "Name|InstallLocation": remember where each one lives (used for its logo)
            var homes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in lines)
            {
                var bar = l.IndexOf('|');
                if (bar > 0 && bar < l.Length - 1) homes[l[..bar]] = l[(bar + 1)..];
            }
            var names = lines.Select(l => { var bar = l.IndexOf('|'); return bar > 0 ? l[..bar] : l; })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            lock (Gate) { _appx = names; _appxHome = homes; AppxValid = r.Ok && names.Count > 0; }
        }
        catch (Exception ex) { Log.Error("App scan failed: " + ex.Message); lock (Gate) AppxValid = false; }
    }

    public static string? TaskState(string fullPath)
    {
        lock (Gate) return _tasks.TryGetValue(fullPath, out var s) ? s : null;
    }

    public static void SetTaskState(string fullPath, string state)
    {
        lock (Gate) _tasks[fullPath] = state;
    }

    /// <summary>Folder of the first installed package that matches one of the wildcard patterns, or null.</summary>
    public static string? AppxHome(IEnumerable<string> patterns)
    {
        Dictionary<string, string> snapshot;
        lock (Gate) snapshot = _appxHome;
        foreach (var p in patterns)
        {
            var rx = new Regex("^" + Regex.Escape(p).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase);
            foreach (var (name, home) in snapshot)
                if (rx.IsMatch(name)) return home;
        }
        return null;
    }

    /// <summary>True when any installed or provisioned package matches one of the wildcard patterns.</summary>
    public static bool AppxMatchesAny(IEnumerable<string> patterns)
    {
        List<string> snapshot;
        lock (Gate) snapshot = _appx;
        foreach (var p in patterns)
        {
            var rx = new Regex("^" + Regex.Escape(p).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase);
            if (snapshot.Any(n => rx.IsMatch(n))) return true;
        }
        return false;
    }
}
