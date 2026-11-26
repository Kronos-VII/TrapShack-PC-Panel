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
    public CleanRuleVM(CleanRule rule) { Rule = rule; _checked = !rule.ShaderCache; }
    public CleanRule Rule { get; }
    public string Name => Rule.Name;
    public Action? Changed;

    bool _checked = true, _busy, _edit = true;
    public bool Checked { get => _checked; set { if (Set(ref _checked, value)) { Raise(nameof(Sub)); Changed?.Invoke(); } } }
    public bool IsBusy { get => _busy; set { if (Set(ref _busy, value)) { Raise(nameof(SizeText)); Raise(nameof(Sub)); } } }
    public bool CanEdit { get => _edit; set => Set(ref _edit, value); }

    CleanScan? _scan;
    public CleanScan? Scan
    {
        get => _scan;
        set { _scan = value; Raise(nameof(SizeText)); Raise(nameof(HasFound)); Raise(nameof(Sub)); }
    }
    public long Bytes => _scan?.Bytes ?? 0;
    public bool HasFound => _scan is { Count: > 0 };
    public bool Scanned => _scan != null;
    public string SizeText => _busy ? "…" : _scan == null ? "" : _scan.Count == 0 ? "Clean" : _scan.Bytes > 0 ? CleanScan.Format(_scan.Bytes) : $"{_scan.Count} items";

    /// <summary>The line under the name: what the scan found.</summary>
    public string Sub
    {
        get
        {
            if (_busy) return "Checking…";
            // shader caches say why they start unticked
            var note = Rule.ShaderCache && !_checked ? "Not ticked by default: games rebuild it and may stutter for a while" : null;
            if (_scan == null) return note ?? "Not scanned yet";
            if (note != null && _scan.Count > 0)
                return (_scan.Files.Count == 1 ? "1 file" : $"{_scan.Files.Count:N0} files") + "  ·  " + "not ticked by default, games rebuild it";
            if (_scan.Count == 0) return "Nothing to clean";
            var parts = new List<string>();
            if (_scan.Files.Count > 0) parts.Add(_scan.Files.Count == 1 ? "1 file" : $"{_scan.Files.Count:N0} files");
            if (_scan.Registry.Count > 0) parts.Add(_scan.Registry.Count == 1 ? "1 registry item" : $"{_scan.Registry.Count:N0} registry items");
            return string.Join("  ·  ", parts);
        }
    }
    public void RaiseSize() { Raise(nameof(SizeText)); Raise(nameof(Sub)); }
}

/// <summary>One program or area (Windows, Google Chrome ...) and its rules. Shown in the list on the left.</summary>
public sealed class CleanCategoryVM : Notify
{
    public string Name { get; init; } = "";
    public string Title => Name;
    public string Glyph { get; init; } = "";
    public int Order { get; init; }
    public List<CleanRuleVM> Rules { get; init; } = new();

    bool _sel;
    public bool Selected { get => _sel; set => Set(ref _sel, value); }

    public long Bytes => Rules.Where(r => r.Checked).Sum(r => r.Bytes);
    public bool Scanned => Rules.All(r => r.Scanned);
    public bool HasFound => Rules.Any(r => r.HasFound);
    public int Ticked => Rules.Count(r => r.Checked);

    /// <summary>The figure on the right of the list entry: the size found, or how many rules before a scan.</summary>
    public string CountText => !Scanned ? $"{Rules.Count}" : HasFound ? (Bytes > 0 ? CleanScan.Format(Bytes) : $"{Rules.Where(r => r.Checked).Sum(r => r.Scan?.Count ?? 0)} items") : "Clean";
    public void Refresh() { Raise(nameof(CountText)); }
}

/// <summary>
/// The PC Cleaner page: finds which programs are on this PC, ticks every safe rule for them, scans (read only),
/// then deletes what the scan found when the user presses Clean. Programs on the left, biggest first; the chosen
/// program's rules on the right.
/// </summary>
public partial class CleanerPage : UserControl
{
    IPageHost? _host;
    List<CleanCategoryVM> _all = new();
    bool _loaded, _working, _scanned, _showEmptyGroups, _showCleanRules;
    Task? _loadTask;
    string? _sel;
    CancellationTokenSource? _cts;

    public CleanerPage() { InitializeComponent(); }

    public void Init(IPageHost host) => _host = host;
    public void FocusSearch() => Search.Focus();

    // ------------------------------------------------------------------------------------------ loading
    /// <summary>Finds the programs on this PC and their safe rules. Runs once; later calls wait for the same work.</summary>
    public Task LoadAsync() => _loadTask ??= LoadCoreAsync();

