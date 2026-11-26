using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace TrapShack.Core;

/// <summary>One progress report from a running install.</summary>
public sealed record DlProgress(string Phase, double Fraction, long Done, long Total);

public sealed record InstallResult(bool Ok, string Message, bool Cancelled = false);

/// <summary>
/// Installs and removes apps through winget (the Windows package manager) and works out which of them are already installed.
/// Nothing here ever throws to the caller; failures come back as results.
/// </summary>
public static class WingetService
{
    // winget's own "nothing to do" codes: the package is already installed / no newer version.
    const int AlreadyInstalled = -1978335135;   // 0x8A150061
    const int NoUpdate = -1978335189;           // 0x8A15002B

    static string? _exe;

    /// <summary>Full path of winget.exe, or null when this PC does not have it.</summary>
    public static string? Exe
    {
        get
        {
            if (_exe != null) return _exe.Length == 0 ? null : _exe;
            _exe = Find() ?? "";
            return _exe.Length == 0 ? null : _exe;
        }
    }

    public static bool Available => Exe != null;

    static string? Find()
    {
        try
        {
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                try { var f = Path.Combine(dir.Trim(), "winget.exe"); if (File.Exists(f)) return f; } catch { }
            }
            var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "winget.exe");
            if (File.Exists(local)) return local;
            var pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            if (Directory.Exists(pf))
            {
                var hit = Directory.GetDirectories(pf, "Microsoft.DesktopAppInstaller_*_x64__*")
                    .Select(d => Path.Combine(d, "winget.exe")).Where(File.Exists)
                    .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if (hit != null) return hit;
            }
        }
        catch (Exception ex) { Log.Error("winget lookup failed: " + ex.Message); }
        return null;
    }

    // ------------------------------------------------------------------------------ detection

    /// <summary>Returns the ids of every catalog app that is installed on this PC right now.</summary>
    public static HashSet<string> DetectInstalled()
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var tokens = ReadWingetTokens();
            var names = ReadUninstallNames();
            var storeApps = new List<AppDef>();
            foreach (var a in AppCatalog.All)
            {
                if (IsPresent(a, tokens, names)) { found.Add(a.Id); continue; }
                // Store apps are often missing from the bulk list, so ask about them directly
                if (Available && Regex.IsMatch(a.Winget, @"^9[A-Z0-9]{11}$")) storeApps.Add(a);
            }
            // a few at a time instead of one after another: each check is its own winget run
            var extra = new System.Collections.Concurrent.ConcurrentBag<string>();
            Parallel.ForEach(storeApps, new ParallelOptions { MaxDegreeOfParallelism = 3 }, a => { if (IsInstalled(a)) extra.Add(a.Id); });
            foreach (var id in extra) found.Add(id);
        }
        catch (Exception ex) { Log.Error("Installed-app scan failed: " + ex.Message); }
        return found;
    }

    /// <summary>Checks one app again (used right after an install or uninstall).</summary>
    public static bool IsInstalled(AppDef a)
    {
        try
        {
            if (PresentByFiles(a) || PresentByRegistry(a, ReadUninstallNames())) return true;
            if (!Available) return false;
            var r = Shell.Run(Exe!, $"list --id {a.Winget} -e --accept-source-agreements --disable-interactivity", 60_000);
            return r.Ok && r.Output.IndexOf(a.Winget, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch { return false; }
    }

    static bool IsPresent(AppDef a, HashSet<string>? tokens, List<string> names)
    {
        if (PresentByFiles(a)) return true;
        if (PresentByRegistry(a, names)) return true;
        if (tokens == null) return false;
        if (tokens.Contains(a.Winget)) return true;
        // winget shortens very long ids with an ellipsis when its output is narrow.
        foreach (var t in tokens)
            if (t.Length > 8 && t.EndsWith('…') && a.Winget.StartsWith(t[..^1], StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static HashSet<string>? ReadWingetTokens()
    {
        if (!Available) return null;
        var r = Shell.Run(Exe!, "list --accept-source-agreements --disable-interactivity", 120_000);
        if (string.IsNullOrWhiteSpace(r.Output)) return null;
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in r.Output.Split('\n'))
            foreach (var t in raw.Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                set.Add(t.Trim());
        return set;
    }

    static bool PresentByFiles(AppDef a)
    {
        foreach (var p in a.Paths)
        {
            try { if (File.Exists(Environment.ExpandEnvironmentVariables(p))) return true; } catch { }
        }
        var m = Regex.Match(a.Id, @"^microsoft-dotnet-desktopruntime-(\d+)$");
        if (m.Success)
        {
            try
            {
                var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "shared", "Microsoft.WindowsDesktop.App");
                if (Directory.Exists(root) && Directory.GetDirectories(root, m.Groups[1].Value + ".*").Length > 0) return true;
            }
            catch { }
        }
        if (a.Id == "microsoft-directx")
        {
            try { return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "d3dx9_43.dll")); } catch { }
        }
        return false;
    }

    /// <summary>
    /// Where the installed program's icon lives: its own exe from the catalog paths, or the icon its uninstall entry names.
    /// Null when the app isn't found.
    /// </summary>
    public static string? IconPath(AppDef a)
    {
        foreach (var p in a.Paths)
        {
            try { var x = Environment.ExpandEnvironmentVariables(p); if (File.Exists(x) && x.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return x; } catch { }
        }
        if (string.IsNullOrEmpty(a.Reg)) return null;
        var rx = new Regex("^" + Regex.Replace(Regex.Escape(a.Reg), @"\\\{(arch|version)}", ".*") + "$", RegexOptions.IgnoreCase);
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var root = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (root == null) continue;
                    foreach (var sub in root.GetSubKeyNames())
                    {
                        try
                        {
                            using var k = root.OpenSubKey(sub);
                            if (k?.GetValue("DisplayName") is not string n || !rx.IsMatch(n.Trim())) continue;
                            var icon = (k.GetValue("DisplayIcon") as string)?.Trim();
                            if (string.IsNullOrEmpty(icon)) continue;
                            icon = icon.Split(',')[0].Trim().Trim('"');   // "C:\app.exe",0  ->  C:\app.exe
                            icon = Environment.ExpandEnvironmentVariables(icon);
                            if (File.Exists(icon)) return icon;
                        }
                        catch { }
                    }
                }
                catch { }
            }
        return null;
    }

    static List<string> ReadUninstallNames()
    {
        var list = new List<string>();
        void Scan(RegistryHive hive, RegistryView view)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var root = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (root == null) return;
                foreach (var sub in root.GetSubKeyNames())
                {
                    try
                    {
                        using var k = root.OpenSubKey(sub);
                        if (k?.GetValue("DisplayName") is string n && n.Length > 0) list.Add(n);
                    }
                    catch { }
                }
            }
            catch { }
        }
        Scan(RegistryHive.LocalMachine, RegistryView.Registry64);
        Scan(RegistryHive.LocalMachine, RegistryView.Registry32);
        Scan(RegistryHive.CurrentUser, RegistryView.Registry64);
        return list;
    }

    static bool PresentByRegistry(AppDef a, List<string> names)
    {
        if (string.IsNullOrEmpty(a.Reg)) return false;
        var arch = a.Id.EndsWith("-x64") ? "x64" : a.Id.EndsWith("-x86") ? "x86" : null;
        var sb = new StringBuilder("^");
        foreach (var part in Regex.Split(a.Reg, @"(\{arch\}|\{version\})"))
        {
            if (part == "{arch}") sb.Append(arch ?? "[^)]*");
            else if (part == "{version}") sb.Append(".*");
            else sb.Append(Regex.Escape(part));
        }
        sb.Append('$');
        var rx = new Regex(sb.ToString(), RegexOptions.IgnoreCase);
        return names.Any(n => rx.IsMatch(n.Trim()));
    }

    // ------------------------------------------------------------------------------ install / remove

    /// <summary>Makes sure winget's package list is fresh. Quiet and harmless when it is already fine.</summary>
    public static void RefreshSources()
    {
        if (!Available) return;
        try { Shell.Run(Exe!, "source update --disable-interactivity", 90_000); } catch { }
    }

    public static async Task<InstallResult> InstallAsync(AppDef a, IProgress<DlProgress> progress, CancellationToken ct)
    {
        var args = $"install --id {a.Winget} -e{SourceFlag(a)} --silent --accept-package-agreements --accept-source-agreements --disable-interactivity";
        var last = "";
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            if (ct.IsCancellationRequested) return new InstallResult(false, "Stopped", true);
            var (code, text, cancelled) = await RunStreamingAsync(args, progress, ct);
            if (cancelled) return new InstallResult(false, "Stopped", true);
            if (code == 0 || code == AlreadyInstalled || code == NoUpdate) return new InstallResult(true, "Installed");
            last = Summarise(text, code);
            Log.Error($"winget install {a.Winget} attempt {attempt} exited {code}: {last}");
            // Some installers report an odd code but the app is there.
            if (await Task.Run(() => IsInstalled(a))) return new InstallResult(true, "Installed");
            if (attempt == 1)
            {
                progress.Report(new DlProgress("Retrying…", 0, 0, 0));
                await Task.Delay(2500, CancellationToken.None);
            }
        }
        return new InstallResult(false, last);
    }

    public static async Task<InstallResult> UninstallAsync(AppDef a, IProgress<DlProgress> progress, CancellationToken ct)
    {
        var args = $"uninstall --id {a.Winget} -e{SourceFlag(a)} --silent --accept-source-agreements --disable-interactivity";
        var (code, text, cancelled) = await RunStreamingAsync(args, progress, ct);
        if (cancelled) return new InstallResult(false, "Stopped", true);
        if (code == 0) return new InstallResult(true, "Removed");
        if (!await Task.Run(() => IsInstalled(a))) return new InstallResult(true, "Removed");
        Log.Error($"winget uninstall {a.Winget} exited {code}: {Summarise(text, code)}");
        return new InstallResult(false, Summarise(text, code));
    }

    /// <summary>Microsoft Store apps are identified by a 12-character id such as 9P1NXTW0PPMJ and come from the msstore source.</summary>
    static string SourceFlag(AppDef a) => Regex.IsMatch(a.Winget, @"^9[A-Z0-9]{11}$") ? " --source msstore" : "";

    static string Summarise(string text, int code)
    {
        var line = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim()).LastOrDefault(l => l.Length > 3 && !l.All(c => "-\\|/ ".Contains(c)) && !l.Contains('█') && !l.Contains('▒'));
        return string.IsNullOrEmpty(line) ? $"winget stopped with code {code}" : line!;
    }

    static readonly Regex SizeRx = new(@"([\d.,]+)\s*(B|KB|MB|GB)\s*/\s*([\d.,]+)\s*(B|KB|MB|GB)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static long ToBytes(string num, string unit)
    {
        if (!double.TryParse(num.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) return 0;
        return (long)(v * unit.ToUpperInvariant() switch { "KB" => 1024.0, "MB" => 1048576.0, "GB" => 1073741824.0, _ => 1.0 });
    }

    static async Task<(int Code, string Text, bool Cancelled)> RunStreamingAsync(string args, IProgress<DlProgress> progress, CancellationToken ct)
    {
        if (Exe == null) return (-1, "winget is not available on this PC", false);
        var psi = new ProcessStartInfo(Exe, args)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
        };
        Process? p = null;
        var all = new StringBuilder();
        try
        {
            p = Process.Start(psi);
            if (p == null) return (-1, "Could not start winget", false);
            using var reg = ct.Register(() => { try { p.Kill(true); } catch { } });
            progress.Report(new DlProgress("Preparing…", 0, 0, 0));

            async Task Pump(StreamReader rd)
            {
                var buf = new char[512];
                var line = new StringBuilder();
                int n;
                while ((n = await rd.ReadAsync(buf, 0, buf.Length)) > 0)
                {
                    for (var i = 0; i < n; i++)
                    {
                        var c = buf[i];
                        if (c == '\r' || c == '\n')
                        {
                            if (line.Length > 0) { var s = line.ToString(); line.Clear(); lock (all) all.AppendLine(s); Handle(s, progress); }
                        }
                        else line.Append(c);
                    }
                }
                if (line.Length > 0) { var s = line.ToString(); lock (all) all.AppendLine(s); Handle(s, progress); }
            }

            await Task.WhenAll(Pump(p.StandardOutput), Pump(p.StandardError));
            await p.WaitForExitAsync(CancellationToken.None);
            if (ct.IsCancellationRequested) return (-1, "", true);
            return (p.ExitCode, all.ToString(), false);
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested) return (-1, "", true);
            return (-1, ex.Message, false);
        }
        finally { p?.Dispose(); }
    }

    static void Handle(string line, IProgress<DlProgress> progress)
    {
        var m = SizeRx.Match(line);
        if (m.Success)
        {
            var done = ToBytes(m.Groups[1].Value, m.Groups[2].Value);
            var total = ToBytes(m.Groups[3].Value, m.Groups[4].Value);
            progress.Report(new DlProgress("Downloading", total > 0 ? Math.Min(1.0, (double)done / total) : 0, done, total));
            return;
        }
        if (line.Contains("Starting package install", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Starting package uninstall", StringComparison.OrdinalIgnoreCase))
            progress.Report(new DlProgress("Installing", 1, 0, 0));
        else if (line.Contains("Successfully verified", StringComparison.OrdinalIgnoreCase))
            progress.Report(new DlProgress("Verified", 1, 0, 0));
        else if (line.Contains("Found ", StringComparison.OrdinalIgnoreCase))
            progress.Report(new DlProgress("Starting download", 0, 0, 0));
    }

    public static string FormatMb(long bytes) =>
        bytes >= 1073741824 ? (bytes / 1073741824.0).ToString("0.0") + " GB" :
        bytes >= 1048576 ? (bytes / 1048576.0).ToString("0") + " MB" : (bytes / 1024.0).ToString("0") + " KB";

    /// <summary>"48 of 120 MB": both sizes in the unit of the total.</summary>
    public static string FormatPair(long done, long total)
    {
        if (total >= 1073741824) return $"{done / 1073741824.0:0.0} of {total / 1073741824.0:0.0} GB";
        if (total >= 1048576) return $"{done / 1048576.0:0} of {total / 1048576.0:0} MB";
        return $"{done / 1024.0:0} of {total / 1024.0:0} KB";
    }
}
