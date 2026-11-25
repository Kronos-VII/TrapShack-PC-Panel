using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>One cleaning rule on the page.</summary>
public sealed class CleanRuleVM : Notify
{
    public CleanRuleVM(CleanRule rule) { Rule = rule; }
    public CleanRule Rule { get; }
    public string Name => Rule.Name;
    public Action? Changed;

    bool _checked = true, _busy, _edit = true;
    public bool Checked { get => _checked; set { if (Set(ref _checked, value)) Changed?.Invoke(); } }
    public bool IsBusy { get => _busy; set => Set(ref _busy, value); }
    public bool CanEdit { get => _edit; set => Set(ref _edit, value); }

    CleanScan? _scan;
    public CleanScan? Scan
    {
        get => _scan;
        set { _scan = value; Raise(nameof(SizeText)); Raise(nameof(HasFound)); }
    }
    public long Bytes => _scan?.Bytes ?? 0;
    public bool HasFound => _scan is { Count: > 0 };
    public string SizeText => _busy ? "…" : _scan == null ? "" : _scan.Count == 0 ? "clean" : _scan.Bytes > 0 ? CleanScan.Format(_scan.Bytes) : $"{_scan.Count} items";
    public void RaiseSize() => Raise(nameof(SizeText));
}

/// <summary>One card: the rules of one program or area (Windows, Google Chrome ...).</summary>
public sealed class CleanCategoryVM : Notify
{
    public string Name { get; init; } = "";
    public string Glyph { get; init; } = "";
    public int Order { get; init; }
    public List<CleanRuleVM> Rules { get; init; } = new();

    bool _edit = true;
    public bool CanEdit { get => _edit; set => Set(ref _edit, value); }

    /// <summary>Ticks or unticks every rule; shows a dash (null) when only some are ticked.</summary>
    public bool? AllChecked
    {
        get => Rules.All(r => r.Checked) ? true : Rules.Any(r => r.Checked) ? false : false;
        set { var v = value == true; foreach (var r in Rules) r.Checked = v; Raise(); }
    }

    public string Summary => $"{Rules.Count(r => r.Checked)} of {Rules.Count} ticked";
    public string SizeText { get { var b = Rules.Where(r => r.Checked).Sum(r => r.Bytes); return b > 0 ? CleanScan.Format(b) : ""; } }
    public void Refresh() { Raise(nameof(Summary)); Raise(nameof(SizeText)); Raise(nameof(AllChecked)); }
}

/// <summary>
/// The PC Cleaner page: finds which programs are on this PC, ticks every safe rule for them, scans (read only),
/// then deletes what the scan found when the user presses Clean.
/// </summary>
public partial class CleanerPage : UserControl
{
    IPageHost? _host;
    List<CleanCategoryVM> _all = new();
    List<CleanCategoryVM> _shown = new();   // filtered copies shown while searching
    IEnumerable<CleanCategoryVM> AllCards => _all.Concat(_shown);
    bool _loaded, _loading, _working, _scanned;
    CancellationTokenSource? _cts;
    double _arc;   // 0..1, how much of the ring is drawn

    public CleanerPage()
    {
        InitializeComponent();
        DrawArc(0);
    }

    public void Init(IPageHost host) => _host = host;
    public void FocusSearch() => Search.Focus();

