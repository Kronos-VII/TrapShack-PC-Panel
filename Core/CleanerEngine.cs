// The PC Cleaner engine. Adapted from FluentCleaner (MIT licence, Copyright (c) 2026 A Belim app creation), which
// reads cleaning rules in the winapp2.ini format. The rules file (Assets/Cleaner/winapp2.ini) comes from the winapp2
// project and is licensed CC BY-SA 4.0; see Assets/Cleaner/LICENSES.txt.
using System.Collections.Concurrent;
using System.IO;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace TrapShack.Core;

public enum CleanFlag { None, Recurse, RemoveSelf }
public enum ExcludeKind { File, Path, Reg }

public sealed record CleanFileKey(string Path, string Pattern, CleanFlag Flag);
public sealed record CleanRegKey(string KeyPath, string? ValueName);
public sealed record CleanExclude(ExcludeKind Kind, string Path, string? Pattern);

/// <summary>One cleaning rule ("Windows Temporary Files", "Google Chrome Cache" ...).</summary>
public sealed class CleanRule
{
    public string Name { get; set; } = "";
    public string? Section { get; set; }
    public int? LangSecRef { get; set; }
    public List<string> DetectKeys { get; } = new();
    public List<string> DetectFiles { get; } = new();
    public string? SpecialDetect { get; set; }
    public List<CleanFileKey> FileKeys { get; } = new();
    public List<CleanRegKey> RegKeys { get; } = new();
    public List<CleanExclude> Excludes { get; } = new();
    public string? Warning { get; set; }
    public bool Default { get; set; } = true;
    /// <summary>
    /// GPU and game shader caches. Not ticked by default: deleting them makes games rebuild their shaders, which
    /// stutters for a while, so they are only cleaned when you tick them yourself.
    /// </summary>
    public bool ShaderCache { get; set; }
}

/// <summary>What one rule found during a scan.</summary>
public sealed class CleanScan
{
    public CleanRule Rule { get; init; } = null!;
    public List<string> Files { get; } = new();
    public List<(string Key, string? Value)> Registry { get; } = new();
    public long Bytes { get; set; }
    public int Count => Files.Count + Registry.Count;

    /// <summary>Three significant figures throughout, so every size in a list lines up: 5.0 MB, 36.4 MB, 186 MB, 1.24 GB.</summary>
    public static string Format(long bytes)
    {
        static string Fig(double v, string unit) => (v < 100 ? v.ToString("0.0") : v.ToString("0")) + " " + unit;
        return bytes < 1024 ? $"{bytes} B"
             : bytes < 1024L * 1024 ? Fig(bytes / 1024.0, "KB")
             : bytes < 1024L * 1024 * 1024 ? Fig(bytes / (1024.0 * 1024), "MB")
             : $"{bytes / (1024.0 * 1024 * 1024):0.00} GB";
    }
}

