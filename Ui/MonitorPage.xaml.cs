using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>One stat under a device's graph: "Load 22 %".</summary>
public sealed record MonitorStat(string Label, string Value);

/// <summary>One device on the Temperatures page (processor, a graphics card, memory), with a minute of history.</summary>
public sealed class MonitorDeviceVM : Notify
{
    const int Samples = 60;
    const double GraphW = 300, GraphH = 80;
    readonly Queue<double> _history = new();
    readonly bool _isTemp;

    public MonitorDeviceVM(string key, string title, string name, string glyph, bool isTemp)
    {
        Key = key; Title = title; Name = name; Glyph = glyph; _isTemp = isTemp;
        GraphLabel = isTemp ? "Temperature, last 60 seconds" : "In use, last 60 seconds";
    }

    public string Key { get; }
    public string Title { get; }
    public string Name { get; }
    public string Glyph { get; }
    public string GraphLabel { get; }

    string _head = "–", _level = "none", _levelText = "", _peak = "";
    double _peakValue = double.MinValue;
    List<MonitorStat> _stats = new();
    PointCollection _line = new(), _area = new();

    public string Headline { get => _head; private set => Set(ref _head, value); }
    /// <summary>"ok", "warm", "hot" or "none": decides the colour of the big figure and the chip.</summary>
    public string Level { get => _level; private set => Set(ref _level, value); }
    public string LevelText { get => _levelText; private set => Set(ref _levelText, value); }
    public string Peak { get => _peak; private set => Set(ref _peak, value); }
    public List<MonitorStat> Stats { get => _stats; private set => Set(ref _stats, value); }
    public PointCollection Line { get => _line; private set => Set(ref _line, value); }
    public PointCollection Area { get => _area; private set => Set(ref _area, value); }

    /// <summary>Takes this device's readings from one sensor pass.</summary>
    public void Update(double? graphValue, string headline, string level, List<MonitorStat> stats, bool hotIsCpu)
    {
        Headline = headline;
        Level = level;
        LevelText = level switch { "ok" => _isTemp ? "Cool" : "Normal", "warm" => _isTemp ? "Warm" : "Busy", "hot" => _isTemp ? "Hot" : "Nearly full", _ => "" };
        Stats = stats;
        if (graphValue is not double v) return;

        _history.Enqueue(v);
        while (_history.Count > Samples) _history.Dequeue();
        if (v > _peakValue) _peakValue = v;
        Peak = _isTemp ? $"Peak {_peakValue:0} °C" : $"Peak {_peakValue:0} %";

        // fixed scale so the line means the same thing on every card: 20-100 °C, or 0-100 %
        double lo = _isTemp ? 20 : 0, hi = 100;
        var pts = _history.ToList();
        var step = GraphW / (Samples - 1);
        var x0 = GraphW - step * (pts.Count - 1);
        var line = new PointCollection();
        for (var i = 0; i < pts.Count; i++)
            line.Add(new Point(x0 + i * step, GraphH - Math.Clamp((pts[i] - lo) / (hi - lo), 0, 1) * GraphH));
        var area = new PointCollection(line) { new Point(GraphW, GraphH), new Point(x0, GraphH) };
        line.Freeze(); area.Freeze();
        Line = line; Area = area;
    }
}

/// <summary>
/// Live temperatures and load: one card per device, each with its figure, a minute of history and the details.
/// Sensors start quietly at app startup, and only update while this page is open.
/// </summary>
public partial class MonitorPage : UserControl
{
    Sensors? _sensors;
    Task? _start;
    bool _failed, _busy;
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    readonly List<MonitorDeviceVM> _devices = new();
    string _cpu = "Processor", _ram = "System memory";
    List<string> _gpus = new();

    public MonitorPage()
    {
        InitializeComponent();
        _timer.Tick += async (_, _) => await TickAsync();
    }

    public void Init(SystemInfo sys)
    {
        _cpu = string.IsNullOrWhiteSpace(sys.Cpu) ? "Processor" : sys.Cpu;
        _gpus = sys.Gpus.ToList();
        if (sys.RamGb > 0) _ram = $"{sys.RamGb:0.#} GB usable";
    }

    /// <summary>Starts the sensor driver in the background (it takes a moment), so the page opens with figures.</summary>
    public Task Preload() => _start ??= StartAsync();

