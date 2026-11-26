using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>One startup program on the page.</summary>
public sealed class StartupVM : Notify
{
    public StartupVM(StartupEntry e) { E = e; }
    public StartupEntry E { get; }
    public string Name => E.Name;
    public string Sub => string.IsNullOrEmpty(E.Publisher) ? E.Where : E.Publisher + "  ·  " + E.Where;
    public string Tip => E.Command;

    ImageSource? _icon;
    public ImageSource? Icon { get => _icon; set { if (Set(ref _icon, value)) Raise(nameof(NoIcon)); } }
    public bool NoIcon => _icon == null;

    public bool Enabled => E.Enabled;
    bool _busy;
    public bool NotBusy { get => !_busy; set { _busy = !value; Raise(); } }
    public void Refresh() { Raise(nameof(Enabled)); Raise(nameof(Impact)); Raise(nameof(ImpactLevel)); Raise(nameof(ImpactShort)); }

    public string Impact => E.DelayMs <= 0 ? (E.Enabled ? "No startup delay recorded" : "Off: doesn't start with Windows")
        : $"Slowed startup by {(E.DelayMs >= 1000 ? $"{E.DelayMs / 1000.0:0.#} s" : $"{E.DelayMs} ms")}";
    public string ImpactShort => E.DelayMs <= 0 ? "" : $"+{(E.DelayMs >= 1000 ? $"{E.DelayMs / 1000.0:0.#} s" : $"{E.DelayMs} ms")} at startup";
    public string ImpactLevel => E.DelayMs >= 1500 ? "high" : E.DelayMs >= 400 ? "medium" : E.DelayMs > 0 ? "low" : "none";
}

/// <summary>The Startup apps page.</summary>
public partial class StartupPage : UserControl
{
    List<StartupVM> _all = new();
    bool _loading;
    string _bootText = "";

    public StartupPage() { InitializeComponent(); }

    public void FocusSearch() => Search.Focus();

    public async Task LoadAsync()
    {
        if (_loading) return;
        _loading = true;
        Loading.Visibility = Visibility.Visible;
        try
        {
            var (items, boot) = await Task.Run(() => (StartupApps.Read(), StartupApps.LastBootTime()));
            _all = items.Select(e => new StartupVM(e)).ToList();
            _bootText = boot is TimeSpan t ? $"{t.TotalSeconds:0.#} s" : "not recorded";
            Refresh();
            // icons come in the background
            foreach (var vm in _all.ToList())
            {
                var path = vm.E.FilePath;
                vm.Icon = await Task.Run(() => { var i = ShellIcon.For(path); return i; });
            }
        }
        catch (Exception ex) { Log.Error("Startup apps: " + ex.Message); Empty.Text = "Couldn't read the startup programs: " + ex.Message; Empty.Visibility = Visibility.Visible; }
        finally { _loading = false; Loading.Visibility = Visibility.Collapsed; }
    }

    // ------------------------------------------------------------------ what to show (the list on the left)
    static readonly (string Key, string Glyph, string Desc)[] Views =
    {
        ("All programs", "\uE7E8", "Everything that opens when Windows starts, slowest first"),
        ("Slow to start", "\uEC4A", "Programs Windows measured slowing your startup down"),
        ("Switched on", "\uE73E", "Programs that open with Windows right now"),
        ("Switched off", "\uE711", "Programs you've stopped from opening with Windows"),
    };
    string _view = "All programs";
    readonly List<RailVM> _rail = new();
    bool _building;

    static bool In(string view, StartupVM v) => view switch
    {
        "Slow to start" => v.E.DelayMs >= 400,
        "Switched on" => v.E.Enabled,
        "Switched off" => !v.E.Enabled,
        _ => true,
    };

    void OnRailChecked(object sender, RoutedEventArgs e)
    {
        if (_building || ((FrameworkElement)sender).DataContext is not RailVM r) return;
        if (r.Key == _view && Search.Text.Length == 0) return;
        _view = r.Key;
        if (Search.Text.Length > 0) { Search.Text = ""; return; }
        Refresh();
        Scroller.ScrollToTop();
    }

    void Refresh()
    {
        var q = Search.Text.Trim();
        _rail.Clear();
        foreach (var v in Views)
            _rail.Add(new RailVM { Key = v.Key, Title = v.Key, Glyph = v.Glyph, Count = _all.Count(x => In(v.Key, x)).ToString(), Selected = q.Length == 0 && v.Key == _view });
        _building = true; Rail.ItemsSource = null; Rail.ItemsSource = _rail; _building = false;

        var shown = _all.Where(v => q.Length > 0
                ? v.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || v.Sub.Contains(q, StringComparison.OrdinalIgnoreCase)
                : In(_view, v))
            .OrderByDescending(v => v.E.DelayMs).ThenByDescending(v => v.E.Enabled).ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
        Tiles.ItemsSource = shown;
        var view = Views.First(x => x.Key == _view);
        PaneGlyph.Text = q.Length > 0 ? "\uE721" : view.Glyph;
        PaneTitle.Text = q.Length > 0 ? $"Results for \u201c{q}\u201d" : view.Key;
        PaneDesc.Text = q.Length > 0 ? $"{shown.Count} program{(shown.Count == 1 ? "" : "s")} found" : view.Desc;
        Empty.Visibility = shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        Empty.Text = q.Length > 0 ? "Nothing matches that." : _all.Count == 0 ? "Nothing starts with Windows on this PC." : "Nothing here.";
        UpdateChips();
    }

    void UpdateChips()
    {
        var on = _all.Count(v => v.E.Enabled);
        StatBoot.Text = _bootText;
        StatOn.Text = $"{on} of {_all.Count}";
        foreach (var r in _rail) r.Count = _all.Count(x => In(r.Key, x)).ToString();
    }

    void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        Refresh();
    }

    /// <summary>What a startup item does that people would miss, matched on its name, publisher and command.</summary>
    static string? StartupWarning(StartupVM vm)
    {
        var t = (vm.Name + " " + vm.E.Publisher + " " + vm.E.Command).ToLowerInvariant();
        if (t.Contains("securityhealth") || t.Contains("windows security") || t.Contains("defender"))
            return "This is Windows Security's tray icon and alerts. Protection keeps running, but you won't see warnings about threats or problems.";
        if (t.Contains("antivirus") || t.Contains("malwarebytes") || t.Contains("avast") || t.Contains("norton") || t.Contains("bitdefender") || t.Contains("kaspersky") || t.Contains("eset") || t.Contains("mcafee"))
            return "This looks like security software. Switching it off can leave the PC unprotected until you open it yourself.";
        if (t.Contains("realtek") || t.Contains("rtkaud") || t.Contains("nahimic") || t.Contains("waves") || t.Contains("dolby") || t.Contains("audio"))
            return "This is part of your sound driver. Audio effects, jack detection or the sound control app may stop working.";
        if (t.Contains("synaptics") || t.Contains("elan") || t.Contains("touchpad") || t.Contains("precision"))
            return "This is part of your touchpad driver. Gestures and touchpad settings may stop working.";
        if (t.Contains("onedrive") || t.Contains("dropbox") || t.Contains("google drive") || t.Contains("icloud"))
            return "Your files stop syncing until you open the app yourself.";
        if (t.Contains("nvidia") || t.Contains("amd") || t.Contains("radeon") || t.Contains("intel") || t.Contains("igfx"))
            return "This belongs to your graphics or chipset driver. Its control panel, hotkeys or overlay may stop working until you open it.";
        if (t.Contains("logi") || t.Contains("razer") || t.Contains("corsair") || t.Contains("steelseries") || t.Contains("icue") || t.Contains("armoury") || t.Contains("openrgb") || t.Contains("signalrgb"))
            return "This runs your mouse, keyboard or lighting software. Custom buttons, DPI and RGB settings may not load until you open it.";
        return null;
    }

    async void OnSwitch(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not StartupVM vm) return;
        var tb = (ToggleButton)sender;
        var want = tb.IsChecked == true;
        // some startup items do real work: say what stops before switching them off
        var why = want ? null : StartupWarning(vm);
        if (why != null && Window.GetWindow(this) is Window w &&
            !Dialogs.Confirm(w, $"Switch off {vm.Name}?", why + "\n\nYou can switch it back on here any time.", "Switch off", "Keep it on"))
        {
            tb.SetCurrentValue(ToggleButton.IsCheckedProperty, vm.Enabled);
            return;
        }
        vm.NotBusy = false;
        var err = await Task.Run(() => StartupApps.SetEnabled(vm.E, want));
        vm.NotBusy = true;
        if (err != null) Log.Error($"Startup switch {vm.Name}: {err}");
        vm.Refresh();
        tb.SetCurrentValue(ToggleButton.IsCheckedProperty, vm.Enabled);   // the switch shows the real state (keeps its binding)
        UpdateChips();
    }
}
