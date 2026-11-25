using LibreHardwareMonitor.Hardware;

namespace TrapShack.Core;

public sealed record SensorReading(string Label, string Value, bool Hot, double Pct = -1);

/// <summary>
/// Live temperatures, clocks and load through LibreHardwareMonitor. Its CPU access uses the PawnIO driver, which is loaded on demand;
/// if the driver or sensors are unavailable the app keeps working and simply reports no readings.
/// </summary>
public sealed class Sensors : IDisposable
{
    Computer? _pc;
    public string? Problem { get; private set; }

    public bool Start()
    {
        try
        {
            _pc = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMemoryEnabled = true, IsMotherboardEnabled = false, IsStorageEnabled = false };
            _pc.Open();
            return true;
        }
        catch (Exception ex)
        {
            Problem = ex.Message;
            Log.Error("Sensors unavailable: " + ex.Message);
            _pc = null;
            return false;
        }
    }

    public List<SensorReading> Read()
    {
        var list = new List<SensorReading>();
        if (_pc == null) return list;
        try
        {
            var gpus = _pc.Hardware.Where(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel).ToList();
            foreach (var hw in _pc.Hardware)
            {
                hw.Update();
                foreach (var sub in hw.SubHardware) sub.Update();

                if (hw.HardwareType == HardwareType.Memory)
                {
                    var used = hw.Sensors.FirstOrDefault(x => x.SensorType == SensorType.Data && x.Name == "Memory Used")?.Value;
                    var free = hw.Sensors.FirstOrDefault(x => x.SensorType == SensorType.Data && x.Name == "Memory Available")?.Value;
                    var pct = hw.Sensors.FirstOrDefault(x => x.SensorType == SensorType.Load && x.Name == "Memory")?.Value;
                    if (used.HasValue && free.HasValue)
                        list.Add(new("RAM used", $"{used.Value:0.0} / {used.Value + free.Value:0.0} GB", pct >= 90, pct ?? used.Value / (used.Value + free.Value) * 100));
                    else if (pct.HasValue)
                        list.Add(new("RAM used", $"{pct.Value:0} %", pct >= 90, pct.Value));
                    continue;
                }

                var isGpu = gpus.Contains(hw);
                var isCpu = hw.HardwareType == HardwareType.Cpu;
                if (!isGpu && !isCpu) continue;
                var tag = isCpu ? "CPU" : gpus.Count > 1 ? $"GPU {gpus.IndexOf(hw) + 1}" : "GPU";

                var all = hw.Sensors.Concat(hw.SubHardware.SelectMany(x => x.Sensors)).ToList();
                var temps = all.Where(x => x.SensorType == SensorType.Temperature && x.Value.HasValue).ToList();
                var main = isCpu
                    ? temps.FirstOrDefault(x => x.Name.Contains("Package") || x.Name.Contains("Tctl"))
                      ?? temps.FirstOrDefault(x => x.Name.Contains("Core Max") || x.Name.Contains("Average"))
                      ?? temps.FirstOrDefault(x => x.Name.Contains("Core"))
                      ?? temps.FirstOrDefault()
                    : temps.FirstOrDefault(x => x.Name == "GPU Core") ?? temps.FirstOrDefault(x => x.Name.Contains("Core"))
                      ?? temps.FirstOrDefault();
                if (main != null) list.Add(new($"{tag} temperature", $"{main.Value:0} °C", main.Value >= (isCpu ? 85 : 83)));

                var load = all.FirstOrDefault(x => x.SensorType == SensorType.Load && (x.Name == "CPU Total" || x.Name == "GPU Core"));
                if (load?.Value != null) list.Add(new($"{tag} load", $"{load.Value:0} %", false, load.Value.Value));

                var clk = isCpu
                    ? all.Where(x => x.SensorType == SensorType.Clock && x.Name.StartsWith("CPU Core") && x.Value.HasValue).Select(x => x.Value!.Value).DefaultIfEmpty(0).Max()
                    : all.FirstOrDefault(x => x.SensorType == SensorType.Clock && x.Name == "GPU Core")?.Value ?? 0;
                if (clk > 0) list.Add(new($"{tag} clock", $"{clk / 1000.0:0.00} GHz", false));

                var pw = all.FirstOrDefault(x => x.SensorType == SensorType.Power && x.Value.HasValue
                    && (x.Name.Contains("Package") || x.Name == "GPU Package" || x.Name == "GPU Power"));
                if (pw?.Value != null) list.Add(new($"{tag} power", $"{pw.Value:0} W", false));

                if (isGpu)
                {
                    var vu = all.FirstOrDefault(x => x.Name == "GPU Memory Used")?.Value;
                    var vt = all.FirstOrDefault(x => x.Name == "GPU Memory Total")?.Value;
                    if (vu.HasValue && vt is > 0) list.Add(new($"{tag} memory", $"{vu.Value / 1024:0.0} / {vt.Value / 1024:0.0} GB", vu / vt > 0.92, vu.Value / vt.Value * 100));
                }
            }
        }
        catch (Exception ex) { Log.Error("Sensor read failed: " + ex.Message); }
        return list;
    }

    public void Dispose() { try { _pc?.Close(); } catch { } _pc = null; }
}
