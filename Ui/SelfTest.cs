using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrapShack.Core;
using TrapShack.Tweaks;

namespace TrapShack.Ui;

/// <summary>
/// Checks every setting in the app on a throwaway copy of Windows: for each one it records the exact current values,
/// switches it on, checks Windows reports it as on, switches it off, and checks every value is back exactly as it was.
///
/// It only runs inside Windows Sandbox (or on a GitHub build machine), never on a real PC, and only when the program
/// is started with --selftest. Normal use of the app never reaches this code.
/// </summary>
public static class SelfTest
{
    /// <summary>True inside Windows Sandbox or on a GitHub Actions build machine: both are wiped afterwards.</summary>
    public static bool Allowed =>
        string.Equals(Environment.UserName, "WDAGUtilityAccount", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Settings that are left out of the test, and why. These remove things for good, take a long time, or need
    /// something a throwaway Windows doesn't have. Their code is the same kind as the settings that are tested.
    /// </summary>
    static readonly Dictionary<string, string> LeftOut = new(StringComparer.OrdinalIgnoreCase)
    {
        ["debloat.onedrive"] = "uninstalls OneDrive (the Sandbox has no OneDrive)",
        ["browsers.edge-remove"] = "uninstalls Microsoft Edge",
        ["clean.bitlocker"] = "decrypts the drive (the Sandbox has no BitLocker)",
        ["ai.recall-feature"] = "changes a Windows feature with DISM, which needs a restart",
        ["clean.reserved-storage"] = "changes reserved storage with DISM",
        ["clean.oosu"] = "downloads and opens another program",
        ["extra.restore-point"] = "makes a restore point (the Sandbox has no System Restore)",
    };

    public sealed record Result(string Id, string Title, string Outcome, string? Detail);

    /// <summary>Runs every check and writes the report. Returns how many settings failed.</summary>
    public static int Run(string outDir, Action<int, int, string>? progress = null)
    {
        Directory.CreateDirectory(outDir);
        var reportPath = Path.Combine(outDir, "TrapShack self-test.txt");
        var results = new List<Result>();
        var problems = new List<string>();
        var started = DateTime.Now;
        SystemInfo? sys = null;
        List<Tweak> tweaks = new();

        void Save(bool finished)
        {
            File.WriteAllText(reportPath, Report(sys, results, problems, started, finished), Encoding.UTF8);
            // at the end, the app's own log goes next to the report: it shows exactly what Windows answered to each step
            if (finished)
            {
                try { if (File.Exists(Log.FilePath)) File.Copy(Log.FilePath, Path.Combine(outDir, "TrapShack log.txt"), true); }
                catch { /* the report matters more than the log */ }
            }
        }

        // ---- 1. start up the same way the app does
        try
        {
            progress?.Invoke(0, 1, "Reading this PC");
            sys = SystemInfo.Detect();
            try { sys.ProbeSlow(); } catch (Exception ex) { problems.Add("Slow hardware checks failed: " + ex.Message); }
            Caches.Refresh();
            tweaks = Catalog.Build();
        }
        catch (Exception ex)
        {
            problems.Add("The app could not build its list of settings (it would not start): " + ex);
            Save(true);
            return 1;
        }

        // ---- 2. the lists that tie the pages together
        foreach (var g in tweaks.GroupBy(t => t.Id).Where(g => g.Count() > 1))
            problems.Add($"Two settings share the id \"{g.Key}\".");
        var ids = tweaks.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var (section, _, list) in Catalog.DebloatLayout)
            foreach (var id in list)
            {
                if (id.StartsWith('@'))
                {
                    if (!tweaks.Any(t => t.ChoiceGroup == id[1..])) problems.Add($"The Debloat page lists the drop-down \"{id[1..]}\" under {section}, but no setting belongs to it.");
                }
                else if (!ids.Contains(id)) problems.Add($"The Debloat page lists \"{id}\" under {section}, but no setting has that id.");
            }
        foreach (var id in Recommended.Standard.Concat(Recommended.OptimizationRecommended))
            if (!ids.Contains(id)) problems.Add($"A recommended list names \"{id}\", but no setting has that id.");
        foreach (var t in tweaks)
        {
            try { _ = t.Evaluate(sys!); }
            catch (Exception ex) { problems.Add($"{t.Id}: deciding whether it suits this PC crashed: {ex.Message}"); }
        }
        Save(false);

        // ---- 3. every setting: on, check, off, check that everything is exactly as before
        var engine = new TweakEngine();
        for (var i = 0; i < tweaks.Count; i++)
        {
            var t = tweaks[i];
            progress?.Invoke(i, tweaks.Count, t.Title);
            results.Add(TestOne(engine, sys!, t));
            Save(false);
        }

        Save(true);
        progress?.Invoke(tweaks.Count, tweaks.Count, "");
        return results.Count(r => r.Outcome == "FAIL") + problems.Count;
    }

