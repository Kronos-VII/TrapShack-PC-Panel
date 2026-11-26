using System.Windows;
using System.Windows.Threading;
using TrapShack.Core;

namespace TrapShack;

public partial class App : Application
{
    static Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        // "--selftest" is only used by Test in Sandbox.bat and the GitHub build; a normal start never takes this path
        if (e.Args.Any(a => a.Equals("--selftest", StringComparison.OrdinalIgnoreCase)))
        {
            base.OnStartup(e);
            RunSelfTest(e.Args);
            return;
        }

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
        new MainWindow().Show();
    }

    /// <summary>Runs TrapShack.Ui.SelfTest. Refuses to run anywhere but Windows Sandbox or a GitHub build machine.</summary>
    void RunSelfTest(string[] args)
    {
        if (!TrapShack.Ui.SelfTest.Allowed)
        {
            MessageBox.Show("The self-test only runs inside Windows Sandbox, so it can never change a real PC.\n\n" +
                            "Use \"Test in Sandbox.bat\" in the project folder instead.", "TrapShack self-test",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var outAt = Array.FindIndex(args, a => a.Equals("--out", StringComparison.OrdinalIgnoreCase));
        var outDir = outAt >= 0 && outAt + 1 < args.Length ? args[outAt + 1] : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var report = System.IO.Path.Combine(outDir, "TrapShack self-test.txt");

        if (args.Any(a => a.Equals("--headless", StringComparison.OrdinalIgnoreCase)))
        {
            // GitHub build machine: no window, the exit code says whether anything failed
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Task.Run(() =>
            {
                int failures;
                try { failures = TrapShack.Ui.SelfTest.Run(outDir); }
                catch (Exception ex) { Log.Error("Self-test crashed: " + ex); failures = -1; }
                Dispatcher.Invoke(() => Shutdown(failures == 0 ? 0 : 1));
            });
            return;
        }

        var window = TrapShack.Ui.SelfTest.MakeWindow(out var progress, out var done);
        window.Show();
        Task.Run(() =>
        {
            int failures;
            try { failures = TrapShack.Ui.SelfTest.Run(outDir, progress); }
            catch (Exception ex) { Log.Error("Self-test crashed: " + ex); failures = -1; }
            done(failures, report);
        });
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
