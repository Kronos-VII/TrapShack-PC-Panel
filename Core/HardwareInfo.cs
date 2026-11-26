using System.Text.Json;

namespace TrapShack.Core;

public sealed record InfoRow(string Label, string Value);
public sealed record InfoSection(string Title, string Glyph, List<InfoRow> Rows);

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
$mon = @(Get-CimInstance Win32_DesktopMonitor -ErrorAction SilentlyContinue)
$vr = @(Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11cd-be05-08002be10318}\0*' -ErrorAction SilentlyContinue |
       Where-Object { $_.'HardwareInformation.qwMemorySize' } | ForEach-Object { [pscustomobject]@{ name=$_.DriverDesc; mem=$(try { [double]$_.'HardwareInformation.qwMemorySize' } catch { 0 }) } })
[pscustomobject]@{
  cpu = [pscustomobject]@{ name=$c.Name.Trim(); cores=$c.NumberOfCores; threads=$c.NumberOfLogicalProcessors; mhz=$c.MaxClockSpeed; l3=$c.L3CacheSize; socket=$c.SocketDesignation }
  gpu = @($g | ForEach-Object { $n = $_.Name; $real = ($vr | Where-Object { $_.name -eq $n } | Select-Object -First 1).mem
                                [pscustomobject]@{ name=$n; driver=$_.DriverVersion; vram=$(if ($real) { $real } else { $_.AdapterRAM }); w=$_.CurrentHorizontalResolution; h=$_.CurrentVerticalResolution; hz=$_.CurrentRefreshRate } })
  board = ""$($b.Manufacturer) $($b.Product)"".Trim()
  bios = ""$($bios.SMBIOSBIOSVersion)  ($($bios.ReleaseDate.ToString('yyyy-MM-dd')))""
  ram = @($m | ForEach-Object { [pscustomobject]@{ cap=$_.Capacity; mhz=$_.ConfiguredClockSpeed; maker=$_.Manufacturer; part=$_.PartNumber } })
  disks = @($d | ForEach-Object { [pscustomobject]@{ name=$_.FriendlyName; type=""$($_.MediaType)""; bus=""$($_.BusType)""; size=$_.Size; health=""$($_.HealthStatus)"" } })
  os = [pscustomobject]@{ caption=$os.Caption; version=$os.Version; installed=$os.InstallDate.ToString('yyyy-MM-dd'); boot=$os.LastBootUpTime.ToString('yyyy-MM-dd HH:mm') }
} | ConvertTo-Json -Depth 5 -Compress";

    public static List<InfoSection> Collect(SystemInfo sys)
    {
        var result = new List<InfoSection>();
        JsonElement root;
        try
        {
            var r = Shell.PowerShell(Script, 60_000);
            if (!r.Ok || string.IsNullOrWhiteSpace(r.Output)) throw new InvalidOperationException(r.Error);
            root = JsonDocument.Parse(r.Output).RootElement;
        }
        catch (Exception ex)
        {
            Log.Error("Hardware query failed: " + ex.Message);
            result.Add(new InfoSection("System", "", new()
            {
                new("Operating system", sys.FriendlyOs), new("Processor", sys.Cpu),
                new("Graphics", string.Join(", ", sys.Gpus)), new("Memory", $"{sys.RamGb:0.#} GB"), new("Device", sys.DeviceType),
            }));
            return result;
        }

        static IEnumerable<JsonElement> Items(JsonElement e) =>
            e.ValueKind == JsonValueKind.Array ? e.EnumerateArray() : e.ValueKind == JsonValueKind.Object ? new[] { e } : Array.Empty<JsonElement>();
        static string S(JsonElement e, string n) =>
            e.TryGetProperty(n, out var v) && v.ValueKind != JsonValueKind.Null ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()) : "";
        static double N(JsonElement e, string n) =>
            e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

        // ---- Operating system
        var os = root.GetProperty("os");
        result.Add(new InfoSection("Operating system", "", new()
        {
            new("Windows", sys.FriendlyOs),
            new("Edition", S(os, "caption")),
            new("Installed", S(os, "installed")),
            new("Last restart", S(os, "boot")),
            new("Device type", sys.DeviceType),
            new("Domain", sys.DomainJoined ? "Joined to a domain" : "Not joined"),
        }));

        // ---- CPU
        var cpu = root.GetProperty("cpu");
        var cpuRows = new List<InfoRow>
        {
            new("Model", S(cpu, "name")),
            new("Cores", $"{N(cpu, "cores"):0} cores  ·  {N(cpu, "threads"):0} threads"),
            new("Base clock", $"{N(cpu, "mhz") / 1000.0:0.00} GHz"),
        };
        if (N(cpu, "l3") > 0) cpuRows.Add(new("L3 cache", $"{N(cpu, "l3") / 1024.0:0.#} MB"));
        if (S(cpu, "socket").Length > 0) cpuRows.Add(new("Socket", S(cpu, "socket")));
        cpuRows.Add(new("Vendor", sys.CpuBrand + (sys.HasX3D ? "  ·  3D V-Cache" : "")));
        result.Add(new InfoSection("Processor", "", cpuRows));

        // ---- GPU
        var gpuRows = new List<InfoRow>();
        var gi = 0;
        foreach (var g in Items(root.GetProperty("gpu")))
        {
            gi++;
            var name = S(g, "name");
            var tag = gi > 1 ? $" {gi}" : "";
            gpuRows.Add(new("Graphics" + tag, name));
            gpuRows.Add(new("Driver" + tag, S(g, "driver")));
            var vram = N(g, "vram");
            // the real size comes from the driver's registry entry; Win32_VideoController.AdapterRAM is 32-bit and stops
            // just under 4 GB, so a value in that last stretch is the cap, not the card, and is left out
            if (vram > 0 && (vram < 4.0e9 || vram >= 4294967296.0)) gpuRows.Add(new("VRAM" + tag, $"{vram / 1073741824.0:0.#} GB"));
            if (N(g, "w") > 0) gpuRows.Add(new("Display" + tag, $"{N(g, "w"):0} × {N(g, "h"):0} @ {N(g, "hz"):0} Hz"));
        }
        if (gpuRows.Count > 0) result.Add(new InfoSection("Graphics", "", gpuRows));

        // ---- Memory
        var mods = Items(root.GetProperty("ram")).ToList();
        var ramRows = new List<InfoRow> { new("Installed", $"{sys.RamGb:0.#} GB") };
        if (mods.Count > 0)
        {
            ramRows.Add(new("Modules", $"{mods.Count} × {mods.Average(x => N(x, "cap")) / 1073741824.0:0.#} GB"));
            var mhz = mods.Max(x => N(x, "mhz"));
            if (mhz > 0) ramRows.Add(new("Speed", $"{mhz:0} MT/s"));
            var maker = S(mods[0], "maker").Trim();
            if (maker.Length > 0 && !maker.StartsWith("0000")) ramRows.Add(new("Maker", maker));
        }
        result.Add(new InfoSection("Memory", "", ramRows));

        // ---- Board
        result.Add(new InfoSection("Motherboard", "", new()
        {
            new("Board", S(root, "board")),
            new("BIOS", S(root, "bios")),
        }));

        // ---- Storage
        var diskRows = new List<InfoRow>();
        var di = 0;
        foreach (var d in Items(root.GetProperty("disks")))
        {
            // name on one line, the facts under it, so nothing gets cut off
            di++;
            var size = N(d, "size") / 1e9;
            var sizeText = size >= 1000 ? $"{size / 1000:0.#} TB" : $"{size:0} GB";
            diskRows.Add(new($"Drive {di}", $"{S(d, "name")}\n{sizeText} {S(d, "type")}  ·  {S(d, "bus")}  ·  {S(d, "health")}"));
        }
        if (diskRows.Count > 0) result.Add(new InfoSection("Storage", "", diskRows));

        return result;
    }
}
