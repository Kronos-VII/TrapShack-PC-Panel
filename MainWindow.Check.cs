using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrapShack.Ui;

namespace TrapShack;

/// <summary>
/// Used only by the page check (Test everything.bat): it opens every page the way a click would, and saves a picture
/// of each. Normal use never sets <see cref="CheckMode"/>.
/// </summary>
public partial class MainWindow
{
    /// <summary>True only during the page check: no welcome or "interrupted change" question at startup.</summary>
    public static bool CheckMode;

    /// <summary>Done once the window has finished its startup scan and shows Home.</summary>
    internal readonly TaskCompletionSource CheckReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Every page, in sidebar order, with the name used in the report and the picture's file name.</summary>
    internal static IEnumerable<(string Key, string Name)> CheckPages()
    {
        yield return ("home", "Home");
        yield return ("system", "System info");
        yield return ("temps", "Temperatures");
        yield return ("search", "Search settings");
        yield return ("debloat", "Debloat");
        yield return ("hub", "Optimizations");
        foreach (var key in PageDefs.AreaOrder) yield return (key, "Optimizations - " + PageDefs.Get(key).Title);
        yield return ("winapps", "Debloat apps");
        yield return ("apps", "Apps");
        yield return ("cleaner", "PC Cleaner");
        yield return ("utilities", "Utilities");
        yield return ("startup", "Startup apps");
        yield return ("restore", "Restore points");
        foreach (var s in SettingsSections) yield return ("settings:" + s.Key, "Settings - " + s.Key);
    }

    /// <summary>Opens a page the same way clicking it in the sidebar does.</summary>
    internal void CheckOpen(string key)
    {
        if (key.StartsWith("settings:"))
        {
            if (_navByKey["settings"].IsChecked != true) _navByKey["settings"].IsChecked = true;
            ShowSettingsSection(key["settings:".Length..]);
            return;
        }
        if (_navByKey.TryGetValue(key, out var rb))
        {
            if (rb.IsChecked == true) Navigate(key);   // already selected (Home at start)
            else rb.IsChecked = true;
        }
        else if (PageDefs.AreaOrder.Contains(key)) SelectNav(key);
        else Navigate(key);                            // Search has no sidebar button of its own
        if (key == "search") SearchAllBox.Text = "power";   // shows that search finds settings across every page
    }

    /// <summary>A picture of the whole window as it is now.</summary>
    internal void CheckPicture(string path)
    {
        var root = (FrameworkElement)Content;
        var dpi = VisualTreeHelper.GetDpi(this);
        var w = (int)Math.Ceiling(root.ActualWidth * dpi.DpiScaleX);
        var h = (int)Math.Ceiling(root.ActualHeight * dpi.DpiScaleY);
        if (w <= 0 || h <= 0) return;
        var bmp = new RenderTargetBitmap(w, h, 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
        bmp.Render(root);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bmp));
        using var f = System.IO.File.Create(path);
        png.Save(f);
    }

    /// <summary>Closes without the "still working" question (a background scan may still be running).</summary>
    internal void CheckClose()
    {
        _closeConfirmed = true;
        StopSensors();
        MonitorPanel.Shutdown();
        Close();
    }
}
