using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>Lists the Windows apps found on this PC, with a clear label on each, and removes the ones the user picks.</summary>
public partial class DebloatAppsPage : UserControl
{
    IPageHost? _host;
    readonly List<WinAppVM> _all = new();
    bool _working, _loaded, _building;
    DateTime _lastScan = DateTime.MinValue;

    public DebloatAppsPage() => InitializeComponent();

    public void Init(IPageHost host) => _host = host;

    /// <summary>Counts for the Home page and the sidebar.</summary>
    public int Found => _all.Count(a => !a.Removed);

    public void FocusSearch() => Search.Focus();

    public async Task LoadAsync(bool force = false)
    {
        if (_host == null || _working) return;
        if (!force && _loaded && (DateTime.Now - _lastScan).TotalSeconds < 60) { Refresh(); return; }
        if (!_loaded) Loading.Visibility = Visibility.Visible;
        try
        {
            if (_loaded) await Task.Run(Caches.RefreshAppx);   // the startup scan already covers the first visit
            var found = await Task.Run(() => WinAppCatalog.All.Where(d => Caches.AppxMatchesAny(d.Appx.Split('|'))).ToList());
            var picked = new HashSet<string>(_all.Where(a => a.Selected).Select(a => a.Def.Id));
            _building = true;
            _all.Clear();
            foreach (var d in found)
            {
                var vm = new WinAppVM(d) { Changed = OnChanged };
                if (picked.Contains(d.Id)) vm.Selected = true;
                _all.Add(vm);
            }
            _building = false;
            _loaded = true; _lastScan = DateTime.Now;
        }
        catch (Exception ex) { Log.Error("Windows app scan failed: " + ex.Message); _building = false; }
        Loading.Visibility = Visibility.Collapsed;
        Refresh();
        _ = LoadLogosAsync(_all.Where(a => a.Photo == null).ToList());
    }

    /// <summary>Reads each app's own icon from its package folder, in the background, and swaps it into the tile.</summary>
    static async Task LoadLogosAsync(List<WinAppVM> apps)
    {
        foreach (var a in apps)
        {
            // an app that is also on the Apps page uses the same brand logo there (Spotify, Teams, WhatsApp ...)
            var twin = AppCatalog.All.FirstOrDefault(d => SameApp(d.Name, a.Name));
            if (twin != null)
            {
                var art = new TileArt(twin.Id, twin.Name);
                if (art.Photo != null || art.HasIcon) { a.Art = art; continue; }
            }
            var img = await Task.Run<System.Windows.Media.ImageSource?>(() =>
            {
                try
                {
                    var file = AppxLogo.Find(Caches.AppxHome(a.Def.Appx.Split('|')));
                    if (file == null) return null;
                    using var fs = System.IO.File.OpenRead(file);   // read the bytes directly: works for files inside WindowsApps too
                    var bi = new System.Windows.Media.Imaging.BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = fs;
                    bi.DecodePixelWidth = 64;
                    bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    return (System.Windows.Media.ImageSource)bi;
                }
                catch { return null; }
            });
            if (img != null) a.Photo = img;
        }
    }

    static bool SameApp(string catalogName, string winName)
    {
        static string N(string s) => new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var c = N(catalogName); var w = N(winName);
        return c.Length > 3 && (c == w || w.StartsWith(c) || c.StartsWith(w) && w.Length > 5);
    }

    void OnChanged(WinAppVM vm) { if (!_building) UpdateStats(); }

