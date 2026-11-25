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
    public static bool Confirm(Window owner, string title, string message, string ok = "Continue", string cancel = "Cancel")
    {
        var d = new DialogWindow(title, message, ok, cancel) { Owner = owner };
        return d.ShowDialog() == true;
    }

    public static void Info(Window owner, string title, string message)
    {
        var d = new DialogWindow(title, message, "OK", null) { Owner = owner };
        d.ShowDialog();
    }
}
