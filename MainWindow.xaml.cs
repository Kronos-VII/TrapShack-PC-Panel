using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using TrapShack.Core;
using TrapShack.Tweaks;
using TrapShack.Ui;

namespace TrapShack;

public partial class MainWindow : Window, IPageHost
{
    readonly TweakEngine _engine = new();

    SystemInfo _sys = SystemInfo.Current;
    List<TweakVM> _vms = new();
    PageDef? _page;
    bool _hub;
    string _hubSection = PageDefs.Optimizations;
    readonly Dictionary<RadioButton, string> _navKeys = new();
    readonly Dictionary<string, RadioButton> _navByKey = new();
    bool _loaded;
    bool _busy;
    bool _searching, _restartArmed;
    readonly HashSet<string> _restartIds = new();
    string? _restartNote;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WorkArea.Hook(this);   // maximised = the screen minus the taskbar
        Closing += OnWindowClosing;
        BuildNav();
        _effectsReady = false;
        Fx.On = _settings.LiveEffects;
        EffectsCheck.IsChecked = _settings.LiveEffects;
        LightCheck.IsChecked = _settings.LightTheme;
        if (_settings.LightTheme) ThemeMode.Apply(true);   // after the window is built, so the terminal keeps its dark look
        _effectsReady = true;
        Fx.Haze(Haze, _settings.LiveEffects);
        QuickPop.Closed += (_, _) => _quickClosed = DateTime.Now;
        Wheel.State = _hsv; ValueBar.State = _hsv;
        Wheel.Picking += OnAccentPicking; ValueBar.Picking += OnAccentPicking;
        Wheel.Picked += OnAccentPicked; ValueBar.Picked += OnAccentPicked;
        AppsPanel.Init(this);
        DebloatAppsPanel.Init(this);
        CleanerPanel.Init(this);
        RestorePanel.Init(this);
        Apply.SetConsole(_settings.ShowConsole);
        Apply.ConsoleToggled += on => { _settings.ShowConsole = on; _settings.Save(); };
        AppsPanel.ScanFinished += () => { if (HomePanel.Visibility == Visibility.Visible) RefreshHome(); };
        PageCrumb.MouseLeftButtonUp += (_, _) => { if (_page?.Section == PageDefs.Optimizations) SelectNav("hub"); };
    }

    // =============================================================================================
    // Startup
    // =============================================================================================

    async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var clock = Stopwatch.StartNew();
        // Home's "can be freed" figure: the last result shows at once, and a fresh scan starts right away, alongside
        // the rest of the startup work, instead of waiting until the app is open.
        CleanerPanel.Measured += total =>
        {
            HomeCleanSize.Text = CleanScan.Format(total);
            HomeCleanText.Text = total > 0 ? "Temporary files and caches the PC Cleaner found. Nothing is deleted until you press Clean." : "Nothing to clean right now.";
            _settings.LastCleanBytes = total; _settings.LastCleanAt = DateTime.Now; _settings.Save();
        };
        if (_settings.LastCleanBytes >= 0)
        {
            HomeCleanSize.Text = CleanScan.Format(_settings.LastCleanBytes);
            HomeCleanText.Text = "From the last scan. Checking again now…";
        }
        // the splash glow pulses until the app is ready (started here, in code, so it can be stopped for good)
        SplashGlow.BeginAnimation(OpacityProperty, new DoubleAnimation(0.2, 0.6, TimeSpan.FromSeconds(1.4)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        var cleanerScan = QuietCleanerScanAsync();
        var probeCached = false;
        try
        {
            SetSplash("Detecting your hardware…", 0.14);
            await Task.Run(() => _sys = SystemInfo.Detect());

            SetSplash("Scanning apps, services and scheduled tasks…", 0.45);
            // The slow hardware checks only change with Windows itself: on the same build, start with last run's answers
            // and re-read them quietly once the window is up.
            probeCached = _settings.ProbeKey == _sys.ProbeKeyNow;
            if (probeCached) _sys.UseCachedProbe(_settings.ProbeSsd, _settings.ProbeRecall, _settings.ProbeGpus);
            await Task.Run(() => Parallel.Invoke(() => { if (!probeCached) _sys.ProbeSlow(); }, Caches.RefreshTasks, Caches.RefreshAppx));
            if (!probeCached) SaveProbe();

            SetSplash($"Choosing settings for your {_sys.CpuBrand} processor and {GpuBrand}…", 0.74);
            var tweaks = await Task.Run(() => Catalog.Build());
            _vms = tweaks.Select(t => new TweakVM(t, Judge(t))).ToList();

            SetSplash("Reading what is already applied…", 0.9);
            // What is ALREADY applied on this PC, whoever applied it.
            // read in parallel: each setting is checked on its own, so this is several times faster than one by one
            var states = await Task.Run(() => _vms.AsParallel().AsOrdered().WithDegreeOfParallelism(Math.Clamp(Environment.ProcessorCount, 2, 8))
                                                  .Select(v => { try { return _engine.IsApplied(v.T); } catch { return false; } }).ToList());
            for (var i = 0; i < _vms.Count; i++) _vms[i].SetOn(states[i]);
            BuildClashMap();
        }
        catch (Exception ex)
        {
            Log.Error("Startup scan failed: " + ex);
            Dialogs.Info(this, "Scan problem", "TrapShack couldn't read everything on this PC:\n\n" + ex.Message +
                "\n\nSettings it couldn't read are left alone.");
        }

        SetSplash("Ready", 1.0);
        var wait = 350 - (int)clock.ElapsedMilliseconds;   // only long enough not to flash on a very fast PC
        if (wait > 0) await Task.Delay(wait);

        _loaded = true;
        StartSilentRecheck();
        PreloadSystemInfo();
        _ = BackgroundStartAsync(probeCached);
        UtilitiesPanel.Init(this, _sys.IsLaptop);
        SidebarSystem.Text = $"{_sys.OsName}\n{_sys.CpuBrand} · {GpuBrand}";
        UpdatePutBack();
        Navigate("home");
        FadeOutSplash();

        await OfferToFinishInterruptedApplyAsync();

        if (!_engine.State.Has("app.welcomed"))
        {
            _engine.State.Set("app.welcomed", DateTime.Now.ToString("o"));
            await Task.Delay(450);
            await ShowWelcomeAsync();
        }
    }

    /// <summary>
    /// Fail-safe: if the app or the PC stopped while a setting was being applied, that setting may be half-done.
    /// Offer to put it back the way it was (TrapShack saved the original values before it changed anything).
    /// </summary>
    async Task OfferToFinishInterruptedApplyAsync()
    {
        var id = _engine.State.Get(TweakEngine.PendingKey);
        if (string.IsNullOrEmpty(id)) return;
        _engine.State.Remove(TweakEngine.PendingKey);
        var vm = _vms.FirstOrDefault(v => v.T.Id == id);
        if (vm == null) return;
        if (!Dialogs.Confirm(this, "A change was interrupted",
                $"TrapShack was closed while it was applying \u201c{vm.T.Title}\u201d, so it may be only partly applied.\n\n" +
                "Put it back the way it was? (You can apply it again afterwards.)", "Put it back", "Leave it"))
            return;
        var err = await Task.Run(() => { try { _engine.RevertSaved(vm.T); return null; } catch (Exception ex) { return ex.Message; } });
        vm.SetOn(await Task.Run(() => _engine.IsApplied(vm.T)));
        if (err != null) Log.Error($"Undo of interrupted {id}: {err}");
        else Log.Info($"Interrupted {id} put back");
        RefreshViews();
    }

    void SetSplash(string text, double progress)
    {
        SplashStatus.Text = text;
        SplashBar.BeginAnimation(RangeBase.ValueProperty,
            new DoubleAnimation(progress, TimeSpan.FromMilliseconds(450)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    /// <summary>
    /// Work that isn't needed to show the app, started one after another once the window is up so the first seconds stay
    /// smooth: the slow hardware re-check (when cached), the installed-apps scan, then the temperature sensors.
    /// Opening Apps or Temperatures earlier simply starts that one straight away.
    /// </summary>
    async Task BackgroundStartAsync(bool probeCached)
    {
        try
        {
            await Task.Delay(1200);
            _ = AppsPanel.ScanAsync();
            if (probeCached)
            {
                var before = GpuBrand;
                await Task.Run(() => _sys.ProbeSlow());
                SaveProbe();
                if (GpuBrand != before) SidebarSystem.Text = $"{_sys.OsName}\n{_sys.CpuBrand} · {GpuBrand}";
            }
            await Task.Delay(1500);
            _ = MonitorPanel.Preload();
        }
        catch (Exception ex) { Log.Error("Background start: " + ex.Message); }
    }

    void SaveProbe()
    {
        _settings.ProbeKey = _sys.ProbeKeyNow;
        _settings.ProbeSsd = _sys.SystemDriveIsSsd;
        _settings.ProbeRecall = _sys.RecallFeatureState;
        _settings.ProbeGpus = _sys.Gpus.ToList();
        _settings.Save();
    }

    void FadeOutSplash()
    {
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        // the splash glow pulses forever: stop it, or WPF keeps drawing frames for a hidden element all session
        fade.Completed += (_, _) => { Splash.Visibility = Visibility.Collapsed; SplashGlow.BeginAnimation(OpacityProperty, null); };
        Splash.BeginAnimation(OpacityProperty, fade);
    }

    /// <summary>Shown once, the first time the app runs.</summary>
    async Task ShowWelcomeAsync()
    {
        var pending = _vms.Count(v => v.V.Fit == Fit.Recommended && !v.IsOn);
        var locked = _vms.Count(v => v.V.Fit == Fit.Unavailable && !v.IsOn);
        var gpu = _sys.Gpus.Count > 0 ? string.Join(" + ", _sys.Gpus.Take(2)) : "Graphics not detected";

        var message =
            "We've scanned this PC and tailored everything to it:\n\n" +
            $"•  {_sys.OsName}\n" +
            $"•  {_sys.Cpu}\n" +
            $"•  {gpu}\n" +
            $"•  {_sys.RamGb:0.#} GB RAM · {_sys.Storage} · {_sys.DeviceType}\n\n" +
            (pending > 0
                ? $"{pending} recommended setting{(pending == 1 ? " is" : "s are")} ready for this PC."
                : "Everything recommended for this PC is already applied.") +
            (locked > 0
                ? $" {locked} other setting{(locked == 1 ? " doesn't" : "s don't")} suit it and {(locked == 1 ? "is" : "are")} locked, so you can't apply {(locked == 1 ? "it" : "them")} by mistake."
                : "") +
            "\n\nNothing has been changed, and nothing is ever applied until you switch it on or press an Apply button yourself. Every change can be switched off again.";

        Dialogs.Info(this, "Welcome to JS Tweak Utility", message);
        await Task.CompletedTask;
    }

    /// <summary>Decides how well a tweak fits this PC. This is where the smart gating lives.</summary>
    Verdict Judge(Tweak t)
    {
        if (!_sys.Supported)
            return Verdict.No("TrapShack needs Windows 10 (version 2004 or newer) or Windows 11.");
        if (!_engine.IsAvailable(t))
            return Verdict.No("Not present on this PC.");

        var v = t.Evaluate(_sys);
        if (_sys.DomainJoined && t.UsesMachinePolicy && v.Fit == Fit.Recommended)
            return Verdict.Opt("This PC is joined to a domain, so your organisation's policies may override this.");

        // What counts as "recommended" depends on the section: the standard set for Debloat, the per-setting recommended values for Optimizations.
        var pageKey = PageDefs.KeyOf(t);
        if (pageKey == "customize") return v;   // Customization sits under Optimizations but keeps its own verdicts
        var section = PageDefs.Get(pageKey).Section;
        if (section == PageDefs.Debloat)
        {
            if (Recommended.Standard.Contains(t.Id)) return v.Fit == Fit.Optional ? Verdict.Rec(v.Note) : v;
            if (v.Fit == Fit.Recommended) return Verdict.Opt(v.Note);
        }
        else if (section == PageDefs.Optimizations)
        {
            if (t.Id.StartsWith("opt.")) return v;   // carries its own verdict
            if (Recommended.OptimizationRecommended.Contains(t.Id)) return v.Fit == Fit.Optional ? Verdict.Rec(v.Note) : v;
            if (v.Fit == Fit.Recommended) return Verdict.Opt(v.Note);
        }
        return v;
    }

    string GpuBrand =>
        _sys.HasNvidiaGpu ? "NVIDIA graphics" :
        _sys.HasAmdDiscreteGpu ? "AMD Radeon graphics" :
        _sys.HasIntelArcGpu ? "Intel Arc graphics" :
        _sys.Gpus.Count > 0 ? "integrated graphics" : "unknown graphics";

    // =============================================================================================
    // Navigation
    // =============================================================================================

    void BuildNav()
    {
        _navKeys[NavHome] = "home"; _navByKey["home"] = NavHome;
        _navKeys[NavSystem] = "system"; _navByKey["system"] = NavSystem;
        _navKeys[NavTemps] = "temps"; _navByKey["temps"] = NavTemps;
        _navKeys[NavSettings] = "settings"; _navByKey["settings"] = NavSettings;

        RadioButton Add(string key, string title, string glyph, bool sub)
        {
            var rb = new RadioButton
            {
                Style = (Style)FindResource("NavButton"), Content = title, Tag = glyph, GroupName = "nav",
                Margin = sub ? new Thickness(16, 0, 0, 0) : new Thickness(0),
            };
            rb.Checked += OnNavChecked;
            _navKeys[rb] = key; _navByKey[key] = rb;
            NavHost.Children.Add(rb);
            return rb;
        }
        // Home, Debloat, Optimizations (every Optimizations page, Customization included, opens from there), Debloat apps, Apps.
        PageDef Page(string key) => PageDefs.Get(key);
        Add("debloat", Page("debloat").Title, Page("debloat").Glyph, false);
        Add("hub", "Optimizations", "\uE945", false);
        Add("winapps", Page("winapps").Title, Page("winapps").Glyph, false);
        Add("apps", Page("apps").Title, Page("apps").Glyph, false);
        Add("cleaner", "PC Cleaner", "\uEA99", false);
        Add("utilities", "Utilities", "\uE90F", false);
        Add("startup", "Startup apps", "\uE7E8", false);
        Add("restore", "Restore points", "\uE81C", false);
    }

    void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (_navKeys.TryGetValue((RadioButton)sender, out var key)) Navigate(key);
    }

    void SelectNav(string key)
    {
        if (_navByKey.TryGetValue(key, out var rb)) { rb.IsChecked = true; return; }
        // an Optimizations area: open the Optimizations page on it
        if (PageDefs.All.Any(p => p.Key == key && p.Section == PageDefs.Optimizations))
        {
            if (_navByKey["hub"].IsChecked == true) { ShowTweakPage(PageDefs.OptimizationsPage, key); return; }
            _pendingArea = key; _navByKey["hub"].IsChecked = true; return;
        }
        // An Optimizations area page: keep "Optimizations" highlighted and open the page directly.
        _loaded = false; _navByKey["hub"].IsChecked = true; _loaded = true;
        Navigate(key);
    }

    // ---- Settings: sections on the left, the chosen one on the right ----
    static readonly (string Key, string Glyph, string Desc)[] SettingsSections =
    {
        ("Appearance", "\uE771", "Accent colour, light or dark, and the animated effects"),
        ("Profiles", "\uE8F1", "Save the settings you use and load them on another PC"),
        ("Safety", "\uEA18", "Restore points and the questions TrapShack asks first"),
        ("Updates", "\uE895", "Keep TrapShack up to date"),
    };
    string _setSection = "Appearance";
    bool _setBuilding;

    void ShowSettingsSection(string key)
    {
        _setSection = key;
        var rail = SettingsSections.Select(x => new RailVM { Key = x.Key, Title = x.Key, Glyph = x.Glyph, Selected = x.Key == key }).ToList();
        _setBuilding = true; SettingsRail.ItemsSource = rail; _setBuilding = false;
        var sec = SettingsSections.First(x => x.Key == key);
        SetGlyph.Text = sec.Glyph; SetTitle.Text = sec.Key; SetDesc.Text = sec.Desc;
        SetAppearance.Visibility = key == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        SetProfiles.Visibility = key == "Profiles" ? Visibility.Visible : Visibility.Collapsed;
        SetSafety.Visibility = key == "Safety" ? Visibility.Visible : Visibility.Collapsed;
        SetUpdates.Visibility = key == "Updates" ? Visibility.Visible : Visibility.Collapsed;
        SettingsScroll.ScrollToTop();
    }

    void OnSettingsRail(object sender, RoutedEventArgs e)
    {
        if (_setBuilding || ((FrameworkElement)sender).DataContext is not RailVM r || r.Key == _setSection) return;
        ShowSettingsSection(r.Key);
    }

    void OnHomeJump(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is string key) SelectNav(key);
    }

    void OnHubRowClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is string key) SelectNav(key);
    }

    /// <summary>Shows one panel and hides the others.</summary>
    void ShowPanel(UIElement panel)
    {
        foreach (UIElement p in new UIElement[] { HomePanel, SystemPanel, MonitorPanel, SearchPanel, HubPanel, TweakPanel, SettingsPanel, AppsPanel, DebloatAppsPanel, CleanerPanel, UtilitiesPanel, StartupPanel, RestorePanel })
            p.Visibility = ReferenceEquals(p, panel) ? Visibility.Visible : Visibility.Collapsed;
        panel.BeginAnimation(OpacityProperty, null);   // pages switch instantly; the cards inside do a short rise
    }

    void Navigate(string key)
    {
        UpdateNavCounts();
        StopSensors();
        _page = null;
        _hub = false;
        switch (key)
        {
            case "home": ShowPanel(HomePanel); RefreshHome(); break;
            case "system": ShowSystem(); break;
            case "temps": ShowPanel(MonitorPanel); MonitorPanel.Show(); break;
            case "settings": ShowPanel(SettingsPanel); BuildSwatches(); ShowRestorePointSetting(); ShowUpdateSetting(); ShowSettingsSection(_setSection); break;
            case "apps": ShowPanel(AppsPanel); _ = AppsPanel.ScanAsync(); break;
            case "winapps": ShowPanel(DebloatAppsPanel); _ = DebloatAppsPanel.LoadAsync(); break;
            case "cleaner": ShowPanel(CleanerPanel); _ = CleanerPanel.LoadAsync(); break;
            case "utilities": ShowPanel(UtilitiesPanel); break;
            case "startup": ShowPanel(StartupPanel); _ = StartupPanel.LoadAsync(); break;
            case "restore": ShowPanel(RestorePanel); _ = RestorePanel.LoadAsync(); break;
            case "search": ShowPanel(SearchPanel); SearchAllBox.Focus(); RunGlobalSearch(); break;
            case "hub": ShowTweakPage(PageDefs.OptimizationsPage, _pendingArea); _pendingArea = null; break;
            default:
                var pd = PageDefs.Get(key);
                if (pd.Section == PageDefs.Optimizations) ShowTweakPage(PageDefs.OptimizationsPage, key);
                else ShowTweakPage(pd);
                break;
        }
    }

    string? _pendingArea;

    void ShowTweakPage(PageDef page, string? startAt = null)
    {
        _page = page;
        ShowPanel(TweakPanel);
        var area = page.Section == PageDefs.Optimizations && page.Key != PageDefs.OptimizationsPage.Key;
        PageCrumb.Text = area ? "\u2039  Optimizations" : page.Section.ToUpperInvariant();
        PageCrumb.Cursor = area ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow;
        PageTitle.Text = page.Title;
        PageSubtitle.Text = page.Subtitle;
        PageSubtitle.ToolTip = page.Subtitle;
        _cat = startAt;
        SearchBox.Text = "";
        TweakScroll.ScrollToTop();

        ApplyPageView();
        UpdatePageProgress();
    }

    void OnSearchAllChanged(object sender, TextChangedEventArgs e) => RunGlobalSearch();

    void RunGlobalSearch()
    {
        if (SearchAllBox == null || _vms == null) return;
        var q = SearchAllBox.Text.Trim();
        SearchAllHint.Visibility = q.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (q.Length < 2)
        {
            SearchAllList.ItemsSource = null;
            SearchAllCount.Text = "Type at least two letters to search every setting in the app.";
            return;
        }
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hits = _vms.Where(v => words.All(w =>
                v.Title.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                v.Summary.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                (v.T.Detail ?? "").Contains(w, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(v => v.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ? 0 : 1).Take(80).ToList();
        SearchAllCount.Text = hits.Count == 0 ? "Nothing matches that." : $"{hits.Count} setting{(hits.Count == 1 ? "" : "s")} found";
        var view = new ListCollectionView(hits);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TweakVM.PageTitle)));
        SearchAllList.ItemsSource = view;
    }

    void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = SearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        ApplyPageView();
    }

    // =============================================================================================
    // List pages (Debloat and every Optimizations page): categories on the left, the chosen one on the right
    // =============================================================================================

    /// <summary>One entry in a page's category list.</summary>
    public sealed class RailItem : Notify
    {
        public string Key { get; init; } = "";
        public string Title { get; init; } = "";
        public string Glyph { get; init; } = "";
        public List<TweakVM> Items { get; init; } = new();
        public string? Desc { get; init; }
        bool _sel; string _count = "";
        public bool Selected { get => _sel; set => Set(ref _sel, value); }
        public string CountText { get => _count; private set => Set(ref _count, value); }
        public void Refresh()
        {
            var sw = Items.Where(v => !v.T.OneShot && v.T.ChoiceGroup == null).ToList();
            CountText = sw.Count == 0 ? $"{Items.Count}" : $"{sw.Count(v => v.IsOn)}/{sw.Count}";
        }
    }

    string? _cat;                          // the category shown on the current page
    List<RailItem> _rail = new();
    List<object> _rows = new();

    /// <summary>The categories of a page, in their natural order, each with an icon.</summary>
    List<RailItem> BuildRail(string key, List<TweakVM> pageItems)
    {
        var list = new List<RailItem>();
        if (key == PageDefs.OptimizationsPage.Key)
        {
            foreach (var area in PageDefs.AreaOrder.Concat(PageDefs.All.Where(p => p.Section == PageDefs.Optimizations).Select(p => p.Key)).Distinct())
            {
                var items = pageItems.Where(v => v.PageKey == area).ToList();
                if (items.Count == 0) continue;
                var pd = PageDefs.Get(area);
                list.Add(new RailItem { Key = area, Title = pd.Title, Glyph = pd.Glyph, Items = items, Desc = pd.Subtitle });
            }
            foreach (var r in list) r.Refresh();
            return list;
        }
        if (key == "debloat")
        {
            var layout = Catalog.DebloatLayout.OrderBy(l => { var i = Array.IndexOf(DebloatOrder, l.Section); return i >= 0 ? i : 50; }).ToList();
            foreach (var (section, glyph, _) in layout)
            {
                var items = pageItems.Where(v => v.Group == section).ToList();
                if (items.Count > 0) list.Add(new RailItem { Key = section, Title = section, Glyph = glyph, Items = items });
            }
        }
        foreach (var g in pageItems.Select(v => v.Group).Distinct())
        {
            if (list.Any(r => r.Key == g)) continue;
            var items = pageItems.Where(v => v.Group == g).ToList();
            list.Add(new RailItem { Key = g, Title = g, Glyph = TweakGlyph.ForGroup(g, items), Items = items });
        }
        foreach (var r in list) r.Refresh();
        return list;
    }

    /// <summary>Rows for a set of settings: settings that share one Windows value become one drop-down row.</summary>
    static List<object> BuildRows(IEnumerable<TweakVM> items, Func<TweakVM, bool> hit, string q)
    {
        var all = items.ToList();
        var rows = new List<object>();
        var done = new HashSet<string>();
        foreach (var v in all)
        {
            var g = v.T.ChoiceGroup;
            if (g == null) { if (hit(v)) rows.Add(v); continue; }
            if (!done.Add(v.Group + "|" + g)) continue;
            var opts = all.Where(o => o.T.ChoiceGroup == g && o.Group == v.Group).ToList();
            if (q.Length == 0 || g.Contains(q, StringComparison.OrdinalIgnoreCase) || opts.Any(hit)) rows.Add(new ChoiceVM(g, opts));
        }
        return rows;
    }

    bool IsOptimizations => _page?.Key == PageDefs.OptimizationsPage.Key;

    /// <summary>The settings a page holds (the Optimizations page holds every Optimizations area).</summary>
    List<TweakVM> PageItems(PageDef page) => page.Key == PageDefs.OptimizationsPage.Key
        ? _vms.Where(v => PageDefs.Get(v.PageKey).Section == PageDefs.Optimizations).ToList()
        : _vms.Where(v => v.PageKey == page.Key).ToList();

    /// <summary>One block of rows in the right-hand pane, with an optional small heading.</summary>
    public sealed record PaneGroup(string Title, string Count, bool ShowTitle, List<object> Rows);

    // Debloat categories, gaming first; Windows AI and Windows Update last.
    static readonly string[] DebloatOrder =
    {
        "Gaming", "System", "Privacy & suggested content", "Start menu & search", "Taskbar", "File Explorer", "Appearance",
        "Multitasking", "Browsers & apps", "Network", "Clean up", "Windows AI", "Windows Update",
    };

    /// <summary>Order of the groups inside an Optimizations area: gaming and performance first, AI, services and updates last.</summary>
    static int GroupRank(string name)
    {
        var n = " " + name.ToLowerInvariant() + " ";
        if (n.Contains("gam") || n.Contains("graphic") || n.Contains("gpu")) return 0;
        if (n.Contains("performance") || n == " system ") return 1;
        if (n.Contains("visual") || n.Contains("display")) return 2;
        if (n.Contains(" ai ") || n.Contains("copilot") || n.Contains("recall")) return 8;
        if (n.Contains("update")) return 9;
        if (n.Contains("service") || n.Contains("scheduled") || n.Contains("task")) return 7;
        return 5;
    }

    void ApplyPageView()
    {
        if (_page == null || _vms == null) return;
        var q = SearchBox.Text.Trim();
        var key = _page.Key;
        var pageItems = PageItems(_page);
        _searching = q.Length > 0;
        QuickBtnText.Text = "Quick actions";

        _rail = BuildRail(key, pageItems);
        if (_cat == null || _rail.All(r => r.Key != _cat)) _cat = _rail.FirstOrDefault()?.Key;
        foreach (var r in _rail) r.Selected = !_searching && r.Key == _cat;
        RailList.ItemsSource = _rail;
        RailLabel.Text = _rail.Count == 1 ? "1 CATEGORY" : $"{_rail.Count} CATEGORIES";

        bool Hit(TweakVM t) => q.Length == 0 ||
            t.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || t.Summary.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            (t.T.Detail ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) || (t.T.ChoiceGroup ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);

        // searching looks through every category of the page; otherwise only the chosen one shows
        var groups = new List<PaneGroup>();
        if (_searching)
        {
            foreach (var r in _rail)
            {
                var rows = BuildRows(r.Items, Hit, q);
                if (rows.Count > 0) groups.Add(new PaneGroup(r.Title, rows.Count.ToString(), true, rows));
            }
        }
        else if (_rail.FirstOrDefault(r => r.Key == _cat) is RailItem cat)
        {
            if (IsOptimizations)
            {
                // an area: its groups, gaming and performance first, each with a small heading
                var names = cat.Items.Select(v => v.Group).Distinct().Select((g, i) => (g, i)).OrderBy(x => GroupRank(x.g)).ThenBy(x => x.i).Select(x => x.g).ToList();
                foreach (var g in names)
                {
                    var rows = BuildRows(cat.Items.Where(v => v.Group == g), _ => true, "");
                    if (rows.Count > 0) groups.Add(new PaneGroup(g, rows.Count.ToString(), names.Count > 1, rows));
                }
            }
            else groups.Add(new PaneGroup(cat.Title, "", false, BuildRows(cat.Items, _ => true, "")));
        }
        _rows = groups.SelectMany(g => g.Rows).ToList();
        TweakList.ItemsSource = groups;
        PanelEmpty.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateCategoryHeader();
    }

    /// <summary>The card above the rows: what the category is, how much of it is on, and its one action.</summary>
    void UpdateCategoryHeader()
    {
        if (_page == null) return;
        var q = SearchBox.Text.Trim();
        if (_searching)
        {
            CatGlyph.Text = "\uE721";
            CatTitle.Text = $"Results for \u201c{q}\u201d";
            CatDesc.Text = _rows.Count == 0 ? "Nothing on this page matches." : $"{_rows.Count} setting{(_rows.Count == 1 ? "" : "s")} found across every category on this page";
            CatProgress.Visibility = Visibility.Collapsed;
            CatRecBtn.Visibility = CatRecDone.Visibility = Visibility.Collapsed;
            return;
        }
        var cat = _rail.FirstOrDefault(r => r.Key == _cat);
        if (cat == null) return;
        var sw = cat.Items.Where(v => !v.T.OneShot && v.T.ChoiceGroup == null).ToList();
        var recTodo = cat.Items.Count(v => v.V.Fit == Fit.Recommended && !v.IsOn && !v.T.OneShot);
        var recAll = cat.Items.Count(v => v.V.Fit == Fit.Recommended && !v.T.OneShot);
        CatGlyph.Text = cat.Glyph;
        CatTitle.Text = cat.Title;
        CatDesc.Text = cat.Desc ?? ($"{cat.Items.Count} setting{(cat.Items.Count == 1 ? "" : "s")}" +
                       (recAll > 0 ? $"  ·  {recAll} recommended for this PC" : "  ·  nothing here is switched on automatically"));
        CatProgress.Visibility = sw.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        var on = sw.Count(v => v.IsOn);
        CatCount.Inlines.Clear();
        CatCount.Inlines.Add(new System.Windows.Documents.Run($"{on} of {sw.Count}") { FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("TextBrush") });
        CatCount.Inlines.Add(new System.Windows.Documents.Run(" on"));
        CatBar.Value = sw.Count == 0 ? 0 : (double)on / sw.Count;
        CatRecBtn.Content = $"Turn on recommended ({recTodo})";
        CatRecBtn.Visibility = recTodo > 0 ? Visibility.Visible : Visibility.Collapsed;
        CatRecDone.Visibility = recTodo == 0 && recAll > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    void OnRailPick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not RailItem r) return;
        if (r.Key == _cat && !_searching) return;
        _cat = r.Key;
        if (_searching) SearchBox.Text = "";   // re-runs the view
        else ApplyPageView();
        TweakScroll.ScrollToTop();
    }

    async void OnCatRecommended(object sender, RoutedEventArgs e)
    {
        if (_page == null || _cat == null) return;
        var key = _page.Key; var cat = _cat;
        if (IsOptimizations) await ApplyRecommendedAsync(v => v.PageKey == cat);
        else await ApplyRecommendedAsync(v => v.PageKey == key && v.Group == cat);
    }

    /// <summary>After any change: the counts in the category list, the drop-downs and the header follow.</summary>
    void RefreshListPage()
    {
        foreach (var r in _rail) r.Refresh();
        foreach (var c in _rows.OfType<ChoiceVM>()) c.Refresh();
        UpdateCategoryHeader();
    }

    /// <summary>A drop-down option was picked: undo the option in effect (so "undo" later restores the true original), then apply the new one.</summary>
    async void OnChoicePick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not ChoiceOptionVM opt) return;
        var choice = opt.Parent;
        foreach (var p in FindPopups(sender as DependencyObject)) p.IsOpen = false;
        if (_busy || opt.IsCurrent && opt.Vm.IsOn) return;
        foreach (var other in choice.Options.Where(o => !ReferenceEquals(o, opt) && o.Vm.IsOn && _engine.HasBackup(o.Vm.T)))
            await ToggleAsync(other.Vm, false);
        await ToggleAsync(opt.Vm, true);
        // every option reads the same value, so re-read them all
        var states = await Task.Run(() => choice.Options.Select(o => _engine.IsApplied(o.Vm.T)).ToList());
        for (var i = 0; i < states.Count; i++) choice.Options[i].Vm.SetOn(states[i]);
        choice.Refresh();
        RefreshListPage();
    }

    static IEnumerable<System.Windows.Controls.Primitives.Popup> FindPopups(DependencyObject? d)
    {
        // the option buttons live inside a Popup; walk up the logical tree to close it
        while (d != null)
        {
            if (d is System.Windows.Controls.Primitives.Popup p) yield return p;
            d = LogicalTreeHelper.GetParent(d) ?? (d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : null);
        }
    }

    /// <summary>Ctrl+F jumps to the search box of whichever page is showing one.</summary>
    void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F || (Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        if (AppsPanel.Visibility == Visibility.Visible) { AppsPanel.FocusSearch(); e.Handled = true; return; }
        if (DebloatAppsPanel.Visibility == Visibility.Visible) { DebloatAppsPanel.FocusSearch(); e.Handled = true; return; }
        if (CleanerPanel.Visibility == Visibility.Visible) { CleanerPanel.FocusSearch(); e.Handled = true; return; }
        if (StartupPanel.Visibility == Visibility.Visible) { StartupPanel.FocusSearch(); e.Handled = true; return; }
        if (UtilitiesPanel.Visibility == Visibility.Visible) { UtilitiesPanel.FocusSearch(); e.Handled = true; return; }
        TextBox? box = TweakPanel.Visibility == Visibility.Visible ? SearchBox
                     : SearchPanel.Visibility == Visibility.Visible ? SearchAllBox : null;
        if (box == null) return;
        box.Focus();
        box.SelectAll();
        e.Handled = true;
    }

    static (int RecDone, int RecTotal, int Default, int Custom, int Total) Stats(List<TweakVM> items)
    {
        items = items.Where(v => !v.T.OneShot && v.T.ChoiceGroup == null).ToList();
        var rec = items.Where(v => v.V.Fit == Fit.Recommended).ToList();
        return (rec.Count(v => v.IsOn), rec.Count, items.Count(v => !v.IsOn),
                items.Count(v => v.IsOn && v.V.Fit != Fit.Recommended), items.Count);
    }

    void RefreshHub()
    {
        HubTitle.Text = "Optimizations";
        HubBlurb.Text = "Pick an area. The bar shows how many of its recommended settings are on; the grey tag counts settings still at the Windows default.";
        var rows = new List<HubRow>();
        foreach (var p in PageDefs.All.Where(p => p.Section == _hubSection))
        {
            var items = _vms.Where(v => v.PageKey == p.Key).ToList();
            if (items.Count == 0) continue;
            var st = Stats(items);
            var groups = items.Select(v => v.Group).Distinct().ToList();
            var text = string.Join(", ", groups.Take(5)) + (groups.Count > 5 ? ", …" : "");
            rows.Add(new HubRow(p.Key, p.Title, text, p.Glyph,
                $"Recommended {st.RecDone}/{st.RecTotal}", st.RecTotal > 0,
                $"Default {st.Default}/{st.Total}",
                $"Custom {st.Custom}/{st.Total}", st.Custom > 0,
                st.RecTotal == 0 ? 0 : (double)st.RecDone / st.RecTotal) { FullGroups = string.Join(", ", groups) });
        }
        // the first area (Gaming & Performance) is the big card on the left; the others fill a 3 x 2 grid beside it
        var featured = rows.Count >= 4 ? rows[0] : null;
        HubFeatured.Content = featured;
        HubFeatured.Visibility = featured != null ? Visibility.Visible : Visibility.Collapsed;
        HubFeaturedCol.Width = featured != null ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        HubGapCol.Width = featured != null ? new GridLength(12) : new GridLength(0);
        HubList.ItemsSource = featured != null ? rows.Skip(1).ToList() : rows;
        var all = _vms.Where(v => PageDefs.All.Any(p => p.Section == _hubSection && p.Key == v.PageKey)).ToList();
        var s = Stats(all);
        HubSummary.Text = s.RecTotal == 0 ? "" : $"{s.RecDone} of {s.RecTotal} recommended settings applied across these pages";
    }

    void UpdatePageProgress()
    {
        if (_page == null) return;
        var items = PageItems(_page);
        var st = Stats(items);

        StatRec.Text = st.RecTotal > 0 ? $"{st.RecDone} of {st.RecTotal}" : "none here";
        StatDef.Text = st.Default.ToString();
        StatCustom.Text = st.Custom.ToString();
    }

    /// <summary>Red number beside a sidebar page: how many of its settings are switched on.</summary>
    void UpdateNavCounts()
    {
        if (_vms == null || _vms.Count == 0) return;
        foreach (var (rb, key) in _navKeys)
        {
            IEnumerable<TweakVM> set = key switch
            {
                "home" or "system" or "temps" or "search" or "settings" or "apps" or "winapps" or "cleaner" or "utilities" or "startup" or "restore" => Enumerable.Empty<TweakVM>(),
                "hub" => _vms.Where(v => PageDefs.Get(v.PageKey).Section == PageDefs.Optimizations),
                _ => _vms.Where(v => v.PageKey == key),
            };
            var n = set.Count(v => v.IsOn && !v.T.OneShot && v.T.ChoiceGroup == null);
            NavBadge.SetText(rb, n > 0 ? n.ToString() : "");
        }
    }

    void RefreshViews()
    {
        UpdateNavCounts();
        UpdatePutBack();
        if (_page != null) { UpdatePageProgress(); RefreshListPage(); }
        else if (_hub) RefreshHub();
        else RefreshHome();
    }

    // =============================================================================================
    // Home
    // =============================================================================================

    // ---- Settings Windows changed back -----------------------------------------------------------
    // A setting TrapShack applied, that was seen working, and now reads as off again was changed outside TrapShack,
    // nearly always by a Windows update. Shown as one quiet card on Home, never as a pop-up.
    // "Seen working" matters: a setting Windows refused in the first place is reported when it is applied, not here.
    const string SeenOnKey = "app.seen-on/";
    List<TweakVM> _putBack = new();

    void UpdatePutBack()
    {
        if (_vms == null || _vms.Count == 0 || _busy) return;
        var found = new List<TweakVM>();
        var newlySeen = new List<string>();
        foreach (var v in _vms)
        {
            if (v.T.OneShot) continue;
            var seenKey = SeenOnKey + v.T.Id;
            if (v.IsOn)
            {
                if (!_engine.State.Has(seenKey) && _engine.HasBackup(v.T)) newlySeen.Add(seenKey);
                continue;
            }
            if (!_engine.State.Has(seenKey)) continue;
            if (!_engine.HasBackup(v.T)) { _engine.State.Remove(seenKey); continue; }   // switched off in TrapShack: nothing to report
            if (v.V.Fit == Fit.Unavailable) continue;
            // a setting that shares its Windows value with another one that is on was switched off on purpose
            if (_clash.TryGetValue(v.T.Id, out var cl) && cl.Any(o => o.IsOn)) continue;
            if (v.T.ChoiceGroup != null && _vms.Any(o => o != v && o.T.ChoiceGroup == v.T.ChoiceGroup && o.IsOn)) continue;
            found.Add(v);
        }
        if (newlySeen.Count > 0) _engine.State.SetMany(newlySeen, "1");
        _putBack = found;
        PutBackCard.Visibility = found.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (found.Count == 0) return;
        PutBackTitle.Text = found.Count == 1 ? "Windows changed one of your settings back" : $"Windows changed {found.Count} of your settings back";
        PutBackText.Text = string.Join(", ", found.Take(2).Select(v => v.T.Title)) + (found.Count > 2 ? $" and {found.Count - 2} more" : "") +
                           "  ·  usually after a Windows update";
        PutBackText.ToolTip = string.Join("\n", found.Select(v => v.T.Title));
    }

    async void OnPutBack(object sender, RoutedEventArgs e)
    {
        if (_putBack.Count == 0) return;
        await ApplyRecommendedAsync(null, _putBack.ToList(), putBack: true);
    }

    /// <summary>Leaves them as Windows has them now; the card only comes back for a setting that is switched on again and later reset.</summary>
    void OnKeepAsIs(object sender, RoutedEventArgs e)
    {
        foreach (var v in _putBack) _engine.State.Remove(SeenOnKey + v.T.Id);
        UpdatePutBack();
    }

    /// <summary>The Home page is deliberately small: who this PC is, how far the recommended set has got, and the two actions.</summary>
    void RefreshHome()
    {
        var recommended = _vms.Where(v => v.V.Fit == Fit.Recommended && !v.T.OneShot).ToList();
        var done = recommended.Count(v => v.IsOn);
        var pending = recommended.Count - done;

        HomeSub.Text = string.Join("  ·  ", new[] { _sys.OsName, _sys.Cpu, _sys.Gpus.Count > 0 ? _sys.Gpus[0] : "", _sys.RamGb > 0 ? $"{Math.Round(_sys.RamGb)} GB RAM" : "" }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        HomeTitle.Text = recommended.Count == 0 ? "Recommended settings"
                       : pending == 0 ? "All recommended settings applied" : "Recommended settings";
        HomeCount.Text = recommended.Count == 0 ? "" : $"{done} / {recommended.Count}";
        HomeBar.Value = recommended.Count == 0 ? 0 : (double)done / recommended.Count;
        HomeText.Text = pending > 0
            ? $"{(pending == 1 ? "1 setting is" : pending + " settings are")} ready for this PC. Nothing is changed until you press the button, and every change can be switched off again."
            : "Everything TrapShack recommends for this PC is already on. You can still browse every page on the left.";
        ApplyAllButton.IsEnabled = pending > 0;
    }

    // =============================================================================================
    // One switch
    // =============================================================================================

    async void OnSwitchClick(object sender, RoutedEventArgs e)
    {
        var button = (ToggleButton)sender;
        if (button.DataContext is not TweakVM vm) return;
        await ToggleAsync(vm, button.IsChecked == true);
    }

    async void OnRunNowClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not TweakVM vm || _busy) return;
        if (!Dialogs.Confirm(this, vm.T.Title, vm.T.Confirm ?? "Run this one-time action now?", "Run now")) return;
        HideRestartBar();
        _busy = true;
        vm.SetBusy(true);
        // single actions run quietly: the card's button is greyed out while it works, and only a problem pops up
        var error = await Task.Run(() => TryRun(vm.T));
        vm.SetBusy(false);
        _busy = false;
        if (error != null) Dialogs.Info(this, "That didn't finish", vm.T.Title + ":\n\n" + error);
        SetStatus(error == null ? $"{vm.T.Title}: done" : $"{vm.T.Title}: failed");
    }

    string? TryRun(Tweak t)
    {
        try { foreach (var a in t.Actions) a.Apply(); return null; }
        catch (Exception ex) { Log.Error($"Run {t.Id}: {ex}"); return ex.Message; }
    }

    /// <summary>For each setting, the other settings that write the same Windows value differently (they would undo each other).</summary>
    readonly Dictionary<string, List<TweakVM>> _clash = new();

    void BuildClashMap()
    {
        _clash.Clear();
        var writes = new Dictionary<string, List<(TweakVM Vm, string Target)>>();
        foreach (var v in _vms.Where(v => !v.T.OneShot && v.T.ChoiceGroup == null))
            foreach (var a in v.T.Actions)
            {
                var (key, target) = a switch
                {
                    RegistryAction r => (r.ValueKey, r.TargetText),
                    RegistryDeleteAction d => (d.ValueKey, "<removed>"),
                    _ => (null as string, null as string),
                };
                if (key == null) continue;
                if (!writes.TryGetValue(key, out var list)) writes[key] = list = new();
                list.Add((v, target!));
            }
        foreach (var list in writes.Values)
            foreach (var x in list)
                foreach (var y in list)
                    if (!ReferenceEquals(x.Vm, y.Vm) && x.Target != y.Target)
                    {
                        if (!_clash.TryGetValue(x.Vm.T.Id, out var c)) _clash[x.Vm.T.Id] = c = new();
                        if (!c.Contains(y.Vm)) c.Add(y.Vm);
                    }
        foreach (var (id, list) in _clash) Log.Info($"Settings that share a value with {id}: {string.Join(", ", list.Select(v => v.T.Id))}");
    }

    async Task ToggleAsync(TweakVM vm, bool wantOn)
    {
        if (_busy) { vm.SetOn(vm.IsOn); return; }

        var clashing = wantOn && _clash.TryGetValue(vm.T.Id, out var cl) ? cl.Where(o => o.IsOn).ToList() : new List<TweakVM>();
        if (wantOn)
        {
            var parts = new List<string>();
            if (clashing.Count > 0)
                parts.Add("This changes the same Windows setting as " + string.Join(", ", clashing.Select(o => "\u201c" + o.T.Title + "\u201d")) +
                          ". To keep them from fighting, " + (clashing.Count == 1 ? "that one is" : "those are") + " switched off first.");
            if (vm.T.Confirm != null) parts.Add(vm.T.Confirm);
            if (vm.V.Fit == Fit.Caution && vm.V.Note != null) parts.Add("Heads-up for this PC: " + vm.V.Note);

            if (parts.Count > 0 || vm.T.Risk == Risk.Advanced)
            {
                var message = parts.Count > 0 ? string.Join("\n\n", parts) : "This is an advanced change.";
                if (!Dialogs.Confirm(this, vm.T.Title, message + "\n\nYou can switch it off again afterwards.", "Apply"))
                {
                    vm.SetOn(vm.IsOn);
                    return;
                }
            }
        }
        else
        {
            if (vm.T.StoreRestore)
            {
                Dialogs.Info(this, vm.T.Title,
                    "These apps were removed from Windows itself, so the only way back is the Microsoft Store. TrapShack will open it for you.");
                OpenStore();
                vm.SetOn(vm.IsOn);
                return;
            }

            if (!_engine.HasBackup(vm.T) &&
                !Dialogs.Confirm(this, vm.T.Title,
                    "TrapShack didn't make this change, so it has no backup of your original value. " +
                    "It will be set back to the normal Windows default.", "Restore default"))
            {
                vm.SetOn(vm.IsOn);
                return;
            }
        }

        if (wantOn && !await OfferRestorePointAsync())
        {
            vm.SetOn(vm.IsOn);
            return;
        }

        // settings that would undo this one are put back first, so only one of them is ever in effect
        foreach (var o in clashing)
        {
            var err = await Task.Run(() => _engine.Revert(o.T));
            if (err != null) Log.Error($"Could not switch off {o.T.Id} before {vm.T.Id}: {err}");
            o.SetOn(await Task.Run(() => _engine.IsApplied(o.T)));
        }

        HideRestartBar();
        _busy = true;
        vm.SetBusy(true);

        // one switch runs quietly: the switch is greyed out while it works, and only a problem pops up
        Action<int>? onStep = null;
        string? error = null;
        await Task.Run(() =>
        {
            try
            {
                if (wantOn) _engine.Apply(vm.T, onStep);
                else _engine.Revert(vm.T, onStep);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Log.Error($"{(wantOn ? "Apply" : "Revert")} {vm.T.Id}: {ex}");
            }
        });
        // read it back from Windows to be sure the change stuck
        var verified = false;
        if (error == null)
        {
            var isOn = await Task.Run(() => _engine.IsApplied(vm.T));
            vm.SetOn(isOn);
            verified = isOn == wantOn;
        }
        else vm.SetOn(await Task.Run(() => _engine.IsApplied(vm.T)));

        var needsRestart = wantOn && verified && (vm.T.RestartRequired || vm.T.SignOutRequired);
        vm.SetBusy(false);
        _busy = false;

        if (error != null)
            Dialogs.Info(this, "Nothing was changed", vm.T.Title + ":\n\nTrapShack put back anything it had already changed.\n\n" + error);
        else if (!verified)
            Dialogs.Info(this, "Check this setting", vm.T.Title +
                ":\n\nWindows accepted the change but the setting still reads as it did. A policy, another program or Windows itself may be overriding it.");

        if (error != null) SetStatus("Something went wrong. Details are in the log.");
        else
        {
            SetStatus((wantOn ? "Applied: " : "Restored: ") + vm.T.Title);
            if (needsRestart) _restartIds.Add(vm.T.Id);
        }
        RefreshViews();
        QueueRestartPrompt();
    }

    static void OpenStore()
    {
        try { Process.Start(new ProcessStartInfo("ms-windows-store://home") { UseShellExecute = true }); }
        catch (Exception ex) { Log.Error("Could not open the Store: " + ex.Message); }
    }

    // =============================================================================================
    // Batches: apply recommended / undo
    // =============================================================================================

    /// <summary>What "this page" means for the Quick actions menu: one page, or every optimization page on the hub.</summary>
    Func<TweakVM, bool>? CurrentScope()
    {
        if (_page != null)
        {
            if (IsOptimizations) return v => PageDefs.Get(v.PageKey).Section == PageDefs.Optimizations;
            var key = _page.Key; return v => v.PageKey == key;
        }
        if (_hub)
        {
            var section = _hubSection;
            var keys = PageDefs.All.Where(p => p.Section == section).Select(p => p.Key).ToHashSet();
            return v => keys.Contains(v.PageKey);
        }
        return null;
    }

    DateTime _quickClosed = DateTime.MinValue;
    const double QuickWidth = 340;

    void OnQuickActions(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement target) return;
        // Clicking the button while the menu is open closes it (the click-away already did); don't reopen it straight after.
        if (QuickPop.IsOpen || (DateTime.Now - _quickClosed).TotalMilliseconds < 250) { QuickPop.IsOpen = false; return; }
        UpdateQuickCounts();
        QuickPop.PlacementTarget = target;
        QuickPop.HorizontalOffset = (target.ActualWidth - QuickWidth) * UiScale.ScaleX;   // right edges line up
        QuickPop.IsOpen = true;
    }

    /// <summary>The little counters on the Quick actions menu: recommended settings on, and settings changed from the default.</summary>
    void UpdateQuickCounts()
    {
        var scope = CurrentScope();
        var items = _vms.Where(v => !v.T.OneShot && v.T.ChoiceGroup == null && (scope == null || scope(v))).ToList();
        var st = Stats(items);
        var changed = items.Count(v => v.IsOn);

        QuickScope.Text = (_page != null ? _page.Title : "All Optimizations").ToUpperInvariant();

        var allRec = st.RecTotal > 0 && st.RecDone == st.RecTotal;
        QuickApplyCount.Text = st.RecTotal == 0 ? "none" : allRec ? "all on" : $"{st.RecDone}/{st.RecTotal}";
        QuickApplyCount.Foreground = (Brush)FindResource(allRec ? "GreenBrush" : "MutedBrush");
        QuickApplySub.Text = st.RecTotal == 0 ? "Nothing is recommended here"
                           : allRec ? "Everything recommended is already on"
                           : $"{st.RecTotal - st.RecDone} still to turn on for this PC";
        QuickApplyBtn.IsEnabled = st.RecDone < st.RecTotal;

        QuickResetCount.Text = $"{changed}/{items.Count}";
        QuickResetSub.Text = changed == 0 ? "Everything here is already at the default" : $"{changed} changed setting{(changed == 1 ? "" : "s")} would go back";
        QuickResetBtn.IsEnabled = changed > 0;
    }

    void OnApplyAll(object sender, RoutedEventArgs e) => _ = ApplyRecommendedAsync(null);

    void OnHomeClean(object sender, RoutedEventArgs e) => SelectNav("cleaner");

    /// <summary>At startup the cleaner scans quietly (it only reads), so Home can show what a clean would free.</summary>
    async Task QuietCleanerScanAsync()
    {
        await Task.Yield();
        try { await CleanerPanel.ScanAsync(); }
        catch (Exception ex) { Log.Error("Background cleaner scan failed: " + ex.Message); }
        if (HomeCleanSize.Text == "…") { HomeCleanSize.Text = "?"; HomeCleanText.Text = "Open the PC Cleaner and press Scan to see it."; }
    }
    void OnApplyPage(object sender, RoutedEventArgs e) { QuickPop.IsOpen = false; _ = ApplyRecommendedAsync(CurrentScope()); }
    void OnUndoPage(object sender, RoutedEventArgs e) { QuickPop.IsOpen = false; _ = UndoAsync(CurrentScope(), toDefaults: true); }
    void OnUndoAll(object sender, RoutedEventArgs e) => _ = UndoAsync(null, toDefaults: false);

    async Task ApplyRecommendedAsync(Func<TweakVM, bool>? scope, List<TweakVM>? profile = null, bool putBack = false)
    {
        if (_busy) return;

        // a loaded profile applies its own list (only settings that are off and allowed on this PC)
        var todo = profile != null
            ? profile.Where(v => !v.IsOn && v.CanToggle && !v.T.OneShot).ToList()
            : _vms.Where(v => (scope == null || scope(v)) && v.V.Fit == Fit.Recommended && !v.IsOn).ToList();
        if (todo.Count == 0)
        {
            Dialogs.Info(this, "Nothing to apply", "Every recommended setting for this PC is already applied.");
            return;
        }

        var lines = string.Join("\n", todo.Take(14).Select(v => "•  " + v.T.Title));
        if (todo.Count > 14) lines += $"\n…and {todo.Count - 14} more";

        if (!Dialogs.Confirm(this, putBack ? "Turn your settings back on" : profile != null ? "Apply this profile" : "Apply recommended settings",
                (putBack
                    ? $"Windows changed {(todo.Count == 1 ? "this setting" : $"these {todo.Count} settings")} back after TrapShack set {(todo.Count == 1 ? "it" : "them")}. " +
                      $"This usually happens after a Windows update. TrapShack will set {(todo.Count == 1 ? "it" : "them")} again:\n\n{lines}\n\n"
                    : profile != null
                    ? $"TrapShack will apply {todo.Count} setting{(todo.Count == 1 ? "" : "s")} from the profile:\n\n{lines}\n\n"
                    : $"TrapShack will apply {todo.Count} setting{(todo.Count == 1 ? "" : "s")} chosen for this PC " +
                      $"({_sys.OsName}, {_sys.CpuBrand} CPU, {GpuBrand}):\n\n{lines}\n\n") +
                "A restore point is created first, and every change can be switched off again.", "Apply"))
            return;

        HideRestartBar();
        _busy = true;
        if (!_settings.RestorePointAsked) { _settings.RestorePointAsked = true; _settings.Save(); }   // this run makes one itself
        var steps = new List<(string, string)> { ("Create a restore point", "Windows System Restore") };
        steps.AddRange(todo.Select(v => (v.T.Title, v.T.Summary)));
        Apply.Begin("Applying", putBack ? "Turning your settings back on" : "Applying recommended settings", steps, true);

        Apply.StartStep(0);
        var (ok, message) = await Task.Run(() => RestorePoint.Create());
        Apply.EndStep(0, ok ? StepResult.Done : StepResult.Warn, ok ? "Created" : "Skipped");
        if (!ok &&
            !Dialogs.Confirm(this, "No restore point",
                $"Windows couldn't create a restore point:\n\n{message}\n\n" +
                "Every change can still be switched off one by one. Continue without one?", "Continue anyway"))
        {
            Apply.Dismiss();
            _busy = false;
            return;
        }

        int good = 0, restart = 0, failed = 0, ran = 0;
        var problems = new List<string>();
        for (var i = 0; i < todo.Count; i++)
        {
            if (Apply.CancelRequested) break;
            var vm = todo[i];
            var row = i + 1;
            Apply.StartStep(row);

            var error = await Task.Run(() => vm.T.OneShot ? TryRun(vm.T) : TryApply(vm.T));
            ran++;
            if (error != null)
            {
                failed++;
                problems.Add($"{vm.T.Title}: {error}");
                Apply.EndStep(row, StepResult.Failed);
                continue;
            }

            var verified = true;
            if (!vm.T.OneShot)
            {
                var isOn = await Task.Run(() => _engine.IsApplied(vm.T));
                vm.SetOn(isOn);
                verified = isOn;
            }
            if (verified)
            {
                good++;
                if (vm.T.RestartRequired || vm.T.SignOutRequired) { restart++; _restartIds.Add(vm.T.Id); }
                Apply.EndStep(row, StepResult.Done, vm.T.OneShot ? "Done" : null);
            }
            else
            {
                failed++;
                problems.Add($"{vm.T.Title}: Windows still reports the old value");
                Apply.EndStep(row, StepResult.Warn);
            }
        }

        var cancelled = Apply.CancelRequested && ran < todo.Count;
        var note = cancelled ? $"Stopped after {ran} of {todo.Count}. Everything already applied stays applied. "
                 : "";
        if (failed > 0) note += "Problems: " + string.Join("; ", problems.Take(2)) + (problems.Count > 2 ? $" and {problems.Count - 2} more. Details are in the log." : ".");
        if (note.Length == 0) note = "Everything was applied and read back from Windows.";

        Apply.Finish(cancelled ? "Stopped" : failed > 0 ? "Finished with problems" : "Complete",
            cancelled ? "Stopped early" : failed > 0 ? "Some settings need attention" : "All settings applied",
            note.Trim(), good, restart, failed, autoClose: !cancelled && failed == 0);
        await Apply.WaitClosedAsync();
        _busy = false;
        RefreshViews();
        SetStatus(putBack ? $"Turned {good} of {todo.Count} settings back on" : $"Applied {good} of {todo.Count} recommended settings");
        QueueRestartPrompt();
    }

    string? TryApply(Tweak t)
    {
        try { _engine.Apply(t); return null; }
        catch (Exception ex)
        {
            Log.Error($"Apply {t.Id}: {ex}");
            return ex.Message;
        }
    }

    async Task UndoAsync(Func<TweakVM, bool>? scope, bool toDefaults)
    {
        if (_busy) return;

        var candidates = _vms.Where(v => (scope == null || scope(v)) && v.IsOn && (toDefaults || _engine.HasBackup(v.T))).ToList();
        var todo = candidates.Where(v => !v.T.StoreRestore).ToList();
        var storeOnly = candidates.Count - todo.Count;

        if (todo.Count == 0)
        {
            Dialogs.Info(this, "Nothing to undo",
                storeOnly > 0
                    ? "The only changes left were removed apps, which can only be reinstalled from the Microsoft Store."
                    : toDefaults ? "Everything here is already at its Windows default." : "TrapShack hasn't changed anything here yet.");
            return;
        }

        var lines = string.Join("\n", todo.Take(14).Select(v => "•  " + v.T.Title));
        if (todo.Count > 14) lines += $"\n…and {todo.Count - 14} more";
        var extra = storeOnly > 0 ? $"\n\n{storeOnly} removed-app change{(storeOnly == 1 ? "" : "s")} can't be undone here. Reinstall those from the Microsoft Store." : "";

        if (!Dialogs.Confirm(this, "Undo changes",
                (toDefaults
                    ? $"TrapShack will set {todo.Count} setting{(todo.Count == 1 ? "" : "s")} back to how they were before TrapShack changed them, or to the Windows default if TrapShack never touched them:\n\n{lines}{extra}"
                    : $"TrapShack will put {todo.Count} setting{(todo.Count == 1 ? "" : "s")} back exactly as they were before:\n\n{lines}{extra}"), toDefaults ? "Revert" : "Undo"))
            return;

        HideRestartBar();
        _busy = true;
        Apply.Begin("Restoring", toDefaults ? "Back to Windows defaults" : "Undoing your changes",
            todo.Select(v => (v.T.Title, v.T.Summary)).ToList(), true);

        int restored = 0, failed = 0, ranCount = 0, restartCount = 0;
        var problems = new List<string>();
        for (var i = 0; i < todo.Count; i++)
        {
            if (Apply.CancelRequested) break;
            var vm = todo[i];
            Apply.StartStep(i);

            var error = await Task.Run(() =>
            {
                try { _engine.Revert(vm.T); return (string?)null; }
                catch (Exception ex) { Log.Error($"Revert {vm.T.Id}: {ex}"); return ex.Message; }
            });
            ranCount++;

            var isOn = await Task.Run(() => _engine.IsApplied(vm.T));
            vm.SetOn(isOn);
            if (error == null && !isOn)
            {
                restored++;
                if (vm.T.RestartRequired || vm.T.SignOutRequired) { restartCount++; _restartIds.Add(vm.T.Id); }
                Apply.EndStep(i, StepResult.Done);
            }
            else
            {
                failed++;
                problems.Add($"{vm.T.Title}: {error ?? "Windows still reports the changed value"}");
                Apply.EndStep(i, error == null ? StepResult.Warn : StepResult.Failed);
            }
        }

        var stopped = Apply.CancelRequested && ranCount < todo.Count;
        var note = stopped ? $"Stopped after {ranCount} of {todo.Count}. " : "";
        if (failed > 0) note += "Problems: " + string.Join("; ", problems.Take(2)) + (problems.Count > 2 ? $" and {problems.Count - 2} more." : ".");
        if (note.Length == 0) note = "Everything was restored and read back from Windows.";

        Apply.Finish(stopped ? "Stopped" : failed > 0 ? "Finished with problems" : "Complete",
            stopped ? "Stopped early" : failed > 0 ? "Some settings could not be restored" : "Restored",
            note.Trim(), restored, restartCount, failed, autoClose: !stopped && failed == 0);
        await Apply.WaitClosedAsync();
        _busy = false;
        RefreshViews();
        SetStatus($"Restored {restored} settings");
        QueueRestartPrompt();
    }

    async void OnRestorePoint(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _busy = true;
        ShowBusy("Creating a restore point…");
        var (ok, message) = await Task.Run(() => RestorePoint.Create());
        _busy = false;
        HideBusy();
        Dialogs.Info(this, ok ? "Restore point created" : "No restore point",
            ok ? "If anything ever goes wrong you can roll back from Windows' System Restore."
               : "Windows couldn't create one:\n\n" + message);
    }

    // =============================================================================================
    // Window chrome
    // =============================================================================================

    void ShowBusy(string text)
    {
        BusyText.Text = text;
        BusyOverlay.Visibility = Visibility.Visible;
        BusySlide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-130, 520, TimeSpan.FromSeconds(1.3)) { RepeatBehavior = RepeatBehavior.Forever });
    }
    void SetBusyText(string text) => BusyText.Text = text;
    void HideBusy()
    {
        BusyOverlay.Visibility = Visibility.Collapsed;
        BusySlide.BeginAnimation(TranslateTransform.XProperty, null);   // a hidden endless animation still costs a frame every 16 ms
    }
    DispatcherTimer? _statusTimer;

    /// <summary>A short message at the bottom of the window (what was just applied). It disappears by itself after a few seconds.</summary>
    void SetStatus(string text)
    {
        StatusText.Text = text;
        StatusBar.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
        _statusTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _statusTimer.Tick -= OnStatusTimeout;
        _statusTimer.Tick += OnStatusTimeout;
        _statusTimer.Stop(); _statusTimer.Start();
    }

    void OnStatusTimeout(object? sender, EventArgs e)
    {
        _statusTimer?.Stop();
        StatusText.Text = "";
        StatusBar.Visibility = Visibility.Collapsed;
    }

    void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    void OnClose(object sender, RoutedEventArgs e)
    {
        if ((_busy || CleanerPanel.Busy) && !Dialogs.Confirm(this, "TrapShack is still working",
                "Closing now could interrupt a change in progress. Close anyway?", "Close anyway"))
            return;
        _closeConfirmed = true;
        StopSensors();
        MonitorPanel.Shutdown();
        Close();
    }

    bool _closeConfirmed;

    /// <summary>Alt+F4 and the taskbar's Close go through here too: never close silently while something is being applied.</summary>
    void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_closeConfirmed || !(_busy || CleanerPanel.Busy)) return;
        if (!Dialogs.Confirm(this, "TrapShack is still working",
                "Changes are still being applied. Closing now could leave a setting half-done.\n\nWait until it finishes, or close anyway?",
                "Close anyway", "Wait"))
        { e.Cancel = true; return; }
        StopSensors();
        MonitorPanel.Shutdown();
    }

    // =============================================================================================
    // Restart prompt and the one-time restore point question
    // =============================================================================================

    /// <summary>
    /// Shows the restart prompt only after every queued change has finished and the progress panel has had a moment
    /// on screen, so nobody clicks it by accident while work is still running.
    /// </summary>
    async void QueueRestartPrompt()
    {
        if (_busy || _restartIds.Count == 0) return;
        await Task.Delay(1600);
        if (_busy || _restartIds.Count == 0) return;

        Apply.Dismiss();
        _restartArmed = false;
        var n = _restartIds.Count;
        RestartTitle.Text = "Restart to finish";
        RestartSub.Text = _restartNote ?? $"{n} change{(n == 1 ? "" : "s")} take{(n == 1 ? "s" : "")} effect after a restart. Save your work first.";
        RestartLaterBtn.Content = "Later";
        RestartNowBtn.Content = "Restart now";
        RestartNowBtn.IsEnabled = false;
        RestartBar.Visibility = Visibility.Visible;
        RestartBar.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220)));

        await Task.Delay(1800);   // the button stays greyed out for a moment
        if (!_busy && RestartBar.Visibility == Visibility.Visible) RestartNowBtn.IsEnabled = true;
    }

    // ---- IPageHost: what the Apps pages use ----
    Window IPageHost.Win => this;
    Settings IPageHost.Cfg => _settings;
    ProgressDock IPageHost.Dock => Apply;
    bool IPageHost.Busy { get => _busy; set => _busy = value; }

    void IPageHost.SuggestRestart(string text)
    {
        _restartNote = text;
        _restartIds.Add("apps.removed");
        QueueRestartPrompt();
    }

    void HideRestartBar()
    {
        _restartNote = null;
        _restartIds.Remove("apps.removed");
        RestartBar.BeginAnimation(OpacityProperty, null);
        RestartBar.Opacity = 0;
        RestartBar.Visibility = Visibility.Collapsed;
        _restartArmed = false;
    }

    void OnRestartLater(object sender, RoutedEventArgs e)
    {
        if (_restartArmed)
        {
            // Backed out of the "are you sure" step: go back to the first state.
            _restartArmed = false;
            var n = _restartIds.Count;
            RestartSub.Text = _restartNote ?? $"{n} change{(n == 1 ? "" : "s")} take{(n == 1 ? "s" : "")} effect after a restart. Save your work first.";
            RestartLaterBtn.Content = "Later";
            RestartNowBtn.Content = "Restart now";
            return;
        }
        HideRestartBar();
    }

    async void OnRestartNow(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (!_restartArmed)
        {
            _restartArmed = true;
            RestartSub.Text = "Windows will restart in 10 seconds. Anything unsaved will be lost.";
            RestartLaterBtn.Content = "Cancel";
            RestartNowBtn.Content = "Yes, restart";
            RestartNowBtn.IsEnabled = false;
            await Task.Delay(900);
            if (_restartArmed) RestartNowBtn.IsEnabled = true;
            return;
        }
        HideRestartBar();
        Shell.Run("shutdown.exe", "/r /t 10 /c \"TrapShack Tweak Utility: finishing your changes\"", 15_000);
    }

    /// <summary>
    /// Asked once, before the very first change. Either answer is remembered, so this never nags.
    /// Returns false only if the window was closed in a way that should cancel the change.
    /// </summary>
    async Task<bool> OfferRestorePointAsync()
    {
        if (_settings.RestorePointAsked) return true;
        _settings.RestorePointAsked = true;
        _settings.Save();

        if (!Dialogs.Confirm(this, "Create a restore point first?",
                "It takes about 20 seconds and lets you roll Windows back if anything misbehaves. You will only be asked this once.",
                "Create restore point", "Skip"))
            return true;

        HideRestartBar();
        _busy = true;
        Apply.Begin("Restore point", "Creating a restore point",
            new List<(string, string)> { ("Windows is saving a rollback point", "Windows System Restore") }, false);
        Apply.StartStep(0);
        var (ok, message) = await Task.Run(() => RestorePoint.Create());
        Apply.EndStep(0, ok ? StepResult.Done : StepResult.Warn);
        if (ok) Apply.Finish("Complete", "Restore point created", "Windows saved a rollback point.", 1, 0, 0, autoClose: true);
        else Apply.Finish("Skipped", "No restore point", "Windows couldn't create one: " + message, 0, 0, 1, autoClose: false);
        _busy = false;
        return true;
    }

    // Everything is laid out for a 1280 x 800 window and scaled smoothly to the real window: smaller in small windows (so
    // pages fit without scrolling), larger on big screens. Cards re-flow by width on their own (see AdaptiveGrid), so the
    // scale only has to keep text a readable size. The Quick actions menu and the detail pop-ups live in their own
    // windows, so they read the same factor from the UiScaleFactor resource.
    const double DesignW = 1280, DesignH = 800;

    void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var w = Math.Max(1, ActualWidth - (WindowState == WindowState.Maximized ? 14 : 0));
        var h = Math.Max(1, ActualHeight - (WindowState == WindowState.Maximized ? 14 : 0));
        var raw = Math.Min(w / DesignW, h / DesignH);
        // shrink in small windows so pages fit; never blow things up on a normal screen (a maximised 1080p window
        // stays at 100% and simply shows more cards). Only really big screens (1440p and up) grow a little.
        if (raw > 1.0) raw = raw < 1.5 ? 1.0 : Math.Min(1.0 + (raw - 1.5) * 0.4, 1.25);
        var scale = Math.Clamp(Math.Round(raw * 40) / 40, 0.75, 1.25);   // steps of 0.025 keep text sharp
        if (Math.Abs(UiScale.ScaleX - scale) < 0.001) return;
        UiScale.ScaleX = scale; UiScale.ScaleY = scale;
        Application.Current.Resources["UiScaleFactor"] = scale;
    }

    bool _effectsReady;

    void OnEffectsToggled(object sender, RoutedEventArgs e)
    {
        if (!_effectsReady) return;
        var on = EffectsCheck.IsChecked == true;
        _settings.LiveEffects = on; _settings.Save();
        Fx.On = on;
        Fx.Haze(Haze, on);
    }

    // =============================================================================================
    // Profiles: save which settings are on to a file, and load that file on another PC

    void OnExportProfile(object sender, RoutedEventArgs e)
    {
        var on = _vms.Where(v => v.IsOn && !v.T.OneShot).Select(v => v.T.Id).OrderBy(x => x).ToList();
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save profile", Filter = "TrapShack profile (*.tsprofile)|*.tsprofile",
            FileName = $"TrapShack profile {DateTime.Now:yyyy-MM-dd}.tsprofile",
        };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new ProfileFile
            {
                App = "TrapShack Tweak Utility", Made = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), PcName = Environment.MachineName, On = on,
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(dlg.FileName, json);
            ProfileStatus.Text = $"Saved {on.Count} settings to {System.IO.Path.GetFileName(dlg.FileName)}.";
        }
        catch (Exception ex) { Dialogs.Info(this, "Couldn't save the profile", ex.Message); }
    }

    async void OnImportProfile(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Load profile", Filter = "TrapShack profile (*.tsprofile)|*.tsprofile|All files|*.*" };
        if (dlg.ShowDialog(this) != true) return;
        ProfileFile? p;
        try { p = System.Text.Json.JsonSerializer.Deserialize<ProfileFile>(System.IO.File.ReadAllText(dlg.FileName)); }
        catch (Exception ex) { Dialogs.Info(this, "That file isn't a profile", ex.Message); return; }
        if (p?.On == null || p.On.Count == 0) { Dialogs.Info(this, "Empty profile", "This profile has no settings in it."); return; }
        var ids = p.On.ToHashSet();
        var list = _vms.Where(v => ids.Contains(v.T.Id)).ToList();
        var skipped = list.Count(v => !v.CanToggle);
        var unknown = ids.Count - list.Count;
        ProfileStatus.Text = $"Profile from {p.PcName} ({p.Made}): {list.Count(v => v.IsOn)} already on" +
                             (skipped > 0 ? $", {skipped} not for this PC" : "") + (unknown > 0 ? $", {unknown} from another version" : "") + ".";
        await ApplyRecommendedAsync(null, list);
    }

    public sealed class ProfileFile
    {
        public string App { get; set; } = "";
        public string Made { get; set; } = "";
        public string PcName { get; set; } = "";
        public List<string> On { get; set; } = new();
    }

    void OnLightToggled(object sender, RoutedEventArgs e)
    {
        if (!_effectsReady) return;
        var on = LightCheck.IsChecked == true;
        _settings.LightTheme = on; _settings.Save();
        ThemeMode.Apply(on);
        BuildSwatches();
    }

    void OnStateChanged(object? sender, EventArgs e)
    {
        var max = WindowState == WindowState.Maximized;
        RootGrid.Margin = max ? new Thickness(7) : new Thickness(0);
        MaxButton.Content = max ? "" : "";
    }

    // =============================================================================================
    // System info (live sensors) and accent colour
    // =============================================================================================

    readonly Settings _settings = Settings.Load();

    Task<List<InfoSection>>? _infoTask;

    /// <summary>Reads hardware details and starts the sensors quietly after startup, so System info opens instantly.</summary>
    void PreloadSystemInfo()
    {
        _infoTask ??= Task.Run(() => HardwareInfo.Collect(_sys));
        MonitorPanel.Init(_sys);   // the sensors themselves start a few seconds later (BackgroundStartAsync)
    }

    async void ShowSystem()
    {
        ShowPanel(SystemPanel);
        FillSystemHeader();

        // Details were read in the background at startup, so they show at once; sensors were started then too.
        _infoTask ??= Task.Run(() => HardwareInfo.Collect(_sys));
        if (_infoTask.IsCompleted) { try { InfoList.ItemsSource = _info = _infoTask.Result; } catch { } }
        try { InfoList.ItemsSource = _info = await _infoTask; }
        catch (Exception ex) { Log.Error("System info failed: " + ex.Message); }
        CopySpecsBtn.IsEnabled = _info is { Count: > 1 };
    }

    // ---- Copy specs ---------------------------------------------------------------------------------
    List<InfoSection>? _info;
    int _copyStamp;

    async void OnCopySpecs(object sender, RoutedEventArgs e)
    {
        if (_info == null) return;
        var text = SpecsText(_info);
        var ok = false;
        for (var i = 0; i < 5 && !ok; i++)
        {
            // the clipboard can be briefly held by another app: try again a moment later
            try { Clipboard.SetDataObject(text, true); ok = true; }
            catch (System.Runtime.InteropServices.ExternalException) { await Task.Delay(80); }
        }
        CopySpecsIcon.Text = ok ? "\uE73E" : "\uE783";
        CopySpecsText.Text = ok ? "Copied" : "Couldn't copy";
        var stamp = ++_copyStamp;
        await Task.Delay(2000);
        if (stamp != _copyStamp) return;
        CopySpecsIcon.Text = "\uE8C8";
        CopySpecsText.Text = "Copy specs";
    }

    /// <summary>
    /// The PC's specs as a short block for Discord (a code block keeps the columns lined up there).
    /// Leaves out the PC name, addresses and DNS: only what helps someone answer a question about the hardware.
    /// </summary>
    string SpecsText(List<InfoSection> info)
    {
        InfoSection? Sec(string title) => info.FirstOrDefault(x => x.Title == title);
        static string Row(InfoSection? sec, string label) => sec?.Rows.FirstOrDefault(r => r.Label == label)?.Value ?? "";
        static string Join(params string[] parts) => string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

        var lines = new List<(string Label, string Value)>();
        lines.Add(("OS", Join($"{_sys.OsName} {_sys.DisplayVersion}".Trim(), _sys.Build > 0 ? $"build {_sys.Build}.{_sys.Ubr}" : "")));

        var cpu = Sec("Processor");
        lines.Add(("CPU", Join(cpu?.Sub ?? _sys.Cpu, Row(cpu, "Cores").Replace("  ·  ", " / "))));

        var gpus = info.Where(x => x.Title.StartsWith("Graphics")).ToList();
        foreach (var g in gpus)
            lines.Add((gpus.Count > 1 ? "GPU " + (gpus.IndexOf(g) + 1) : "GPU", Join(g.Sub, Row(g, "VRAM"), Row(g, "Driver").Length > 0 ? "driver " + Row(g, "Driver") : "")));

        var ram = Sec("Memory");
        var ramDetail = Join(Row(ram, "Modules"), Row(ram, "Speed"), Row(ram, "Maker"));
        lines.Add(("RAM", $"{Math.Round(_sys.RamGb):0} GB" + (ramDetail.Length > 0 ? $" ({ramDetail})" : "")));

        var board = Sec("Motherboard");
        if (board != null)
        {
            var maker = System.Text.RegularExpressions.Regex.Match(Row(board, "Maker"), @"^[\p{L}\p{N}]+").Value;
            var name = board.Sub.StartsWith(maker, StringComparison.OrdinalIgnoreCase) ? board.Sub : $"{maker} {board.Sub}".Trim();
            lines.Add(("Board", Join(name, Row(board, "BIOS").Length > 0 ? "BIOS " + Row(board, "BIOS") : "")));
        }

        var disks = Sec("Storage")?.Rows ?? new List<InfoRow>();
        for (var i = 0; i < disks.Count; i++)
        {
            var parts = disks[i].Value.Split('\n');
            // "Healthy" is the normal case; anything else is worth passing on
            var facts = parts.Length > 1 ? string.Join(", ", parts[1].Split("  ·  ").Where(f => f != "Healthy")) : "";
            lines.Add((i == 0 ? "Storage" : "", parts[0] + (facts.Length > 0 ? $" ({facts})" : "")));
        }

        var first = gpus.FirstOrDefault();
        if (first != null && Row(first, "Display").Length > 0)
            lines.Add(("Display", Row(first, "Display") + (Row(first, "Refresh rate").Length > 0 ? " at " + Row(first, "Refresh rate") : "")));

        var width = lines.Max(l => l.Label.Length) + 2;
        var body = string.Join("\n", lines.Where(l => l.Value.Length > 0).Select(l => l.Label.PadRight(width) + l.Value));
        return "```\n" + body + "\n```";
    }

    void FillSystemHeader()
    {
        SysName.Text = Environment.MachineName.ToUpperInvariant();
        var gpu = _sys.Gpus.FirstOrDefault() ?? "";
        SysLine.Text = string.Join("  ·  ", new[] { _sys.OsName, _sys.Cpu, gpu, _sys.RamGb > 0 ? $"{Math.Round(_sys.RamGb)} GB RAM" : "" }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        var chips = new List<string> { _sys.IsLaptop ? "Laptop" : "Desktop", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit" };
        if (_sys.Build > 0) chips.Add("Build " + _sys.Build + (_sys.Ubr > 0 ? "." + _sys.Ubr : ""));
        if (_sys.IsVirtualMachine) chips.Add("Virtual machine");
        if (_sys.DomainJoined) chips.Add("Domain joined");
        chips.Add("Up " + FormatUptime(TimeSpan.FromMilliseconds(Environment.TickCount64)));
        SysChips.ItemsSource = chips;
    }

    static string FormatUptime(TimeSpan t) =>
        t.TotalDays >= 1 ? $"{(int)t.TotalDays} d {t.Hours} h" : t.TotalHours >= 1 ? $"{(int)t.TotalHours} h {t.Minutes} min" : $"{t.Minutes} min";

    // ---------- updates ----------
    UpdateInfo? _update;

    /// <summary>Where releases are published. Kept in code only; the Settings page just shows a GitHub mark and the button.</summary>
    const string UpdateRepo = "Kronos-VII/TrapShack-PC-Panel";

    void ShowUpdateSetting()
    {
        // the GitHub mark, drawn in the theme's text colour
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/Brand/github.png");
            if (Application.GetResourceStream(uri) != null)
            {
                GitHubMark.OpacityMask = new ImageBrush(new System.Windows.Media.Imaging.BitmapImage(uri)) { Stretch = Stretch.Uniform };
                GitHubMark.Visibility = Visibility.Visible;
                GitHubGlyph.Visibility = Visibility.Collapsed;
            }
        }
        catch { /* no mark: the icon stays */ }
    }

    async void OnCheckUpdate(object sender, RoutedEventArgs e)
    {
        UpdCheck.IsEnabled = false;
        UpdInstall.Visibility = Visibility.Collapsed;
        UpdStatus.Foreground = (Brush)FindResource("MutedBrush");
        UpdStatus.Text = "Checking GitHub…";
        try
        {
            _update = await Updater.CheckAsync(UpdateRepo);
            if (_update == null)
            {
                UpdStatus.Foreground = (Brush)FindResource("GreenBrush");
                UpdStatus.Text = "You're up to date.";
            }
            else
            {
                UpdStatus.Foreground = (Brush)FindResource("AmberBrush");
                UpdStatus.Text = "A new version is ready to install.";
                UpdInstall.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Update check failed: " + ex.Message);
            UpdStatus.Text = ex is HttpRequestException ? "Could not reach GitHub. Check your internet connection." : ex.Message;
        }
        finally { UpdCheck.IsEnabled = true; }
    }

    async void OnInstallUpdate(object sender, RoutedEventArgs e)
    {
        if (_update == null) return;
        if (!Dialogs.Confirm(this, "Update TrapShack", "TrapShack will download version " + _update.Latest.ToString(3) + ", close, and open again by itself. Your settings and undo data are kept.", "Update now", "Not now")) return;
        UpdInstall.IsEnabled = false; UpdCheck.IsEnabled = false;
        UpdBar.Value = 0; UpdBar.Visibility = Visibility.Visible;
        UpdStatus.Foreground = (Brush)FindResource("MutedBrush");
        UpdStatus.Text = "Downloading…";
        try
        {
            var file = await Updater.DownloadAsync(_update, new Progress<(long Done, long Total)>(p =>
            {
                if (p.Total <= 0) { UpdStatus.Text = "Downloading… " + WingetService.FormatMb(p.Done); return; }
                var f = (double)p.Done / p.Total; UpdBar.Value = f;
                UpdStatus.Text = $"Downloading…  {WingetService.FormatPair(p.Done, p.Total)}  ·  {(int)(f * 100)}%";
            }));
            UpdStatus.Text = "Installing and restarting…";
            Updater.StartSwap(file);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Error("Update failed: " + ex.Message);
            UpdStatus.Text = "The update did not work: " + ex.Message;
            UpdBar.Visibility = Visibility.Collapsed;
            UpdInstall.IsEnabled = true; UpdCheck.IsEnabled = true;
        }
    }

    void StopSensors() => MonitorPanel.Hide();

    readonly HsvState _hsv = new();

    /// <summary>Called when Settings opens: puts the wheel, brightness bar, hex box and swatches on the saved accent.</summary>
    void BuildSwatches()
    {
        AccentTheme.TryParse(_settings.Accent, out var current);
        _hsv.Set(current);
        ShowAccent(current);
        AccentHexNote.Text = "Type a hex code and press Enter, or pick a ready-made colour:";

        SwatchRow.Children.Clear();
        foreach (var (name, hex) in AccentTheme.Swatches)
        {
            AccentTheme.TryParse(hex, out var c);
            var selected = c == current;
            var b = new Button
            {
                Width = 28, Height = 28, Margin = new Thickness(0, 0, 8, 6), Cursor = Cursors.Hand, ToolTip = name, Tag = hex,
                Background = new SolidColorBrush(c), BorderThickness = new Thickness(selected ? 3 : 1),
                BorderBrush = (Brush)FindResource(selected ? "TextBrush" : "BorderBrushSoft"),
                Template = SwatchTemplate(),
            };
            b.Click += (_, _) => SetAccent(hex);
            SwatchRow.Children.Add(b);
        }
    }

    static ControlTemplate SwatchTemplate()
    {
        var t = new ControlTemplate(typeof(Button));
        var f = new FrameworkElementFactory(typeof(Border));
        f.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
        f.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
        f.SetBinding(Border.BorderBrushProperty, new Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
        f.SetBinding(Border.BorderThicknessProperty, new Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
        t.VisualTree = f;
        return t;
    }

    void ShowAccent(Color c)
    {
        AccentHex.Text = AccentTheme.ToHex(c);
        AccentPreview.Background = new SolidColorBrush(c);
        Wheel.Redraw(); ValueBar.Redraw();
    }

    /// <summary>While dragging on the wheel or the brightness bar: recolour the whole app live.</summary>
    void OnAccentPicking()
    {
        var c = _hsv.Color;
        AccentTheme.Apply(c);
        ShowAccent(c);
    }

    void OnAccentPicked()
    {
        var c = _hsv.Color;
        _settings.Accent = AccentTheme.ToHex(c); _settings.Save();
        AccentHexNote.Text = "Saved. This colour will be used next time too.";
        BuildSwatches();
    }

    void SetAccent(string hex)
    {
        if (!AccentTheme.TryParse(hex, out var c))
        {
            AccentHexNote.Text = "That is not a valid colour. Use six hex digits, like #E5213B.";
            return;
        }
        _hsv.Set(c);
        AccentTheme.Apply(c);
        _settings.Accent = AccentTheme.ToHex(c);
        _settings.Save();
        BuildSwatches();
        AccentHexNote.Text = "Saved. This colour will be used next time too.";
    }

    void OnThemeClick(object sender, RoutedEventArgs e) => SelectNav("settings");

    void ShowRestorePointSetting() =>
        RpSettingNote.Text = _settings.RestorePointAsked
            ? "TrapShack asks once, before your first change, whether to create a restore point. That question has been answered."
            : "TrapShack will ask once, before your first change, whether to create a restore point.";

    void OnAskRestorePointAgain(object sender, RoutedEventArgs e)
    {
        _settings.RestorePointAsked = false;
        _settings.Save();
        ShowRestorePointSetting();
    }

    void OnAccentHexKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SetAccent(AccentHex.Text);
    }

    void OnAccentHexLost(object sender, RoutedEventArgs e)
    {
        if (AccentTheme.TryParse(AccentHex.Text, out var c) && AccentTheme.ToHex(c) != _settings.Accent) SetAccent(AccentHex.Text);
    }

    // =============================================================================================
    // Silent re-check: keeps every switch matching what Windows really has, with no UI noise.
    // =============================================================================================

    DispatcherTimer? _recheckTimer;
    DateTime _lastCheck = DateTime.MinValue;
    DateTime _lastDeepCheck = DateTime.Now;
    bool _rechecking;

    void StartSilentRecheck()
    {
        _recheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _recheckTimer.Tick += (_, _) => _ = SilentRecheckAsync();
        _recheckTimer.Start();
        Activated += (_, _) => _ = SilentRecheckAsync();
    }

    async Task SilentRecheckAsync()
    {
        // Never while something is being changed, and never more than about once every 20 seconds.
        if (_busy || _rechecking || !_loaded || _vms.Count == 0) return;
        // Nothing at all while the app is in the background (a game has focus, or it is minimised): it re-checks the
        // moment you switch back to it.
        if (!IsActive || WindowState == WindowState.Minimized) return;
        if ((DateTime.Now - _lastCheck).TotalSeconds < 20) return;
        _rechecking = true;
        try
        {
            var deep = (DateTime.Now - _lastDeepCheck).TotalMinutes >= 10;
            var vms = _vms.ToList();
            var states = await Task.Run(() =>
            {
                if (deep) { try { Caches.RefreshTasks(); Caches.RefreshAppx(); } catch (Exception ex) { Log.Error("Cache refresh: " + ex.Message); } }
                return vms.Select(v => { try { return (bool?)_engine.IsApplied(v.T); } catch { return null; } }).ToList();
            });
            if (deep) _lastDeepCheck = DateTime.Now;
            if (_busy) return;   // something started while we were reading: those results may be stale

            var changed = false;
            for (var i = 0; i < vms.Count; i++)
            {
                if (states[i] is bool on && on != vms[i].IsOn && !vms[i].T.OneShot) { vms[i].SetOn(on); changed = true; }
            }
            _lastCheck = DateTime.Now;
            if (changed) RefreshViews();
        }
        catch (Exception ex) { Log.Error("Silent re-check failed: " + ex.Message); }
        finally { _rechecking = false; }
    }
}
