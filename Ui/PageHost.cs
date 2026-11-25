using System.Windows;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>What the Apps and Debloat apps pages need from the main window.</summary>
public interface IPageHost
{
    Window Win { get; }
    Settings Cfg { get; }
    ProgressDock Dock { get; }
    bool Busy { get; set; }
    void SuggestRestart(string text);
}

/// <summary>True becomes Collapsed, false becomes Visible.</summary>
public sealed class InverseBoolToVisibility : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        throw new NotSupportedException();
}