    static Result TestOne(TweakEngine engine, SystemInfo sys, Tweak t)
    {
        Result R(string outcome, string? detail = null) => new(t.Id, t.Title, outcome, detail);

        if (t.OneShot) return R("SKIP", "one-time action (nothing to undo)");
        if (t.StoreRestore || t.Actions.OfType<AppxAction>().Any()) return R("SKIP", "removes apps (they come back only from the Microsoft Store)");
        if (LeftOut.TryGetValue(t.Id, out var why)) return R("SKIP", why);
        try
        {
            if (t.Evaluate(sys).Fit == Fit.Unavailable) return R("N/A", "not for this PC: " + (t.Evaluate(sys).Note ?? ""));
            if (!engine.IsAvailable(t)) return R("N/A", "the setting doesn't exist on this copy of Windows");
        }
        catch (Exception ex) { return R("FAIL", "checking it crashed: " + ex.Message); }

        var usesTasks = t.Actions.OfType<ScheduledTaskAction>().Any();
        List<(TweakAction A, string? State)> Snapshot()
        {
            if (usesTasks) Caches.RefreshTasks();   // read task states fresh from Windows, not from the app's memory
            return t.Actions.Where(a => a.IsApplicable()).Select(a => (a, a.TestState())).ToList();
        }
        bool On() { if (usesTasks) Caches.RefreshTasks(); return engine.IsApplied(t); }

        try
        {
            var before = Snapshot();
            var wasOn = On();
            if (!wasOn)
            {
                engine.Apply(t);
                if (!On()) { engine.Revert(t); return R("FAIL", "after switching it on, Windows still reports the old value" + Diff(before, Snapshot(), onlyChanged: false)); }
                engine.Revert(t);
                if (On()) return R("FAIL", "after switching it off, it still reads as on");
            }
            else
            {
                // already on (Windows ships this way, or an earlier setting set it): test off, then on again
                engine.Revert(t);
                engine.Apply(t);
                if (!On()) return R("FAIL", "was on; after switching it off and on again it no longer reads as on");
            }
            var after = Snapshot();
            var diff = Diff(before, after, onlyChanged: true);
            return diff.Length == 0 ? R("PASS", wasOn ? "was already on; off and on again restored every value" : null)
                                    : R("FAIL", "not everything came back exactly as it was:" + diff);
        }
        catch (Exception ex)
        {
            try { engine.Revert(t); } catch { /* best effort, the Sandbox is thrown away anyway */ }
            return R("FAIL", "crashed: " + ex.Message);
        }
    }

    static string Diff(List<(TweakAction A, string? State)> before, List<(TweakAction A, string? State)> after, bool onlyChanged)
    {
        var sb = new StringBuilder();
        foreach (var (a, was) in before)
        {
            var now = after.FirstOrDefault(x => ReferenceEquals(x.A, a)).State;
            if (onlyChanged && string.Equals(was, now, StringComparison.OrdinalIgnoreCase)) continue;
            sb.Append($"\n        {a.Describe()}\n          before: {was ?? "(nothing)"}\n          after:  {now ?? "(nothing)"}");
        }
        return sb.ToString();
    }