    async Task StartAsync()
    {
        _sensors = new Sensors();
        _failed = !await Task.Run(() => _sensors.Start());
    }

    public async void Show()
    {
        await Preload();
        if (!IsVisible) return;
        if (_failed)
        {
            Note.Text = "Live sensors could not start on this PC (" + (_sensors?.Problem ?? "unknown reason") + ").";
            return;
        }
        await TickAsync();
        _timer.Start();
    }

    public void Hide() => _timer.Stop();

    public void Shutdown() { _timer.Stop(); _sensors?.Dispose(); _sensors = null; }

    async Task TickAsync()
    {
        if (_sensors == null || _busy) return;
        _busy = true;
        try
        {
            var readings = await Task.Run(() => _sensors.Read());
            if (!IsVisible) { _timer.Stop(); return; }
            Apply(readings);
        }
        catch (Exception ex) { Log.Error("Temperatures update failed: " + ex.Message); }
        finally { _busy = false; }
    }

    MonitorDeviceVM Device(string key, string title, string name, string glyph, bool isTemp)
    {
        var d = _devices.FirstOrDefault(x => x.Key == key);
        if (d != null) return d;
        d = new MonitorDeviceVM(key, title, name, glyph, isTemp);
        _devices.Add(d);
        return d;
    }

    void Apply(List<SensorReading> readings)
    {
        var count = _devices.Count;
        static string Kind(string label) => label[(label.LastIndexOf(' ') + 1)..];
        static string Cap(string w) => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..];

        // "CPU temperature", "GPU 2 load", "RAM used": everything before the last word names the device
        foreach (var g in readings.GroupBy(r => r.Label.Contains(' ') ? r.Label[..r.Label.LastIndexOf(' ')] : r.Label))
        {
            var rows = g.ToList();
            if (g.Key == "RAM")
            {
                var used = rows[0];
                var dev = Device("RAM", "Memory", _ram, "", false);
                var pct = used.Pct >= 0 ? used.Pct : (double?)null;
                var lvl = pct is null ? "none" : pct >= 90 ? "hot" : pct >= 75 ? "warm" : "ok";
                dev.Update(pct, used.Value.Replace(" GB", "") + " GB", lvl,
                    new List<MonitorStat> { new("In use", pct is null ? "–" : $"{pct:0} %") }, false);
                continue;
            }
            var isCpu = g.Key == "CPU";
            var gpuIndex = isCpu ? -1 : g.Key == "GPU" ? 0 : int.TryParse(g.Key.Replace("GPU", "").Trim(), out var n) ? n - 1 : 0;
            var sensorGpus = _sensors?.GpuNames ?? new List<string>();
            var name = isCpu ? _cpu
                     : gpuIndex >= 0 && gpuIndex < sensorGpus.Count ? sensorGpus[gpuIndex]
                     : gpuIndex >= 0 && gpuIndex < _gpus.Count ? _gpus[gpuIndex] : "Graphics card";
            var device = Device(g.Key, isCpu ? "Processor" : g.Key == "GPU" ? "Graphics" : "Graphics " + (gpuIndex + 1), name, isCpu ? "" : "", true);

            var temp = rows.FirstOrDefault(r => Kind(r.Label) == "temperature");
            double? t = null;
            if (temp != null && double.TryParse(temp.Value.Replace("°C", "").Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out var tv)) t = tv;
            var hot = isCpu ? 85 : 83;
            var level = t is null ? "none" : t >= hot ? "hot" : t >= hot - 15 ? "warm" : "ok";
            var stats = rows.Where(r => r != temp).Select(r => new MonitorStat(Cap(Kind(r.Label)), r.Value)).ToList();
            device.Update(t, t is null ? "No reading" : $"{t:0} °C", level, stats, isCpu);
        }

        if (_devices.Count != count)
            Cards.ItemsSource = _devices.OrderBy(d => d.Key == "CPU" ? 0 : d.Key.StartsWith("GPU") ? 1 : 2).ThenBy(d => d.Key).ToList();

        var cpuTemp = readings.Any(r => r.Label == "CPU temperature");
        Note.Text = readings.Count == 0
            ? "No sensors reported anything. Virtual machines and some laptops hide them."
            : cpuTemp ? "Updates every second while this page is open. Peaks count from when you first opened this page."
            : "This processor did not report a temperature. Reading it needs the PawnIO driver (pawnio.eu), which can be installed once.";
    }
}
