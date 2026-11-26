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

    async void OnSwitch(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not StartupVM vm) return;
        var tb = (ToggleButton)sender;
        var want = tb.IsChecked == true;
        vm.NotBusy = false;
        var err = await Task.Run(() => StartupApps.SetEnabled(vm.E, want));
        vm.NotBusy = true;
        if (err != null) Log.Error($"Startup switch {vm.Name}: {err}");
        vm.Refresh();
        tb.SetCurrentValue(ToggleButton.IsCheckedProperty, vm.Enabled);   // the switch shows the real state (keeps its binding)
        UpdateChips();
    }
}