    // ------------------------------------------------------------------------------------------ loading
    public async Task LoadAsync()
    {
        if (_loaded || _loading) return;
        _loading = true;
        Loading.Visibility = Visibility.Visible;
        try
        {
            var cats = await Task.Run(() =>
            {
                var rules = CleanerEngine.LoadBundledRules();
                var det = new CleanerEngine.Detector();
                var found = new System.Collections.Concurrent.ConcurrentBag<CleanRule>();
                Parallel.ForEach(rules, r => { if (det.IsInstalled(r)) found.Add(r); });
                return found
                    .GroupBy(r => CleanerEngine.CategoryOf(r))
                    .OrderBy(g => g.Key.Order).ThenBy(g => g.Key.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => (g.Key.Name, g.Key.Order, Rules: g.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList()))
                    .ToList();
            });
            _all = cats.Select(c => new CleanCategoryVM
            {
                Name = c.Name, Order = c.Order, Glyph = GlyphFor(c.Name),
                Rules = c.Rules.Select(r => new CleanRuleVM(r)).ToList(),
            }).ToList();
            foreach (var cat in _all)
                foreach (var r in cat.Rules) { var rule = r; r.Changed = () => { foreach (var c in AllCards.Where(c => c.Rules.Contains(rule))) c.Refresh(); UpdateChips(); }; }
            _loaded = true;
        }
        catch (Exception ex) { Log.Error("Cleaner rules failed to load: " + ex.Message); StateLine.Text = "The cleaning rules could not be read: " + ex.Message; }
        finally { _loading = false; Loading.Visibility = Visibility.Collapsed; }
        Rebuild();
        UpdateChips();
    }

    static string GlyphFor(string category) => category switch
    {
        "Windows" => "",
        "Microsoft Store" => "",
        "Multimedia" or "Spotify" or "iTunes" => "",
        "Utilities" => "",
        "Applications" or "Other applications" => "",
        "Thunderbird" => "",
        "WhatsApp" => "",
        _ => "",   // browsers and the internet group
    };

    void Rebuild()
    {
        var q = Search.Text.Trim();
        if (q.Length == 0) { _shown = new(); Cards.ItemsSource = _all; return; }
        // while searching, show matching rules only (the ticks stay on the real rules)
        Cards.ItemsSource = _all
            .Select(c => (c, rules: c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ? c.Rules
                                   : c.Rules.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList()))
            .Where(x => x.rules.Count > 0)
            .Select(x => new CleanCategoryVM { Name = x.c.Name, Glyph = x.c.Glyph, Order = x.c.Order, Rules = x.rules, CanEdit = !_working })
            .ToList();
        _shown = (List<CleanCategoryVM>)Cards.ItemsSource;
    }

