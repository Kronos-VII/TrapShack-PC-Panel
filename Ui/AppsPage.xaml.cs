using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>One entry in a page's category list (the same list style on every page).</summary>
public sealed class RailVM : Notify
{
    bool _sel;
    public string Key { get; init; } = "";
    public string Title { get; init; } = "";
    public string Glyph { get; init; } = "\uE71D";
    string _count = "";
    public string Count { get => _count; set { if (Set(ref _count, value)) Raise(nameof(CountText)); } }
    public string CountText => _count;
    public bool Selected { get => _sel; set => Set(ref _sel, value); }
}

/// <summary>The Apps page: pick apps and runtimes, install them through winget, and see what is already installed.</summary>
public partial class AppsPage : UserControl
{
    static readonly string[] GroupOrder =
    {
        "Game launchers", "Gaming tools", "Runtimes & dependencies", "Communication", "Browsers", "Media", "Utilities", "Creative & documents", "Development",
    };

    IPageHost? _host;
    readonly List<AppCardVM> _apps = new();
    readonly List<PairCardVM> _pairs = new();
    readonly List<RailVM> _rail = new();
    string _group = "Game launchers";

    static string GlyphOf(string group) => group switch
    {
        "Game launchers" => "\uE7FC", "Gaming tools" => "\uE945", "Runtimes & dependencies" => "\uE943", "Communication" => "\uE8BD",
        "Browsers" => "\uE774", "Media" => "\uE8D6", "Utilities" => "\uE90F", "Creative & documents" => "\uE70F", "Development" => "\uE943",
        _ => "\uE71D",
    };
    bool _building, _scanning, _working;
    DateTime _lastScan = DateTime.MinValue;

    /// <summary>True once the first look at what is installed has finished.</summary>
    public bool Scanned { get; private set; }
    public int MissingGameRuntimes => _apps.Count(a => a.Def.Dep && a.Def.Game && !a.Installed);
    public event Action? ScanFinished;

    public AppsPage()
    {
        InitializeComponent();
        foreach (var d in AppCatalog.All)
        {
            var vm = new AppCardVM(d);
            vm.Changed = OnVmChanged;
            _apps.Add(vm);
        }
        // 64-bit and 32-bit builds of the same Visual C++ runtime become one card.
        foreach (var a64 in _apps.Where(a => a.Def.Dep && a.Id.StartsWith("microsoft-vcredist-") && a.Id.EndsWith("-x64")))
        {
            var a86 = _apps.FirstOrDefault(a => a.Id == a64.Id[..^4] + "-x86");
            if (a86 != null) _pairs.Add(new PairCardVM(a64, a86));
        }
    }

    public void Init(IPageHost host)
    {
        _host = host;
        var picks = new HashSet<string>(host.Cfg.AppPicks, StringComparer.OrdinalIgnoreCase);
        _building = true;
        foreach (var a in _apps) a.Selected = picks.Contains(a.Id);
        _building = false;
        Rebuild();
    }

    // ------------------------------------------------------------------ scanning

    /// <summary>Installed apps with no logo of their own get the program's real icon instead of a letter tile.</summary>
    async Task LoadRealIconsAsync()
    {
        foreach (var a in _apps.Where(x => x.Installed && x.Art.NeedsRealIcon && x.Art.RealIcon == null).ToList())
        {
            var img = await Task.Run(() => ShellIcon.For(WingetService.IconPath(a.Def)));
            if (img != null) a.Art.UseRealIcon(img);
        }
    }

    /// <summary>Looks at what is installed. Safe to call often; it never runs twice at once.</summary>
    public async Task ScanAsync(bool force = false)
    {
        if (_scanning || _working) return;
        if (!force && Scanned && (DateTime.Now - _lastScan).TotalSeconds < 45) { UpdateStats(); return; }
        _scanning = true;
        try
        {
            var found = await Task.Run(WingetService.DetectInstalled);
            _building = true;
            foreach (var a in _apps) a.Installed = found.Contains(a.Id);
            _building = false;
            Scanned = true; _lastScan = DateTime.Now;
            SavePicks();
        }
        catch (Exception ex) { Log.Error("App scan failed: " + ex.Message); _building = false; }
        finally { _scanning = false; }
        WingetNotice.Visibility = WingetService.Available ? Visibility.Collapsed : Visibility.Visible;
        Rebuild();
        ScanFinished?.Invoke();
        _ = LoadRealIconsAsync();
    }

    // ------------------------------------------------------------------ selection

    void OnVmChanged(AppCardVM vm)
    {
        if (_building) return;
        SavePicks();
        UpdateStats();
        UpdateSectionCounters();
    }

    void SavePicks()
    {
        if (_host == null) return;
        _host.Cfg.AppPicks = _apps.Where(a => a.Selected && !a.Installed).Select(a => a.Id).ToList();
        _host.Cfg.Save();
    }

