using System.Windows;
using System.Windows.Threading;
using TrapShack.Core;

namespace TrapShack;

public partial class App : Application
{
    static Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        // "--pcscan" is only used by Test everything.bat. It only reads; it never changes anything on the PC.
        if (e.Args.Any(a => a.Equals("--pcscan", StringComparison.OrdinalIgnoreCase)))
        {
            base.OnStartup(e);
            RunPcScan(e.Args);
            return;
        }

        // "--selftest" is only used by Test everything.bat (inside Windows Sandbox) and the GitHub build; a normal start never takes this path
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

    static string OutDir(string[] args)
    {
        var at = Array.FindIndex(args, a => a.Equals("--out", StringComparison.OrdinalIgnoreCase));
        return at >= 0 && at + 1 < args.Length ? args[at + 1] : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    /// <summary>
    /// The scan on your own PC (Test everything.bat). It only reads: every setting's state, the hardware, the sensors,
    /// what the cleaner would find, and every page opened once. Every value the app can change is read before and after,
    /// and the report shows they're identical. Nothing is switched, removed, cleaned or installed.
    /// </summary>
    async void RunPcScan(string[] args)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _singleInstance = new Mutex(true, @"Global\TrapShackTweakUtility", out var isFirst);
        if (!isFirst)
        {
            MessageBox.Show("Close TrapShack Tweak Utility first, then run Test everything.bat again.", "TrapShack PC scan", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        DispatcherUnhandledException += (_, ev) => { Log.Error("PC scan: " + ev.Exception); ev.Handled = true; };

        var outDir = OutDir(args);
        System.IO.Directory.CreateDirectory(outDir);
        var window = TrapShack.Ui.SelfTest.MakeWindow(out var progress, out var done, heading: "TrapShack PC scan",
            runningNote: "This only reads. Nothing on your PC is switched on or off, removed, cleaned or installed. The app's window opens by itself to check every page, then closes. Please leave the PC alone for a few minutes.",
            doneNote: "Finished. This window closes by itself.");
        window.Show();
        var started = DateTime.Now;
        var scan = new TrapShack.Ui.FullTest.PcScan(outDir);
        int failures;
        try
        {
            await Task.Run(() => scan.ReadEverything(progress));
            var pages = await TrapShack.Ui.FullTest.RunPageCheck(System.IO.Path.Combine(outDir, "Pages"), progress);
            await Task.Run(() => scan.Finish(pages, progress));
            failures = scan.Failures;
        }
        catch (Exception ex) { Log.Error("PC scan crashed: " + ex); failures = -1; }
        var summary = TrapShack.Ui.FullTest.WriteSummary(outDir, "this PC", started, new() { ("PC scan (reads only)", "TrapShack PC scan.txt", failures) });
        done(failures, summary);
        await Task.Delay(2500);
        Shutdown();
    }

    /// <summary>Runs TrapShack.Ui.SelfTest. Refuses to run anywhere but Windows Sandbox or a GitHub build machine.</summary>
    void RunSelfTest(string[] args)
    {
        if (!TrapShack.Ui.SelfTest.Allowed)
        {
            MessageBox.Show("The self-test only runs inside Windows Sandbox, so it can never change a real PC.\n\n" +
                            "Use \"Test everything.bat\" in the project folder instead.", "TrapShack self-test",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var outDir = OutDir(args);
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

        if (args.Any(a => a.Equals("--full", StringComparison.OrdinalIgnoreCase)))
        {
            RunFullSandbox(outDir);
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

    /// <summary>
    /// Everything, inside Windows Sandbox: every page, every setting on and off, then every feature, utility, one-time
    /// action and removal for real. The summary is written last; Test everything.bat waits for it.
    /// </summary>
    async void RunFullSandbox(string outDir)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += (_, ev) => { Log.Error("Sandbox test: " + ev.Exception); ev.Handled = true; };
        System.IO.Directory.CreateDirectory(outDir);
        var window = TrapShack.Ui.SelfTest.MakeWindow(out var progress, out var done, heading: "TrapShack full test",
            runningNote: "Part 1: every page.  Part 2: every setting on, checked, off, checked.  Part 3: every feature, utility, one-time action and app removal, for real. " +
                         "This all happens inside the Sandbox, which is thrown away when it closes. It takes a while; you can leave it.",
            doneNote: "Finished. The results are already on your PC; the Sandbox can be closed.");
        window.Show();
        var started = DateTime.Now;
        var parts = new List<(string, string, int)>();

        int pageFails;
        try
        {
            var pages = await TrapShack.Ui.FullTest.RunPageCheck(System.IO.Path.Combine(outDir, "Pages"), progress);
            System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "TrapShack page check.txt"),
                string.Join("\n", pages.Select(r => $"[{r.Outcome}] {r.Name}" + (r.Detail != null ? "\n      " + r.Detail.Replace("\n", "\n      ") : ""))));
            pageFails = pages.Count(r => r.Outcome == "FAIL");
        }
        catch (Exception ex) { Log.Error("Page check crashed: " + ex); pageFails = -1; }
        parts.Add(("Part 1: every page", "TrapShack page check.txt + Pages folder", pageFails));

        int settingFails;
        try { settingFails = await Task.Run(() => TrapShack.Ui.SelfTest.Run(outDir, progress)); }
        catch (Exception ex) { Log.Error("Self-test crashed: " + ex); settingFails = -1; }
        parts.Add(("Part 2: every setting on and off", "TrapShack self-test.txt", settingFails));

        int extraFails;
        try { extraFails = await Task.Run(() => TrapShack.Ui.FullTest.RunSandboxExtras(outDir, progress)); }
        catch (Exception ex) { Log.Error("Features test crashed: " + ex); extraFails = -1; }
        parts.Add(("Part 3: features, utilities, one-time actions, removals", "TrapShack features and one-time actions.txt", extraFails));

        var summary = TrapShack.Ui.FullTest.WriteSummary(outDir, "Windows Sandbox", started, parts);
        var total = parts.Any(p => p.Item3 < 0) ? -1 : parts.Sum(p => p.Item3);
        done(total, summary);
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
