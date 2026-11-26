using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using TrapShack.Tweaks;
using Microsoft.Win32;

namespace TrapShack.Core;

/// <summary>Everything TrapShack learns about the PC so it can recommend the right tweaks.</summary>
public sealed class SystemInfo
{
    public static SystemInfo Current { get; private set; } = new();

    const string NtKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    public string OsName { get; private set; } = "Windows";
    public string EditionId { get; private set; } = "";
    public string DisplayVersion { get; private set; } = "";
    public int Build { get; private set; }
    public int Ubr { get; private set; }

    public ulong RamKb { get; private set; }
    public double RamGb => RamKb / 1024.0 / 1024.0;
    public string Cpu { get; private set; } = "Unknown CPU";
    /// <summary>"GenuineIntel", "AuthenticAMD", ...</summary>
    public string CpuVendor { get; private set; } = "";
    public List<string> Gpus { get; private set; } = new();

    public bool IsIntelCpu => CpuVendor.Contains("Intel", StringComparison.OrdinalIgnoreCase);
    public bool IsAmdCpu => CpuVendor.Contains("AMD", StringComparison.OrdinalIgnoreCase);
    /// <summary>Ryzen X3D chips rely on AMD's 3D V-Cache driver and Xbox Game Bar to place games on the right cores.</summary>
    public bool HasX3D => Cpu.Contains("X3D", StringComparison.OrdinalIgnoreCase);
    public string CpuBrand => IsIntelCpu ? "Intel" : IsAmdCpu ? "AMD" : "Other";