    List<AppCardVM> Picked => _apps.Where(a => a.Selected && !a.Installed).ToList();

    void OnFxMove(object sender, MouseEventArgs e) => Fx.Move(sender, e);
    void OnFxEnter(object sender, MouseEventArgs e) => Fx.Enter(sender, e);
    void OnFxLeave(object sender, MouseEventArgs e) => Fx.Leave(sender, e);

    void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (_working) return;
        if (((FrameworkElement)sender).DataContext is AppCardVM vm && vm.CanSelect) vm.Selected = !vm.Selected;
    }

    /// <summary>The small amber chip: picks every game runtime that is missing.</summary>
    void OnRuntimeChipClick(object sender, RoutedEventArgs e) => Pick(a => a.Def.Dep && a.Def.Game);

    void Pick(Func<AppCardVM, bool> which)
    {
        if (_working) return;
        _building = true;
        foreach (var a in _apps.Where(which)) if (a.CanSelect) a.Selected = true;
        _building = false;
        SavePicks(); UpdateStats(); UpdateSectionCounters();
    }

    void OnClearPicks(object sender, RoutedEventArgs e)
    {
        if (_working) return;
        _building = true;
        foreach (var a in _apps) { a.Selected = false; a.Failed = false; a.Phase = ""; }
        _building = false;
        SavePicks(); UpdateStats(); UpdateSectionCounters();
    }

    // ------------------------------------------------------------------ view

    void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        Rebuild();
    }

    public void FocusSearch() => Search.Focus();

    void OnRailChecked(object sender, RoutedEventArgs e)
    {
        if (_building || ((FrameworkElement)sender).DataContext is not RailVM r) return;
        if (r.Key == _group && Search.Text.Length == 0) return;
        _group = r.Key;
        if (Search.Text.Length > 0) { Search.Text = ""; return; }   // clearing the search rebuilds
        Rebuild(false);
        Scroller.ScrollToTop();
    }

    void Rebuild(bool rail = true)
    {
        if (_host == null) return;
        var q = Search.Text.Trim();
        var pairIds = new HashSet<string>(_pairs.SelectMany(p => new[] { p.X64.Id, p.X86.Id }));

        // left rail: every group with how many of its apps are installed
        if (rail || _rail.Count == 0)
        {
            _rail.Clear();
            foreach (var g in GroupOrder)
                if (_apps.Any(a => a.Group == g)) _rail.Add(new RailVM { Key = g, Title = g, Glyph = GlyphOf(g) });
            _rail.Add(new RailVM { Key = "", Title = "All apps", Glyph = "\uE71D" });
            UpdateRailCounts();
            _building = true; Rail.ItemsSource = null; Rail.ItemsSource = _rail; _building = false;
        }
        var searching = q.Length > 0;
        foreach (var r in _rail) r.Selected = !searching && r.Key == _group;

        var sections = new List<AppSectionVM>();
        foreach (var g in GroupOrder)
        {
            if (!searching && _group != "" && _group != g) continue;
            var items = new List<object>();
            var members = _apps.Where(a => a.Group == g).ToList();
            foreach (var a in members)
            {
                if (pairIds.Contains(a.Id))
                {
                    var pair = _pairs.First(p => p.X64 == a || p.X86 == a);
                    if (ReferenceEquals(pair.X64, a) && pair.Matches(q)) items.Add(pair);
                    continue;
                }
                if (a.Matches(q)) items.Add(a);
            }
            if (items.Count == 0) continue;
            sections.Add(new AppSectionVM { Title = g, Items = items, Total = members.Count, ShowTitle = searching || _group == "" });
        }
        Sections.ItemsSource = sections;
        NoMatch.Visibility = sections.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdatePaneHeader();
        UpdateSectionCounters();
        UpdateStats();
    }

    /// <summary>"installed / total" beside each category in the list.</summary>
    void UpdateRailCounts()
    {
        foreach (var r in _rail)
        {
            var apps = _apps.Where(a => r.Key == "" || a.Group == r.Key).ToList();
            r.Count = Scanned ? $"{apps.Count(a => a.Installed)}/{apps.Count}" : apps.Count.ToString();
        }
    }

    /// <summary>The card above the list: the category, what it holds, and how much of it is installed.</summary>
    void UpdatePaneHeader()
    {
        var q = Search.Text.Trim();
        if (q.Length > 0)
        {
            PaneGlyph.Text = "\uE721";
            PaneTitle.Text = $"Results for \u201c{q}\u201d";
            var n = (Sections.ItemsSource as List<AppSectionVM>)?.Sum(x => x.Items.Count) ?? 0;
            PaneDesc.Text = n == 0 ? "Nothing matches." : $"{n} app{(n == 1 ? "" : "s")} found in every category";
            PaneProgress.Visibility = Visibility.Collapsed;
            return;
        }
        var apps = _apps.Where(a => _group == "" || a.Group == _group).ToList();
        PaneGlyph.Text = GlyphOf(_group);
        PaneTitle.Text = _group == "" ? "All apps" : _group;
        PaneDesc.Text = _group switch
        {
            "Game launchers" => "Stores and launchers for your games",
            "Gaming tools" => "Benchmarks, monitoring, drivers and overlays",
            "Runtimes & dependencies" => "What games and programs need to start. Missing ones cause \u201cmissing .dll\u201d errors",
            "Communication" => "Chat, voice and video calls",
            "Browsers" => "Web browsers",
            "Media" => "Music, video and recording",
            "Utilities" => "Everyday tools for files, screenshots and remote help",
            "Creative & documents" => "Photo, 3D and office apps",
            "Development" => "Code editors and developer tools",
            _ => "Every app TrapShack can install, by category",
        };
        PaneProgress.Visibility = Scanned ? Visibility.Visible : Visibility.Collapsed;
        var inst = apps.Count(a => a.Installed);
        PaneCount.Text = $"{inst} of {apps.Count} installed";
        PaneBar.Value = apps.Count == 0 ? 0 : (double)inst / apps.Count;
    }

    void UpdateSectionCounters()
    {
        UpdateRailCounts();
        if (Sections.ItemsSource is List<AppSectionVM> && Search.Text.Trim().Length == 0) UpdatePaneHeader();
        if (Sections.ItemsSource is not List<AppSectionVM> list) return;
        foreach (var s in list)
        {
            var apps = s.Items.SelectMany(i => i is PairCardVM p ? new[] { p.X64, p.X86 } : i is AppCardVM a ? new[] { a } : Array.Empty<AppCardVM>()).ToList();
            var inst = apps.Count(a => a.Installed);
            s.Counter = Scanned ? $"{inst} of {apps.Count} installed" : $"{apps.Count}";
        }
    }

    void UpdateStats()
    {
        var installed = _apps.Count(a => a.Installed);
        StatInstalled.Text = Scanned ? $"{installed} of {_apps.Count}" : "Checking…";
        var picked = Picked.Count;
        var missing = MissingGameRuntimes;
        RuntimeChip.Visibility = Scanned && missing > 0 ? Visibility.Visible : Visibility.Collapsed;
        RuntimeChipText.Text = $"Pick {missing} missing game runtime{(missing == 1 ? "" : "s")}";
        RuntimeChip.ToolTip = "Games commonly need these. Missing ones can cause “missing .dll” errors. Click to tick them.";

        var failed = Picked.Count(a => a.Failed);
        PickBar.Visibility = picked > 0 || _working ? Visibility.Visible : Visibility.Collapsed;
        PickText.Text = $"{picked} app{(picked == 1 ? "" : "s")} picked";
        PickSub.Text = failed > 0 ? $"{failed} didn't install last time. They are still picked so you can try again."
            : string.Join(", ", Picked.Take(5).Select(a => a.Name)) + (picked > 5 ? $" and {picked - 5} more" : "");
        InstallBtn.Content = failed > 0 && failed == picked ? $"Retry {failed} failed" : $"Install {picked}";
        InstallBtn.IsEnabled = !_working && picked > 0 && WingetService.Available;
        InstallBtn.ToolTip = WingetService.Available ? null : "winget is not available on this PC";
    }

    // ------------------------------------------------------------------ installing

    async void OnInstallClick(object sender, RoutedEventArgs e)
    {
        if (_host == null || _working || _host.Busy) return;
        var queue = Picked;
        if (queue.Count == 0) return;
        if (!WingetService.Available)
        {
            Dialogs.Info(_host.Win, "Installing isn't available",
                "Windows' package manager (winget) wasn't found on this PC. Open the Microsoft Store, update “App Installer”, then reopen TrapShack.");
            return;
        }

        _working = true; _host.Busy = true;
        foreach (var a in queue) { a.Failed = false; a.Phase = "Waiting"; a.Busy = true; }
        UpdateStats();

        var dock = _host.Dock;
        dock.Begin("Installing", queue.Count == 1 ? $"Installing {queue[0].Name}" : $"Installing {queue.Count} apps",
            queue.Select(a => (a.Name, a.Package)).ToList(), true);

        using var cts = new CancellationTokenSource();
        var watch = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        watch.Tick += (_, _) => { if (dock.CancelRequested) cts.Cancel(); };
        watch.Start();

        int ok = 0, bad = 0, stopped = 0;
        try
        {
            await Task.Run(WingetService.RefreshSources);
            for (var i = 0; i < queue.Count; i++)
            {
                var a = queue[i];
                if (cts.IsCancellationRequested) { a.Phase = ""; a.Busy = false; stopped++; continue; }
                dock.StartStep(i, a.Name);
                a.Phase = "Starting…";
                var progress = new Progress<DlProgress>(p => ShowProgress(dock, a, p));
                var res = await WingetService.InstallAsync(a.Def, progress, cts.Token);
                a.Busy = false;
                if (res.Ok)
                {
                    ok++; a.Phase = ""; a.Failed = false;
                    a.Installed = true;            // also un-ticks it
                    dock.EndStep(i, StepResult.Done);
                }
                else if (res.Cancelled) { stopped++; a.Phase = ""; dock.EndStep(i, StepResult.Warn); }
                else
                {
                    bad++; a.Failed = true; a.Phase = "Didn't install. Tap Retry.";
                    dock.EndStep(i, StepResult.Failed);
                }
            }
        }
        catch (Exception ex) { Log.Error("Install run failed: " + ex); }
        finally
        {
            watch.Stop();
            foreach (var a in queue) { a.Busy = false; if (a.Phase is "Waiting" or "Starting…") a.Phase = ""; }
            _working = false; _host.Busy = false;
        }

        var note = bad == 0 && stopped == 0 ? "Everything installed."
            : bad > 0 ? $"{ok} installed, {bad} didn't. They stay picked so you can retry."
            : $"{ok} installed, the rest were stopped.";
        dock.Finish("", ok > 0 && bad == 0 ? "Done" : bad > 0 ? "Finished with problems" : "Stopped", note, ok, 0, bad, bad == 0);

        SavePicks();
        UpdateSectionCounters();
        UpdateStats();
        ScanFinished?.Invoke();
    }

    static void ShowProgress(ProgressDock dock, AppCardVM a, DlProgress p)
    {
        switch (p.Phase)
        {
            case "Downloading":
                var pct = (int)Math.Round(p.Fraction * 100);
                var line = p.Total > 0 ? $"{WingetService.FormatPair(p.Done, p.Total)}  ·  {pct}%" : $"{pct}%";
                a.Phase = $"Downloading  {pct}%";
                dock.SetStepProgress(p.Fraction * 0.8, line);
                break;
            case "Verified": a.Phase = "Checking download"; dock.SetStepProgress(0.85, "Checking the download"); break;
            case "Installing": a.Phase = "Installing…"; dock.SetStepProgress(0.92, "Installing…"); break;
            case "Retrying…": a.Phase = "Trying again…"; dock.SetStepProgress(0.02, "Trying again…"); break;
            default: a.Phase = p.Phase; dock.SetStepProgress(0.03, p.Phase); break;
        }
    }

    async void OnUninstallClick(object sender, RoutedEventArgs e)
    {
        if (_host == null || _working || _host.Busy) return;
        if (((FrameworkElement)sender).DataContext is not AppCardVM a) return;
        if (!WingetService.Available) return;
        // runtimes are what games and programs run on: removing one breaks them, so say so plainly
        var msg = a.IsDep
            ? $"{a.Name} is a runtime that games and programs need to start. Anything built on it will stop opening until it is installed again." +
              "\n\nOnly remove it if you know nothing on this PC uses it. You can install it again from this page."
            : $"{a.Name} will be removed from this PC. You can install it again from this page.";
        if (!Dialogs.Confirm(_host.Win, a.IsDep ? $"Remove {a.Name}?" : $"Uninstall {a.Name}?", msg, a.IsDep ? "Remove anyway" : "Uninstall", "Cancel")) return;

        _working = true; _host.Busy = true; a.Busy = true; a.Phase = "Removing…";
        var dock = _host.Dock;
        dock.Begin("Removing", $"Removing {a.Name}", new List<(string, string)> { (a.Name, a.Package) }, false);
        dock.StartStep(0, a.Name);
        InstallResult res;
        try { res = await WingetService.UninstallAsync(a.Def, new Progress<DlProgress>(p => dock.SetStepProgress(p.Phase == "Installing" ? 0.6 : 0.1, "Removing…")), CancellationToken.None); }
        catch (Exception ex) { res = new InstallResult(false, ex.Message); }
        a.Busy = false; a.Phase = "";
        if (res.Ok) { a.Installed = false; dock.EndStep(0, StepResult.Done); }
        else { a.Failed = true; a.Phase = "Couldn't remove it"; dock.EndStep(0, StepResult.Failed); }
        _working = false; _host.Busy = false;
        dock.Finish("", res.Ok ? "Removed" : "Couldn't remove it", res.Ok ? $"{a.Name} was removed." : res.Message, res.Ok ? 1 : 0, 0, res.Ok ? 0 : 1, res.Ok);
        UpdateSectionCounters(); UpdateStats(); ScanFinished?.Invoke();
    }
}