public static class CleanerEngine
{
    // ------------------------------------------------------------------------------------------ rules file
    static readonly Regex RxFileKey = new(@"^FileKey\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex RxRegKey = new(@"^RegKey\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex RxExclude = new(@"^ExcludeKey\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex RxDetect = new(@"^Detect\d*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex RxDetectFile = new(@"^DetectFile\d*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Reads the bundled rules (embedded in the program, so the single-file build carries them).</summary>
    public static List<CleanRule> LoadBundledRules()
    {
        var asm = typeof(CleanerEngine).Assembly;
        var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("winapp2.ini", StringComparison.OrdinalIgnoreCase));
        if (name == null) return new();
        using var s = asm.GetManifestResourceStream(name)!;
        using var r = new StreamReader(s);
        // only the safe set: on by default and no warning (the bundled file is already trimmed; this keeps it that way)
        return SplitShaderCaches(Parse(r.ReadToEnd()).Where(e => e.Default && e.Warning == null).ToList());
    }

    // The folders where graphics drivers, DirectX, Steam and games keep compiled shaders. Browser "shader-cache"
    // folders are not included: those rebuild instantly and never cause stutter.
    static readonly Regex RxShaderPath = new(
        @"dxcache|glcache|vkcache|dxccache|nv_cache|computecache|d3dscache|shadercache|shader_cache|\\shaders\\(cache|generated)|\\shaders$|\\amd\\\*cache|\\nvidia\\\*cache|\\nvidia\\perdriverversion",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static bool IsShaderKey(CleanFileKey k) =>
        RxShaderPath.IsMatch(k.Path) || k.Pattern.EndsWith(".upipelinecache", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gives shader caches their own rules so they can stay unticked: a rule that is only shader caches is marked as
    /// one, and a rule that mixes them with other files ("Steam Games", "AMD") is split in two.
    /// </summary>
    static List<CleanRule> SplitShaderCaches(List<CleanRule> rules)
    {
        var result = new List<CleanRule>(rules.Count + 16);
        foreach (var r in rules)
        {
            var shader = r.FileKeys.Where(IsShaderKey).ToList();
            if (shader.Count == 0) { result.Add(r); continue; }
            if (shader.Count == r.FileKeys.Count && r.RegKeys.Count == 0) { r.ShaderCache = true; result.Add(r); continue; }

            var part = new CleanRule
            {
                Name = r.Name + " Shader Cache", Section = r.Section, LangSecRef = r.LangSecRef,
                SpecialDetect = r.SpecialDetect, ShaderCache = true,
            };
            part.DetectKeys.AddRange(r.DetectKeys);
            part.DetectFiles.AddRange(r.DetectFiles);
            part.FileKeys.AddRange(shader);
            part.Excludes.AddRange(r.Excludes);
            r.FileKeys.RemoveAll(IsShaderKey);
            result.Add(r);
            result.Add(part);
        }
        return result;
    }

    public static List<CleanRule> Parse(string content)
    {
        var list = new List<CleanRule>();
        CleanRule? cur = null;
        foreach (var raw in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                if (cur != null && Valid(cur)) list.Add(cur);
                var name = line[1..^1].Trim();
                cur = name.StartsWith("Winapp2", StringComparison.OrdinalIgnoreCase) || name.StartsWith("version", StringComparison.OrdinalIgnoreCase)
                    ? null : new CleanRule { Name = name.TrimEnd('*').TrimEnd() };
                continue;
            }
            if (cur == null) continue;
            var eq = line.IndexOf('=');
            if (eq < 0) continue;
            var key = line[..eq].Trim();
            var val = line[(eq + 1)..].Trim();
            if (val.Length == 0) continue;
            if (key.Equals("LangSecRef", StringComparison.OrdinalIgnoreCase)) { if (int.TryParse(val, out var n)) cur.LangSecRef = n; }
            else if (key.Equals("Section", StringComparison.OrdinalIgnoreCase)) cur.Section = val;
            else if (key.Equals("SpecialDetect", StringComparison.OrdinalIgnoreCase)) cur.SpecialDetect = val;
            else if (key.Equals("Warning", StringComparison.OrdinalIgnoreCase)) cur.Warning = val;
            else if (key.Equals("Default", StringComparison.OrdinalIgnoreCase)) cur.Default = val.Equals("True", StringComparison.OrdinalIgnoreCase);
            else if (RxDetect.IsMatch(key)) cur.DetectKeys.Add(val);
            else if (RxDetectFile.IsMatch(key)) cur.DetectFiles.Add(val);
            else if (RxFileKey.IsMatch(key)) cur.FileKeys.Add(ParseFileKey(val));
            else if (RxRegKey.IsMatch(key)) { var i = val.IndexOf('|'); cur.RegKeys.Add(i >= 0 ? new(val[..i].Trim(), val[(i + 1)..].Trim()) : new(val.Trim(), null)); }
            else if (RxExclude.IsMatch(key)) cur.Excludes.Add(ParseExclude(val));
        }
        if (cur != null && Valid(cur)) list.Add(cur);
        return list;
    }

    static bool Valid(CleanRule e) =>
        (e.DetectKeys.Count > 0 || e.DetectFiles.Count > 0 || e.SpecialDetect != null) && (e.FileKeys.Count > 0 || e.RegKeys.Count > 0);

    static CleanFileKey ParseFileKey(string v)
    {
        var p = v.Split('|');
        string pattern = "*.*"; var flag = CleanFlag.None;
        if (p.Length == 2)
        {
            var u = p[1].Trim().ToUpperInvariant();
            if (u is "RECURSE" or "REMOVESELF") flag = u == "RECURSE" ? CleanFlag.Recurse : CleanFlag.RemoveSelf;
            else pattern = p[1].Trim();
        }
        else if (p.Length > 2)
        {
            pattern = p[1].Trim();
            flag = p[2].Trim().ToUpperInvariant() switch { "RECURSE" => CleanFlag.Recurse, "REMOVESELF" => CleanFlag.RemoveSelf, _ => CleanFlag.None };
        }
        return new(p[0].Trim(), pattern, flag);
    }

    static CleanExclude ParseExclude(string v)
    {
        var p = v.Split('|');
        var kind = p[0].Trim().ToUpperInvariant() switch { "PATH" => ExcludeKind.Path, "REG" => ExcludeKind.Reg, _ => ExcludeKind.File };
        return new(kind, p.Length > 1 ? p[1].Trim() : "", p.Length > 2 ? p[2].Trim() : null);
    }

    // ------------------------------------------------------------------------------------------ categories
    static readonly Dictionary<int, (string Name, int Order)> Categories = new()
    {
        [3025] = ("Windows", 1), [3006] = ("Microsoft Edge", 5), [3021] = ("Applications", 10), [3022] = ("Internet", 20),
        [3023] = ("Multimedia", 30), [3024] = ("Utilities", 40), [3026] = ("Firefox", 60), [3027] = ("Opera", 70),
        [3028] = ("Safari", 80), [3029] = ("Google Chrome", 90), [3030] = ("Thunderbird", 100), [3031] = ("Microsoft Store", 110),
        [3033] = ("Vivaldi", 130), [3034] = ("Brave", 140), [3035] = ("Opera GX", 150), [3036] = ("Spotify", 160),
        [3037] = ("Avast Secure Browser", 170), [3038] = ("AVG Secure Browser", 180), [3039] = ("Arc Browser", 190),
        [3040] = ("iTunes", 200), [3042] = ("WhatsApp", 210), [3043] = ("Norton Private Browser", 220), [3044] = ("Avira Secure Browser", 230),
    };

    public static (string Name, int Order) CategoryOf(CleanRule r) =>
        r.LangSecRef is int c && Categories.TryGetValue(c, out var cat) ? cat
        : !string.IsNullOrWhiteSpace(r.Section) ? (r.Section!, 1000) : ("Other applications", 2000);

    // ------------------------------------------------------------------------------------------ paths
    static readonly Dictionary<string, string> Vars = BuildVars();

    static Dictionary<string, string> BuildVars()
    {
        var m = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Add(string n, string? v) { if (!string.IsNullOrEmpty(v)) m[$"%{n}%"] = v.TrimEnd('\\', '/'); }
        string F(Environment.SpecialFolder f) => Environment.GetFolderPath(f);
        Add("AppData", F(Environment.SpecialFolder.ApplicationData));
        Add("LocalAppData", F(Environment.SpecialFolder.LocalApplicationData));
        Add("LocalLowAppData", Path.GetFullPath(Path.Combine(F(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow")));
        Add("ProgramFiles", F(Environment.SpecialFolder.ProgramFiles));
        Add("ProgramFiles(x86)", F(Environment.SpecialFolder.ProgramFilesX86));
        Add("ProgramFilesX86", F(Environment.SpecialFolder.ProgramFilesX86));
        Add("ProgramData", F(Environment.SpecialFolder.CommonApplicationData));
        Add("CommonAppData", F(Environment.SpecialFolder.CommonApplicationData));
        Add("UserProfile", F(Environment.SpecialFolder.UserProfile));
        Add("Documents", F(Environment.SpecialFolder.MyDocuments));
        Add("Desktop", F(Environment.SpecialFolder.DesktopDirectory));
        Add("Music", F(Environment.SpecialFolder.MyMusic));
        Add("Pictures", F(Environment.SpecialFolder.MyPictures));
        Add("Videos", F(Environment.SpecialFolder.MyVideos));
        Add("SystemRoot", F(Environment.SpecialFolder.Windows));
        Add("WinDir", F(Environment.SpecialFolder.Windows));
        Add("System", F(Environment.SpecialFolder.System));
        Add("SystemX86", F(Environment.SpecialFolder.SystemX86));
        Add("Temp", Path.GetTempPath());
        Add("Tmp", Path.GetTempPath());
        Add("SystemDrive", Path.GetPathRoot(F(Environment.SpecialFolder.Windows))?.TrimEnd('\\') ?? "C:");
        return m;
    }

    static string Expand(string path)
    {
        foreach (var (k, v) in Vars)
        {
            var i = path.IndexOf(k, StringComparison.OrdinalIgnoreCase);
            while (i >= 0) { path = path[..i] + v + path[(i + k.Length)..]; i = path.IndexOf(k, i + v.Length, StringComparison.OrdinalIgnoreCase); }
        }
        path = Environment.ExpandEnvironmentVariables(path);
        if (path.Length == 2 && char.IsLetter(path[0]) && path[1] == ':') path += "\\";   // "C:" alone would mean the current folder
        return path;
    }

    /// <summary>Every real path a rule path points to (wildcards in folder names are walked; %ProgramFiles% also checks the x86 folder).</summary>
    public static List<string> Resolve(string raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Walk(Expand(raw), set);
        if (raw.Contains("%ProgramFiles%", StringComparison.OrdinalIgnoreCase))
            Walk(Expand(Regex.Replace(raw, "%ProgramFiles%", "%ProgramFiles(x86)%", RegexOptions.IgnoreCase)), set);
        return set.ToList();
    }

    static void Walk(string path, HashSet<string> results)
    {
        var parts = path.Split('\\', '/');
        var wc = Array.FindIndex(parts, p => p.Contains('*') || p.Contains('?'));
        if (wc < 0) { results.Add(path); return; }
        var basePath = wc == 0 ? Path.GetPathRoot(path) ?? "" : string.Join("\\", parts.Take(wc));
        if (basePath.Length == 2 && basePath[1] == ':') basePath += "\\";
        if (!Directory.Exists(basePath)) return;
        var rest = parts.Skip(wc + 1).ToArray();
        try
        {
            var hits = rest.Length == 0 ? Directory.GetFileSystemEntries(basePath, parts[wc]) : Directory.GetDirectories(basePath, parts[wc]);
            foreach (var h in hits)
                if (rest.Length == 0) results.Add(h);
                else Walk(Path.Combine(h, string.Join("\\", rest)), results);
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    // ------------------------------------------------------------------------------------------ detection
    /// <summary>Is the program this rule cleans actually on this PC? Results are cached for one scan.</summary>
    public sealed class Detector
    {
        readonly ConcurrentDictionary<string, bool> _reg = new(StringComparer.OrdinalIgnoreCase);
        readonly ConcurrentDictionary<string, bool> _file = new(StringComparer.OrdinalIgnoreCase);

        public bool IsInstalled(CleanRule r)
        {
            if (r.SpecialDetect != null && Special(r.SpecialDetect, out var known)) return known;
            foreach (var k in r.DetectKeys) if (_reg.GetOrAdd(k, RegExists)) return true;
            foreach (var f in r.DetectFiles) if (FileExists(f)) return true;
            return false;
        }

        bool FileExists(string raw) => _file.GetOrAdd(raw, p =>
        {
            try { return Resolve(p).Any(x => File.Exists(x) || Directory.Exists(x)); } catch { return false; }
        });

        bool Special(string code, out bool result)
        {
            switch (code.ToUpperInvariant())
            {
                case "DET_CHROME": result = FileExists(@"%LocalAppData%\Google\Chrome\User Data"); return true;
                case "DET_FIREFOX": result = FileExists(@"%AppData%\Mozilla\Firefox"); return true;
                case "DET_IE": result = RegExists(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\IEXPLORE.EXE"); return true;
                case "DET_THUNDERBIRD": result = FileExists(@"%AppData%\Thunderbird"); return true;
                case "DET_OPERA": result = FileExists(@"%AppData%\Opera Software\Opera Stable"); return true;
                case "DET_EDGE": result = FileExists(@"%LocalAppData%\Microsoft\Edge\User Data"); return true;
                case "DET_WINSTORE": result = FileExists(@"%LocalAppData%\Packages"); return true;
                default: result = false; return false;
            }
        }

        static bool RegExists(string path)
        {
            try
            {
                string? value = null;
                var bar = path.LastIndexOf('|');
                if (bar >= 0) { value = path[(bar + 1)..]; path = path[..bar]; }
                var (hive, sub) = SplitHive(path);
                using var k = Hive(hive)?.OpenSubKey(sub, false);
                return k != null && (value == null || k.GetValue(value) != null);
            }
            catch { return false; }
        }
    }

    // ------------------------------------------------------------------------------------------ scan (reads only)
    public static CleanScan Scan(CleanRule rule, CancellationToken token)
    {
        var res = new CleanScan { Rule = rule };
        var excl = FileExclusions(rule);
        var regExcl = RegExclusions(rule);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fk in rule.FileKeys)
        {
            try
            {
                var recurse = fk.Flag != CleanFlag.None;
                var patterns = fk.Pattern.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var dir in Resolve(fk.Path))
                {
                    if (!Directory.Exists(dir)) continue;
                    foreach (var f in Files(dir, patterns, excl, recurse, token))
                    {
                        if (IsExcluded(f, excl) || IsProtected(f) || !seen.Add(f)) continue;
                        var size = DeletableSize(f);
                        if (size < 0) continue;   // in use: it could not be deleted anyway
                        res.Files.Add(f);
                        res.Bytes += size;
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }
        foreach (var rk in rule.RegKeys)
        {
            try
            {
                if (RegExcluded(rk.KeyPath, regExcl)) continue;
                var (hive, sub) = SplitHive(rk.KeyPath);
                using var k = Hive(hive)?.OpenSubKey(sub, false);
                if (k == null) continue;
                if (rk.ValueName == null || k.GetValue(rk.ValueName) != null) res.Registry.Add((rk.KeyPath, rk.ValueName));
            }
            catch { }
        }
        return res;
    }

    static IEnumerable<string> Files(string root, string[] patterns, List<(string Dir, string? Pattern)> excl, bool recurse, CancellationToken token)
    {
        var scanRoot = root.TrimEnd('\\') + "\\";
        if (excl.Any(r => r.Pattern == null && scanRoot.StartsWith(r.Dir, StringComparison.OrdinalIgnoreCase))) yield break;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in patterns)
        {
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(root, p).ToList(); } catch { files = Array.Empty<string>(); }
            foreach (var f in files) if (seen.Add(f)) yield return f;
        }
        if (!recurse) yield break;
        List<string> dirs;
        try { dirs = Directory.EnumerateDirectories(root).Where(d => (File.GetAttributes(d) & FileAttributes.ReparsePoint) == 0).ToList(); }
        catch { yield break; }
        foreach (var d in dirs)
        {
            token.ThrowIfCancellationRequested();
            foreach (var f in Files(d, patterns, excl, true, token)) yield return f;
        }
    }

    // ------------------------------------------------------------------------------------------ clean (deletes)
    public static (int Count, long Bytes) Clean(CleanScan scan, CancellationToken token)
    {
        int count = 0; long bytes = 0;
        var regExcl = RegExclusions(scan.Rule);
        foreach (var f in scan.Files)
        {
            token.ThrowIfCancellationRequested();
            try { var size = new FileInfo(f).Length; File.Delete(f); count++; bytes += size; }
            catch { }   // in use or already gone
        }
        foreach (var (key, value) in scan.Registry)
        {
            try { if (DeleteReg(key, value, regExcl)) count++; } catch { }
        }
        foreach (var fk in scan.Rule.FileKeys.Where(k => k.Flag == CleanFlag.RemoveSelf))
            foreach (var dir in Resolve(fk.Path)) PruneEmpty(dir);
        if (scan.Files.Any(f => f.Contains(@"\$Recycle.Bin\", StringComparison.OrdinalIgnoreCase)))
            try { SHUpdateRecycleBinIcon(); } catch { }
        return (count, bytes);
    }

    static bool DeleteReg(string keyPath, string? value, List<string> excl)
    {
        var norm = NormReg(keyPath);
        if (RegExcluded(norm, excl)) return false;
        var (hive, sub) = SplitHive(keyPath);
        var root = Hive(hive);
        if (root == null) return false;
        if (value != null)
        {
            using var k = root.OpenSubKey(sub, true);
            if (k == null) return false;
            k.DeleteValue(value, false);
            return true;
        }
        var keep = excl.Where(e => SameOrChild(e, norm)).ToList();
        if (keep.Count == 0)
        {
            var parent = Path.GetDirectoryName(sub) ?? "";
            using var p = root.OpenSubKey(parent, true);
            if (p == null) return false;
            p.DeleteSubKeyTree(Path.GetFileName(sub), false);
            return true;
        }
        using var t = root.OpenSubKey(sub, true);
        if (t == null) return false;
        DeleteTreeExcept(t, norm, keep);
        return true;
    }

    static void DeleteTreeExcept(RegistryKey key, string path, List<string> keep)
    {
        foreach (var v in key.GetValueNames()) key.DeleteValue(v, false);
        foreach (var name in key.GetSubKeyNames())
        {
            var child = path + "\\" + name;
            if (RegExcluded(child, keep)) continue;
            if (keep.Any(k => SameOrChild(k, child)))
            {
                using var c = key.OpenSubKey(name, true);
                if (c != null) DeleteTreeExcept(c, child, keep);
            }
            else key.DeleteSubKeyTree(name, false);
        }
    }

    static void PruneEmpty(string path)
    {
        if (!Directory.Exists(path)) return;
        try
        {
            foreach (var d in Directory.GetDirectories(path, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                if (Directory.GetFileSystemEntries(d).Length == 0) Directory.Delete(d);
            if (Directory.GetFileSystemEntries(path).Length == 0) Directory.Delete(path);
        }
        catch { }
    }

    // ------------------------------------------------------------------------------------------ helpers
    static List<(string Dir, string? Pattern)> FileExclusions(CleanRule r)
    {
        var list = new List<(string, string?)>();
        foreach (var e in r.Excludes.Where(e => e.Kind != ExcludeKind.Reg))
            foreach (var p in Resolve(e.Path)) list.Add((p.TrimEnd('\\') + "\\", e.Pattern));
        return list;
    }

    static List<string> RegExclusions(CleanRule r) =>
        r.Excludes.Where(e => e.Kind == ExcludeKind.Reg).Select(e => NormReg(e.Path)).Where(p => p.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    static bool IsExcluded(string file, List<(string Dir, string? Pattern)> rules)
    {
        foreach (var (dir, pattern) in rules)
        {
            if (!file.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) continue;
            if (pattern == null) return true;
            if (pattern.Contains('*') || pattern.Contains('?'))
            { if (FileSystemName.MatchesSimpleExpression(pattern, Path.GetFileName(file), true)) return true; }
            else if (file[dir.Length..].Equals(pattern, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>Paths never deleted whatever a rule says: browser extension storage breaks extensions when wiped.</summary>
    static readonly string[] Protected = { @"\IndexedDB\chrome-extension_" };
    static bool IsProtected(string path) => Protected.Any(s => path.Contains(s, StringComparison.OrdinalIgnoreCase));

    static bool RegExcluded(string path, List<string> excl) { var n = NormReg(path); return excl.Any(e => SameOrChild(n, e)); }
    static bool SameOrChild(string path, string parent) =>
        path.Equals(parent, StringComparison.OrdinalIgnoreCase) || path.StartsWith(parent + "\\", StringComparison.OrdinalIgnoreCase);

    static string NormReg(string path)
    {
        var (hive, sub) = SplitHive(path.Trim().TrimEnd('\\'));
        hive = hive switch
        {
            "HKEY_CURRENT_USER" => "HKCU", "HKEY_LOCAL_MACHINE" => "HKLM", "HKEY_USERS" => "HKU",
            "HKEY_CURRENT_CONFIG" => "HKCC", "HKEY_CLASSES_ROOT" => "HKCR", _ => hive,
        };
        return sub.Length == 0 ? hive : hive + "\\" + sub.Trim('\\');
    }

    static (string Hive, string Sub) SplitHive(string path)
    {
        var i = path.IndexOf('\\');
        return i < 0 ? (path.ToUpperInvariant(), "") : (path[..i].ToUpperInvariant(), path[(i + 1)..]);
    }

    static RegistryKey? Hive(string hive) => hive switch
    {
        "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
        "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
        "HKU" or "HKEY_USERS" => Registry.Users,
        "HKCC" or "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
        "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
        _ => null,
    };

    /// <summary>Size of a file, or -1 when another program holds it open without allowing delete (it would fail anyway).</summary>
    static long DeletableSize(string path)
    {
        const uint DELETE = 0x00010000, SHARE_ALL = 0x7, OPEN_EXISTING = 3;
        using var h = CreateFileW(path, DELETE, SHARE_ALL, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (h.IsInvalid) return -1;
        try { return new FileInfo(path).Length; } catch { return -1; }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr template);

    [DllImport("shell32.dll")]
    static extern void SHUpdateRecycleBinIcon();
}
