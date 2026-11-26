using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using Microsoft.Win32;

namespace TrapShack.Core;

/// <summary>One program that starts with Windows.</summary>
public sealed class StartupEntry
{
    public string Name { get; init; } = "";
    public string Command { get; init; } = "";
    public string Where { get; init; } = "";          // shown to the user: "Registry (this user)", "Startup folder" ...
    public string? FilePath { get; init; }            // the program (or shortcut) on disk, for its icon and publisher
    public string Publisher { get; init; } = "";
    public bool Enabled { get; set; }
    /// <summary>How much Windows measured this program slowing startup, in milliseconds (0 = nothing recorded).</summary>
    public long DelayMs { get; set; }

    // where Windows keeps the on/off switch (the same place Task Manager's Startup tab uses)
    internal RegistryKey ApprovedRoot { get; init; } = Registry.CurrentUser;
    internal string ApprovedSub { get; init; } = "";
    internal string ApprovedName { get; init; } = "";
}

/// <summary>
/// Reads and switches startup programs exactly like Task Manager's Startup tab: the program's own entry is left
/// alone and only Windows' StartupApproved flag is changed, so switching back on restores it perfectly.
/// </summary>
public static class StartupApps
{
    const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Run32 = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    const string Approved = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

    public static List<StartupEntry> Read()
    {
        var list = new List<StartupEntry>();
        void FromKey(RegistryKey root, string sub, string where, string approvedName)
        {
            try
            {
                using var k = root.OpenSubKey(sub);
                if (k == null) return;
                foreach (var name in k.GetValueNames())
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var cmd = k.GetValue(name)?.ToString() ?? "";
                    var file = ExeFrom(cmd);
                    list.Add(new StartupEntry
                    {
                        Name = Friendly(name, file), Command = cmd, Where = where, FilePath = file, Publisher = PublisherOf(file),
                        ApprovedRoot = root, ApprovedSub = Approved + approvedName, ApprovedName = name,
                    });
                }
            }
            catch (Exception ex) { Log.Error($"Reading {sub}: {ex.Message}"); }
        }
        void FromFolder(string? folder, RegistryKey root, string where)
        {
            try
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
                foreach (var f in Directory.GetFiles(folder))
                {
                    var fn = Path.GetFileName(f);
                    if (fn.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    list.Add(new StartupEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(f), Command = f, Where = where, FilePath = f, Publisher = PublisherOf(f),
                        ApprovedRoot = root, ApprovedSub = Approved + "StartupFolder", ApprovedName = fn,
                    });
                }
            }
            catch (Exception ex) { Log.Error($"Reading {folder}: {ex.Message}"); }
        }

        FromKey(Registry.CurrentUser, Run, "Starts for you", "Run");
        FromKey(Registry.LocalMachine, Run, "Starts for everyone", "Run");
        FromKey(Registry.LocalMachine, Run32, "Starts for everyone (32-bit)", "Run32");
        FromFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Registry.CurrentUser, "Startup folder (you)");
        FromFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), Registry.LocalMachine, "Startup folder (everyone)");

        foreach (var e in list) e.Enabled = IsEnabled(e);
        ApplyMeasuredDelays(list);
        return list.OrderByDescending(e => e.Enabled).ThenByDescending(e => e.DelayMs).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Off when the first byte of Windows' StartupApproved value is odd (03); absent or even (02, 06) means on.</summary>
    static bool IsEnabled(StartupEntry e)
    {
        try
        {
            using var k = e.ApprovedRoot.OpenSubKey(e.ApprovedSub);
            return k?.GetValue(e.ApprovedName) is not byte[] b || b.Length == 0 || (b[0] & 1) == 0;
        }
        catch { return true; }
    }

    /// <summary>Switches a startup program on or off the way Task Manager does. Returns null, or what went wrong.</summary>
    public static string? SetEnabled(StartupEntry e, bool on)
    {
        try
        {
            using var k = e.ApprovedRoot.CreateSubKey(e.ApprovedSub, true);
            var data = new byte[12];
            data[0] = on ? (byte)2 : (byte)3;
            if (!on) BitConverter.GetBytes(DateTime.Now.ToFileTime()).CopyTo(data, 4);   // Task Manager stores when it was switched off
            k.SetValue(e.ApprovedName, data, RegistryValueKind.Binary);
            e.Enabled = on;
            Log.Info($"Startup {(on ? "on" : "off")}: {e.Name}");
            return null;
        }
        catch (Exception ex) { return ex.Message; }
    }

    /// <summary>How long the last startups took, as Windows measured them (null when Windows has no record).</summary>
    public static TimeSpan? LastBootTime()
    {
        try
        {
            var q = new EventLogQuery("Microsoft-Windows-Diagnostics-Performance/Operational", PathType.LogName, "*[System[EventID=100]]")
            { ReverseDirection = true };
            using var r = new EventLogReader(q);
            using var ev = r.ReadEvent();
            if (ev == null) return null;
            var xml = System.Xml.Linq.XDocument.Parse(ev.ToXml());
            var v = xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "Data" && (string?)x.Attribute("Name") == "BootTime")?.Value;
            return long.TryParse(v, out var ms) ? TimeSpan.FromMilliseconds(ms) : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Windows records apps that slowed startup (Diagnostics-Performance event 101, with the extra time in ms).
    /// The largest recorded delay per program is shown. Programs Windows never flagged show nothing.
    /// </summary>
    static void ApplyMeasuredDelays(List<StartupEntry> list)
    {
        var delays = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var q = new EventLogQuery("Microsoft-Windows-Diagnostics-Performance/Operational", PathType.LogName, "*[System[EventID=101]]")
            { ReverseDirection = true };
            using var r = new EventLogReader(q);
            for (var n = 0; n < 400; n++)
            {
                using var ev = r.ReadEvent();
                if (ev == null) break;
                var xml = System.Xml.Linq.XDocument.Parse(ev.ToXml());
                string? Get(string name) => xml.Descendants().FirstOrDefault(x => x.Name.LocalName == "Data" && (string?)x.Attribute("Name") == name)?.Value;
                var file = Get("Name") ?? Get("FileName");
                if (!long.TryParse(Get("DegradationTime"), out var ms) || string.IsNullOrEmpty(file)) continue;
                var key = Path.GetFileName(file);
                if (!delays.TryGetValue(key, out var have) || ms > have) delays[key] = ms;
            }
        }
        catch { /* the log may be off or empty: no measurements */ }
        foreach (var e in list)
            if (e.FilePath != null && delays.TryGetValue(Path.GetFileName(e.FilePath), out var ms)) e.DelayMs = ms;
    }

    // ------------------------------------------------------------------------------------------ helpers
    /// <summary>The program a startup command runs: the quoted first part, or everything up to ".exe".</summary>
    static string? ExeFrom(string cmd)
    {
        cmd = Environment.ExpandEnvironmentVariables(cmd.Trim());
        string? path = null;
        if (cmd.StartsWith('"')) { var end = cmd.IndexOf('"', 1); if (end > 1) path = cmd[1..end]; }
        else
        {
            var i = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            path = i > 0 ? cmd[..(i + 4)] : cmd.Split(' ')[0];
        }
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (File.Exists(path)) return Path.GetFullPath(path);
        // a bare name like "ctfmon.exe": look where Windows would (System32, then the PATH folders)
        if (!Path.IsPathRooted(path))
        {
            var dirs = new[] { Environment.SystemDirectory, Environment.GetFolderPath(Environment.SpecialFolder.Windows) }
                .Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries));
            foreach (var d in dirs)
            {
                try { var full = Path.Combine(d.Trim(), path); if (File.Exists(full)) return full; } catch { }
            }
        }
        return null;
    }

    static string PublisherOf(string? file)
    {
        try
        {
            if (file == null || !file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return "";
            return FileVersionInfo.GetVersionInfo(file).CompanyName?.Trim() ?? "";
        }
        catch { return ""; }
    }

    /// <summary>The program's own product name when the registry name is cryptic.</summary>
    static string Friendly(string name, string? file)
    {
        try
        {
            if (file != null && file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var d = FileVersionInfo.GetVersionInfo(file).FileDescription?.Trim();
                if (!string.IsNullOrEmpty(d) && d.Length <= 48 && !d.Contains("setup", StringComparison.OrdinalIgnoreCase)) return d;
            }
        }
        catch { }
        return name;
    }
}
