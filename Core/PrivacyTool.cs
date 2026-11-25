using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace TrapShack.Core;

/// <summary>
/// Downloads and starts O&amp;O ShutUp10++ from the publisher's own server. Every copy is checked for a valid
/// O&amp;O Software digital signature before it runs, a restore point and a registry backup are made first,
/// and the last verified copy is kept so the tool still opens when the download is blocked.
/// </summary>
public static class PrivacyTool
{
    const string Url = "https://dl5.oo-software.com/files/ooshutup10/OOSU10.exe";
    public const string PageUrl = "https://www.oo-software.com/en/shutup10";

    static string ToolsDir => Path.Combine(Log.Dir, "tools");
    static string Cached => Path.Combine(ToolsDir, "OOSU10.exe");

    public static void Run(Action<string>? status = null)
    {
        Directory.CreateDirectory(ToolsDir);

        status?.Invoke("Backing up before opening the tool");
        var rp = RestorePoint.Create("TrapShack - before O&O ShutUp10++");
        if (!rp.Ok) Log.Info("OOSU: restore point not created: " + rp.Message);
        BackupPolicies();

        status?.Invoke("Getting O&O ShutUp10++");
        string? exe = null;
        string? downloadError = null;
        for (var attempt = 1; attempt <= 3 && exe == null; attempt++)
        {
            try { exe = Download(); }
            catch (Exception ex)
            {
                downloadError = ex.Message;
                Log.Error($"OOSU download attempt {attempt}: {ex.Message}");
                Thread.Sleep(1500 * attempt);
            }
        }

        if (exe == null && File.Exists(Cached) && IsSigned(Cached, out _))
        {
            Log.Info("OOSU: using the last verified copy");
            exe = Cached;
        }

        if (exe == null)
        {
            try { Process.Start(new ProcessStartInfo(PageUrl) { UseShellExecute = true }); } catch { }
            throw new InvalidOperationException(
                "O&O ShutUp10++ could not be downloaded (" + downloadError + "). The official download page was opened in your browser instead.");
        }

        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = ToolsDir });
        Log.Info("OOSU launched");
    }

    static string Download()
    {
        var tmp = Cached + ".download";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TrapShack");
            using var resp = http.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            resp.EnsureSuccessStatusCode();
            using (var src = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
            using (var dst = File.Create(tmp))
                src.CopyTo(dst);

            if (new FileInfo(tmp).Length < 500_000) throw new InvalidDataException("The download was incomplete.");
            if (!IsSigned(tmp, out var why)) throw new InvalidDataException("The file's signature did not verify: " + why);

            File.Move(tmp, Cached, true);
            return Cached;
        }
        finally { try { if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
    }

    static bool IsSigned(string path, out string reason)
    {
        var r = Shell.PowerShell(
            "$s = Get-AuthenticodeSignature -LiteralPath '" + path.Replace("'", "''") + "'\n" +
            "Write-Output ($s.Status.ToString() + '|' + $s.SignerCertificate.Subject)", 60_000);
        var o = r.Output.Trim();
        reason = o.Length > 0 ? o : r.Error.Trim();
        return r.Ok && o.StartsWith("Valid|", StringComparison.OrdinalIgnoreCase)
               && o.Contains("O&O Software", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Exports the policy hives the tool writes to, so they can be re-imported by double-clicking the .reg files.</summary>
    static void BackupPolicies()
    {
        try
        {
            var dir = Path.Combine(Log.Dir, "backups", "oosu-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(dir);
            Shell.Run("reg.exe", $"export \"HKLM\\SOFTWARE\\Policies\" \"{Path.Combine(dir, "machine-policies.reg")}\" /y", 60_000);
            Shell.Run("reg.exe", $"export \"HKCU\\SOFTWARE\\Policies\" \"{Path.Combine(dir, "user-policies.reg")}\" /y", 60_000);
            Log.Info("OOSU backup saved to " + dir);
        }
        catch (Exception ex) { Log.Error("OOSU backup failed: " + ex.Message); }
    }
}
