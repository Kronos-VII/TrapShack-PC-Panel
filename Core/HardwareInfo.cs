using System.Text.Json;

namespace TrapShack.Core;

public sealed record InfoRow(string Label, string Value);

/// <summary>One card on the System info page: what it is, the one-line answer under its title, and the details.</summary>
public sealed record InfoSection(string Title, string Glyph, List<InfoRow> Rows, string Sub = "");

/// <summary>Reads detailed hardware and OS information (WMI/CIM through Windows PowerShell). Blocking: call off the UI thread.</summary>
public static class HardwareInfo
{
    const string Script = @"
$c = Get-CimInstance Win32_Processor | Select-Object -First 1
$g = @(Get-CimInstance Win32_VideoController)
$b = Get-CimInstance Win32_BaseBoard | Select-Object -First 1
$bios = Get-CimInstance Win32_BIOS | Select-Object -First 1
$m = @(Get-CimInstance Win32_PhysicalMemory)
$d = @(Get-PhysicalDisk -ErrorAction SilentlyContinue)
$os = Get-CimInstance Win32_OperatingSystem
$vr = @(@(Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11cd-be05-08002be10318}\0*' -ErrorAction SilentlyContinue) +
       @(Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Video\*\0000' -ErrorAction SilentlyContinue) |
       Where-Object { $_.DriverDesc -and $_.'HardwareInformation.qwMemorySize' } |
       ForEach-Object { $v = $_.'HardwareInformation.qwMemorySize'
                        [pscustomobject]@{ name=""$($_.DriverDesc)"".Trim(); mem=$(try { if ($v -is [byte[]]) { [double][BitConverter]::ToUInt64($v, 0) } else { [double]$v } } catch { 0 }) } })
$slots = $null; try { $slots = (Get-CimInstance Win32_PhysicalMemoryArray -ErrorAction Stop | Measure-Object -Property MemoryDevices -Sum).Sum } catch {}
$tpm = $null
try {
  $t = Get-CimInstance -Namespace root/cimv2/security/microsofttpm -ClassName Win32_Tpm -ErrorAction Stop
  if ($t) { $tpm = [pscustomobject]@{ ver = ""$($t.SpecVersion)"".Split(',')[0].Trim(); on = [bool]$t.IsEnabled_InitialValue; act = [bool]$t.IsActivated_InitialValue } }
} catch {}
$sb = ''; try { $sb = ""$(Confirm-SecureBootUEFI -ErrorAction Stop)"" } catch { $sb = 'unsupported' }
$dg = $null
try {
  $x = Get-CimInstance -Namespace root/Microsoft/Windows/DeviceGuard -ClassName Win32_DeviceGuard -ErrorAction Stop
  $dg = [pscustomobject]@{ vbs = [int]$x.VirtualizationBasedSecurityStatus; run = @($x.SecurityServicesRunning | ForEach-Object { [int]$_ }) }
} catch {}
$net = @()
try {
  $net = @(Get-NetAdapter -Physical -ErrorAction Stop | Where-Object { $_.Status -eq 'Up' } | ForEach-Object {
    $ip = (Get-NetIPAddress -InterfaceIndex $_.ifIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue | Select-Object -First 1).IPAddress
    $dns = @((Get-DnsClientServerAddress -InterfaceIndex $_.ifIndex -AddressFamily IPv4 -ErrorAction SilentlyContinue).ServerAddresses)
    [pscustomobject]@{ desc=""$($_.InterfaceDescription)""; speed=""$($_.LinkSpeed)""; media=""$($_.PhysicalMediaType)""; ip=""$ip""; dns=($dns -join ', ') } })
} catch {}
[pscustomobject]@{
  cpu = [pscustomobject]@{ name=$c.Name.Trim(); cores=$c.NumberOfCores; threads=$c.NumberOfLogicalProcessors; mhz=$c.MaxClockSpeed; l3=$c.L3CacheSize; socket=$c.SocketDesignation }
  gpu = @($g | ForEach-Object { $n = $_.Name; $real = ($vr | Where-Object { $_.name -eq ""$n"".Trim() -and $_.mem -gt 0 } | Select-Object -First 1).mem
                                $dd = $(try { $_.DriverDate.ToString('yyyy-MM-dd') } catch { '' })
                                [pscustomobject]@{ name=$n; driver=$_.DriverVersion; date=$dd; vram=$(if ($real) { $real } else { $_.AdapterRAM }); w=$_.CurrentHorizontalResolution; h=$_.CurrentVerticalResolution; hz=$_.CurrentRefreshRate } })
  boardMaker = ""$($b.Manufacturer)"".Trim()
  board = ""$($b.Product)"".Trim()
  biosVer = ""$($bios.SMBIOSBIOSVersion)"".Trim()
  biosDate = $(try { $bios.ReleaseDate.ToString('yyyy-MM-dd') } catch { '' })
  firmware = ""$env:firmware_type""
  ram = @($m | ForEach-Object { [pscustomobject]@{ cap=$_.Capacity; mhz=$_.ConfiguredClockSpeed; maker=""$($_.Manufacturer)"".Trim(); part=""$($_.PartNumber)"".Trim() } })
  slots = $slots
  disks = @($d | ForEach-Object { [pscustomobject]@{ name=$_.FriendlyName; type=""$($_.MediaType)""; bus=""$($_.BusType)""; size=$_.Size; health=""$($_.HealthStatus)"" } })
  os = [pscustomobject]@{ caption=$os.Caption; installed=$os.InstallDate.ToString('yyyy-MM-dd'); boot=$os.LastBootUpTime.ToString('yyyy-MM-dd HH:mm') }
  tpm = $tpm
  secureBoot = $sb
  dg = $dg
  net = $net
} | ConvertTo-Json -Depth 5 -Compress";

    // icons (Segoe MDL2 Assets)
    const string GOs = "", GCpu = "", GGpu = "", GRam = "", GBoard = "", GDisk = "", GNet = "", GShield = "";

    public static List<InfoSection> Collect(SystemInfo sys)
    {
        var result = new List<InfoSection>();
        JsonElement root;
        try
        {
            var r = Shell.PowerShell(Script, 60_000);
            if (!r.Ok || string.IsNullOrWhiteSpace(r.Output)) throw new InvalidOperationException(r.Error);
            var json = r.Output.Trim();
            var start = json.IndexOf('{');
            root = JsonDocument.Parse(start > 0 ? json[start..] : json).RootElement;
        }
        catch (Exception ex)
        {
            Log.Error("Hardware query failed: " + ex.Message);
            result.Add(new InfoSection("This PC", GOs, new()
            {
                new("Windows", sys.FriendlyOs), new("Processor", sys.Cpu),
                new("Graphics", sys.Gpus.Count > 0 ? string.Join(", ", sys.Gpus) : "Not detected"), new("Memory", $"{sys.RamGb:0.#} GB"), new("Device", sys.DeviceType),
            }, "Details couldn't be read, so this is the short version"));
            return result;
        }

        static IEnumerable<JsonElement> Items(JsonElement root, string n) =>
            !root.TryGetProperty(n, out var e) ? Array.Empty<JsonElement>()
            : e.ValueKind == JsonValueKind.Array ? e.EnumerateArray() : e.ValueKind == JsonValueKind.Object ? new[] { e } : Array.Empty<JsonElement>();
        static string S(JsonElement e, string n) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && v.ValueKind != JsonValueKind.Null
                ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()).Trim() : "";
        static double N(JsonElement e, string n) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
        static void Add(List<InfoRow> rows, string label, string value) { if (!string.IsNullOrWhiteSpace(value)) rows.Add(new(label, value)); }

        // ---- Operating system
        var os = root.TryGetProperty("os", out var o) ? o : default;
        var osRows = new List<InfoRow>();
        Add(osRows, "Edition", S(os, "caption").Replace("Microsoft ", ""));
        Add(osRows, "Version", sys.DisplayVersion);
        Add(osRows, "Build", sys.Build > 0 ? $"{sys.Build}.{sys.Ubr}" : "");
        Add(osRows, "Installed", S(os, "installed"));
        Add(osRows, "Last restart", S(os, "boot"));
        Add(osRows, "Domain", sys.DomainJoined ? "Joined to a domain" : "Not joined");
        result.Add(new InfoSection("Operating system", GOs, osRows, $"{sys.OsName}  ·  {sys.DeviceType}"));

        // ---- CPU
        var cpu = root.TryGetProperty("cpu", out var c) ? c : default;
        var cpuRows = new List<InfoRow>();
        Add(cpuRows, "Cores", N(cpu, "cores") > 0 ? $"{N(cpu, "cores"):0} cores  ·  {N(cpu, "threads"):0} threads" : "");
        Add(cpuRows, "Base clock", N(cpu, "mhz") > 0 ? $"{N(cpu, "mhz") / 1000.0:0.00} GHz" : "");
        Add(cpuRows, "L3 cache", N(cpu, "l3") > 0 ? $"{N(cpu, "l3") / 1024.0:0.#} MB" : "");
        Add(cpuRows, "Socket", S(cpu, "socket"));
        Add(cpuRows, "Vendor", sys.CpuBrand + (sys.HasX3D ? "  ·  3D V-Cache" : ""));
        result.Add(new InfoSection("Processor", GCpu, cpuRows, S(cpu, "name").Length > 0 ? S(cpu, "name") : sys.Cpu));

        // ---- GPU: one card each
        var gpus = Items(root, "gpu").Where(g => !S(g, "name").Contains("Basic", StringComparison.OrdinalIgnoreCase)).ToList();
        for (var i = 0; i < gpus.Count; i++)
        {
            var g = gpus[i];
            var rows = new List<InfoRow>();
            var vram = N(g, "vram");
            // the real size comes from the driver's registry entry; Win32_VideoController.AdapterRAM is 32-bit and stops
            // just under 4 GB, so a value in that last stretch is the cap, not the card, and is left out
            Add(rows, "VRAM", vram > 0 && (vram < 4.0e9 || vram >= 4294967296.0) ? $"{vram / 1073741824.0:0.#} GB" : "");
            Add(rows, "Driver", S(g, "driver"));
            Add(rows, "Driver date", S(g, "date"));
            Add(rows, "Display", N(g, "w") > 0 ? $"{N(g, "w"):0} × {N(g, "h"):0}" : "");
            Add(rows, "Refresh rate", N(g, "hz") > 1 ? $"{N(g, "hz"):0} Hz" : "");
            result.Add(new InfoSection(gpus.Count > 1 ? $"Graphics {i + 1}" : "Graphics", GGpu, rows, S(g, "name")));
        }

        // ---- Memory
        var mods = Items(root, "ram").ToList();
        var ramRows = new List<InfoRow>();
        if (mods.Count > 0)
        {
            Add(ramRows, "Modules", $"{mods.Count} × {mods.Average(x => N(x, "cap")) / 1073741824.0:0.#} GB");
            var slots = N(root, "slots");
            Add(ramRows, "Slots", slots >= mods.Count && slots > 0 ? $"{mods.Count} of {slots:0} used" : "");
            var mhz = mods.Max(x => N(x, "mhz"));
            Add(ramRows, "Speed", mhz > 0 ? $"{mhz:0} MT/s" : "");
            var maker = S(mods[0], "maker");
            Add(ramRows, "Maker", maker.Length > 0 && !maker.StartsWith("0000") && !maker.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? maker : "");
            Add(ramRows, "Part number", S(mods[0], "part"));
        }
        result.Add(new InfoSection("Memory", GRam, ramRows, $"{sys.RamGb:0.#} GB usable"));

        // ---- Board and firmware
        var boardRows = new List<InfoRow>();
        Add(boardRows, "Maker", S(root, "boardMaker"));
        Add(boardRows, "BIOS", S(root, "biosVer"));
        Add(boardRows, "BIOS date", S(root, "biosDate"));
        Add(boardRows, "Firmware", S(root, "firmware") switch { "UEFI" => "UEFI", "Legacy" => "Legacy BIOS", var f => f });
        result.Add(new InfoSection("Motherboard", GBoard, boardRows, S(root, "board").Length > 0 ? S(root, "board") : "Unknown board"));

        // ---- Storage
        var diskRows = new List<InfoRow>();
        double totalTb = 0;
        var di = 0;
        foreach (var d in Items(root, "disks"))
        {
            // name on one line, the facts under it, so nothing gets cut off
            di++;
            var size = N(d, "size") / 1e9;
            totalTb += size / 1000;
            var sizeText = size >= 1000 ? $"{size / 1000:0.#} TB" : $"{size:0} GB";
            var facts = string.Join("  ·  ", new[] { $"{sizeText} {S(d, "type")}".Trim(), S(d, "bus"), S(d, "health") }.Where(x => x.Length > 0 && x != "Unspecified"));
            diskRows.Add(new($"Drive {di}", $"{S(d, "name")}\n{facts}"));
        }
        if (diskRows.Count > 0)
            result.Add(new InfoSection("Storage", GDisk, diskRows,
                $"{di} drive{(di == 1 ? "" : "s")}  ·  {(totalTb >= 1 ? $"{totalTb:0.#} TB" : $"{totalTb * 1000:0} GB")} in total"));

        // ---- Network: the connections that are up
        var nets = Items(root, "net").ToList();
        if (nets.Count > 0)
        {
            var ni = nets.FindIndex(x => !S(x, "media").Contains("802.11"));
            if (ni < 0) ni = 0;
            var n = nets[ni];
            var wifi = S(n, "media").Contains("802.11") || S(n, "desc").Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase)
                       || S(n, "desc").Contains("Wireless", StringComparison.OrdinalIgnoreCase);
            var rows = new List<InfoRow>();
            Add(rows, "Connection", wifi ? "Wi-Fi" : "Wired (Ethernet)");
            Add(rows, "Link speed", S(n, "speed"));
            Add(rows, "Local address", S(n, "ip"));
            Add(rows, "DNS", S(n, "dns"));
            if (nets.Count > 1) Add(rows, "Also connected", string.Join(", ", nets.Where((_, k) => k != ni).Select(x => S(x, "desc"))));
            result.Add(new InfoSection("Network", wifi ? "\uE701" : GNet, rows, S(n, "desc")));
        }

        // ---- Security features that matter for games and Windows 11
        var secRows = new List<InfoRow>();
        var sb = S(root, "secureBoot");
        Add(secRows, "Secure Boot", sb switch { "True" => "On", "False" => "Off", "unsupported" => "Not supported here", _ => "" });
        if (root.TryGetProperty("tpm", out var tpm) && tpm.ValueKind == JsonValueKind.Object)
        {
            var ver = S(tpm, "ver");
            var on = tpm.TryGetProperty("on", out var te) && te.ValueKind == JsonValueKind.True;
            Add(secRows, "TPM", (ver.Length > 0 ? ver : "Present") + (on ? "  ·  on" : "  ·  off in firmware"));
        }
        else Add(secRows, "TPM", "Not found");
        if (root.TryGetProperty("dg", out var dg) && dg.ValueKind == JsonValueKind.Object)
        {
            var vbs = (int)N(dg, "vbs");
            Add(secRows, "Virtualization security", vbs switch { 2 => "Running", 1 => "Set up, not running", _ => "Off" });
            var running = Items(dg, "run").Select(x => x.ValueKind == JsonValueKind.Number ? x.GetInt32() : 0).ToList();
            Add(secRows, "Memory integrity", running.Contains(2) ? "On" : "Off");
        }
        if (secRows.Count > 0)
            result.Add(new InfoSection("Security", GShield, secRows, "What the hardware and Windows have switched on"));

        return result;
    }
}
