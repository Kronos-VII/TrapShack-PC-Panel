using System.IO;
using System.Diagnostics;
using System.Text;

namespace TrapShack.Core;

/// <summary>Small helpers for running external tools and logging.</summary>
public static class Shell
{
    public sealed record Result(int ExitCode, string Output, string Error)
    {
        public bool Ok => ExitCode == 0;
    }

    public static Result Run(string file, string args, int timeoutMs = 120_000) => Run(file, args, timeoutMs, echo: true);

    static Result Run(string file, string args, int timeoutMs, bool echo)
    {
        if (echo) Log.Echo($"> {Path.GetFileName(file)} {(args.Length > 160 ? args[..160] + "…" : args)}");
        var psi = new ProcessStartInfo(file, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {file}");
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEndAsync();

        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(true); } catch { /* already gone */ }
            return new Result(-1, "", "Timed out");
        }

        var result = new Result(p.ExitCode, stdout.Result, stderr.Result);
        EchoOutput(result);
        return result;
    }

    /// <summary>Shows the first lines of what a command printed in the in-app console.</summary>
    static void EchoOutput(Result r)
    {
        foreach (var line in r.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(40))
            Log.Echo("  " + line.TrimEnd());
        foreach (var line in r.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(15))
            Log.Echo("! " + line.TrimEnd());
        if (!r.Ok) Log.Echo($"  exit code {r.ExitCode}");
    }

    /// <summary>Runs a Windows PowerShell 5.1 script (always present on Windows 10/11).</summary>
    public static Result PowerShell(string script, int timeoutMs = 300_000)
    {
        var full = "$ProgressPreference='SilentlyContinue'\n" + script;
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(full));
        foreach (var line in script.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(8))
            Log.Echo("PS> " + (line.Trim().Length > 150 ? line.Trim()[..150] + "…" : line.Trim()));
        return Run("powershell.exe",
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -OutputFormat Text -EncodedCommand {encoded}", timeoutMs, echo: false);
    }

    public static void KillProcess(string imageName) =>
        Run("taskkill.exe", $"/f /im {imageName}", 15_000);
}

public static class Log
{
    static readonly object Gate = new();

    public static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TrapShack");

    public static string FilePath => Path.Combine(Dir, "trapshack.log");

    public static void Info(string message) { Write("INFO ", message); Echo(message); }
    public static void Error(string message) { Write("ERROR", message); Echo("! " + message); }

    /// <summary>Lines for the in-app console. Raised on whatever thread wrote them.</summary>
    public static event Action<string>? Console;

    public static void Echo(string line)
    {
        try { Console?.Invoke(line); } catch { /* the console must never break the app */ }
    }

    static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Dir);
                File.AppendAllText(FilePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch { /* logging must never break the app */ }
    }
}
