using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using TrapShack.Core;
using TrapShack.Tweaks;

namespace TrapShack.Ui;

/// <summary>
/// The parts of "Test everything.bat" that go beyond switching settings on and off:
/// <list type="bullet">
/// <item>the page check: opens every page and saves a picture of it (in the Sandbox and on this PC);</item>
/// <item>in Windows Sandbox only: every feature, every utility, and the one-time actions and removals, run for real
///       on the throwaway copy of Windows;</item>
/// <item>on this PC: a scan that only reads. It reads every value the app can change before and after, and the report
///       shows they are identical.</item>
/// </list>
/// Normal use of the app never reaches this code.
/// </summary>
public static class FullTest
{
    /// <summary>One line in a report. Outcome: PASS, FAIL, SKIP, N/A or INFO.</summary>
    public sealed record Row(string Part, string Name, string Outcome, string? Detail = null);

    // ============================================================================================ reports
    static string Report(string title, string where, DateTime started, bool finished, IEnumerable<string> notes, List<Row> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine($"Started {started:yyyy-MM-dd HH:mm}, {(finished ? $"finished after {(DateTime.Now - started).TotalMinutes:0} min" : "STILL RUNNING (or stopped part-way)")}");
        sb.AppendLine("Where: " + where);
        sb.AppendLine();
        var fail = rows.Count(r => r.Outcome == "FAIL");
        sb.AppendLine($"PASSED {rows.Count(r => r.Outcome == "PASS")}   FAILED {fail}   NOT HERE {rows.Count(r => r.Outcome == "N/A")}   SKIPPED {rows.Count(r => r.Outcome == "SKIP")}");
        sb.AppendLine(fail == 0 ? "Nothing failed." : "Send this file to get the failures fixed.");
        var noteList = notes.ToList();
        if (noteList.Count > 0)
        {
            sb.AppendLine().AppendLine("==== NOTES");
            foreach (var n in noteList) sb.AppendLine("  " + n.Replace("\n", "\n  "));
        }
        void Line(Row r) => sb.AppendLine($"  [{r.Outcome}] {r.Name}" + (r.Detail != null ? "\n        " + r.Detail.Replace("\n", "\n        ") : ""));
        if (fail > 0)
        {
            sb.AppendLine().AppendLine($"==== FAILED ({fail})");
            foreach (var r in rows.Where(r => r.Outcome == "FAIL")) Line(r);
        }
        foreach (var part in rows.Select(r => r.Part).Distinct())
        {
            var list = rows.Where(r => r.Part == part).ToList();
            sb.AppendLine().AppendLine($"==== {part} ({list.Count})");
            foreach (var r in list) Line(r);
        }
        return sb.ToString();
    }

    /// <summary>Runs <paramref name="work"/> but stops waiting after <paramref name="limit"/>.</summary>
    static (bool Finished, T? Value, Exception? Error) Within<T>(TimeSpan limit, Func<T> work)
    {
        var task = Task.Run(work);
        try { return task.Wait(limit) ? (true, task.Result, null) : (false, default, null); }
        catch (AggregateException ae) { return (true, default, ae.InnerException ?? ae); }
    }

    static string Took(TimeSpan t) => t.TotalMinutes >= 1 ? $"{(int)t.TotalMinutes} min {t.Seconds} s" : $"{t.TotalSeconds:0.0} s";

    // ============================================================================================ page check
    /// <summary>
    /// Opens the app's main window, visits every page the way a click would, and saves a picture of each one.
    /// Questions the app would ask are noted and answered "no", so nothing is started. Must be called on the UI thread.
    /// </summary>
    public static async Task<List<Row>> RunPageCheck(string picturesDir, Action<int, int, string>? progress)
    {
        const string part = "Pages";
        var rows = new List<Row>();
        Directory.CreateDirectory(picturesDir);
        var pages = MainWindow.CheckPages().ToList();

        var crashes = new List<string>();
        var errors = new List<string>();
        var questions = new List<string>();
        void OnCrash(object s, DispatcherUnhandledExceptionEventArgs e) { crashes.Add(e.Exception.GetType().Name + ": " + e.Exception.Message); e.Handled = true; }
        void OnLog(string line) { if (line.StartsWith("! ")) lock (errors) errors.Add(line[2..]); }
        Application.Current.DispatcherUnhandledException += OnCrash;
        Log.Console += OnLog;
        Dialogs.Intercept = (title, message) => questions.Add(title);
        MainWindow.CheckMode = true;

        MainWindow? w = null;
        try
        {
            progress?.Invoke(0, pages.Count + 1, "Starting the app");
            var clock = System.Diagnostics.Stopwatch.StartNew();
            w = new MainWindow { WindowStartupLocation = WindowStartupLocation.CenterScreen, Width = 1440, Height = 900 };
            w.Show();
            var ready = await Task.WhenAny(w.CheckReady.Task, Task.Delay(TimeSpan.FromMinutes(4)));
            if (ready != w.CheckReady.Task)
            {
                rows.Add(new(part, "Start-up", "FAIL", "the app didn't finish starting within 4 minutes" + Extra()));
                return rows;
            }
            rows.Add(new(part, "Start-up", crashes.Count == 0 ? "PASS" : "FAIL", $"ready in {Took(clock.Elapsed)}" + Extra()));

            for (var i = 0; i < pages.Count; i++)
            {
                var (key, name) = pages[i];
                progress?.Invoke(i + 1, pages.Count + 1, "Opening " + name);
                crashes.Clear(); lock (errors) errors.Clear(); questions.Clear();
                var file = $"{i + 1:00} {name.Replace(" - ", " ")}.png";
                try
                {
                    w.CheckOpen(key);
                    // let the page load what it shows (installed apps, sensors, cleaner sizes ...)
                    await Task.Delay(key is "temps" or "system" ? 6000 : key is "apps" or "cleaner" or "winapps" ? 4500 : 2500);
                    await w.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    w.CheckPicture(Path.Combine(picturesDir, file));
                    rows.Add(new(part, name, crashes.Count == 0 ? "PASS" : "FAIL", "picture: " + file + Extra()));
                }
                catch (Exception ex)
                {
                    rows.Add(new(part, name, "FAIL", "opening it crashed: " + ex.Message + Extra()));
                }
            }
        }
        catch (Exception ex)
        {
            rows.Add(new(part, "Page check", "FAIL", "the check itself stopped: " + ex.Message));
        }
        finally
        {
            try { w?.CheckClose(); } catch { }
            Application.Current.DispatcherUnhandledException -= OnCrash;
            Log.Console -= OnLog;
            Dialogs.Intercept = null;
            MainWindow.CheckMode = false;
        }
        return rows;

        string Extra()
        {
            var sb = new StringBuilder();
            foreach (var c in crashes) sb.Append("\nCRASH: " + c);
            lock (errors) foreach (var e in errors.Distinct().Take(8)) sb.Append("\nthe app logged: " + e);
            foreach (var q in questions.Distinct()) sb.Append($"\nthe app asked \"{q}\" (answered no)");
            return sb.ToString();
        }
    }

