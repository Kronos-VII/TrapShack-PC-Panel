using System.Windows;
using System.Windows.Controls;
using TrapShack.Core;

namespace TrapShack.Ui;

public sealed class RestoreVM : Notify
{
    public RestoreVM(RestorePointInfo p) { P = p; }
    public RestorePointInfo P { get; }
    public string Description => string.IsNullOrWhiteSpace(P.Description) ? "Restore point" : P.Description;
    public string Kind => P.Kind;
    public string When => P.Created == DateTime.MinValue ? "" : P.Created.ToLocalTime().ToString("d MMM yyyy, HH:mm");
    public string Ago
    {
        get
        {
            if (P.Created == DateTime.MinValue) return "";
            var d = DateTime.Now - P.Created.ToLocalTime();
            return d.TotalMinutes < 60 ? $"{Math.Max(1, (int)d.TotalMinutes)} min ago"
                 : d.TotalHours < 24 ? $"{(int)d.TotalHours} h ago"
                 : $"{(int)d.TotalDays} day{((int)d.TotalDays == 1 ? "" : "s")} ago";
        }
    }
    bool _busy;
    public bool NotBusy { get => !_busy; set { _busy = !value; Raise(); } }
}

/// <summary>The Restore points page: see, create and delete System Restore points.</summary>
public partial class RestorePage : UserControl
{
    IPageHost? _host;
    bool _working;

    public RestorePage() { InitializeComponent(); }
    public void Init(IPageHost host) => _host = host;

    public async Task LoadAsync()
    {
        var (on, points) = await Task.Run(() => (RestorePoints.IsEnabled(), RestorePoints.List()));
        Points.ItemsSource = points.Select(p => new RestoreVM(p)).ToList();
        CountText.Text = points.Count == 0 ? "" : $"{points.Count} saved";
        Empty.Visibility = points.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        Empty.Text = on ? "No restore points yet. Create one before making big changes."
                        : "System Restore is off, so Windows keeps no restore points. Turn it on to make one.";
        StatusTitle.Text = on ? "System Restore is on" : "System Restore is off";
        StatusLine.Text = on
            ? (points.Count > 0 ? $"Newest: {new RestoreVM(points[0]).When}. Windows also makes them before updates and driver installs." : "Windows also makes them before updates and driver installs.")
            : "Without it, Windows can't roll back a bad update, driver or change.";
        TurnOnBtn.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
        CreateBtn.IsEnabled = on && !_working;
    }

    async void OnCreate(object sender, RoutedEventArgs e)
    {
        if (_working || _host == null || _host.Busy) return;
        _working = true; _host.Busy = true; CreateBtn.IsEnabled = false;
        StatusTitle.Text = "Creating a restore point…";
        StatusLine.Text = "This usually takes under a minute.";
        var (ok, msg) = await Task.Run(() => RestorePoint.Create("TrapShack - made by hand " + DateTime.Now.ToString("d MMM HH:mm")));
        _working = false; _host.Busy = false;
        await LoadAsync();
        if (!ok) { StatusTitle.Text = "Windows didn't create it"; StatusLine.Text = msg; }
    }

    async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not RestoreVM vm || _host == null || _working) return;
        if (!Dialogs.Confirm(_host.Win, "Delete this restore point?",
                $"“{vm.Description}” from {vm.When} will be deleted. You won't be able to go back to it.", "Delete", "Cancel"))
            return;
        vm.NotBusy = false;
        var (ok, msg) = await Task.Run(() => RestorePoints.Delete(vm.P.Sequence));
        if (!ok) Dialogs.Info(_host.Win, "Couldn't delete it", msg);
        await LoadAsync();
    }

    async void OnTurnOn(object sender, RoutedEventArgs e)
    {
        if (_working || _host == null) return;
        _working = true; TurnOnBtn.IsEnabled = false;
        StatusTitle.Text = "Turning on System Restore…";
        var (ok, msg) = await Task.Run(() => RestorePoints.Enable());
        _working = false; TurnOnBtn.IsEnabled = true;
        await LoadAsync();
        if (!ok) { StatusTitle.Text = "Windows didn't turn it on"; StatusLine.Text = msg; }
    }

    void OnOpenRestore(object sender, RoutedEventArgs e) => CommandRunner.Open("rstrui.exe");
}
