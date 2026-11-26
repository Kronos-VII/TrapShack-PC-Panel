using System.Text;
using Microsoft.Win32;

namespace TrapShack.Core;

/// <summary>
/// One reversible change. Every action can: describe itself, say whether it is applicable on this PC,
/// detect whether it is currently applied, capture the original state, apply, and revert.
/// </summary>
public abstract class TweakAction
{
    public abstract string Describe();
    public virtual bool IsApplicable() => true;
    public abstract bool IsApplied();
    /// <summary>Returns the original state as text (stored so Revert can restore it exactly).</summary>
    public abstract string? Capture();
    public abstract void Apply();
    /// <param name="saved">What Capture returned before the tweak was applied, or null if unknown.</param>
    public abstract void Revert(string? saved);
    /// <summary>
    /// What the self-test compares before applying and after undoing: the setting itself, without things that change on
    /// their own (whether a service happens to be running, whether a task is "Ready" or "Running" right now).
    /// </summary>
    public virtual string? TestState() => Capture();
}

// ---------------------------------------------------------------------------------------------
// Registry
// ---------------------------------------------------------------------------------------------

public sealed class RegistryAction : TweakAction
{
    readonly RegistryHive _hive;
    readonly string _path;
    readonly string _name;
    readonly RegistryValueKind _kind;
    readonly Func<object> _value;
    readonly object? _windowsDefault;
    readonly Func<object?, bool>? _matcher;

    /// <param name="windowsDefault">
    /// Value to restore if no backup exists (for example the tweak was applied by another tool).
    /// Null means "delete the value", which is the Windows default for policy keys.
    /// </param>
    /// <param name="matcher">
    /// Optional smarter "is this already applied?" test, for values where several numbers mean the same thing.
    /// </param>
    public RegistryAction(RegistryHive hive, string path, string name, object value,
        RegistryValueKind kind = RegistryValueKind.DWord, object? windowsDefault = null,
        Func<object?, bool>? matcher = null)
        : this(hive, path, name, () => value, kind, windowsDefault, matcher) { }

    public RegistryAction(RegistryHive hive, string path, string name, Func<object> value,
        RegistryValueKind kind = RegistryValueKind.DWord, object? windowsDefault = null,
        Func<object?, bool>? matcher = null)
    {
        _hive = hive; _path = path; _name = name; _value = value; _kind = kind;
        _windowsDefault = windowsDefault; _matcher = matcher;
    }

    string HiveName => _hive switch { RegistryHive.LocalMachine => "HKLM", RegistryHive.Users => "HKU", _ => "HKCU" };

    /// <summary>Which registry value this writes, and to what: used to find settings that would fight each other.</summary>
    public string ValueKey => $"{HiveName}\\{_path}|{_name}".ToLowerInvariant();
    public string TargetText => Display(_value());

    public bool IsMachinePolicy =>
        _hive == RegistryHive.LocalMachine && _path.Contains("\\Policies\\", StringComparison.OrdinalIgnoreCase);

    public override string Describe() => $"{HiveName}\\{_path}  →  {_name} = {Display(_value())}";

    static string Display(object v) => v is byte[] b ? BitConverter.ToString(b).Replace('-', ' ') : v.ToString() ?? "";

    public override bool IsApplied()
    {
        var current = RegUtil.Read(_hive, _path, _name);
        return _matcher != null ? _matcher(current) : Same(current, _value());
    }

    static bool Same(object? a, object b)
    {
        try
        {
            return (a, b) switch
            {
                (null, _) => false,
                (byte[] x, byte[] y) => x.AsSpan().SequenceEqual(y),
                (string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase),
                (int x, int y) => x == y,
                _ => false,
            };
        }
        catch { return false; }
    }