    public bool HasNvidiaGpu => Gpus.Any(g => g.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase));
    /// <summary>A discrete Radeon card (RX series), not the Radeon graphics built into a Ryzen chip.</summary>
    public bool HasAmdDiscreteGpu => Gpus.Any(g => Regex.IsMatch(g, @"\bRX\s?\d{3,4}", RegexOptions.IgnoreCase));
    public bool HasIntelArcGpu => Gpus.Any(g => Regex.IsMatch(g, @"Intel.*\bArc(\(TM\))?\s+[AB]\d{3}", RegexOptions.IgnoreCase));

    /// <summary>NVIDIA GeForce RTX 40 or 50 series.</summary>
    public bool HasNvidiaRtx40OrNewer => Gpus.Any(g => Regex.IsMatch(g, @"NVIDIA.*\bRTX\s*[45]\d{3}", RegexOptions.IgnoreCase));
    /// <summary>NVIDIA cards that support hardware-accelerated GPU scheduling (GTX 10 series and newer).</summary>
    public bool HasNvidiaHagsCapable => Gpus.Any(g =>
        Regex.IsMatch(g, @"NVIDIA.*\b(RTX|GTX\s*16|GTX\s*10)", RegexOptions.IgnoreCase));
    /// <summary>AMD Radeon RX 5000 series and newer.</summary>
    public bool HasAmdHagsCapable => Gpus.Any(g => Regex.IsMatch(g, @"\bRX\s?([5-9])\d{3}", RegexOptions.IgnoreCase));
    public bool IsLaptop { get; private set; }
    public bool IsVirtualMachine { get; private set; }
    public bool DomainJoined { get; private set; }

    public bool EdgeInstalled { get; set; }
    public bool BraveInstalled { get; set; }
    public bool OneDriveInstalled => OneDriveOps.IsInstalled();
    public bool DocumentsInOneDrive { get; private set; }

    /// <summary>Null until the slow probe has run (or if it could not tell).</summary>
    public bool? SystemDriveIsSsd { get; private set; }
    /// <summary>"Enabled", "Disabled" or null when the Recall feature does not exist on this PC.</summary>
    public string? RecallFeatureState { get; set; }

    public bool IsWindows11 => Build >= 22000;
    public bool Is24H2OrNewer => Build >= 26100;
    public bool Supported => Build >= 19041;
    public bool IsHome => EditionId.StartsWith("Core", StringComparison.OrdinalIgnoreCase);

    public string FriendlyOs => $"{OsName}{(DisplayVersion.Length > 0 ? " " + DisplayVersion : "")}  (build {Build}.{Ubr})";
    public string DeviceType => IsVirtualMachine ? "Virtual machine" : IsLaptop ? "Laptop / portable" : "Desktop";
    public string Storage => SystemDriveIsSsd switch { true => "SSD", false => "Hard disk (HDD)", null => "Unknown" };

    // -----------------------------------------------------------------------------------------

    public static SystemInfo Detect()
    {
        var s = new SystemInfo();

        var product = RegUtil.ReadString(RegistryHive.LocalMachine, NtKey, "ProductName") ?? "Windows";
        s.EditionId = RegUtil.ReadString(RegistryHive.LocalMachine, NtKey, "EditionID") ?? "";
        s.DisplayVersion = RegUtil.ReadString(RegistryHive.LocalMachine, NtKey, "DisplayVersion")
                           ?? RegUtil.ReadString(RegistryHive.LocalMachine, NtKey, "ReleaseId") ?? "";
        int.TryParse(RegUtil.ReadString(RegistryHive.LocalMachine, NtKey, "CurrentBuildNumber"), out var build);
        s.Build = build;
        s.Ubr = RegUtil.ReadInt(RegistryHive.LocalMachine, NtKey, "UBR") ?? 0;

        // Windows 11 still reports "Windows 10" in ProductName. The build number is the truth.
        s.OsName = s.IsWindows11 ? product.Replace("Windows 10", "Windows 11") : product;

        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref mem)) s.RamKb = mem.ullTotalPhys / 1024;

        const string cpuKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";
        s.Cpu = (RegUtil.ReadString(RegistryHive.LocalMachine, cpuKey, "ProcessorNameString") ?? "Unknown CPU").Trim();
        s.CpuVendor = RegUtil.ReadString(RegistryHive.LocalMachine, cpuKey, "VendorIdentifier") ?? "";
        s.Gpus = ReadGpus();

        var maker = RegUtil.ReadString(RegistryHive.LocalMachine, @"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer") ?? "";
        var model = RegUtil.ReadString(RegistryHive.LocalMachine, @"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName") ?? "";
        s.IsVirtualMachine = Regex.IsMatch(maker + " " + model, "virtual|vmware|qemu|kvm|xen|parallels", RegexOptions.IgnoreCase);

        s.IsLaptop = GetSystemPowerStatus(out var power) && power.BatteryFlag != 128 && power.BatteryFlag != 255;

        var domain = RegUtil.ReadString(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Domain");
        s.DomainJoined = !string.IsNullOrWhiteSpace(domain);

        var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        s.EdgeInstalled = File.Exists(Path.Combine(pf86, "Microsoft", "Edge", "Application", "msedge.exe"));
        s.BraveInstalled =
            File.Exists(Path.Combine(pf, "BraveSoftware", "Brave-Browser", "Application", "brave.exe")) ||
            File.Exists(Path.Combine(pf86, "BraveSoftware", "Brave-Browser", "Application", "brave.exe")) ||
            File.Exists(Path.Combine(local, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"));

        var oneDriveRoot = Environment.GetEnvironmentVariable("OneDrive");
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        s.DocumentsInOneDrive = !string.IsNullOrEmpty(oneDriveRoot) &&
                                docs.StartsWith(oneDriveRoot, StringComparison.OrdinalIgnoreCase);

        Current = s;
        return s;
    }

    /// <summary>What the slow checks are tied to: they only change with Windows itself (or new hardware, caught by the background re-check).</summary>
    public string ProbeKeyNow => $"{Build}.{Ubr}|{Cpu}";

    /// <summary>Starts with the slow checks from the last run on this build; <see cref="ProbeSlow"/> refreshes them afterwards.</summary>
    public void UseCachedProbe(bool? ssd, string? recall, IEnumerable<string> gpus)
    {
        SystemDriveIsSsd = ssd;
        RecallFeatureState = recall;
        Gpus = Gpus.Concat(gpus).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Slower checks (PowerShell). Safe to call from a background thread.</summary>
    public void ProbeSlow()
    {
        try
        {
            var r = Shell.PowerShell(@"
$disk = $null
try {
  $letter = $env:SystemDrive.TrimEnd(':')
  $part = Get-Partition -DriveLetter $letter -ErrorAction Stop
  $disk = (Get-PhysicalDisk -ErrorAction Stop | Where-Object { $_.DeviceId -eq [string]$part.DiskNumber } | Select-Object -First 1).MediaType
} catch {}
$recall = $null
if (" + (Is24H2OrNewer ? "$true" : "$false") + @") {
  try { $recall = (Get-WindowsOptionalFeature -Online -FeatureName Recall -ErrorAction Stop).State.ToString() } catch {}
}
$gpus = @()
try { $gpus = @(Get-CimInstance Win32_VideoController -ErrorAction Stop | ForEach-Object { ""$($_.Name)"".Trim() } | Where-Object { $_ }) } catch {}
@{ disk = ""$disk""; recall = ""$recall""; gpus = $gpus } | ConvertTo-Json -Compress
", 120_000);

            var json = r.Output.Trim();
            var start = json.IndexOf('{');
            if (start < 0) return;
            using var doc = JsonDocument.Parse(json[start..]);

            var disk = doc.RootElement.TryGetProperty("disk", out var d) ? d.GetString() : null;
            SystemDriveIsSsd = disk switch { "SSD" => true, "HDD" => false, _ => null };

            var recall = doc.RootElement.TryGetProperty("recall", out var rc) ? rc.GetString() : null;
            RecallFeatureState = string.IsNullOrWhiteSpace(recall) ? null : recall;

            // the graphics cards as Windows' own device list names them: covers PCs where the driver keys can't be read
            if (doc.RootElement.TryGetProperty("gpus", out var g))
            {
                var names = (g.ValueKind == JsonValueKind.Array ? g.EnumerateArray().Select(x => x.GetString()) : new[] { g.GetString() })
                    .Where(n => !string.IsNullOrWhiteSpace(n) && !n!.Contains("Basic", StringComparison.OrdinalIgnoreCase)
                                && !n.Contains("Remote", StringComparison.OrdinalIgnoreCase))
                    .Select(n => n!.Trim());
                Gpus = Gpus.Concat(names).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
        }
        catch (Exception ex) { Log.Error("Hardware probe failed: " + ex.Message); }
    }

    static List<string> ReadGpus()
    {
        var list = new List<string>();
        try
        {
            using var b = RegUtil.Base(RegistryHive.LocalMachine);
            using var cls = b.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11cd-be05-08002be10318}");
            if (cls == null) return list;
            foreach (var n in cls.GetSubKeyNames())
            {
                if (!n.All(char.IsDigit)) continue;
                try
                {
                    using var k = cls.OpenSubKey(n);
                    var desc = k?.GetValue("DriverDesc") as string;
                    if (!string.IsNullOrWhiteSpace(desc) && !desc.Contains("Basic", StringComparison.OrdinalIgnoreCase))
                        list.Add(desc.Trim());
                }
                catch { /* one adapter key we can't read: try the rest */ }
            }
        }
        catch { /* best effort */ }

        // second source: the display adapters Windows is driving right now (works even when the registry keys are locked)
        try
        {
            var dd = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
            for (uint i = 0; EnumDisplayDevices(null, i, ref dd, 0); i++)
            {
                var name = dd.DeviceString?.Trim();
                if (!string.IsNullOrEmpty(name) && !name.Contains("Basic", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("Remote", StringComparison.OrdinalIgnoreCase))
                    list.Add(name);
                dd.cb = Marshal.SizeOf<DISPLAY_DEVICE>();
            }
        }
        catch { /* best effort */ }
        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    /// <summary>
    /// Whether this PC's firmware supports hibernation (S4). Read from Windows' power capabilities; virtual machines
    /// and some Modern Standby devices report no.
    /// </summary>
    public static bool FirmwareSupportsHibernation()
    {
        var buffer = Marshal.AllocHGlobal(256);   // SYSTEM_POWER_CAPABILITIES is about 76 bytes
        try
        {
            for (var i = 0; i < 256; i++) Marshal.WriteByte(buffer, i, 0);
            if (!GetPwrCapabilities(buffer)) return true;   // can't tell: assume yes, as nearly every PC can
            // PowerButtonPresent, SleepButtonPresent, LidPresent, SystemS1, S2, S3, then SystemS4 (one byte each)
            return Marshal.ReadByte(buffer, 6) != 0 || Marshal.ReadByte(buffer, 8) != 0;   // S4, or a hibernation file exists
        }
        catch { return true; }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    [DllImport("powrprof.dll", SetLastError = true)]
    static extern bool GetPwrCapabilities(IntPtr capabilities);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplayDevices(string? device, uint index, ref DISPLAY_DEVICE dd, uint flags);

    // --- native ------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);
}
