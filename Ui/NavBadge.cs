using System.Windows;

namespace TrapShack.Ui;

/// <summary>Lets a sidebar button carry a small count that its template shows on the right.</summary>
public static class NavBadge
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached("Text", typeof(string), typeof(NavBadge), new PropertyMetadata(""));

    public static string GetText(DependencyObject o) => (string)o.GetValue(TextProperty);
    public static void SetText(DependencyObject o, string value) => o.SetValue(TextProperty, value);
}