    public override string? Capture()
    {
        using var b = RegUtil.Base(_hive);
        using var k = b.OpenSubKey(_path);
        if (k == null || !k.GetValueNames().Any(n => string.Equals(n, _name, StringComparison.OrdinalIgnoreCase)))
            return "absent";

        var kind = k.GetValueKind(_name);
        var v = k.GetValue(_name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        var payload = v switch
        {
            byte[] bytes => Convert.ToBase64String(bytes),
            string[] multi => string.Join("\n", multi),
            _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "",
        };
        return $"{kind}|{payload}";
    }

    public override void Apply()
    {
        using var b = RegUtil.Base(_hive);
        using var k = b.CreateSubKey(_path, true) ?? throw new InvalidOperationException($"Cannot open {HiveName}\\{_path}");
        k.SetValue(_name, _value(), _kind);
    }

    public override void Revert(string? saved)
    {
        using var b = RegUtil.Base(_hive);

        if (saved == null)
        {
            if (_windowsDefault != null)
            {
                using var kd = b.CreateSubKey(_path, true);
                kd?.SetValue(_name, _windowsDefault, _kind);
            }
            else Delete(b);
            return;
        }

        if (saved == "absent") { Delete(b); return; }

        var sep = saved.IndexOf('|');
        if (sep < 0) { Delete(b); return; }
        var kind = Enum.Parse<RegistryValueKind>(saved[..sep]);
        var payload = saved[(sep + 1)..];

        object value = kind switch
        {
            RegistryValueKind.DWord => int.Parse(payload, System.Globalization.CultureInfo.InvariantCulture),
            RegistryValueKind.QWord => long.Parse(payload, System.Globalization.CultureInfo.InvariantCulture),
            RegistryValueKind.Binary => Convert.FromBase64String(payload),
            RegistryValueKind.MultiString => payload.Split('\n'),
            _ => payload,
        };

        using var k = b.CreateSubKey(_path, true);
        k?.SetValue(_name, value, kind);
    }

    void Delete(RegistryKey b)
    {
        using var k = b.OpenSubKey(_path, true);
        k?.DeleteValue(_name, false);
    }
}

/// <summary>
/// Makes a registry value NOT exist (the Windows default for many policy and preference values).
/// Switching off puts back exactly what was there before.
/// </summary>
public sealed class RegistryDeleteAction : TweakAction
{
    readonly RegistryHive _hive;
    readonly string _path;
    readonly string _name;
    readonly RegistryAction _inner;

    public RegistryDeleteAction(RegistryHive hive, string path, string name,
        RegistryValueKind kind = RegistryValueKind.DWord, object? windowsDefault = null)
    {
        _hive = hive; _path = path; _name = name;
        _inner = new RegistryAction(hive, path, name, 0, kind, windowsDefault);
    }

    string HiveName => _hive switch { RegistryHive.LocalMachine => "HKLM", RegistryHive.Users => "HKU", _ => "HKCU" };

    public override string Describe() => $"{HiveName}\\{_path}  →  {_name} removed (Windows default)";

    public string ValueKey => $"{HiveName}\\{_path}|{_name}".ToLowerInvariant();

    public override bool IsApplied() => RegUtil.Read(_hive, _path, _name) == null;

    public override string? Capture() => _inner.Capture();

    public override void Apply()
    {
        using var b = RegUtil.Base(_hive);
        using var k = b.OpenSubKey(_path, true);
        k?.DeleteValue(_name, false);
    }

    public override void Revert(string? saved) => _inner.Revert(saved);
}

/// <summary>
/// Sets or clears one bit of a binary registry value (Windows keeps several visual effects as bits of
/// UserPreferencesMask). Only that bit is touched, so several of these can share one value; switching off puts
/// exactly that bit back.
/// </summary>
public sealed class RegistryBitAction : TweakAction
{
    readonly RegistryHive _hive;
    readonly string _path, _name;
    readonly int _index;
    readonly byte _mask;
    readonly bool _set, _defaultSet;

    /// <param name="set">True sets the bit, false clears it.</param>
    /// <param name="defaultSet">The bit as Windows ships it (used only if no backup exists).</param>
    public RegistryBitAction(RegistryHive hive, string path, string name, int byteIndex, byte mask, bool set, bool defaultSet)
    {
        _hive = hive; _path = path; _name = name; _index = byteIndex; _mask = mask; _set = set; _defaultSet = defaultSet;
    }

