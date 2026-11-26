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
    public void Refresh() { Raise(nameof(Enabled)); Raise(nameof(Impact)); Raise(nameof(ImpactLevel)); }

    public string Impact => E.DelayMs <= 0 ? (E.Enabled ? "No startup delay recorded" : "Off: doesn't start with Windows")
        : $"Slowed startup by {(E.DelayMs >= 1000 ? $"{E.DelayMs / 1000.0:0.#} s" : $"{E.DelayMs} ms")}";
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
            _bootText = boot is TimeSpan t ? $"Last startup: {t.TotalSeconds:0.#} s" : "";
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

    void Refresh()
    {
        var q = Search.Text.Trim();
        var shown = _all.Where(v => q.Length == 0 || v.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || v.Sub.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        Tiles.ItemsSource = shown;
        Empty.Visibility = shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        Empty.Text = q.Length > 0 ? "Nothing matches that." : "Nothing starts with Windows on this PC.";
        UpdateChips();
    }

    void UpdateChips()
    {
        var on = _all.Count(v => v.E.Enabled);
        var chips = new List<string> { $"{on} on", $"{_all.Count - on} off" };
        if (_bootText.Length > 0) chips.Add(_bootText);
        Chips.ItemsSource = chips;
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
