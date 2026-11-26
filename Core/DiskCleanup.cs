using System.Diagnostics;
using System.Text;

namespace TrapShack.Core;

/// <summary>
/// Runs Disk Cleanup and Windows' update-file cleanup in the background, with no windows and at low priority, so
/// nothing waits for it: Apply recommended carries on and the app stays usable.
///
/// It is the same pair the standard debloat set runs (Disk Cleanup, then DISM /StartComponentCleanup /ResetBase).
/// Disk Cleanup is started with its own silent mode (/sagerun, Microsoft's documented way to run it unattended)
/// instead of /VERYLOWDISK, which shows a window and a dialog at the end. Every cleanup Windows offers is ticked for
/// that run except the Downloads folder, which is never touched; the ticks are removed again when it finishes.
///
/// Everything runs in one hidden PowerShell process, so it finishes properly even if the app is closed meanwhile.
/// </summary>
public static class DiskCleanup
{
    const int Slot = 6501;   // the /sagerun number; only TrapShack uses it

    static Process? _job;
    static readonly object Gate = new();

    /// <summary>Raised (on a background thread) when the cleanup has finished, with a short line to show.</summary>
    public static event Action<string>? Finished;

    /// <summary>True while a cleanup started by TrapShack, or Disk Cleanup itself, is still running.</summary>
    public static bool Running
    {
        get
        {
            lock (Gate) { if (_job is { HasExited: false }) return true; }
            try { return Process.GetProcessesByName("cleanmgr").Length > 0; } catch { return false; }
        }
    }

    const string Script = @"
$ErrorActionPreference = 'SilentlyContinue'
$caches = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches'
$flag = 'StateFlags" + "6501" + @"'
Get-ChildItem $caches | Where-Object { $_.PSChildName -ne 'DownloadsFolder' } | ForEach-Object {
    New-ItemProperty -Path $_.PSPath -Name $flag -Value 2 -PropertyType DWord -Force | Out-Null }
Start-Process -FilePath ""$env:SystemRoot\System32\cleanmgr.exe"" -ArgumentList '/sagerun:" + "6501" + @"' -WindowStyle Hidden -Wait
Get-ChildItem $caches | ForEach-Object { Remove-ItemProperty -Path $_.PSPath -Name $flag -ErrorAction SilentlyContinue }
& ""$env:SystemRoot\System32\Dism.exe"" /Online /Cleanup-Image /StartComponentCleanup /ResetBase /Quiet /NoRestart | Out-Null
exit $LASTEXITCODE
";

    /// <summary>
    /// Starts the cleanup and returns at once. Returns false (and does nothing) when one is already running.
    /// </summary>
    public static bool StartInBackground()
    {
        lock (Gate)
        {
            if (Running) { Log.Info("Disk Cleanup is already running; not starting another"); return false; }
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(Script)))
            {
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            };
            var p = Process.Start(psi) ?? throw new InvalidOperationException("Windows didn't start Disk Cleanup.");
            // below normal: programs it starts (Disk Cleanup, DISM) inherit this, so games and the app stay smooth
            try { p.PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }
            _job = p;
            Log.Info($"Disk Cleanup started in the background (/sagerun:{Slot}, then DISM component cleanup)");
            var started = DateTime.Now;
            Task.Run(() =>
            {
                try
                {
                    p.WaitForExit();
                    var mins = (int)Math.Round((DateTime.Now - started).TotalMinutes);
                    var took = mins < 1 ? "under a minute" : mins == 1 ? "1 minute" : $"{mins} minutes";
                    // DISM: 0 = done, 3010 = done (a restart finishes it)
                    var ok = p.ExitCode is 0 or 3010;
                    Log.Info($"Disk Cleanup finished after {took} (code {p.ExitCode})");
                    Finished?.Invoke(ok ? $"Disk Cleanup finished in the background ({took})"
                                        : $"Disk Cleanup finished; Windows' update-file cleanup reported code {p.ExitCode} (details in the log)");
                }
                catch (Exception ex) { Log.Error("Disk Cleanup: " + ex.Message); }
            });
            return true;
        }
    }
}