    string HiveName => _hive switch { RegistryHive.LocalMachine => "HKLM", RegistryHive.Users => "HKU", _ => "HKCU" };

    public override string Describe() => $"{HiveName}\\{_path}  →  {_name} byte {_index} bit 0x{_mask:X2} {(_set ? "on" : "off")}";

    bool? Bit()
    {
        return RegUtil.Read(_hive, _path, _name) is byte[] b && b.Length > _index ? (b[_index] & _mask) != 0 : null;
    }

    public override bool IsApplicable() => RegUtil.Read(_hive, _path, _name) is byte[] b && b.Length > _index;

    public override bool IsApplied() => Bit() == _set;

    public override string? Capture() => Bit() switch { true => "1", false => "0", _ => null };

    public override string? TestState() => Capture();

    void Write(bool on)
    {
        using var root = RegUtil.Base(_hive);
        using var k = root.OpenSubKey(_path, true) ?? throw new InvalidOperationException($"Cannot open {HiveName}\\{_path}");
        if (k.GetValue(_name) is not byte[] b || b.Length <= _index) throw new InvalidOperationException($"{_name} isn't there to change.");
        b[_index] = (byte)(on ? b[_index] | _mask : b[_index] & ~_mask);
        k.SetValue(_name, b, RegistryValueKind.Binary);
    }

    public override void Apply() => Write(_set);

    public override void Revert(string? saved) => Write(saved == null ? _defaultSet : saved == "1");
}

/// <summary>
/// Makes a registry key exist or not exist (some Explorer items, such as the folders under This PC, are shown only
/// while their key is there). Switching off puts back whether it was there.
/// </summary>
public sealed class RegistryKeyAction : TweakAction
{
    readonly RegistryHive _hive;
    readonly string _path;
    readonly bool _present;

    public RegistryKeyAction(RegistryHive hive, string path, bool present) { _hive = hive; _path = path; _present = present; }

    string HiveName => _hive switch { RegistryHive.LocalMachine => "HKLM", RegistryHive.Users => "HKU", _ => "HKCU" };

    public override string Describe() => $"{HiveName}\\{_path}  →  key {(_present ? "added" : "removed")}";

    public override bool IsApplied() => RegUtil.KeyExists(_hive, _path) == _present;

    /// <summary>"absent", or "present" followed by the key's own values, so switching off puts back exactly that.</summary>
    public override string? Capture()
    {
        using var root = RegUtil.Base(_hive);
        using var k = root.OpenSubKey(_path);
        if (k == null) return "absent";
        var sb = new StringBuilder("present");
        foreach (var name in k.GetValueNames())
        {
            var kind = k.GetValueKind(name);
            var v = k.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            var payload = v switch
            {
                byte[] b => Convert.ToBase64String(b),
                string[] m => Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join("\n", m))),
                _ => Convert.ToBase64String(Encoding.UTF8.GetBytes(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "")),
            };
            sb.Append('\n').Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(name))).Append('|').Append(kind).Append('|').Append(payload);
        }
        return sb.ToString();
    }

    public override string? TestState() => Capture();

    public override void Apply()
    {
        using var root = RegUtil.Base(_hive);
        if (_present) { using var k = root.CreateSubKey(_path, true); return; }
        using (var k = root.OpenSubKey(_path))
        {
            if (k == null) return;
            // only a key without sub-keys is removed, so nothing that couldn't be put back is ever lost
            if (k.SubKeyCount > 0) throw new InvalidOperationException($"{HiveName}\\{_path} has sub-keys, so it was left alone.");
        }
        root.DeleteSubKey(_path, false);
    }

    public override void Revert(string? saved)
    {
        using var root = RegUtil.Base(_hive);
        if (saved == null) saved = _present ? "absent" : "present";
        if (saved == "absent")
        {
            using (var k = root.OpenSubKey(_path)) { if (k == null || k.SubKeyCount > 0) return; }
            root.DeleteSubKey(_path, false);
            return;
        }
        using var key = root.CreateSubKey(_path, true);
        foreach (var line in saved.Split('\n').Skip(1))
        {
            var parts = line.Split('|');
            if (parts.Length != 3) continue;
            var name = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var kind = Enum.Parse<RegistryValueKind>(parts[1]);
            var raw = Convert.FromBase64String(parts[2]);
            object value = kind switch
            {
                RegistryValueKind.Binary => raw,
                RegistryValueKind.DWord => int.Parse(Encoding.UTF8.GetString(raw), System.Globalization.CultureInfo.InvariantCulture),
                RegistryValueKind.QWord => long.Parse(Encoding.UTF8.GetString(raw), System.Globalization.CultureInfo.InvariantCulture),
                RegistryValueKind.MultiString => Encoding.UTF8.GetString(raw).Split('\n'),
                _ => Encoding.UTF8.GetString(raw),
            };
            key.SetValue(name, value, kind);
        }
    }
}