    void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        Rebuild();
    }

    void OnRuleRowClick(object sender, MouseButtonEventArgs e)
    {
        if (_working || e.OriginalSource is DependencyObject d && FindParent<CheckBox>(d) != null) return;
        if (((FrameworkElement)sender).DataContext is CleanRuleVM r && r.CanEdit) r.Checked = !r.Checked;
    }

    static T? FindParent<T>(DependencyObject d) where T : DependencyObject
    {
        while (d != null)
        {
            if (d is T t) return t;
            d = d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return null;
    }

    void OnResetTicks(object sender, RoutedEventArgs e)
    {
        if (_working) return;
        foreach (var r in _all.SelectMany(c => c.Rules)) r.Checked = true;
    }

    IEnumerable<CleanRuleVM> Ticked => _all.SelectMany(c => c.Rules).Where(r => r.Checked);

    void UpdateChips()
    {
        var rules = _all.SelectMany(c => c.Rules).ToList();
        var chips = new List<string>
        {
            $"{_all.Count} programs and areas found",
            $"{rules.Count(r => r.Checked)} of {rules.Count} rules ticked",
        };
        if (_scanned)
        {
            var files = Ticked.Sum(r => r.Scan?.Files.Count ?? 0);
            var reg = Ticked.Sum(r => r.Scan?.Registry.Count ?? 0);
            chips.Add($"{files:N0} files");
            if (reg > 0) chips.Add($"{reg:N0} registry items");
        }
        Chips.ItemsSource = chips;
        if (_scanned && !_working)
        {
            var total = Ticked.Sum(r => r.Bytes);
            BigSize.Text = CleanScan.Format(total);
            CleanBtn.IsEnabled = Ticked.Any(r => r.HasFound);
        }
    }

    // ------------------------------------------------------------------------------------------ scan
    async void OnScanClick(object sender, RoutedEventArgs e)
    {
        if (_working) { _cts?.Cancel(); return; }
        if (!_loaded) { await LoadAsync(); if (!_loaded) return; }
        var queue = Ticked.ToList();
        if (queue.Count == 0) { StateTitle.Text = "Nothing is ticked"; StateLine.Text = "Tick at least one rule, or press “Tick the safe ones”."; return; }

        Begin(true);
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        foreach (var r in _all.SelectMany(c => c.Rules)) r.Scan = null;
        long total = 0;
        var done = 0;
        try
        {
            foreach (var r in queue)
            {
                token.ThrowIfCancellationRequested();
                r.IsBusy = true; r.RaiseSize();
                StateTitle.Text = "Scanning " + r.Name;
                StateLine.Text = $"{done + 1} of {queue.Count}";
                var res = await Task.Run(() => CleanerEngine.Scan(r.Rule, token), token);
                r.IsBusy = false; r.Scan = res;
                total += res.Bytes;
                done++;
                BigSize.Text = CleanScan.Format(total);
                AnimateArc((double)done / queue.Count);
                foreach (var c in AllCards) if (c.Rules.Contains(r)) c.Refresh();
            }
            _scanned = true;
            var found = queue.Count(r => r.HasFound);
            StateTitle.Text = total > 0 ? $"{CleanScan.Format(total)} can be cleaned" : found > 0 ? "A few items can be cleaned" : "This PC is already clean";
            StateLine.Text = found > 0
                ? $"{found} rules found something. Untick anything you want to keep, then press Clean. Close your browsers first for a full clean."
                : "Nothing to clean right now.";
            BigLabel.Text = "found";
        }
        catch (OperationCanceledException)
        {
            foreach (var r in queue) { r.IsBusy = false; r.RaiseSize(); }
            _scanned = done > 0;
            StateTitle.Text = "Scan stopped";
            StateLine.Text = $"Stopped after {done} of {queue.Count} rules. What was found so far can still be cleaned.";
        }
        catch (Exception ex)
        {
            Log.Error("Cleaner scan failed: " + ex);
            StateTitle.Text = "The scan ran into a problem";
            StateLine.Text = ex.Message;
        }
        finally { End(); }
    }

    // ------------------------------------------------------------------------------------------ clean
    async void OnCleanClick(object sender, RoutedEventArgs e)
    {
        if (_working || _host == null || _host.Busy) return;
        var queue = Ticked.Where(r => r.HasFound).ToList();
        if (queue.Count == 0) return;
        var files = queue.Sum(r => r.Scan!.Files.Count);
        var reg = queue.Sum(r => r.Scan!.Registry.Count);
        var bytes = queue.Sum(r => r.Bytes);
        if (!Dialogs.Confirm(_host.Win, "Clean this PC?",
                $"{files:N0} files ({CleanScan.Format(bytes)})" + (reg > 0 ? $" and {reg:N0} registry items" : "") +
                $" from {queue.Count} rules will be deleted. Deleted files can't be brought back.\n\n" +
                "Files that a running program is using are skipped. Close your browsers first for a full clean.", "Clean", "Cancel"))
            return;

        Begin(false);
        _host.Busy = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        int count = 0; long freed = 0; var done = 0;
        try
        {
            foreach (var r in queue)
            {
                token.ThrowIfCancellationRequested();
                r.IsBusy = true; r.RaiseSize();
                StateTitle.Text = "Cleaning " + r.Name;
                StateLine.Text = $"{done + 1} of {queue.Count}";
                var scan = r.Scan!;
                var (c, b) = await Task.Run(() => CleanerEngine.Clean(scan, token), token);
                count += c; freed += b; done++;
                r.IsBusy = false; r.Scan = new CleanScan { Rule = r.Rule };
                BigSize.Text = CleanScan.Format(freed);
                AnimateArc(1 - (double)done / queue.Count);
                foreach (var cat in AllCards) if (cat.Rules.Contains(r)) cat.Refresh();
            }
            StateTitle.Text = $"Cleaned {CleanScan.Format(freed)}";
            StateLine.Text = $"{count:N0} items removed. Scan again any time.";
        }
        catch (OperationCanceledException)
        {
            StateTitle.Text = $"Stopped. Cleaned {CleanScan.Format(freed)}";
            StateLine.Text = $"{count:N0} items removed before stopping.";
        }
        catch (Exception ex)
        {
            Log.Error("Cleaner clean failed: " + ex);
            StateTitle.Text = "Cleaning ran into a problem";
            StateLine.Text = ex.Message;
        }
        finally
        {
            foreach (var r in queue) { r.IsBusy = false; r.RaiseSize(); }
            _host.Busy = false;
            _scanned = false;
            End();
            BigSize.Text = CleanScan.Format(freed);
            BigLabel.Text = "freed";
            CleanBtn.IsEnabled = false;
            AnimateArc(1);
        }
    }

    // ------------------------------------------------------------------------------------------ look
    void Begin(bool scanning)
    {
        _working = true;
        foreach (var c in AllCards) { c.CanEdit = false; foreach (var r in c.Rules) r.CanEdit = false; }
        ScanText.Text = "Stop"; ScanIcon.Text = "";
        CleanBtn.IsEnabled = false; ResetBtn.IsEnabled = false;
        BigLabel.Text = scanning ? "found so far" : "freed so far";
        BigSize.Text = "0 B";
        if (scanning) AnimateArc(0);
        Sweep.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
        SweepTurn.BeginAnimation(RotateTransform.AngleProperty,
            new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1.1)) { RepeatBehavior = RepeatBehavior.Forever });
    }

    void End()
    {
        _working = false;
        foreach (var c in AllCards) { c.CanEdit = true; foreach (var r in c.Rules) r.CanEdit = true; c.Refresh(); }
        ScanText.Text = _scanned ? "Scan again" : "Scan"; ScanIcon.Text = "";
        ResetBtn.IsEnabled = true;
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(400));
        fade.Completed += (_, _) => SweepTurn.BeginAnimation(RotateTransform.AngleProperty, null);
        Sweep.BeginAnimation(OpacityProperty, fade);
        UpdateChips();
    }

    /// <summary>Smoothly moves the ring to a new fill (0..1).</summary>
    void AnimateArc(double to)
    {
        var from = _arc;
        var start = DateTime.Now;
        const double ms = 350;
        EventHandler? tick = null;
        tick = (_, _) =>
        {
            var t = Math.Min(1, (DateTime.Now - start).TotalMilliseconds / ms);
            var eased = 1 - Math.Pow(1 - t, 3);
            DrawArc(from + (to - from) * eased);
            if (t >= 1) CompositionTarget.Rendering -= tick;
        };
        CompositionTarget.Rendering += tick;
    }

    void DrawArc(double f)
    {
        _arc = Math.Clamp(f, 0, 1);
        const double size = 156, r = (size - 8 - 9) / 2;   // inside the 4px margin, centred on the 9px stroke
        var c = new Point(size / 2, size / 2);
        if (_arc <= 0.001) { Arc.Data = null; return; }
        if (_arc >= 0.999) { Arc.Data = new EllipseGeometry(c, r, r); return; }
        var a0 = -Math.PI / 2;
        var a1 = a0 + _arc * 2 * Math.PI;
        var p0 = new Point(c.X + r * Math.Cos(a0), c.Y + r * Math.Sin(a0));
        var p1 = new Point(c.X + r * Math.Cos(a1), c.Y + r * Math.Sin(a1));
        var fig = new PathFigure { StartPoint = p0, IsClosed = false };
        fig.Segments.Add(new ArcSegment(p1, new Size(r, r), 0, _arc > 0.5, SweepDirection.Clockwise, true));
        Arc.Data = new PathGeometry(new[] { fig });
    }
}
