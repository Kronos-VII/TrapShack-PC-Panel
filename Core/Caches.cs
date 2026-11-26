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
            // Asking the Task Scheduler directly takes a fraction of a second; PowerShell is the fallback.
            var fast = TasksViaScheduler();
            if (fast is { Count: > 0 }) { lock (Gate) { _tasks = fast; TasksValid = true; } return; }

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

    /// <summary>Every scheduled task as "\\Path\\Name" → state, in the same words PowerShell uses (Ready, Disabled, ...).</summary>
    static Dictionary<string, string>? TasksViaScheduler()
    {
        try
        {
            var type = Type.GetTypeFromProgID("Schedule.Service");
            if (type == null) return null;
            dynamic service = Activator.CreateInstance(type)!;
            service.Connect();
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void Walk(object folderObject)
            {
                dynamic folder = folderObject;
                foreach (dynamic task in folder.GetTasks(1))   // 1 = include hidden tasks, like Get-ScheduledTask
                {
                    try
                    {
                        string path = task.Path;
                        int state = task.State;
                        map[path] = state switch { 1 => "Disabled", 2 => "Queued", 3 => "Ready", 4 => "Running", _ => "Unknown" };
                    }
                    catch { /* one task we can't read: skip it */ }
                }
                foreach (dynamic sub in folder.GetFolders(0)) Walk((object)sub);
            }
            Walk((object)service.GetFolder("\\"));
            return map;
        }
        catch (Exception ex) { Log.Error("Task Scheduler query failed, using PowerShell: " + ex.Message); return null; }
    }

    /// <summary>
    /// Apps Windows installs for every new account (provisioned), read from the registry list Windows keeps of them.
    /// The same answer as Get-AppxProvisionedPackage, without the several seconds DISM takes. Null when the list can't be read.
    /// </summary>
    static List<string>? ProvisionedViaRegistry()
    {
        try
        {
            using var b = RegUtil.Base(Microsoft.Win32.RegistryHive.LocalMachine);
            using var k = b.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications");
            var names = k?.GetSubKeyNames().Select(n => n.Split('_')[0]).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return names is { Count: > 0 } ? names : null;
        }
        catch { return null; }
    }

    public static void RefreshAppx()
    {
        try
        {
            var provisioned = ProvisionedViaRegistry();
            var r = Shell.PowerShell(
                "Get-AppxPackage -AllUsers | ForEach-Object { 'I|' + $_.Name + '|' + $_.InstallLocation }" +
                (provisioned == null ? "\nGet-AppxProvisionedPackage -Online | ForEach-Object { 'P|' + $_.DisplayName }" : ""));
            if (provisioned != null) r = r with { Output = r.Output + "\n" + string.Join("\n", provisioned.Select(p => "P|" + p)) };
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
