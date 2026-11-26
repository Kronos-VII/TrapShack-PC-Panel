using System.Text.RegularExpressions;
using Microsoft.Win32;
using TrapShack.Tweaks;

namespace TrapShack.Core;

/// <summary>
/// "Repair harmful tweaks": finds changes other tools or guides commonly make that break Windows or hurt games, and puts
/// each one back to Microsoft's default.
///
/// Rules, so it never lists a proper setting:
///  * Only changes with a known downside are checked: a broken feature, lost stability, or a security hole. Taste and
///    debatable "maybe faster" settings are not checked at all.
///  * Anything TrapShack's own settings manage (the same registry value or service) is skipped, because those are the
///    verified tweaks and have their own switch and Undo.
///  * Security switches someone turned off on purpose with a third-party antivirus or firewall installed are not flagged.
/// Scanning only reads. Fixing changes only the items listed, after a restore point.
/// </summary>
public static class TweakDoctor
{
    public sealed record Finding(string Id, string Area, string Title, string Problem, string Fix, bool Restart, Action Repair, Func<bool> StillBad);

    // ------------------------------------------------------------------ what TrapShack itself manages

    static HashSet<string>? _managedValues, _managedServices;

    static void LoadManaged()
    {
        if (_managedValues != null) return;
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var services = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var t in Catalog.Build())
                foreach (var a in t.Actions)
                    switch (a)
                    {
                        case RegistryAction r: values.Add(r.ValueKey); break;
                        case ServiceAction s: services.Add(s.ServiceName); break;
                    }
        }
        catch (Exception ex) { Log.Error("Repair tool: couldn't read TrapShack's own settings: " + ex.Message); }
        _managedValues = values;
        _managedServices = services;
    }

    static string Key(RegistryHive hive, string path, string name) =>
        $"{(hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU")}\\{path}|{name}".ToLowerInvariant();

    static bool Managed(RegistryHive hive, string path, string name) => _managedValues!.Contains(Key(hive, path, name));

    // ------------------------------------------------------------------ registry helpers (64-bit view)

    static object? Get(string path, string name) => RegUtil.Read(RegistryHive.LocalMachine, path, name);

    static int? GetInt(string path, string name) => Get(path, name) switch
    {
        int i => i,
        long l => (int)l,
        string s when int.TryParse(s, out var n) => n,
        _ => null,
    };

    static void SetDword(string path, string name, int value)
    {
        using var k = RegUtil.Base(RegistryHive.LocalMachine).CreateSubKey(path, true)
                      ?? throw new InvalidOperationException("Windows refused to open " + path);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    static void Delete(string path, string name)
    {
        using var k = RegUtil.Base(RegistryHive.LocalMachine).OpenSubKey(path, true);
        k?.DeleteValue(name, false);
    }

    // ------------------------------------------------------------------ checks

    /// <summary>
    /// Services Windows needs, with the startup type Microsoft ships (sc.exe words). Flagged only when set to Disabled.
    /// </summary>
    static readonly (string Name, string Display, string Default, string Breaks)[] CriticalServices =
    {
        ("wuauserv", "Windows Update", "demand", "Windows Update, the Microsoft Store and Game Pass installs stop working."),
        ("BITS", "Background Intelligent Transfer Service", "demand", "Windows Update, Store and many game launchers can't download."),
        ("UsoSvc", "Update Orchestrator Service", "delayed-auto", "Windows Update can't scan or install updates."),
        ("CryptSvc", "Cryptographic Services", "auto", "Updates, driver installs and signed downloads fail to verify."),
        ("InstallService", "Microsoft Store Install Service", "demand", "Store apps and Game Pass games can't install or update."),
        ("AppXSvc", "AppX Deployment Service", "demand", "Store apps (including Xbox and Game Pass) can't install, update or open."),
        ("ClipSVC", "Client License Service", "demand", "Store apps and Game Pass games stop opening (licence check fails)."),
        ("TokenBroker", "Web Account Manager", "demand", "Signing in to Microsoft, Xbox and Store accounts fails."),
        ("wlidsvc", "Microsoft Account Sign-in Assistant", "demand", "Microsoft and Xbox account sign-in fails."),
        ("Audiosrv", "Windows Audio", "auto", "There's no sound at all."),
        ("AudioEndpointBuilder", "Windows Audio Endpoint Builder", "auto", "Audio devices disappear and there's no sound."),
        ("Dnscache", "DNS Client", "auto", "Websites and online games resolve slowly or not at all."),
        ("Dhcp", "DHCP Client", "auto", "The PC can lose its network address and drop offline."),
        ("nsi", "Network Store Interface Service", "auto", "Network connections stop working."),
        ("LanmanWorkstation", "Workstation", "auto", "Shared folders and some network features stop working."),
        ("Wcmsvc", "Windows Connection Manager", "auto", "Wi-Fi and network switching stop working properly."),
        ("mpssvc", "Windows Defender Firewall", "auto", "The firewall is off and some apps and Store installs fail."),
        ("BFE", "Base Filtering Engine", "auto", "The firewall, VPNs and some anti-cheats stop working."),
        ("wscsvc", "Security Center", "delayed-auto", "Windows can't report antivirus or firewall status."),
        ("EventLog", "Windows Event Log", "auto", "Crashes can't be diagnosed and several services fail to start."),
        ("Schedule", "Task Scheduler", "auto", "Windows maintenance and many app updaters stop running."),
        ("Winmgmt", "Windows Management Instrumentation", "auto", "Hardware monitoring, many games and system tools break."),
        ("Power", "Power", "auto", "Power plans and sleep stop working properly."),
        ("PlugPlay", "Plug and Play", "demand", "New devices aren't detected."),
        ("ProfSvc", "User Profile Service", "auto", "Signing in to Windows can fail."),
        ("Themes", "Themes", "auto", "Windows falls back to a broken basic look."),
        ("FontCache", "Windows Font Cache Service", "auto", "Apps and games start slower while fonts load."),
        ("Appinfo", "Application Information", "demand", "Run as administrator stops working."),
        ("msiserver", "Windows Installer", "demand", ".msi installers (many drivers and game runtimes) can't install."),
        ("TrustedInstaller", "Windows Modules Installer", "demand", "Updates and Windows features can't be installed."),
        ("W32Time", "Windows Time", "demand", "The clock drifts, which breaks sign-ins and some online games."),
        ("KeyIso", "CNG Key Isolation", "demand", "Wi-Fi passwords, sign-ins and some launchers fail."),
        ("VaultSvc", "Credential Manager", "demand", "Saved passwords and launcher sign-ins are lost."),
        ("DeviceInstall", "Device Install Service", "demand", "New devices and drivers can't install."),
        ("netprofm", "Network List Service", "demand", "The network icon and network settings stop working."),
    };

    static int? ServiceStart(string name) => GetInt($@"SYSTEM\CurrentControlSet\Services\{name}", "Start");

    static void SetServiceStart(string name, string mode)
    {
        var r = Shell.Run("sc.exe", $"config \"{name}\" start= {mode}", 30_000);
        if (r.Ok) return;
        // some services refuse sc.exe; the registry is what sc.exe writes anyway
        var path = $@"SYSTEM\CurrentControlSet\Services\{name}";
        SetDword(path, "Start", mode == "demand" ? 3 : 2);
        if (mode == "delayed-auto") SetDword(path, "DelayedAutostart", 1);
        else if (Get(path, "DelayedAutostart") != null) SetDword(path, "DelayedAutostart", 0);
    }

    static string ModeText(string mode) => mode switch { "auto" => "Automatic", "delayed-auto" => "Automatic (delayed start)", _ => "Manual" };

    static IEnumerable<Finding> ServiceChecks()
    {
        foreach (var (name, display, def, breaks) in CriticalServices)
        {
            if (_managedServices!.Contains(name)) continue;
            if (ServiceStart(name) != 4) continue;   // 4 = Disabled
            var n = name; var d = def;
            yield return new Finding("svc." + name, "Services", $"{display} is disabled",
                breaks, $"Set it back to {ModeText(def)}, Windows' default.", true,
                () => SetServiceStart(n, d), () => ServiceStart(n) == 4);
        }
    }

    // --- boot settings (bcdedit): element names are the same in every language; yes/no values are not
    static readonly HashSet<string> NoWords = new(StringComparer.OrdinalIgnoreCase)
        { "No", "Nein", "Non", "Não", "Nao", "Nee", "Nej", "Ei", "Nie", "Ne", "Nem", "Hayır", "Нет", "Ні", "いいえ", "否", "아니요", "Όχι", "Nu" };

    static Dictionary<string, string> ReadBoot()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var r = Shell.Run("bcdedit.exe", "/enum {current}", 30_000);
            foreach (var line in r.Output.Split('\n'))
            {
                var m = Regex.Match(line.TrimEnd('\r'), @"^([a-z0-9]+)\s+(.+)$");
                if (m.Success) map[m.Groups[1].Value] = m.Groups[2].Value.Trim();
            }
        }
        catch (Exception ex) { Log.Error("Repair tool: bcdedit: " + ex.Message); }
        return map;
    }

    static bool BootOn(Dictionary<string, string> b, string name) => b.TryGetValue(name, out var v) && !NoWords.Contains(v);

    static void Bcd(string args)
    {
        var r = Shell.Run("bcdedit.exe", args, 30_000);
        if (!r.Ok) throw new InvalidOperationException((r.Output + r.Error).Trim());
    }

    static IEnumerable<Finding> BootChecks()
    {
        var b = ReadBoot();
        if (b.Count == 0) yield break;

        if (BootOn(b, "useplatformclock"))
            yield return new Finding("boot.useplatformclock", "Boot settings", "Games are forced onto the slow HPET timer (useplatformclock)",
                "Every timer read goes through the slow HPET chip, which adds input latency and lowers FPS in many games.",
                "Remove the setting so Windows picks the timer itself (its default).", true,
                () => Bcd("/deletevalue {current} useplatformclock"), () => BootOn(ReadBoot(), "useplatformclock"));

        if (b.TryGetValue("nx", out var nx) && nx.Equals("AlwaysOff", StringComparison.OrdinalIgnoreCase))
            yield return new Finding("boot.nx", "Boot settings", "Data Execution Prevention is switched off (nx AlwaysOff)",
                "A core memory protection is off. Anti-cheats such as Vanguard and FACEIT refuse to start, and malware has an easier time.",
                "Set it back to OptIn, Windows' default.", true,
                () => Bcd("/set {current} nx OptIn"),
                () => ReadBoot().TryGetValue("nx", out var v) && v.Equals("AlwaysOff", StringComparison.OrdinalIgnoreCase));

        if (BootOn(b, "testsigning"))
            yield return new Finding("boot.testsigning", "Boot settings", "Test mode is on (testsigning)",
                "Unsigned drivers can load, a \"Test Mode\" watermark shows, and anti-cheats (Vanguard, FACEIT, EAC games) refuse to run.",
                "Turn test mode off (Windows' default).", true,
                () => Bcd("/deletevalue {current} testsigning"), () => BootOn(ReadBoot(), "testsigning"));

        if (BootOn(b, "nointegritychecks"))
            yield return new Finding("boot.nointegritychecks", "Boot settings", "Driver integrity checks are off (nointegritychecks)",
                "Windows skips checking driver signatures. Anti-cheats refuse to run and the PC is less safe.",
                "Turn the checks back on (Windows' default).", true,
                () => Bcd("/deletevalue {current} nointegritychecks"), () => BootOn(ReadBoot(), "nointegritychecks"));

        if (b.ContainsKey("numproc"))
            yield return new Finding("boot.numproc", "Boot settings", $"Windows is limited to {b["numproc"]} CPU core(s) at startup",
                "Set in System Configuration > Boot > Advanced options. It never speeds anything up; it only stops Windows using all your cores.",
                "Remove the limit so every core is used (Windows' default).", true,
                () => Bcd("/deletevalue {current} numproc"), () => ReadBoot().ContainsKey("numproc"));

        foreach (var mem in new[] { "truncatememory", "removememory" })
            if (b.ContainsKey(mem))
            {
                var m = mem;
                yield return new Finding("boot." + mem, "Boot settings", "Windows is limited to part of your RAM",
                    "A maximum-memory limit (System Configuration > Boot > Advanced options) hides some of the installed RAM from Windows and games.",
                    "Remove the limit so all RAM is used (Windows' default).", true,
                    () => Bcd("/deletevalue {current} " + m), () => ReadBoot().ContainsKey(m));
            }
    }

    // --- registry values with a clear downside
    sealed record RegCheck(string Id, string Area, string Path, string Name, Func<int?, bool> Bad, int? FixTo,
        string Title, string Problem, string Fix, bool Restart, string? AlsoDelete = null);

    static readonly RegCheck[] RegChecks =
    {
        new("gpu.tdr", "Stability", @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrLevel", v => v == 0, null,
            "Graphics driver recovery is switched off (TdrLevel 0)",
            "When the GPU driver hangs, Windows can no longer reset it, so a game hang freezes the whole PC until you hold the power button.",
            "Remove the value so Windows recovers the driver again (its default).", true),
        new("mem.largecache", "Performance", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", v => v == 1, 0,
            "Server-style file cache is on (LargeSystemCache 1)",
            "Windows gives file caching priority over programs, which takes memory away from games and can cause stutter.",
            "Set it back to 0, Windows' default for PCs.", true),
        new("sec.spectre", "Security", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", v => v is int i && (i & 3) != 0, null,
            "CPU security fixes (Spectre and Meltdown) are switched off",
            "The CPU is open to attacks that can read passwords from memory. Modern CPUs gain little or no speed from this.",
            "Remove the override so Windows' protections are on again (its default).", true, AlsoDelete: "FeatureSettingsOverrideMask"),
        new("sec.uac-off", "Security", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", v => v == 0, 1,
            "User Account Control is completely off (EnableLUA 0)",
            "Every program runs with full admin rights. Store apps, Xbox and some Settings pages stop opening.",
            "Turn it back on (EnableLUA 1, Windows' default).", true),
        new("disk.trim-off", "Performance", @"SYSTEM\CurrentControlSet\Control\FileSystem", "DisableDeleteNotification", v => v == 1, 0,
            "SSD TRIM is switched off",
            "Without TRIM an SSD slows down over time and wears out faster.",
            "Turn TRIM back on (Windows' default).", false),
        new("store.removed", "Breaks things", @"SOFTWARE\Policies\Microsoft\WindowsStore", "RemoveWindowsStore", v => v == 1, null,
            "The Microsoft Store is blocked by policy",
            "The Store can't open, Store apps don't update, and Xbox and Game Pass games can't install.",
            "Remove the policy (Windows' default).", false),
        new("wu.access", "Windows Update", @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DisableWindowsUpdateAccess", v => v == 1, null,
            "Windows Update is blocked by policy",
            "No security or driver updates install, and some games and Store apps that need a newer Windows won't install.",
            "Remove the policy (Windows' default).", false),
        new("wu.internet", "Windows Update", @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DoNotConnectToWindowsUpdateInternetLocations", v => v == 1, null,
            "Windows Update is cut off from Microsoft's servers",
            "Updates, the Store and Windows features that download from Microsoft fail.",
            "Remove the policy (Windows' default).", false),
    };

    static IEnumerable<Finding> RegistryChecks()
    {
        foreach (var c in RegChecks)
        {
            if (Managed(RegistryHive.LocalMachine, c.Path, c.Name)) continue;
            if (!c.Bad(GetInt(c.Path, c.Name))) continue;
            var check = c;
            yield return new Finding("reg." + c.Id, c.Area, c.Title, c.Problem, c.Fix, c.Restart,
                () =>
                {
                    if (check.FixTo is int to) SetDword(check.Path, check.Name, to); else Delete(check.Path, check.Name);
                    if (check.AlsoDelete != null) Delete(check.Path, check.AlsoDelete);
                    if (check.Id == "disk.trim-off") Shell.Run("fsutil.exe", "behavior set DisableDeleteNotify 0", 30_000);
                },
                () => check.Bad(GetInt(check.Path, check.Name)));
        }
    }

    // --- page file
    const string MemPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";

    static bool PageFileOff() =>
        Get(MemPath, "PagingFiles") is string[] files && files.All(f => string.IsNullOrWhiteSpace(f));

    static IEnumerable<Finding> PageFileCheck()
    {
        if (Managed(RegistryHive.LocalMachine, MemPath, "PagingFiles") || !PageFileOff()) yield break;
        yield return new Finding("mem.pagefile", "Stability", "The page file is switched off",
            "Games that use a lot of memory crash or close with \"out of memory\" errors, even with plenty of RAM, and crash dumps can't be saved.",
            "Let Windows manage the page file size again (its default).", true,
            () =>
            {
                var r = Shell.PowerShell("$cs = Get-CimInstance Win32_ComputerSystem; Set-CimInstance -InputObject $cs -Property @{AutomaticManagedPagefile=$true} -ErrorAction Stop", 60_000);
                if (!r.Ok) throw new InvalidOperationException((r.Output + r.Error).Trim());
            },
            () => !Equals(Shell.PowerShell("(Get-CimInstance Win32_ComputerSystem).AutomaticManagedPagefile", 30_000).Output.Trim(), "True"));
    }

    // --- Defender and firewall switched off by policy (only when no other product protects the PC)
    static (bool OtherAntivirus, bool OtherFirewall) OtherSecurity()
    {
        try
        {
            var r = Shell.PowerShell(
                "$av = Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct -ErrorAction SilentlyContinue | Where-Object { $_.displayName -notmatch 'Defender' };" +
                "$fw = Get-CimInstance -Namespace root/SecurityCenter2 -ClassName FirewallProduct -ErrorAction SilentlyContinue | Where-Object { $_.displayName -notmatch 'Defender|Windows' };" +
                "\"$([bool]$av)|$([bool]$fw)\"", 30_000);
            var parts = r.Output.Trim().Split('|');
            return (parts.Length == 2 && parts[0] == "True", parts.Length == 2 && parts[1] == "True");
        }
        catch { return (false, false); }
    }

    static IEnumerable<Finding> SecurityChecks()
    {
        var (otherAv, otherFw) = OtherSecurity();

        if (!otherAv)
        {
            const string def = @"SOFTWARE\Policies\Microsoft\Windows Defender";
            const string rtp = def + @"\Real-Time Protection";
            foreach (var (path, name, title) in new[]
            {
                (def, "DisableAntiSpyware", "Microsoft Defender is switched off by policy"),
                (rtp, "DisableRealtimeMonitoring", "Defender real-time protection is switched off by policy"),
            })
            {
                if (Managed(RegistryHive.LocalMachine, path, name) || GetInt(path, name) != 1) continue;
                var (p, n) = (path, name);
                yield return new Finding("sec." + name, "Security", title,
                    "No other antivirus is installed, so nothing is protecting this PC from malware.",
                    "Remove the policy so Defender runs again (Windows' default).", true,
                    () => Delete(p, n), () => GetInt(p, n) == 1);
            }
        }

        if (!otherFw)
        {
            foreach (var profile in new[] { "DomainProfile", "StandardProfile", "PublicProfile" })
            {
                var label = profile switch { "DomainProfile" => "domain", "StandardProfile" => "private", _ => "public" };
                var pol = $@"SOFTWARE\Policies\Microsoft\WindowsFirewall\{profile}";
                if (!Managed(RegistryHive.LocalMachine, pol, "EnableFirewall") && GetInt(pol, "EnableFirewall") == 0)
                {
                    var p = pol;
                    yield return new Finding($"fw.policy.{profile}", "Security", $"The firewall is switched off by policy ({label} networks)",
                        "No other firewall is installed, so the PC is open to the network.",
                        "Remove the policy (Windows' default is on).", false,
                        () => Delete(p, "EnableFirewall"), () => GetInt(p, "EnableFirewall") == 0);
                }
                var local = $@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\{profile}";
                if (!Managed(RegistryHive.LocalMachine, local, "EnableFirewall") && GetInt(local, "EnableFirewall") == 0)
                {
                    var p = local; var pr = profile;
                    yield return new Finding($"fw.local.{profile}", "Security", $"The firewall is off ({label} networks)",
                        "No other firewall is installed, so the PC is open to the network.",
                        "Turn it back on (Windows' default).", false,
                        () =>
                        {
                            var name = pr switch { "DomainProfile" => "domain", "StandardProfile" => "private", _ => "public" };
                            var r = Shell.Run("netsh.exe", $"advfirewall set {name}profile state on", 30_000);
                            if (!r.Ok) SetDword(p, "EnableFirewall", 1);
                        },
                        () => GetInt(p, "EnableFirewall") == 0);
                }
            }
        }
    }

    // --- Windows Update pointed at a server that doesn't exist (a common way to "block" updates)
    static IEnumerable<Finding> FakeUpdateServer()
    {
        const string wu = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";
        const string au = wu + @"\AU";
        if (Managed(RegistryHive.LocalMachine, au, "UseWUServer") || GetInt(au, "UseWUServer") != 1) yield break;
        var server = (Get(wu, "WUServer") as string ?? "").Trim();
        var fake = server.Length == 0 || Regex.IsMatch(server, @"^(https?://)?(127\.|localhost|0\.0\.0\.0|\[?::1\]?)", RegexOptions.IgnoreCase);
        if (!fake) yield break;   // a real company update server (WSUS) is left alone
        yield return new Finding("wu.fake-server", "Windows Update", "Windows Update points at a server that doesn't exist",
            "Updates, drivers from Windows Update and some Store downloads silently fail.",
            "Remove the fake server so Windows uses Microsoft's servers again (its default).", false,
            () => { Delete(au, "UseWUServer"); Delete(wu, "WUServer"); Delete(wu, "WUStatusServer"); },
            () => GetInt(au, "UseWUServer") == 1);
    }

    // ------------------------------------------------------------------ public

    /// <summary>Reads everything and returns what's harmful. Changes nothing.</summary>
    public static List<Finding> Scan()
    {
        LoadManaged();
        var all = new List<Finding>();
        void Add(string what, Func<IEnumerable<Finding>> f)
        {
            try { all.AddRange(f()); }
            catch (Exception ex) { Log.Error($"Repair tool: {what} check failed: {ex.Message}"); }
        }
        Add("service", ServiceChecks);
        Add("boot", BootChecks);
        Add("registry", RegistryChecks);
        Add("page file", PageFileCheck);
        Add("security", SecurityChecks);
        Add("update server", FakeUpdateServer);
        return all;
    }

    /// <summary>Everything the scan looks at, for the description shown before it runs.</summary>
    public const string Covers =
        "Windows services that are needed but disabled, boot settings that cost FPS or block anti-cheats, CPU-core and RAM limits, " +
        "graphics driver recovery, the page file, SSD TRIM, Windows Update and Store blocks, and security protection switched off with nothing replacing it.";
}
