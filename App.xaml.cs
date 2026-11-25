using System.Windows;
using System.Windows.Threading;
using TrapShack.Core;

namespace TrapShack;

public partial class App : Application
{
    static Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Two copies changing the same settings at once could corrupt the saved originals.
        _singleInstance = new Mutex(true, @"Global\TrapShackTweakUtility", out var isFirst);
        if (!isFirst)
        {
            MessageBox.Show("TrapShack Tweak Utility is already running.", "TrapShack Tweak Utility",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        try { if (TrapShack.Ui.AccentTheme.TryParse(TrapShack.Core.Settings.Load().Accent, out var accent)) TrapShack.Ui.AccentTheme.Apply(accent); }
        catch (Exception ex) { TrapShack.Core.Log.Error("Could not apply saved accent: " + ex.Message); }

        DispatcherUnhandledException += OnUnhandled;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Error("Fatal: " + args.ExceptionObject);
        base.OnStartup(e);
    }

    static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("Unhandled: " + e.Exception);
        MessageBox.Show(
            "Something unexpected happened.\n\n" + e.Exception.Message +
            "\n\nDetails were saved to:\n" + Log.FilePath,
            "TrapShack", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
}