    void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = Search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        Refresh();
    }

    void OnRowClick(object sender, MouseButtonEventArgs e)
    {
        if (_working) return;
        if (((FrameworkElement)sender).DataContext is WinAppVM vm && vm.CanSelect) vm.Selected = !vm.Selected;
    }

    void OnPickSafe(object sender, RoutedEventArgs e)
    {
        if (_working) return;
        _building = true;
        foreach (var a in _all) if (WinAppCatalog.IsSafePick(a.Def) && a.CanSelect) a.Selected = true;
        _building = false; UpdateStats();
    }

    void OnClear(object sender, RoutedEventArgs e)
    {
        if (_working) return;
        _building = true;
        foreach (var a in _all) a.Selected = false;
        _building = false; UpdateStats();
    }

    void Refresh()
    {
        var q = Search.Text.Trim();
        var tiles = _all.Where(a => !a.Removed && a.Matches(q))
            .OrderBy(a => a.Def.Tier == RemoveTier.Protected ? 1 : 0).ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
        Tiles.ItemsSource = tiles;
        Empty.Visibility = tiles.Count == 0 && _loaded ? Visibility.Visible : Visibility.Collapsed;
        Empty.Text = q.Length > 0 ? "Nothing matches that." : "None of the apps on this list are installed on this PC. Nothing to remove.";
        UpdateStats();
    }

    List<WinAppVM> Picked => _all.Where(a => a.Selected && !a.Removed).ToList();

    void UpdateStats()
    {
        var live = _all.Where(a => !a.Removed).ToList();
        StatFound.Text = _loaded ? $"{live.Count} found" : "Checking…";
        var p = Picked;
        PickBar.Visibility = p.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        PickText.Text = $"{p.Count} app{(p.Count == 1 ? "" : "s")} picked";
        var perm = p.Count(a => a.Def.Tier == RemoveTier.Perm);
        PickSub.Text = perm > 0 ? $"{perm} of them can't be reinstalled once removed."
            : string.Join(", ", p.Take(5).Select(a => a.Name)) + (p.Count > 5 ? $" and {p.Count - 5} more" : "");
        RemoveBtn.Content = $"Remove {p.Count}";
        RemoveBtn.IsEnabled = !_working && p.Count > 0;
    }

    async void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (_host == null || _working || _host.Busy) return;
        var queue = Picked;
        if (queue.Count == 0) return;

        var perm = queue.Where(a => a.Def.Tier == RemoveTier.Perm).Select(a => a.Name).ToList();
        var core = queue.Where(a => a.Def.Tier == RemoveTier.Core).Select(a => a.Name).ToList();
        var msg = $"{queue.Count} app{(queue.Count == 1 ? "" : "s")} will be removed from this PC.";
        if (perm.Count > 0) msg += $"\n\nThese can't be reinstalled, because Microsoft no longer offers them:\n•  {string.Join("\n•  ", perm)}";
        if (core.Count > 0) msg += $"\n\nThese are part of everyday Windows. You can get them back from the Microsoft Store:\n•  {string.Join("\n•  ", core)}";
        msg += "\n\nThe rest can be reinstalled from the Microsoft Store whenever you want.";
        if (!Dialogs.Confirm(_host.Win, "Remove these apps?", msg, "Remove", "Cancel")) return;

        _working = true; _host.Busy = true;
        foreach (var a in queue) { a.Busy = true; }
        UpdateStats();

        var dock = _host.Dock;
        dock.Begin("Removing", queue.Count == 1 ? $"Removing {queue[0].Name}" : $"Removing {queue.Count} apps",
            queue.Select(a => (a.Name, a.Def.Appx.Split('|')[0])).ToList(), true);

        int ok = 0, bad = 0;
        try
        {
            for (var i = 0; i < queue.Count; i++)
            {
                var a = queue[i];
                if (dock.CancelRequested) { a.Busy = false; continue; }
                dock.StartStep(i, a.Name);
                var removed = await Task.Run(() =>
                {
                    try
                    {
                        new AppxAction(a.Def.Appx.Split('|')).Apply();
                        return !Caches.AppxMatchesAny(a.Def.Appx.Split('|'));
                    }
                    catch (Exception ex) { Log.Error("Removing " + a.Name + " failed: " + ex.Message); return false; }
                });
                a.Busy = false;
                if (removed) { ok++; a.Removed = true; dock.EndStep(i, StepResult.Done); }
                else { bad++; dock.EndStep(i, StepResult.Failed); }
            }
        }
        finally { foreach (var a in queue) a.Busy = false; _working = false; _host.Busy = false; }

        var note = bad == 0 ? "Removed." : $"{ok} removed, {bad} couldn't be removed. Windows may be using them.";
        dock.Finish("", bad == 0 ? "Done" : "Finished with problems", note, ok, 0, bad, bad == 0);
        Refresh();
        if (ok > 0) _host.SuggestRestart("Restart recommended after removing Windows apps, so everything is cleaned up.");
    }

}
