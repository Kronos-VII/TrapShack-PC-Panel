using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace TrapShack.Core;

/// <summary>What kind of line the terminal is showing (decides its colour).</summary>
public enum TermKind { Output, Command, Info, Success, Warning, Error }

/// <summary>
/// Runs a console program hidden and streams what it prints, line by line, to the in-app terminal. Handles programs
/// that redraw one line in place with a carriage return (DISM and SFC progress bars), and SFC's Unicode output.
/// </summary>
public static class CommandRunner
{
    static CommandRunner()
    {
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
    }

    /// <summary>The console code page (what ipconfig, chkdsk, netsh and DISM print in).</summary>
    public static Encoding Oem
    {
        get
        {
            try { return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage); }
            catch { return Encoding.UTF8; }
        }
    }

    /// <param name="onLine">text, isLive (true = the line is still being redrawn), kind.</param>
    /// <returns>The exit code, or -1 if the program could not start, or -2 if it was stopped.</returns>
    public static async Task<int> RunAsync(string file, string args, Encoding? encoding, Action<string, bool, TermKind> onLine, CancellationToken token)
    {
        var enc = encoding ?? Oem;
        var psi = new ProcessStartInfo(file, args)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = enc, StandardErrorEncoding = enc,
            WorkingDirectory = Environment.SystemDirectory,
        };
        Process? p;
        try { p = Process.Start(psi); }
        catch (Exception ex) { onLine("Could not start " + file + ": " + ex.Message, false, TermKind.Error); return -1; }
        if (p == null) return -1;

        using (p)
        using (token.Register(() => { try { p.Kill(true); } catch { } }))
        {
            var outTask = Pump(p.StandardOutput, onLine, TermKind.Output);
            var errTask = Pump(p.StandardError, onLine, TermKind.Error);
            try { await p.WaitForExitAsync(token); }
            catch (OperationCanceledException) { }
            try { await Task.WhenAll(outTask, errTask).WaitAsync(TimeSpan.FromSeconds(3)); } catch { }
            if (token.IsCancellationRequested) return -2;
            return p.ExitCode;
        }
    }

    /// <summary>Reads characters and turns them into lines; a lone carriage return means "redraw this line".</summary>
    static async Task Pump(StreamReader reader, Action<string, bool, TermKind> onLine, TermKind kind)
    {
        var buf = new char[2048];
        var line = new StringBuilder();
        var sawCr = false;
        var lastLive = DateTime.MinValue;
        int n;
        try
        {
            while ((n = await reader.ReadAsync(buf, 0, buf.Length)) > 0)
            {
                for (var i = 0; i < n; i++)
                {
                    var c = buf[i];
                    if (c == '\0') continue;
                    if (sawCr)
                    {
                        sawCr = false;
                        if (c == '\n') { onLine(line.ToString(), false, kind); line.Clear(); continue; }
                        // a progress bar redrawing itself: show it in place
                        if (line.Length > 0) onLine(line.ToString(), true, kind);
                        line.Clear();
                    }
                    if (c == '\r') { sawCr = true; continue; }
                    if (c == '\n') { onLine(line.ToString(), false, kind); line.Clear(); continue; }
                    line.Append(c);
                }
                // text without a line end yet (SFC prints its percentage this way): show it live, at most ~8 times a second
                if (line.Length > 0 && (DateTime.Now - lastLive).TotalMilliseconds > 120)
                {
                    onLine(line.ToString(), true, kind);
                    lastLive = DateTime.Now;
                }
            }
        }
        catch { /* the program was stopped */ }
        if (line.Length > 0) onLine(line.ToString(), false, kind);
    }

    /// <summary>Opens a Windows tool or settings page the normal way (its own window).</summary>
    public static bool Open(string target, string? args = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target, args ?? "") { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) { Log.Error($"Could not open {target}: {ex.Message}"); return false; }
    }
}