// ---------------------------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------------------------

public sealed class ServiceAction : TweakAction
{
    public const string Disabled = "disabled";
    readonly string _name;
    readonly string _target;
    readonly string _windowsDefault;

    /// <param name="windowsDefault">
    /// Startup type Windows ships with (sc.exe words: auto, delayed-auto, demand). Used only if no backup exists.
    /// </param>
    readonly Func<bool>? _applicable;

    /// <param name="applicable">Optional extra condition: when it returns false this part is left out on this PC.</param>
    public ServiceAction(string serviceName, string targetMode = Disabled, string windowsDefault = "demand", Func<bool>? applicable = null)
    {
        _name = serviceName; _target = targetMode; _windowsDefault = windowsDefault; _applicable = applicable;
    }

    /// <summary>The service this changes (the repair tool leaves services TrapShack manages alone).</summary>
    public string ServiceName => _name;

    string KeyPath => $@"SYSTEM\CurrentControlSet\Services\{_name}";

    public override string Describe() => $"Service \"{_name}\"  →  startup type: {_target}";

    public override bool IsApplicable() => RegUtil.KeyExists(RegistryHive.LocalMachine, KeyPath) && (_applicable?.Invoke() ?? true);

    /// <summary>Current startup mode in the vocabulary sc.exe understands.</summary>
    string? CurrentMode()
    {
        var start = RegUtil.ReadInt(RegistryHive.LocalMachine, KeyPath, "Start");
        if (start == null) return null;
        var delayed = RegUtil.ReadInt(RegistryHive.LocalMachine, KeyPath, "DelayedAutostart") == 1;
        return start switch
        {
            0 => "boot",
            1 => "system",
            2 => delayed ? "delayed-auto" : "auto",
            3 => "demand",
            _ => "disabled",
        };
    }

    bool IsRunning() => Shell.Run("sc.exe", $"query {_name}", 15_000).Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);

    public override bool IsApplied() => CurrentMode() == _target;

    public override string? Capture() => $"{CurrentMode()}|{(IsRunning() ? 1 : 0)}";

    public override string? TestState() => CurrentMode();

    public override void Apply()
    {
        var r = Shell.Run("sc.exe", $"config {_name} start= {_target}", 30_000);
        if (!r.Ok) throw new InvalidOperationException($"Could not change service {_name}: {r.Output.Trim()}");
        if (_target == Disabled) Shell.Run("sc.exe", $"stop {_name}", 30_000);
    }

    public override void Revert(string? saved)
    {
        // Without a backup, fall back to the startup type Windows ships with for this service.
        var mode = _windowsDefault;
        var wasRunning = false;
        if (!string.IsNullOrEmpty(saved))
        {
            var parts = saved.Split('|');
            if (parts[0].Length > 0) mode = parts[0];
            wasRunning = parts.Length > 1 && parts[1] == "1";
        }

        var r = Shell.Run("sc.exe", $"config {_name} start= {mode}", 30_000);
        if (!r.Ok) throw new InvalidOperationException($"Could not restore service {_name}: {r.Output.Trim()}");
        if (wasRunning) Shell.Run("sc.exe", $"start {_name}", 30_000);
    }
}

