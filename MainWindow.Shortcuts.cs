using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TrapShack.Core;
using TrapShack.Ui;

namespace TrapShack;

/// <summary>Settings → Updates: the shortcut card and the "what TrapShack needs" card.</summary>
public partial class MainWindow
{
    const string ShortcutName = "TrapShack Tweak Utility.lnk";

    static string DesktopShortcut => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), ShortcutName);
    static string StartShortcut => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), ShortcutName);

    void RefreshShortcutStatus()
    {
        var exe = Environment.ProcessPath ?? "";
        bool Points(string lnk) => File.Exists(lnk) && string.Equals(ShortcutTarget(lnk), exe, StringComparison.OrdinalIgnoreCase);
        var d = Points(DesktopShortcut);
        var s = Points(StartShortcut);
        ShortcutStatus.Text = d && s
            ? "Shortcuts are on the desktop and in the Start menu, pointing at this copy of the app."
            : (d || s)
                ? $"There's a shortcut {(d ? "on the desktop" : "in the Start menu")} only. Create shortcuts adds the other one."
                : "Adds TrapShack to the desktop and the Start menu, pointing at this copy of the app.";
        ShortcutBtn.Content = d && s ? "Create again" : "Create shortcuts";
    }

    void OnCreateShortcuts(object sender, RoutedEventArgs e)
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
        {
            Dialogs.Info(this, "Couldn't make shortcuts", "TrapShack couldn't find its own file.");
            return;
        }
        var made = new List<string>();
        var failed = new List<string>();
        foreach (var (lnk, where) in new[] { (DesktopShortcut, "desktop"), (StartShortcut, "Start menu") })
        {
            try { MakeShortcut(lnk, exe); made.Add(where); }
            catch (Exception ex) { failed.Add(where); Log.Error($"Shortcut ({where}): {ex.Message}"); }
        }
        Log.Info($"Shortcuts created: {string.Join(", ", made)}");
        RefreshShortcutStatus();
        if (failed.Count > 0)
            Dialogs.Info(this, "Shortcuts", made.Count > 0
                ? $"Added to the {made[0]}, but Windows refused the {failed[0]} one. The log has the details."
                : "Windows refused to create the shortcuts. The log has the details.");
        else if (IsTemporaryPlace(exe))
            Dialogs.Info(this, "Shortcuts added",
                "Done. The app is running from your Downloads or a temporary folder, so the shortcuts stop working if that file is moved or deleted. " +
                "Moving the app somewhere permanent first (for example a TrapShack folder in Documents) and then making the shortcuts again avoids that.");
    }

    static bool IsTemporaryPlace(string path)
    {
        var temp = Path.GetTempPath();
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        return path.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || path.StartsWith(downloads, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Makes a normal Windows shortcut with the same COM object Windows' own scripts use.</summary>
    static void MakeShortcut(string lnk, string exe)
    {
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Windows Script Host isn't available.");
        dynamic shell = Activator.CreateInstance(type)!;
        try
        {
            dynamic sc = shell.CreateShortcut(lnk);
            sc.TargetPath = exe;
            sc.WorkingDirectory = Path.GetDirectoryName(exe);
            sc.IconLocation = exe + ",0";
            sc.Description = "TrapShack Tweak Utility";
            sc.Save();
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(sc);
        }
        finally { System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell); }
    }

    static string? ShortcutTarget(string lnk)
    {
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type == null) return null;
            dynamic shell = Activator.CreateInstance(type)!;
            try { return (string)shell.CreateShortcut(lnk).TargetPath; }
            finally { System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell); }
        }
        catch { return null; }
    }

    // ---------------------------------------------------------------- what TrapShack needs

    void OnCheckNeeds(object sender, RoutedEventArgs e) => _ = RefreshNeedsAsync();

    async Task RefreshNeedsAsync()
    {
        NeedsList.Children.Clear();
        NeedsList.Children.Add(NeedRow(null, "Checking…", ""));
        var rows = await Task.Run(() => new (bool? Ok, string Title, string Note)[]
        {
            (true, "The app itself", "Built in. No .NET or other runtime to install."),
            (WingetService.Available, "Windows Package Manager (winget)",
                WingetService.Available
                    ? "Found. The Apps page uses it to install and update apps."
                    : "Not found. Only the Apps page needs it: install \"App Installer\" from the Microsoft Store to get it."),
            (RegUtil.KeyExists(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\PawnIO") ? true : (bool?)null,
                "PawnIO driver (optional)",
                RegUtil.KeyExists(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\PawnIO")
                    ? "Found. CPU temperatures can be read on the Monitor page."
                    : "Not installed. Only needed for CPU temperatures on the Monitor page; everything else works without it. Get it from pawnio.eu."),
            (true, "Game runtimes", "Not needed by TrapShack. Games use them: Visual C++, DirectX and .NET are on the Apps page under runtimes, installed only when you choose them."),
        });
        NeedsList.Children.Clear();
        foreach (var r in rows) NeedsList.Children.Add(NeedRow(r.Ok, r.Title, r.Note));
    }

    FrameworkElement NeedRow(bool? ok, string title, string note)
    {
        var g = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition());
        var icon = new TextBlock
        {
            Text = ok == true ? "" : ok == false ? "" : "",
            FontFamily = (FontFamily)FindResource("IconFont"),
            FontSize = 13, Margin = new Thickness(0, 2, 10, 0),
        };
        icon.SetResourceReference(TextBlock.ForegroundProperty, ok == false ? "AccentBrush" : "MutedBrush");
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold });
        if (note.Length > 0)
        {
            var n = new TextBlock { Text = note, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
            n.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
            text.Children.Add(n);
        }
        Grid.SetColumn(text, 1);
        g.Children.Add(icon);
        g.Children.Add(text);
        return g;
    }
}
