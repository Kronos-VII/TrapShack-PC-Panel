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

    public static Result Run(string file, string args, int timeoutMs = 120_000)
    {
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

        return new Result(p.ExitCode, stdout.Result, stderr.Result);
    }

    /// <summary>Runs a Windows PowerShell 5.1 script (always present on Windows 10/11).</summary>
    public static Result PowerShell(string script, int timeoutMs = 300_000)
    {
        var full = "$ProgressPreference='SilentlyContinue'\n" + script;
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(full));
        return Run("powershell.exe",
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -OutputFormat Text -EncodedCommand {encoded}", timeoutMs);
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

    public static void Info(string message) => Write("INFO ", message);
    public static void Error(string message) => Write("ERROR", message);

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
