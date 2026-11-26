using System.Windows;
using System.Windows.Input;

namespace TrapShack.Ui;

/// <summary>Themed replacement for MessageBox.</summary>
public partial class DialogWindow : Window
{
    public DialogWindow(string title, string message, string primary, string? secondary)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        PrimaryButton.Content = primary;

        if (secondary == null) SecondaryButton.Visibility = Visibility.Collapsed;
        else SecondaryButton.Content = secondary;
    }

    void OnDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    void OnPrimary(object sender, RoutedEventArgs e) => DialogResult = true;
    void OnSecondary(object sender, RoutedEventArgs e) => DialogResult = false;
}

public static class Dialogs
{
    /// <summary>
    /// Only set by the page check: instead of showing a question, it is noted down and answered "no" (Cancel), so
    /// nothing is ever started by a question the check can't answer.
    /// </summary>
    public static Action<string, string>? Intercept;

    public static bool Confirm(Window owner, string title, string message, string ok = "Continue", string cancel = "Cancel")
    {
        if (Intercept != null) { Intercept(title, message); return false; }
        var d = new DialogWindow(title, message, ok, cancel) { Owner = owner };
        return d.ShowDialog() == true;
    }

    public static void Info(Window owner, string title, string message)
    {
        if (Intercept != null) { Intercept(title, message); return; }
        var d = new DialogWindow(title, message, "OK", null) { Owner = owner };
        d.ShowDialog();
    }
}