    // ============================================================================================ Sandbox: features, utilities, one-time actions
    /// <summary>
    /// Runs, for real, everything that can't be switched back: every feature, every utility, the one-time actions and
    /// the app removals. Only inside Windows Sandbox (the caller checks <see cref="SelfTest.Allowed"/>).
    /// </summary>
    public static int RunSandboxExtras(string outDir, Action<int, int, string>? progress = null)
    {
        var path = Path.Combine(outDir, "TrapShack features and one-time actions.txt");
        var rows = new List<Row>();
        var started = DateTime.Now;
        var notes = new List<string>
        {
            "Everything here was run for real on the Sandbox's throwaway copy of Windows, which is wiped when it closes.",
            "Utilities are run in the order a person would; the network and Windows Update resets run last.",
        };
        void Save(bool finished) => File.WriteAllText(path, Report("TrapShack Tweak Utility - features, utilities and one-time actions", "Windows Sandbox", started, finished, notes, rows), Encoding.UTF8);
        void Add(Row r) { rows.Add(r); Save(false); }

        progress?.Invoke(0, 1, "Reading this PC");
        var sys = SystemInfo.Detect();
        try { sys.ProbeSlow(); } catch { }
        Caches.Refresh();
        var tweaks = Catalog.Build();
        var engine = new TweakEngine();
        Tweak? T(string id) => tweaks.FirstOrDefault(t => t.Id == id);

        // every step, in order, so the progress bar knows the total
        var steps = new List<(string Name, Action Run)>();
        void Step(string name, Action run) => steps.Add((name, run));

        // ---------------------------------------------------------------- features
        const string F = "Features";
        Step("System info", () =>
        {
            var cards = HardwareInfo.Collect(sys);
            Add(cards.Count >= 4 ? new(F, "System info cards", "PASS", $"{cards.Count} cards: " + string.Join(", ", cards.Select(c => c.Title)))
                                 : new(F, "System info cards", "FAIL", $"only {cards.Count} cards were read"));
        });
        Step("Temperature sensors", () =>
        {
            using var s = new Sensors();
            s.Start();
            Thread.Sleep(1500);
            var r = s.Read();
            Add(r.Count > 0 ? new(F, "Temperature sensors", "PASS", string.Join(", ", r.Select(x => x.Label + " " + x.Value)))
                            : new(F, "Temperature sensors", "INFO", "no readings: the Sandbox has no real sensors" + (s.Problem != null ? " (" + s.Problem + ")" : "") + ". Checked on your PC by the PC scan."));
        });
        Step("Startup apps", () =>
        {
            const string run = @"Software\Microsoft\Windows\CurrentVersion\Run";
            const string name = "TrapShack test entry";
            var list = StartupApps.Read();
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(run)) k.SetValue(name, "\"" + Path.Combine(Environment.SystemDirectory, "notepad.exe") + "\"");
                StartupEntry Find() => StartupApps.Read().First(e => e.ApprovedName == name);
                var err = StartupApps.SetEnabled(Find(), false);
                var off = Find().Enabled;
                var err2 = StartupApps.SetEnabled(Find(), true);
                var on = Find().Enabled;
                Add(err == null && err2 == null && !off && on
                    ? new(F, "Startup apps: list and switch off/on", "PASS", $"{list.Count} startup programs read; a test entry was switched off and on again and read back correctly")
                    : new(F, "Startup apps: list and switch off/on", "FAIL", $"off: {(off ? "still on" : "ok")} {err}; on: {(on ? "ok" : "still off")} {err2}"));
            }
            finally
            {
                try { using var k = Registry.CurrentUser.OpenSubKey(run, true); k?.DeleteValue(name, false); } catch { }
                try { using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", true); k?.DeleteValue(name, false); } catch { }
            }
        });
        Step("Restore points", () =>
        {
            var list = RestorePoints.List();
            Add(new(F, "Restore points: read the list", "INFO",
                $"System Restore is {(RestorePoints.IsEnabled() ? "on" : "off")} here, {list.Count} restore points (the Sandbox has no System Restore). On your PC, making one worked in the earlier PC check, and the PC scan reads the list."));
        });
        Step("Installed-app detection", () =>
        {
            var found = WingetService.DetectInstalled();
            Add(new(F, "Apps page: installed-app detection", "PASS", $"{found.Count} of {AppCatalog.All.Length} apps detected as installed" +
                (WingetService.Available ? "" : ". The Sandbox has no winget, so installs can't be tried here; on your PC the PC scan checks winget is found.")));
        });
        Step("Debloat apps list", () =>
        {
            var present = WinAppCatalog.All.Where(d => Caches.AppxMatchesAny(d.Appx.Split('|'))).ToList();
            Add(new(F, "Debloat apps: list of apps found", "PASS", $"{present.Count} of {WinAppCatalog.All.Length} listed apps are in the Sandbox"));
        });
        Step("Profile file", () =>
        {
            var on = tweaks.Where(t => !t.OneShot).Take(25).Select(t => t.Id).ToList();
            var json = JsonSerializer.Serialize(new MainWindow.ProfileFile { App = "TrapShack Tweak Utility", Made = "test", PcName = "test", On = on });
            var back = JsonSerializer.Deserialize<MainWindow.ProfileFile>(json);
            Add(back != null && back.On.SequenceEqual(on) ? new(F, "Profiles: save and load", "PASS", "a profile was written and read back identically")
                                                          : new(F, "Profiles: save and load", "FAIL", "the profile read back differently"));
        });
        Step("Update check", () =>
        {
            var r = Within(TimeSpan.FromSeconds(90), () => Updater.CheckAsync(MainWindow.UpdateRepo).GetAwaiter().GetResult());
            Add(!r.Finished ? new(F, "Settings: check for updates", "FAIL", "GitHub didn't answer within 90 seconds")
                : r.Error != null ? UpdateRow(F, r.Error)
                : new(F, "Settings: check for updates", "PASS", $"running {Updater.CurrentText}; " + (r.Value == null ? "GitHub says this is the newest version" : "GitHub has a newer release")));
        });
        Step("O&O ShutUp10++", () =>
        {
            var r = Within(TimeSpan.FromMinutes(5), PrivacyTool.TestWithoutOpening);
            Add(!r.Finished ? new(F, "O&O ShutUp10++: download and signature check", "FAIL", "took longer than 5 minutes")
                : r.Error != null ? new(F, "O&O ShutUp10++: download and signature check", "FAIL", r.Error.Message)
                : new(F, "O&O ShutUp10++: download and signature check", "PASS", r.Value + " (the tool itself isn't opened: it's a window someone has to use)"));
        });
        List<CleanScan> scans = new();
        Step("PC Cleaner scan", () =>
        {
            var rules = CleanerEngine.LoadBundledRules();
            var det = new CleanerEngine.Detector();
            var bad = new List<string>();
            foreach (var r in rules.Where(det.IsInstalled))
            {
                try { scans.Add(CleanerEngine.Scan(r, CancellationToken.None)); }
                catch (Exception ex) { bad.Add(r.Name + ": " + ex.Message); }
            }
            // ask the PC Cleaner page's own tick logic, exactly what a person sees
            var shader = rules.Count(r => r.ShaderCache);
            var shaderOn = rules.Count(r => r.ShaderCache && new CleanRuleVM(r).Checked);
            Add(new(F, "PC Cleaner: scan", bad.Count == 0 ? "PASS" : "FAIL",
                $"{rules.Count} rules loaded, {scans.Count} apply here, {scans.Count(s => s.Count > 0)} found something, {CleanScan.Format(scans.Sum(s => s.Bytes))} in all" +
                (bad.Count > 0 ? "\n" + string.Join("\n", bad.Take(10)) : "")));
            Add(new(F, "PC Cleaner: shader caches start unticked", shaderOn == 0 ? "PASS" : "FAIL", $"{shader} shader-cache rules, {shaderOn} ticked by default"));
        });
        Step("PC Cleaner clean", () =>
        {
            long bytes = 0; var files = 0; var bad = new List<string>();
            foreach (var s in scans.Where(x => x.Count > 0))
            {
                try { var (c, b) = CleanerEngine.Clean(s, CancellationToken.None); files += c; bytes += b; }
                catch (Exception ex) { bad.Add(s.Rule.Name + ": " + ex.Message); }
            }
            var left = scans.Select(s => s.Rule).Sum(r => { try { return CleanerEngine.Scan(r, CancellationToken.None).Bytes; } catch { return 0L; } });
            Add(new(F, "PC Cleaner: clean everything (shader caches included)", bad.Count == 0 ? "PASS" : "FAIL",
                $"deleted {files} items ({CleanScan.Format(bytes)}); {CleanScan.Format(left)} left (files Windows is using)" +
                (bad.Count > 0 ? "\n" + string.Join("\n", bad.Take(10)) : "")));
        });
        Step("Shortcuts on the Utilities page", () =>
        {
            var targets = UtilityCatalog.Tools.Where(t => t.Open != null).Select(t => (t.Title, Target: t.Open!))
                .Concat(UtilityCatalog.WindowsTools.Select(t => (Title: t.Name, t.Target))).ToList();
            var missing = targets.Where(t => !OpenTargetExists(t.Target)).Select(t => $"{t.Title} ({t.Target})").ToList();
            Add(new(F, "Utilities: shortcuts to Windows tools", missing.Count == 0 ? "PASS" : "FAIL",
                missing.Count == 0 ? $"all {targets.Count} shortcuts point at something that exists" : "not found here: " + string.Join(", ", missing)));
        });

