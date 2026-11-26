using System.IO;
using System.Text.RegularExpressions;

namespace TrapShack.Core;

/// <summary>
/// Extra ways to spot an installed app, for apps whose installer doesn't match one fixed uninstall name.
/// Names are matched against the DisplayName of every uninstall entry (machine-wide and per-user, 64- and 32-bit).
/// Paths are where each official installer puts the program; a path ending in * is a folder name prefix.
/// Both only ever read; nothing here changes anything.
/// </summary>
static class AppDetect
{
    public sealed record Hint(Regex Names, string[] Paths);

    static Hint H(string names, params string[] paths) => new(new Regex(names, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), paths);

    const string PF = "%ProgramFiles%", PF86 = "%ProgramFiles(x86)%", LAD = "%LocalAppData%", AD = "%AppData%";

    public static readonly Dictionary<string, Hint> Hints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["valve-steam"] = H(@"^Steam$"),
        ["epicgames-epicgameslauncher"] = H(@"^Epic Games Launcher$",
            PF86 + @"\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe",
            PF + @"\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe"),
        ["blizzard-battlenet"] = H(@"^Battle\.net$", PF86 + @"\Battle.net\Battle.net Launcher.exe"),
        ["electronicarts-eadesktop"] = H(@"^EA app$", PF + @"\Electronic Arts\EA Desktop\EA Desktop\EADesktop.exe"),
        ["ubisoft-connect"] = H(@"^Ubisoft (Connect|Game Launcher)$", PF86 + @"\Ubisoft\Ubisoft Game Launcher\UbisoftConnect.exe"),
        ["rockstargames-launcher"] = H(@"^Rockstar Games Launcher$", PF + @"\Rockstar Games\Launcher\Launcher.exe"),
        ["nvidia-geforcenow"] = H(@"^NVIDIA GeForce NOW$", LAD + @"\NVIDIA Corporation\GeForceNOW\CEF\GeForceNOW.exe"),
        ["moonsworth-lunarclient"] = H(@"^Lunar Client\b", LAD + @"\Programs\Lunar Client\Lunar Client.exe", PF + @"\Lunar Client\Lunar Client.exe"),
        ["realix-hwinfo"] = H(@"^HWiNFO(64|32)?\b", PF + @"\HWiNFO64\HWiNFO64.exe", PF86 + @"\HWiNFO32\HWiNFO32.exe"),
        ["guru3d-afterburner"] = H(@"^MSI Afterburner\b", PF86 + @"\MSI Afterburner\MSIAfterburner.exe"),
        ["cpuid-cpu-z"] = H(@"^CPUID CPU-Z\b", PF + @"\CPUID\CPU-Z\cpuz.exe"),
        ["techpowerup-gpu-z"] = H(@"^(TechPowerUp )?GPU-Z\b", PF86 + @"\GPU-Z\GPU-Z.exe", PF + @"\GPU-Z\GPU-Z.exe"),
        ["wagnardsoft-displaydriveruninstaller"] = H(@"^Display Driver Uninstaller\b"),
        ["techpowerup-nvcleanstall"] = H(@"^(TechPowerUp )?NVCleanstall\b", PF + @"\NVCleanstall\NVCleanstall.exe", PF86 + @"\NVCleanstall\NVCleanstall.exe"),
        ["openrgb-openrgb"] = H(@"^OpenRGB\b", PF + @"\OpenRGB\OpenRGB.exe"),
        ["maxon-cinebenchr23"] = H(@"^Cinebench R23\b"),
        ["obsproject-obsstudio"] = H(@"^OBS Studio\b", PF + @"\obs-studio\bin\64bit\obs64.exe"),
        ["amazon-corretto-21-jdk"] = H(@"^Amazon Corretto( Development Kit)?.*\b21\b", PF + @"\Amazon Corretto\jdk21*"),
        ["discord-discord"] = H(@"^Discord$", LAD + @"\Discord\Update.exe"),
        ["telegram-telegramdesktop"] = H(@"^Telegram Desktop\b", AD + @"\Telegram Desktop\Telegram.exe", PF + @"\Telegram Desktop\Telegram.exe"),
        ["zoom-zoom"] = H(@"^Zoom( Workplace)?( \(.*\))?$", AD + @"\Zoom\bin\Zoom.exe", PF + @"\Zoom\bin\Zoom.exe"),
        ["google-chrome"] = H(@"^Google Chrome$",
            PF + @"\Google\Chrome\Application\chrome.exe", PF86 + @"\Google\Chrome\Application\chrome.exe", LAD + @"\Google\Chrome\Application\chrome.exe"),
        ["brave-brave"] = H(@"^Brave$",
            PF + @"\BraveSoftware\Brave-Browser\Application\brave.exe", PF86 + @"\BraveSoftware\Brave-Browser\Application\brave.exe",
            LAD + @"\BraveSoftware\Brave-Browser\Application\brave.exe"),
        ["opera-operagx"] = H(@"^Opera GX\b", LAD + @"\Programs\Opera GX\opera.exe"),
        ["videolan-vlc"] = H(@"^VLC media player$", PF + @"\VideoLAN\VLC\vlc.exe", PF86 + @"\VideoLAN\VLC\vlc.exe"),
        // the Microsoft Store version has no uninstall entry, only its package folder
        ["spotify-spotify"] = H(@"^Spotify$", AD + @"\Spotify\Spotify.exe", LAD + @"\Packages\SpotifyAB.SpotifyMusic_zpdnekdrzrea0*"),
        ["audacity-audacity"] = H(@"^Audacity\b", PF + @"\Audacity\Audacity.exe"),
        ["apple-itunes"] = H(@"^iTunes$", PF + @"\iTunes\iTunes.exe", LAD + @"\Packages\AppleInc.iTunes_nzyj5cx40ttqa*"),
        ["bytedance-capcut"] = H(@"^CapCut$", LAD + @"\CapCut\Apps\CapCut.exe"),
        ["7zip-7zip"] = H(@"^7-Zip\b", PF + @"\7-Zip\7zFM.exe", PF86 + @"\7-Zip\7zFM.exe"),
        ["rarlab-winrar"] = H(@"^WinRAR\b", PF + @"\WinRAR\WinRAR.exe", PF86 + @"\WinRAR\WinRAR.exe"),
        ["microsoft-powertoys"] = H(@"^PowerToys\b", PF + @"\PowerToys\PowerToys.exe", LAD + @"\PowerToys\PowerToys.exe"),
        ["sharex-sharex"] = H(@"^ShareX\b", PF + @"\ShareX\ShareX.exe"),
        ["notepad-notepad"] = H(@"^Notepad\+\+", PF + @"\Notepad++\notepad++.exe", PF86 + @"\Notepad++\notepad++.exe"),
        ["autohotkey-autohotkey"] = H(@"^AutoHotkey\b", PF + @"\AutoHotkey\UX\AutoHotkeyUX.exe", LAD + @"\Programs\AutoHotkey\UX\AutoHotkeyUX.exe"),
        ["anydesk-anydesk"] = H(@"^AnyDesk$", PF86 + @"\AnyDesk\AnyDesk.exe", PF + @"\AnyDesk\AnyDesk.exe"),
        ["rustdesk-rustdesk"] = H(@"^RustDesk$", PF + @"\RustDesk\rustdesk.exe"),
        ["malwarebytes-malwarebytes"] = H(@"^Malwarebytes\b", PF + @"\Malwarebytes\Anti-Malware\Malwarebytes.exe"),
        ["ramensoftware-windhawk"] = H(@"^Windhawk$", PF + @"\Windhawk\windhawk.exe"),
        ["dotpdn-paintdotnet"] = H(@"^paint\.net\b", PF + @"\paint.net\paintdotnet.exe"),
        ["gimp-gimp-3"] = H(@"^GIMP 3\b", PF + @"\GIMP 3\bin\gimp-3.exe", LAD + @"\Programs\GIMP 3\bin\gimp-3.exe"),
        ["blenderfoundation-blender"] = H(@"^Blender\b", PF + @"\Blender Foundation\Blender*"),
        ["kde-krita"] = H(@"^Krita\b", PF + @"\Krita (x64)\bin\krita.exe"),
        ["thedocumentfoundation-libreoffice"] = H(@"^LibreOffice\b", PF + @"\LibreOffice\program\soffice.exe"),
        ["microsoft-visualstudiocode"] = H(@"^Microsoft Visual Studio Code\b", LAD + @"\Programs\Microsoft VS Code\Code.exe", PF + @"\Microsoft VS Code\Code.exe"),
        ["git-git"] = H(@"^Git( version .*)?$", PF + @"\Git\cmd\git.exe"),
        ["github-githubdesktop"] = H(@"^GitHub Desktop$", LAD + @"\GitHubDesktop\GitHubDesktop.exe"),
        ["python-python-3-13"] = H(@"^Python 3\.13\.", LAD + @"\Programs\Python\Python313\python.exe", PF + @"\Python313\python.exe"),
        ["microsoft-powershell"] = H(@"^PowerShell 7\b", PF + @"\PowerShell\7\pwsh.exe"),
    };

    /// <summary>True when one of the app's known install paths exists (a trailing * matches a folder name prefix).</summary>
    public static bool ByPaths(Hint h)
    {
        foreach (var raw in h.Paths)
        {
            try
            {
                var p = Environment.ExpandEnvironmentVariables(raw);
                if (p.Contains('%')) continue;
                if (!p.EndsWith('*')) { if (File.Exists(p)) return true; continue; }
                var dir = Path.GetDirectoryName(p);
                var prefix = Path.GetFileName(p).TrimEnd('*');
                if (dir != null && Directory.Exists(dir) && Directory.EnumerateDirectories(dir, prefix + "*").Any()) return true;
            }
            catch { }
        }
        return false;
    }

    /// <summary>
    /// True when winget installed the app as a portable package: winget keeps those in its own Packages folder,
    /// named after the package id.
    /// </summary>
    public static bool ByWingetPortable(AppDef a)
    {
        try
        {
            foreach (var root in new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinGet", "Packages"),
            })
                if (Directory.Exists(root) && Directory.EnumerateDirectories(root, a.Winget + "_*").Any()) return true;
        }
        catch { }
        return false;
    }

    public static bool Present(AppDef a, List<string> names)
    {
        if (Hints.TryGetValue(a.Id, out var h))
        {
            if (ByPaths(h)) return true;
            if (names.Any(n => h.Names.IsMatch(n.Trim()))) return true;
        }
        return ByWingetPortable(a);
    }

    /// <summary>The first existing .exe among the app's known install paths, for its icon.</summary>
    public static string? Exe(AppDef a)
    {
        if (!Hints.TryGetValue(a.Id, out var h)) return null;
        foreach (var raw in h.Paths)
        {
            try
            {
                var p = Environment.ExpandEnvironmentVariables(raw);
                if (p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(p) && !p.EndsWith("Update.exe", StringComparison.OrdinalIgnoreCase)) return p;
            }
            catch { }
        }
        return null;
    }
}