// ---------------------------------------------------------------------------------------------
// Scheduled tasks
// ---------------------------------------------------------------------------------------------

public sealed class ScheduledTaskAction : TweakAction
{
    readonly string _fullPath;

    /// <param name="fullPath">Full task path, e.g. \Microsoft\Windows\Application Experience\ProgramDataUpdater</param>
    public ScheduledTaskAction(string fullPath) => _fullPath = fullPath;

    public override string Describe() => $"Scheduled task {_fullPath}  →  disabled";

    public override bool IsApplicable() => Caches.TasksValid && Caches.TaskState(_fullPath) != null;

    public override bool IsApplied() => string.Equals(Caches.TaskState(_fullPath), "Disabled", StringComparison.OrdinalIgnoreCase);

    public override string? Capture() => Caches.TaskState(_fullPath);

    public override string? TestState() => Caches.TaskState(_fullPath) is string s
        ? (string.Equals(s, "Disabled", StringComparison.OrdinalIgnoreCase) ? "Disabled" : "Enabled") : null;

    public override void Apply()
    {
        var r = Shell.Run("schtasks.exe", $"/Change /TN \"{_fullPath}\" /DISABLE", 30_000);
        if (!r.Ok) throw new InvalidOperationException($"Could not disable task {_fullPath}: {(r.Output + r.Error).Trim()}");
        Caches.SetTaskState(_fullPath, "Disabled");
    }

    public override void Revert(string? saved)
    {
        // Only re-enable tasks that were enabled before we touched them.
        if (string.Equals(saved, "Disabled", StringComparison.OrdinalIgnoreCase)) return;
        var r = Shell.Run("schtasks.exe", $"/Change /TN \"{_fullPath}\" /ENABLE", 30_000);
        if (!r.Ok) throw new InvalidOperationException($"Could not re-enable task {_fullPath}: {(r.Output + r.Error).Trim()}");
        Caches.SetTaskState(_fullPath, saved ?? "Ready");
    }
}

// ---------------------------------------------------------------------------------------------
// Store apps
// ---------------------------------------------------------------------------------------------

public sealed class AppxAction : TweakAction
{
    readonly string[] _patterns;

    public AppxAction(params string[] patterns) => _patterns = patterns;

    public override string Describe() => "Remove apps: " + string.Join(", ", _patterns);

    public override bool IsApplicable() => Caches.AppxValid;

    public override bool IsApplied() => !Caches.AppxMatchesAny(_patterns);

    public override string? Capture() => null;

    public override void Apply()
    {
        var sb = new StringBuilder();
        foreach (var p in _patterns)
        {
            var q = p.Replace("'", "''");
            sb.AppendLine($"Get-AppxPackage -AllUsers -Name '{q}' | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue");
            sb.AppendLine($"Get-AppxProvisionedPackage -Online | Where-Object {{ $_.DisplayName -like '{q}' }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Out-Null");
        }
        var r = Shell.PowerShell(sb.ToString(), 900_000);
        if (r.ExitCode != 0) Log.Error("App removal reported: " + (r.Error + r.Output).Trim());
        Caches.RefreshAppx();
    }

    /// <summary>
    /// Removed Store apps can only come back through the Microsoft Store; the engine tells the user so
    /// (see Tweak.StoreRestore). Nothing to do here.
    /// </summary>
    public override void Revert(string? saved) { }
}

// ---------------------------------------------------------------------------------------------
// Anything that needs real code
// ---------------------------------------------------------------------------------------------

public sealed class CustomAction : TweakAction
{
    readonly string _describe;
    readonly Func<bool> _isApplied;
    readonly Action _apply;
    readonly Action<string?>? _revert;
    readonly Func<string?>? _capture;
    readonly Func<bool>? _applicable;