        // ---------------------------------------------------------------- utilities
        const string U = "Utilities (run in the Sandbox)";
        var longRepairs = new HashSet<string> { "repair-system", "sfc", "component-cleanup" };
        var last = new[] { "wu-reset", "net-reset" };   // these reset the network or Windows Update, so they go at the very end
        var utilities = UtilityCatalog.Tools.Where(t => t.Steps.Length > 0 && !last.Contains(t.Id)).ToList();
        foreach (var tool in utilities)
            Step(tool.Title, () => Add(RunUtility(U, tool, sys, longRepairs)));

        // ---------------------------------------------------------------- one-time actions and removals
        const string O = "One-time actions and removals";
        if (T("extra.disk-cleanup") is { } dc)
            Step(dc.Title, () =>
            {
                var missing = new[] { "cleanmgr.exe", "dism.exe" }.Where(f => FindProgram(f) == null).ToList();
                Add(new(O, dc.Title, missing.Count == 0 ? "SKIP" : "FAIL", missing.Count == 0
                    ? "not run here: after Disk Cleanup it runs DISM /StartComponentCleanup /ResetBase, the same 10 to 30 minute Windows repair as \"Clean up old update files\", which the Sandbox's Windows can't do (the last run gave up after 20 minutes). Both programs are present."
                    : "program not found: " + string.Join(", ", missing)));
            });
        foreach (var id in new[] { "extra.delete-temp" })
        {
            var t = T(id);
            if (t == null) continue;
            Step(t.Title, () =>
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var r = Within(TimeSpan.FromMinutes(20), () => { engine.Apply(t); return true; });
                Add(!r.Finished ? new(O, t.Title, "FAIL", "still running after 20 minutes")
                    : r.Error != null ? new(O, t.Title, "FAIL", r.Error.Message)
                    : new(O, t.Title, "PASS", "ran and finished in " + Took(clock.Elapsed)));
            });
        }
        if (T("extra.restore-point") is { } rp)
            Step(rp.Title, () => Add(new(O, rp.Title, "SKIP", "the Sandbox has no System Restore. On your PC, making a restore point worked in the earlier PC check (log: \"Restore point created\").")));
        if (T("clean.oosu") is { } oosu)
            Step(oosu.Title, () => Add(new(O, oosu.Title, "SKIP", "covered under Features: downloaded, signature checked, policy backup made. The tool's own window isn't opened.")));
        if (T("clean.bitlocker") is { } bl)
            Step(bl.Title, () =>
            {
                var r = Within(TimeSpan.FromMinutes(2), () => engine.IsAvailable(bl) && engine.IsApplied(bl));
                Add(new(O, bl.Title, r.Finished && r.Error == null ? "SKIP" : "FAIL",
                    r.Finished && r.Error == null ? "never run in a test (it decrypts the drive). Reading BitLocker's state worked." : "reading BitLocker's state failed: " + (r.Error?.Message ?? "took over 2 minutes")));
            });
        foreach (var id in new[] { "ai.recall-feature", "clean.reserved-storage" })
        {
            var t = T(id);
            if (t == null) continue;
            Step(t.Title, () =>
            {
                var r = Within(TimeSpan.FromMinutes(20), () => SelfTest.TestOne(engine, sys, t, includeLeftOut: true));
                Add(!r.Finished ? new(O, t.Title, "FAIL", "still running after 20 minutes")
                    : r.Error != null ? new(O, t.Title, "FAIL", r.Error.Message)
                    : new(O, t.Title, r.Value!.Outcome, "switched on and back off with DISM: " + (r.Value.Detail ?? (r.Value.Outcome == "PASS" ? "everything came back exactly as it was" : ""))));
            });
        }
        foreach (var t in tweaks.Where(x => !x.OneShot && (x.StoreRestore || x.Actions.OfType<AppxAction>().Any())))
            Step(t.Title, () => Add(RemoveOnce(O, engine, t, TimeSpan.FromMinutes(10))));
        Step("Debloat apps: remove every app found", () =>
        {
            Caches.RefreshAppx();
            var present = WinAppCatalog.All.Where(d => Caches.AppxMatchesAny(d.Appx.Split('|'))).ToList();
            if (present.Count == 0) { Add(new(O, "Debloat apps: remove", "N/A", "none of the listed apps are in the Sandbox")); return; }
            foreach (var d in present)
            {
                if (d.Tier == RemoveTier.Protected) { Add(new(O, "Debloat apps: " + d.Name, "SKIP", "locked in the app (never removed)")); continue; }
                var r = Within(TimeSpan.FromMinutes(5), () => { new AppxAction(d.Appx.Split('|')).Apply(); Caches.RefreshAppx(); return !Caches.AppxMatchesAny(d.Appx.Split('|')); });
                Add(!r.Finished ? new(O, "Debloat apps: remove " + d.Name, "FAIL", "still running after 5 minutes")
                    : r.Error != null ? new(O, "Debloat apps: remove " + d.Name, "FAIL", r.Error.Message)
                    : new(O, "Debloat apps: remove " + d.Name, r.Value ? "PASS" : "FAIL", r.Value ? "removed for all users" : "still installed afterwards"));
            }
        });
        foreach (var id in new[] { "debloat.onedrive", "browsers.edge-remove" })
            if (T(id) is { } t) Step(t.Title, () => Add(RemoveOnce(O, engine, t, TimeSpan.FromMinutes(20))));

        foreach (var tool in UtilityCatalog.Tools.Where(t => last.Contains(t.Id)))
            Step(tool.Title, () => Add(RunUtility(U, tool, sys, longRepairs)));

        // ---------------------------------------------------------------- run them
        for (var i = 0; i < steps.Count; i++)
        {
            progress?.Invoke(i, steps.Count, steps[i].Name);
            try { steps[i].Run(); }
            catch (Exception ex) { Add(new("Test problems", steps[i].Name, "FAIL", "the test step crashed: " + ex.Message)); }
        }
        Save(true);
        progress?.Invoke(steps.Count, steps.Count, "");
        return rows.Count(r => r.Outcome == "FAIL");
    }

    /// <summary>
    /// GitHub answering "no release yet" is the right answer while nothing has been published with release.bat, so
    /// it counts as a pass: the check reached GitHub and read it correctly.
    /// </summary>
    static Row UpdateRow(string part, Exception error) =>
        error.Message.StartsWith("No release has been published", StringComparison.Ordinal)
            ? new(part, "Settings: check for updates", "PASS", $"running {Updater.CurrentText}; GitHub answered: no release published yet (publish one with release.bat)")
            : new(part, "Settings: check for updates", "FAIL", error.Message);

    /// <summary>Removes an app (or runs an uninstall) once and checks Windows no longer has it.</summary>
    static Row RemoveOnce(string part, TweakEngine engine, Tweak t, TimeSpan limit)
    {
        try
        {
            Caches.RefreshAppx();
            if (!engine.IsAvailable(t)) return new(part, t.Title, "N/A", "not in the Sandbox");
            if (engine.IsApplied(t)) return new(part, t.Title, "N/A", "not in the Sandbox (nothing to remove)");
        }
        catch (Exception ex) { return new(part, t.Title, "FAIL", "checking it crashed: " + ex.Message); }
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var r = Within(limit, () => { engine.Apply(t); Caches.RefreshAppx(); return engine.IsApplied(t); });
        return !r.Finished ? new(part, t.Title, "FAIL", $"still running after {limit.TotalMinutes:0} minutes")
             : r.Error != null ? new(part, t.Title, "FAIL", r.Error.Message)
             : new(part, t.Title, r.Value ? "PASS" : "FAIL", r.Value ? "removed in " + Took(clock.Elapsed) : "Windows still reports it as there afterwards");
    }

    /// <summary>Runs one utility's steps exactly like the Utilities page does, and checks each step's result.</summary>
    static Row RunUtility(string part, UtilTool tool, SystemInfo sys, HashSet<string> longRepairs)
    {
        if (longRepairs.Contains(tool.Id))
        {
            var missing = tool.Steps.Where(s => FindProgram(s.File) == null).Select(s => s.File).ToList();
            return new(part, tool.Title, missing.Count == 0 ? "SKIP" : "FAIL", missing.Count == 0
                ? "not run: Windows' own repair (" + string.Join(", ", tool.Steps.Select(s => Path.GetFileNameWithoutExtension(s.File) + " " + s.Args)) + ") takes 10 to 30 minutes and the Sandbox's Windows can't be repaired. The programs it uses are present."
                : "program not found: " + string.Join(", ", missing));
        }
        if (tool.LaptopOnly && !sys.IsLaptop) return new(part, tool.Title, "N/A", "laptops only (the Sandbox has no battery)");
        var notHere = tool.Steps.Where(s => FindProgram(s.File) == null).Select(s => s.File).ToList();
        if (notHere.Count > 0) return new(part, tool.Title, "N/A", "the Sandbox's Windows doesn't have " + string.Join(", ", notHere));

        var limit = TimeSpan.FromMinutes(tool.Id is "optimize-all" or "chkdsk" ? 15 : 8);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var problems = new List<string>();
        foreach (var step in tool.Steps)
        {
            var output = new List<string>();
            using var cts = new CancellationTokenSource(limit);
            int code;
            try
            {
                code = CommandRunner.RunAsync(step.File, UtilityCatalog.Expand(step.Args), step.Unicode ? Encoding.Unicode : null,
                    (text, live, kind) => { if (!live && text.Trim().Length > 0) lock (output) output.Add(text.Trim()); }, cts.Token).GetAwaiter().GetResult();
            }
            catch (Exception ex) { problems.Add($"{step.Label}: crashed: {ex.Message}"); continue; }
            if (code == -2) problems.Add($"{step.Label}: still running after {limit.TotalMinutes:0} minutes");
            else if (code == -1) problems.Add($"{step.Label}: couldn't start {step.File}");
            else if (code != 0 && !step.AnyExitIsFine)
                problems.Add($"{step.Label}: finished with code {code}" + (output.Count > 0 ? "\n  " + string.Join("\n  ", output.TakeLast(6)) : ""));
        }

        // the DNS buttons: check the adapters really changed
        string? dnsCheck = null;
        var want = tool.Id switch { "dns-cloudflare" => "1.1.1.1", "dns-google" => "8.8.8.8", "dns-quad9" => "9.9.9.9", _ => null };
        if (want != null || tool.Id == "dns-auto")
        {
            var now = Shell.PowerShell("Get-NetAdapter | Where-Object Status -eq 'Up' | ForEach-Object { (Get-DnsClientServerAddress -InterfaceIndex $_.ifIndex -AddressFamily IPv4).ServerAddresses } | Select-Object -Unique", 60_000).Output.Trim();
            var servers = string.Join(", ", now.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (want != null && !servers.Contains(want)) problems.Add($"afterwards the adapters use {servers}, not {want}");
            if (tool.Id == "dns-auto" && (servers.Contains("1.1.1.1") || servers.Contains("8.8.8.8") || servers.Contains("9.9.9.9"))) problems.Add($"afterwards the adapters still use {servers}");
            dnsCheck = "adapters now use " + (servers.Length > 0 ? servers : "the network's own DNS");
        }
        return problems.Count == 0
            ? new(part, tool.Title, "PASS", $"{tool.Steps.Length} step{(tool.Steps.Length == 1 ? "" : "s")} finished in {Took(clock.Elapsed)}" + (dnsCheck != null ? "; " + dnsCheck : ""))
            : new(part, tool.Title, "FAIL", string.Join("\n", problems));
    }

    /// <summary>The full path of a program the way Windows would find it, or null.</summary>
    static string? FindProgram(string file)
    {
        if (Path.IsPathRooted(file)) return File.Exists(file) ? file : null;
        var dirs = new[] { Environment.SystemDirectory, Environment.GetFolderPath(Environment.SpecialFolder.Windows) }
            .Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries));
        foreach (var d in dirs)
        {
            try { var f = Path.Combine(d.Trim(), file); if (File.Exists(f)) return f; } catch { }
        }
        return null;
    }

    /// <summary>Does a shortcut's target exist here: a program, a console (.msc/.cpl), or a registered link like ms-settings:.</summary>
    static bool OpenTargetExists(string target)
    {
        if (target.EndsWith(':') || (target.Contains(':') && !Path.IsPathRooted(target)))
        {
            var scheme = target[..target.IndexOf(':')];
            using var k = Registry.ClassesRoot.OpenSubKey(scheme);
            return k != null;
        }
        return FindProgram(target) != null;
    }

    // ============================================================================================ this PC: read-only scan
    /// <summary>
    /// The scan on your own PC. It only reads: every value the app can change is read before the scan and again after
    /// it, and the report lists any difference (there should be none).
    /// </summary>
    public sealed class PcScan
    {
        readonly string _outDir;
        readonly DateTime _started = DateTime.Now;
        readonly List<Row> _rows = new();
        readonly List<string> _notes = new();
        readonly List<string> _hardware = new();
        SystemInfo _sys = null!;
        List<Tweak> _tweaks = new();
        readonly TweakEngine _engine = new();
        Dictionary<string, string?> _before = new();

        public PcScan(string outDir) { _outDir = outDir; Directory.CreateDirectory(outDir); }
        public string ReportPath => Path.Combine(_outDir, "TrapShack PC scan.txt");
        public int Failures => _rows.Count(r => r.Outcome == "FAIL");

        void Save(bool finished)
        {
            var notes = new List<string> { "This scan only reads. Nothing on this PC was switched on, off, removed, cleaned or installed." };
            notes.AddRange(_notes);
            var text = Report("TrapShack Tweak Utility - PC scan (reads only)", "this PC", _started, finished, notes, _rows);
            if (_hardware.Count > 0) text += "\n==== SYSTEM INFO AS THE APP READS IT\n" + string.Join("\n", _hardware) + "\n";
            File.WriteAllText(ReportPath, text, Encoding.UTF8);
            if (finished) SelfTest.CopyLogSince(_started, Path.Combine(_outDir, "TrapShack PC scan log.txt"));
        }

        /// <summary>Every value the app could change, read straight from Windows: key = setting id / part.</summary>
        Dictionary<string, string?> Snapshot()
        {
            SlowCheck.ForgetAll();
            Caches.Refresh();
            var parts = _tweaks.Where(t => !t.OneShot)
                .SelectMany(t => t.Actions.Select((a, i) => (Key: $"{t.Id}/{i}", A: a))).ToList();
            return parts.AsParallel().WithDegreeOfParallelism(Math.Clamp(Environment.ProcessorCount, 2, 8))
                .Select(p =>
                {
                    try { return (p.Key, State: p.A.IsApplicable() ? p.A.TestState() : "(not on this PC)"); }
                    catch (Exception ex) { return (p.Key, State: (string?)("(couldn't read: " + ex.Message + ")")); }
                })
                .ToDictionary(x => x.Key, x => x.State);
        }

        /// <summary>Part 1, before the pages are opened: reads everything the app shows or could change.</summary>
        public void ReadEverything(Action<int, int, string>? progress)
        {
            const string S = "Start-up";
            const int total = 9;
            progress?.Invoke(0, total, "Detecting the hardware");
            var clock = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                _sys = SystemInfo.Detect();
                _sys.ProbeSlow();
                _tweaks = Catalog.Build();
                _rows.Add(new(S, "Hardware detection and list of settings", "PASS",
                    $"{_sys.FriendlyOs}; {_sys.Cpu}; graphics: {(_sys.Gpus.Count > 0 ? string.Join(" + ", _sys.Gpus) : "none found")}; {_sys.RamGb:0.#} GB; " +
                    $"{_sys.Storage}; {_sys.DeviceType}; {_tweaks.Count} settings in the app; read in {Took(clock.Elapsed)}"));
            }
            catch (Exception ex)
            {
                _rows.Add(new(S, "Hardware detection and list of settings", "FAIL", ex.ToString()));
                Save(true);
                throw;
            }
            Save(false);

            progress?.Invoke(1, total, "Reading every value the app can change (first read)");
            _before = Snapshot();
            _notes.Add($"{_before.Count} values the app can change were read before the scan.");

            // ---- every setting: does it suit this PC, and is it on?
            progress?.Invoke(2, total, "Reading every setting");
            const string On = "Settings that read as on", Unfit = "Settings locked or warned on this PC", Gone = "Settings that don't exist on this PC", Bad = "Settings that couldn't be read";
            var offByPage = new Dictionary<string, int>();
            foreach (var t in _tweaks)
            {
                if (t.OneShot) continue;
                var page = SafePage(t);
                try
                {
                    if (!_engine.IsAvailable(t)) { _rows.Add(new(Gone, $"{t.Id}  -  {t.Title}", "N/A", page)); continue; }
                    var v = t.Evaluate(_sys);
                    var on = _engine.IsApplied(t);
                    if (v.Fit == Fit.Unavailable) _rows.Add(new(Unfit, $"{t.Id}  -  {t.Title}", "INFO", $"{page}; {(on ? "on" : "off")}; locked: {v.Note}"));
                    else if (on) _rows.Add(new(On, $"{t.Id}  -  {t.Title}", "INFO", page + (v.Fit == Fit.Caution ? "; warned: " + v.Note : "")));
                    else offByPage[page] = offByPage.GetValueOrDefault(page) + 1;
                }
                catch (Exception ex) { _rows.Add(new(Bad, $"{t.Id}  -  {t.Title}", "FAIL", page + "; " + ex.Message)); }
            }
            _notes.Add("Settings that read as off, by page: " + string.Join(", ", offByPage.OrderBy(x => x.Key).Select(x => $"{x.Key} {x.Value}")));
            Save(false);

            // ---- what the pages show
            const string P = "Features (read only)";
            progress?.Invoke(3, total, "Reading System info");
            Try(P, "System info cards", () =>
            {
                var cards = HardwareInfo.Collect(_sys);
                foreach (var c in cards)
                {
                    _hardware.Add($"  {c.Title}: {c.Sub}");
                    foreach (var r in c.Rows.Where(r => !r.Label.Contains("address", StringComparison.OrdinalIgnoreCase)))
                        _hardware.Add($"      {r.Label}: {r.Value.Replace("\n", "  |  ")}");
                }
                return (cards.Count >= 5 ? "PASS" : "FAIL", $"{cards.Count} cards: " + string.Join(", ", cards.Select(c => c.Title)));
            });
            progress?.Invoke(4, total, "Reading the temperature sensors");
            Try(P, "Temperatures", () =>
            {
                using var s = new Sensors();
                s.Start();
                Thread.Sleep(1500);
                var r = s.Read();
                return (r.Count > 0 ? "PASS" : "FAIL", r.Count > 0 ? string.Join(", ", r.Select(x => x.Label + " " + x.Value)) : "no readings" + (s.Problem != null ? ": " + s.Problem : ""));
            });
            progress?.Invoke(5, total, "Reading startup apps and restore points");
            Try(P, "Startup apps", () =>
            {
                var list = StartupApps.Read();
                return ("PASS", $"{list.Count} startup programs ({list.Count(e => e.Enabled)} on)");
            });
            Try(P, "Restore points", () =>
            {
                var list = RestorePoints.List();
                return ("PASS", $"System Restore is {(RestorePoints.IsEnabled() ? "on" : "off")}; {list.Count} restore points" +
                                (list.Count > 0 ? $", newest: \"{list[0].Description}\" {list[0].Created:yyyy-MM-dd HH:mm}" : ""));
            });
            progress?.Invoke(6, total, "Measuring what the PC Cleaner would find (nothing is deleted)");
            Try(P, "PC Cleaner (measure only)", () =>
            {
                var rules = CleanerEngine.LoadBundledRules();
                var det = new CleanerEngine.Detector();
                var scans = new List<CleanScan>(); var bad = new List<string>();
                foreach (var r in rules.Where(det.IsInstalled))
                {
                    try { scans.Add(CleanerEngine.Scan(r, CancellationToken.None)); } catch (Exception ex) { bad.Add(r.Name + ": " + ex.Message); }
                }
                var shader = scans.Where(s => s.Rule.ShaderCache).Sum(s => s.Bytes);
                return (bad.Count == 0 ? "PASS" : "FAIL",
                    $"{scans.Count} of {rules.Count} rules apply here; {CleanScan.Format(scans.Where(s => !s.Rule.ShaderCache).Sum(s => s.Bytes))} ticked by default, " +
                    $"{CleanScan.Format(shader)} in shader caches (unticked). Nothing was deleted." + (bad.Count > 0 ? "\n" + string.Join("\n", bad.Take(10)) : ""));
            });
            progress?.Invoke(7, total, "Checking installed apps, winget and shortcuts");
            Try(P, "Apps page: winget and installed apps", () =>
            {
                var found = WingetService.DetectInstalled();
                return (WingetService.Available ? "PASS" : "FAIL", (WingetService.Available ? "winget found" : "winget NOT found (installs won't work)") + $"; {found.Count} of {AppCatalog.All.Length} apps detected as installed");
            });
            Try(P, "Debloat apps list", () =>
            {
                var present = WinAppCatalog.All.Where(d => Caches.AppxMatchesAny(d.Appx.Split('|'))).ToList();
                return ("PASS", $"{present.Count} of {WinAppCatalog.All.Length} listed apps are on this PC ({present.Count(d => d.Tier == RemoveTier.Protected)} of them locked)");
            });
            Try(P, "Utilities: programs and shortcuts", () =>
            {
                var progs = UtilityCatalog.Tools.SelectMany(t => t.Steps.Select(s => s.File)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var missingProgs = progs.Where(f => FindProgram(f) == null).ToList();
                var targets = UtilityCatalog.Tools.Where(t => t.Open != null).Select(t => t.Open!).Concat(UtilityCatalog.WindowsTools.Select(t => t.Target)).ToList();
                var missingTargets = targets.Where(t => !OpenTargetExists(t)).ToList();
                var missing = missingProgs.Concat(missingTargets).ToList();
                return (missing.Count == 0 ? "PASS" : "FAIL", missing.Count == 0
                    ? $"all {progs.Count} programs the utilities run and all {targets.Count} shortcuts exist (none were run)"
                    : "not found: " + string.Join(", ", missing));
            });
            Try(P, "Settings: check for updates", () =>
            {
                var r = Within(TimeSpan.FromSeconds(90), () => Updater.CheckAsync(MainWindow.UpdateRepo).GetAwaiter().GetResult());
                if (!r.Finished) return ("FAIL", "GitHub didn't answer within 90 seconds");
                if (r.Error != null) { var row = UpdateRow("", r.Error); return (row.Outcome, row.Detail!); }
                return ("PASS", $"running {Updater.CurrentText}; " + (r.Value == null ? "GitHub says this is the newest version" : "GitHub has a newer release (nothing was downloaded)"));
            });
            progress?.Invoke(8, total, "Reading the app's own records");
            Try(P, "Saved originals (state.json)", () =>
            {
                var file = Path.Combine(Log.Dir, "state.json");
                if (!File.Exists(file)) return ("PASS", "none yet (nothing changed by the app on this PC)");
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var n = doc.RootElement.ValueKind == JsonValueKind.Object ? doc.RootElement.EnumerateObject().Count() : 0;
                return ("PASS", $"readable; {n} saved entries");
            });
            Save(false);
        }

        void Try(string part, string name, Func<(string Outcome, string Detail)> read)
        {
            try { var (o, d) = read(); _rows.Add(new(part, name, o, d)); }
            catch (Exception ex) { _rows.Add(new(part, name, "FAIL", "crashed: " + ex.Message)); }
            Save(false);
        }

        static string SafePage(Tweak t)
        {
            try { return PageDefs.Get(PageDefs.KeyOf(t)).Title; } catch { return "?"; }
        }

        /// <summary>Part 3, after the pages: reads every value again and compares it with the first read.</summary>
        public void Finish(List<Row> pageRows, Action<int, int, string>? progress)
        {
            _rows.AddRange(pageRows);
            Save(false);
            progress?.Invoke(0, 1, "Reading every value again to prove nothing changed");
            var after = Snapshot();
            var changed = _before.Where(kv => !string.Equals(kv.Value, after.GetValueOrDefault(kv.Key), StringComparison.OrdinalIgnoreCase)).ToList();
            const string Proof = "Proof nothing was changed";
            if (changed.Count == 0)
                _rows.Add(new(Proof, $"All {_before.Count} values read before and after the scan", "PASS", "identical: nothing on this PC was changed"));
            else
            {
                var sb = new StringBuilder("The scan only reads, so these were changed by Windows or another program while it ran:");
                foreach (var (key, was) in changed.Take(30)) sb.Append($"\n{key}\n  before: {was ?? "(nothing)"}\n  after:  {after.GetValueOrDefault(key) ?? "(nothing)"}");
                _rows.Add(new(Proof, $"{changed.Count} of {_before.Count} values differ", "INFO", sb.ToString()));
            }
            Save(true);
        }
    }

    // ============================================================================================ summary
    /// <summary>The one-page summary written last; the .bat waits for the "ALL DONE" line in it.</summary>
    public static string WriteSummary(string outDir, string where, DateTime started, List<(string Part, string File, int Failures)> parts)
    {
        var path = Path.Combine(outDir, "TrapShack test summary.txt");
        var sb = new StringBuilder();
        sb.AppendLine("TrapShack Tweak Utility - test summary (" + where + ")");
        sb.AppendLine($"Started {started:yyyy-MM-dd HH:mm}, took {(DateTime.Now - started).TotalMinutes:0} min");
        sb.AppendLine();
        foreach (var (part, file, failures) in parts)
            sb.AppendLine($"  {(failures == 0 ? "OK      " : failures < 0 ? "STOPPED " : $"{failures} FAILED")}  {part}  ->  {file}");
        sb.AppendLine();
        sb.AppendLine(parts.All(p => p.Failures == 0) ? "Everything passed." : "Send the whole results zip to get anything fixed.");
        sb.AppendLine();
        sb.AppendLine("ALL DONE");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        try { if (File.Exists(Log.FilePath)) File.Copy(Log.FilePath, Path.Combine(outDir, "TrapShack log.txt"), true); } catch { }
        return path;
    }
}