    async Task LoadCoreAsync()
    {
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
                foreach (var r in cat.Rules)
                {
                    var owner = cat;
                    r.Changed = () => { owner.Refresh(); UpdateHeader(); UpdatePaneHeader(); };
                }
            _loaded = true;
        }
        catch (Exception ex) { Log.Error("Cleaner rules failed to load: " + ex.Message); StateLine.Text = "The cleaning rules could not be read: " + ex.Message; }
        finally { Loading.Visibility = Visibility.Collapsed; }
        Rebuild();
        UpdateHeader();
    }

    static string GlyphFor(string category) => category switch
    {
        "Windows" => "",
        "Microsoft Store" => "",
        "Multimedia" or "Spotify" or "iTunes" => "",
        "Utilities" => "",
        "Applications" or "Other applications" => "",
        "Thunderbird" => "",
        "WhatsApp" or "Discord" or "Communication" => "",
        "Games" => "",
        _ => "",   // browsers and the internet group
    };

    // ------------------------------------------------------------------------------------------ the two columns
    void Rebuild()
    {
        var q = Search.Text.Trim();
        // before a scan: the natural order; after: biggest first, programs with nothing to clean tucked away
        var ordered = _scanned
            ? _all.OrderByDescending(c => c.HasFound).ThenByDescending(c => c.Bytes).ThenBy(c => c.Order).ToList()
            : _all.OrderBy(c => c.Order).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var main = _scanned ? ordered.Where(c => c.HasFound).ToList() : ordered;
        var empty = _scanned ? ordered.Where(c => !c.HasFound).ToList() : new List<CleanCategoryVM>();
        if (_scanned && main.Count == 0) { main = ordered; empty = new(); }   // a clean PC still lists its programs

        if (_sel == null || _all.All(c => c.Name != _sel)) _sel = main.FirstOrDefault()?.Name;
        if (_sel != null && empty.Any(c => c.Name == _sel)) _showEmptyGroups = true;
        foreach (var c in _all) c.Selected = q.Length == 0 && c.Name == _sel;

        RailList.ItemsSource = main;
        EmptyList.ItemsSource = empty;
        EmptyList.Visibility = _showEmptyGroups && empty.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyToggle.Visibility = empty.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyToggle.Content = (_showEmptyGroups ? "Hide " : "Show ") + (empty.Count == 1 ? "1 program with nothing to clean" : $"{empty.Count} programs with nothing to clean");
        RailTitle.Text = _scanned ? "WHERE THE SPACE IS" : $"{_all.Count} PROGRAMS AND AREAS";

        List<CleanRuleVM> rules;
        if (q.Length > 0)
        {
            rules = _all.SelectMany(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ? c.Rules
                                         : c.Rules.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase))).Distinct().ToList();
        }
        else
        {
            var cat = _all.FirstOrDefault(c => c.Name == _sel);
            rules = cat?.Rules ?? new List<CleanRuleVM>();
        }
        var hidden = 0;
        if (_scanned && !_showCleanRules && q.Length == 0)
        {
            var visible = rules.Where(r => r.HasFound || !r.Scanned || r.IsBusy).ToList();
            hidden = rules.Count - visible.Count;
            if (visible.Count > 0) rules = visible; else hidden = 0;   // a group with nothing found still shows its rules
        }
        if (_scanned) rules = rules.OrderByDescending(r => r.Bytes).ThenByDescending(r => r.Scan?.Count ?? 0).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
        Rules.ItemsSource = rules;
        CleanRulesToggle.Visibility = _scanned && q.Length == 0 && (hidden > 0 || _showCleanRules) ? Visibility.Visible : Visibility.Collapsed;
        CleanRulesToggle.Content = _showCleanRules ? "Hide clean rules" : $"Show {hidden} clean rule{(hidden == 1 ? "" : "s")}";
        PaneEmpty.Visibility = rules.Count == 0 && _loaded ? Visibility.Visible : Visibility.Collapsed;
        PaneEmpty.Text = q.Length > 0 ? "Nothing matches that." : "Nothing here.";
        UpdatePaneHeader();
    }

    void UpdatePaneHeader()
    {
        var q = Search.Text.Trim();
        if (q.Length > 0)
        {
            PaneTitle.Text = $"Results for “{q}”";
            PaneSub.Text = "";
            TickAll.Visibility = Visibility.Collapsed;
            return;
        }
        var cat = _all.FirstOrDefault(c => c.Name == _sel);
        TickAll.Visibility = cat != null ? Visibility.Visible : Visibility.Collapsed;
        if (cat == null) { PaneTitle.Text = _loaded ? "Nothing found" : ""; PaneSub.Text = ""; return; }
        PaneTitle.Text = cat.Name;
        PaneSub.Text = $"{cat.Ticked} of {cat.Rules.Count} ticked" + (cat.Scanned && cat.Bytes > 0 ? $"  ·  {CleanScan.Format(cat.Bytes)}" : "");
        TickAll.IsChecked = cat.Rules.All(r => r.Checked);
        TickAll.IsEnabled = !_working;
    }

    void OnRailPick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not CleanCategoryVM c) return;
        if (c.Name == _sel && Search.Text.Length == 0) return;
        _sel = c.Name;
        if (Search.Text.Length > 0) Search.Text = "";   // re-runs Rebuild
        else Rebuild();
        PaneScroll.ScrollToTop();
    }

    void OnToggleEmptyGroups(object sender, RoutedEventArgs e) { _showEmptyGroups = !_showEmptyGroups; Rebuild(); }
    void OnToggleCleanRules(object sender, RoutedEventArgs e) { _showCleanRules = !_showCleanRules; Rebuild(); }

    void OnTickAll(object sender, RoutedEventArgs e)
    {
        var cat = _all.FirstOrDefault(c => c.Name == _sel);
        if (cat == null || _working) return;
        var v = TickAll.IsChecked == true;
        foreach (var r in cat.Rules) r.Checked = v;
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
        // every safe rule except the GPU shader caches, which stay your choice
        foreach (var r in _all.SelectMany(c => c.Rules)) r.Checked = !r.Rule.ShaderCache;
        UpdatePaneHeader();
    }

    IEnumerable<CleanRuleVM> Ticked => _all.SelectMany(c => c.Rules).Where(r => r.Checked);

    /// <summary>The result bar: the figure, the line under it and the Clean button.</summary>
    void UpdateHeader()
    {
        if (_working) return;
        var total = Ticked.Sum(r => r.Bytes);
        var found = Ticked.Where(r => r.HasFound).ToList();
        if (!_scanned)
        {
            StateSize.Text = "";
            StateRest.Text = _loaded ? "Ready to scan" : "Getting ready…";
            StateLine.Text = _loaded ? $"{_all.Count} programs and areas found on this PC. Press Scan to see how much space can be freed." : "Finding the programs on this PC…";
            CleanText.Text = "Clean";
            CleanBtn.IsEnabled = false;
            return;
        }
        var programs = _all.Count(c => c.Rules.Any(r => r.Checked && r.HasFound));
        if (found.Count == 0)
        {
            StateSize.Text = "";
            StateRest.Text = "This PC is already clean";
            StateLine.Text = "Nothing to clean right now. Scan again any time.";
        }
        else
        {
            StateSize.Text = total > 0 ? CleanScan.Format(total) : $"{found.Sum(r => r.Scan!.Count)} items";
            StateRest.Text = " ready to clean";
            StateLine.Text = $"{found.Sum(r => r.Scan!.Count):N0} items across {programs} program{(programs == 1 ? "" : "s")}  ·  close your browsers first for a full clean";
        }
        CleanText.Text = total > 0 ? $"Clean {CleanScan.Format(total)}" : "Clean";
        CleanBtn.IsEnabled = found.Count > 0;
    }

    // ------------------------------------------------------------------------------------------ scan
    /// <summary>Raised after a scan or clean finishes, with the size that can still be freed.</summary>
    public event Action<long>? Measured;
    public bool Busy => _working;
    public bool Scanned => _scanned;

    async void OnScanClick(object sender, RoutedEventArgs e)
    {
        if (_working) { _cts?.Cancel(); return; }
        await ScanAsync();
    }

    /// <summary>
    /// Scans every ticked rule (reads only). Several rules are checked at the same time, so a full scan takes a few
    /// seconds. Also used quietly at startup for the Home page's figure.
    /// </summary>
    public async Task ScanAsync()
    {
        if (_working) return;
        await LoadAsync();
        if (!_loaded) return;
        // shader caches are measured even when unticked, so you can see how big they are before deciding
        var queue = _all.SelectMany(c => c.Rules).Where(r => r.Checked || r.Rule.ShaderCache).ToList();
        if (!Ticked.Any()) { StateSize.Text = ""; StateRest.Text = "Nothing is ticked"; StateLine.Text = "Tick at least one rule, or press “Tick every safe rule again”."; return; }

        Begin("Scanning");
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        foreach (var r in _all.SelectMany(c => c.Rules)) r.Scan = null;
        foreach (var r in queue) r.IsBusy = true;
        var done = 0;
        long total = 0;
        try
        {
            var results = new System.Collections.Concurrent.ConcurrentDictionary<CleanRuleVM, CleanScan>();
            var progress = new Progress<(CleanRuleVM Rule, CleanScan Res)>(p =>
            {
                p.Rule.IsBusy = false; p.Rule.Scan = p.Res;
                done++; if (p.Rule.Checked) total += p.Res.Bytes;
                StateSize.Text = CleanScan.Format(total);
                StateRest.Text = " found so far";
                StateLine.Text = $"Checked {done} of {queue.Count}: {p.Rule.Name}";
                WorkBar.Value = (double)done / queue.Count;
                foreach (var c in _all) if (c.Rules.Contains(p.Rule)) c.Refresh();
            });
            IProgress<(CleanRuleVM, CleanScan)> report = progress;
            await Task.Run(() => Parallel.ForEach(queue, new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount / 2, 2, 6) },
                r => report.Report((r, CleanerEngine.Scan(r.Rule, token)))), token);
            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);   // let the last reports land
            _scanned = true;
            Measured?.Invoke(Ticked.Sum(r => r.Bytes));
        }
        catch (OperationCanceledException)
        {
            _scanned = done > 0;
            StateLine.Text = $"Stopped after {done} of {queue.Count} rules. What was found so far can still be cleaned.";
        }
        catch (Exception ex)
        {
            Log.Error("Cleaner scan failed: " + ex);
            StateLine.Text = "The scan ran into a problem: " + ex.Message;
        }
        finally
        {
            foreach (var r in queue) if (r.IsBusy) { r.IsBusy = false; r.RaiseSize(); }
            End();
        }
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

        Begin("Cleaning");
        _host.Busy = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        int count = 0; long freed = 0; var done = 0;
        try
        {
            foreach (var r in queue)
            {
                token.ThrowIfCancellationRequested();
                r.IsBusy = true;
                StateLine.Text = $"Cleaning {r.Name}  ({done + 1} of {queue.Count})";
                var scan = r.Scan!;
                var (c, b) = await Task.Run(() => CleanerEngine.Clean(scan, token), token);
                count += c; freed += b; done++;
                r.IsBusy = false; r.Scan = new CleanScan { Rule = r.Rule };
                StateSize.Text = CleanScan.Format(freed);
                StateRest.Text = " freed so far";
                WorkBar.Value = (double)done / queue.Count;
                foreach (var cat in _all) if (cat.Rules.Contains(r)) cat.Refresh();
            }
            StateLineAfter = $"Cleaned {CleanScan.Format(freed)}: {count:N0} items removed. Scan again any time.";
        }
        catch (OperationCanceledException) { StateLineAfter = $"Stopped. {CleanScan.Format(freed)} freed, {count:N0} items removed before stopping."; }
        catch (Exception ex)
        {
            Log.Error("Cleaner clean failed: " + ex);
            StateLineAfter = "Cleaning ran into a problem: " + ex.Message;
        }
        finally
        {
            foreach (var r in queue) if (r.IsBusy) { r.IsBusy = false; r.RaiseSize(); }
            _host.Busy = false;
            End();
            StateLine.Text = StateLineAfter;
            Measured?.Invoke(Ticked.Sum(r => r.Bytes));
        }
    }

    string StateLineAfter = "";

    // ------------------------------------------------------------------------------------------ look
    void Begin(string what)
    {
        _working = true;
        foreach (var c in _all) foreach (var r in c.Rules) r.CanEdit = false;
        ScanText.Text = "Stop"; ScanIcon.Text = "";
        CleanBtn.IsEnabled = false; ResetBtn.IsEnabled = false; TickAll.IsEnabled = false;
        StateSize.Text = "0 B"; StateRest.Text = what == "Scanning" ? " found so far" : " freed so far";
        StateLine.Text = what + "…";
        WorkBar.Value = 0; WorkBar.Visibility = Visibility.Visible;
    }

    void End()
    {
        _working = false;
        foreach (var c in _all) { foreach (var r in c.Rules) r.CanEdit = true; c.Refresh(); }
        ScanText.Text = _scanned ? "Scan again" : "Scan"; ScanIcon.Text = "";
        ResetBtn.IsEnabled = true;
        WorkBar.Visibility = Visibility.Collapsed;
        var line = StateLine.Text;
        UpdateHeader();
        if (line.StartsWith("Stopped") || line.StartsWith("The scan")) StateLine.Text = line;
        Rebuild();
    }
}