    public CustomAction(string describe, Func<bool> isApplied, Action apply,
        Action<string?>? revert = null, Func<string?>? capture = null, Func<bool>? applicable = null)
    {
        _describe = describe; _isApplied = isApplied; _apply = apply;
        _revert = revert; _capture = capture; _applicable = applicable;
    }

    public override string Describe() => _describe;
    public override bool IsApplicable() => _applicable?.Invoke() ?? true;
    public override bool IsApplied() => _isApplied();
    public override string? Capture() => _capture?.Invoke();
    public override void Apply() => _apply();
    public override void Revert(string? saved) => _revert?.Invoke(saved);

    // code-based changes have no single value to read back; on/off (plus the saved original, when there is one) is the test
    public override string? TestState() => (_isApplied() ? "on" : "off") + (_capture?.Invoke() is string c ? "|" + c : "");
}

// ---------------------------------------------------------------------------------------------
// Power plan values (the same settings the Advanced power options dialog edits)
// ---------------------------------------------------------------------------------------------

/// <summary>
/// One setting of the ACTIVE power plan, with separate plugged-in (AC) and on-battery (DC) values.
/// Reads the plan from the registry (fast) and writes with powercfg, so Windows applies it immediately.
/// </summary>
public sealed class PowerCfgAction : TweakAction
{
    readonly string _title;
    readonly string _sub;
    readonly string _setting;
    readonly int _ac, _dc, _defAc, _defDc;

    /// <param name="defaultAc">Value Windows uses when the plan has no explicit entry (plugged in).</param>
    public PowerCfgAction(string title, string subgroupGuid, string settingGuid, int ac, int dc, int defaultAc, int defaultDc)
    {
        _title = title; _sub = subgroupGuid; _setting = settingGuid;
        _ac = ac; _dc = dc; _defAc = defaultAc; _defDc = defaultDc;
    }

    const string PowerRoot = @"SYSTEM\CurrentControlSet\Control\Power";

    public override string Describe() => $"Power plan  →  {_title}: plugged in {_ac}, on battery {_dc}";

    public override bool IsApplicable() =>
        RegUtil.KeyExists(RegistryHive.LocalMachine, $@"{PowerRoot}\PowerSettings\{_sub}\{_setting}");

    static string? ActivePlan()
    {
        var v = RegUtil.Read(RegistryHive.LocalMachine, $@"{PowerRoot}\User\PowerSchemes", "ActivePowerScheme");
        return v as string;
    }

    (int Ac, int Dc) Current()
    {
        var plan = ActivePlan();
        if (plan == null) return (_defAc, _defDc);
        var path = $@"{PowerRoot}\User\PowerSchemes\{plan}\{_sub}\{_setting}";
        var ac = RegUtil.ReadInt(RegistryHive.LocalMachine, path, "ACSettingIndex") ?? _defAc;
        var dc = RegUtil.ReadInt(RegistryHive.LocalMachine, path, "DCSettingIndex") ?? _defDc;
        return (ac, dc);
    }

    public override bool IsApplied() { var c = Current(); return c.Ac == _ac && c.Dc == _dc; }

    public override string? Capture() { var c = Current(); return $"{c.Ac}|{c.Dc}"; }

    static void Set(string args)
    {
        var r = Shell.Run("powercfg.exe", args, 30_000);
        if (!r.Ok) throw new InvalidOperationException($"powercfg {args} failed: {(r.Output + r.Error).Trim()}");
    }

    void Write(int ac, int dc)
    {
        Set($"/setacvalueindex SCHEME_CURRENT {_sub} {_setting} {ac}");
        Set($"/setdcvalueindex SCHEME_CURRENT {_sub} {_setting} {dc}");
        Set("/setactive SCHEME_CURRENT");
    }

    public override void Apply() => Write(_ac, _dc);

    public override void Revert(string? saved)
    {
        var parts = saved?.Split('|');
        if (parts is { Length: 2 } && int.TryParse(parts[0], out var ac) && int.TryParse(parts[1], out var dc)) Write(ac, dc);
        else Write(_defAc, _defDc);
    }
}
