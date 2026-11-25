using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace TrapShack.Core;

public sealed record UpdateInfo(Version Latest, string Tag, string DownloadUrl, string Notes);

/// <summary>Checks GitHub releases for a newer TrapShackTweakUtility.exe and swaps it in with a small helper script.</summary>
public static class Updater
{
    static readonly HttpClient Http = Make();

    static HttpClient Make()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("TrapShack-Updater");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }

    public static Version Current =>
        Assembly.GetExecutingAssembly().GetName().Version is { } v ? new Version(v.Major, v.Minor, Math.Max(0, v.Build)) : new Version(0, 0, 0);

    public static string CurrentText => Current.ToString(3);

    /// <summary>Returns null when already up to date. Throws with a readable message when the check cannot be done.</summary>
    public static async Task<UpdateInfo?> CheckAsync(string repo)
    {
        repo = (repo ?? "").Trim().Trim('/');
        if (repo.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase)) repo = repo[19..];
        if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) repo = repo[..^4];
        var parts = repo.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) throw new InvalidOperationException("Enter the repository as owner/name, for example Kronos-VII/TrapShack-PC-Panel.");
        repo = parts[0] + "/" + parts[1];

        using var resp = await Http.GetAsync($"https://api.github.com/repos/{repo}/releases/latest");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException("No release has been published on that repository yet.");
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException("GitHub answered with " + (int)resp.StatusCode + ". Try again in a minute.");

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var latest))
            throw new InvalidOperationException("The latest release has a version name TrapShack cannot read (" + tag + ").");
        if (latest <= Current) return null;

        string? url = null;
        if (root.TryGetProperty("assets", out var assets))
            foreach (var a in assets.EnumerateArray())
            {
                var name = a.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) { url = a.GetProperty("browser_download_url").GetString(); break; }
            }
        if (url == null) throw new InvalidOperationException("Release " + tag + " has no .exe file attached.");
        var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
        return new UpdateInfo(latest, tag, url, notes);
    }

    /// <summary>Downloads the new exe next to the running one's temp folder and reports progress 0..1.</summary>
    public static async Task<string> DownloadAsync(UpdateInfo info, IProgress<(long Done, long Total)> progress)
    {
        var dir = Path.Combine(Path.GetTempPath(), "TrapShackUpdate");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "TrapShackTweakUtility.new.exe");
        using var resp = await Http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? 0;
        await using (var src = await resp.Content.ReadAsStreamAsync())
        await using (var dst = File.Create(file))
        {
            var buf = new byte[81920]; long done = 0; int n;
            while ((n = await src.ReadAsync(buf)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, n));
                done += n;
                progress.Report((done, total));
            }
        }
        if (new FileInfo(file).Length < 1_000_000) throw new InvalidOperationException("The downloaded file looks incomplete.");
        return file;
    }

    /// <summary>Starts a hidden helper that waits for this app to close, replaces the exe, and starts it again. Caller then shuts the app down.</summary>
    public static void StartSwap(string newFile)
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot find the running program.");
        if (!exe.EndsWith("TrapShackTweakUtility.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Updating only works from the built TrapShackTweakUtility.exe, not from a development run.");
        var bat = Path.Combine(Path.GetTempPath(), "TrapShackUpdate", "swap.bat");
        File.WriteAllText(bat,
            "@echo off\r\n" +
            "set tries=0\r\n" +
            ":retry\r\n" +
            "ping -n 3 127.0.0.1 >nul\r\n" +
            $"move /y \"{newFile}\" \"{exe}\" >nul 2>&1\r\n" +
            "if errorlevel 1 (\r\n  set /a tries+=1\r\n  if %tries% lss 20 goto retry\r\n  exit /b 1\r\n)\r\n" +
            $"start \"\" \"{exe}\"\r\n" +
            "(goto) 2>nul & del \"%~f0\"\r\n");
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{bat}\"") { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden });
    }
}