    static string Report(SystemInfo? sys, List<Result> results, List<string> problems, DateTime started, bool finished)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TrapShack Tweak Utility - self-test");
        sb.AppendLine($"Started {started:yyyy-MM-dd HH:mm}, {(finished ? $"finished after {(DateTime.Now - started).TotalMinutes:0} min" : "STILL RUNNING (or stopped part-way)")}");
        if (sys != null) sb.AppendLine($"Windows: {sys.FriendlyOs}   CPU: {sys.Cpu}   Where: {(Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ? "GitHub build machine" : "Windows Sandbox")}");
        sb.AppendLine();
        var pass = results.Count(r => r.Outcome == "PASS");
        var fail = results.Count(r => r.Outcome == "FAIL");
        sb.AppendLine($"PASSED {pass}   FAILED {fail}   NOT FOR THIS PC {results.Count(r => r.Outcome == "N/A")}   LEFT OUT {results.Count(r => r.Outcome == "SKIP")}   PROBLEMS IN THE LISTS {problems.Count}");
        sb.AppendLine(fail == 0 && problems.Count == 0 ? "Everything tested came back exactly as it was." : "Send this file to get the failures fixed.");
        void Section(string title, IEnumerable<Result> rows)
        {
            var list = rows.ToList();
            if (list.Count == 0) return;
            sb.AppendLine().AppendLine($"==== {title} ({list.Count})");
            foreach (var r in list) sb.AppendLine($"  {r.Id}  -  {r.Title}" + (r.Detail != null ? $"\n      {r.Detail}" : ""));
        }
        if (problems.Count > 0)
        {
            sb.AppendLine().AppendLine($"==== PROBLEMS IN THE LISTS ({problems.Count})");
            foreach (var p in problems) sb.AppendLine("  " + p);
        }
        Section("FAILED", results.Where(r => r.Outcome == "FAIL"));
        Section("PASSED", results.Where(r => r.Outcome == "PASS"));
        Section("NOT FOR THIS PC", results.Where(r => r.Outcome == "N/A"));
        Section("LEFT OUT", results.Where(r => r.Outcome == "SKIP"));
        return sb.ToString();
    }

    // ------------------------------------------------------------------------------------------ the window
    /// <summary>A small progress window for the Sandbox run.</summary>
    public static Window MakeWindow(out Action<int, int, string> progress, out Action<int, string> done)
    {
        var bg = new SolidColorBrush(Color.FromRgb(0x0B, 0x0B, 0x0E));
        var muted = new SolidColorBrush(Color.FromRgb(0xA1, 0xA1, 0xAA));
        var accent = Application.Current.TryFindResource("AccentBrush") as Brush ?? Brushes.Crimson;
        var title = new TextBlock { Text = "TrapShack self-test", FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
        var line = new TextBlock { Text = "Starting…", FontSize = 13, Foreground = muted, Margin = new Thickness(0, 8, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
        var bar = new ProgressBar { Height = 6, Maximum = 1, Margin = new Thickness(0, 18, 0, 0), Foreground = accent, Background = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x28)), BorderThickness = new Thickness(0) };
        var note = new TextBlock
        {
            Text = "Every setting is switched on, checked, switched off and checked again. This takes several minutes. Nothing here touches your real PC.",
            FontSize = 12.5, Foreground = muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 18, 0, 0),
        };
        var panel = new StackPanel { Margin = new Thickness(32) };
        panel.Children.Add(title); panel.Children.Add(line); panel.Children.Add(bar); panel.Children.Add(note);
        var w = new Window
        {
            Title = "TrapShack self-test", Width = 560, SizeToContent = SizeToContent.Height, Background = bg,
            WindowStartupLocation = WindowStartupLocation.CenterScreen, ResizeMode = ResizeMode.NoResize, Content = panel,
        };
        progress = (i, n, name) => w.Dispatcher.InvokeAsync(() =>
        {
            bar.Value = n == 0 ? 0 : (double)i / n;
            line.Text = name.Length > 0 ? $"{i + 1} of {n}: {name}" : "Finishing…";
        });
        done = (failures, path) => w.Dispatcher.InvokeAsync(() =>
        {
            bar.Value = 1;
            title.Text = failures < 0 ? "Self-test stopped with an error" : failures == 0 ? "Self-test passed" : $"Self-test finished: {failures} to fix";
            line.Text = "The report is saved as " + path;
            note.Text = "You can close the Sandbox now. The report is in the \"Self-test results\" folder next to Test in Sandbox.bat. Send that file over.";
        });
        return w;
    }
}
