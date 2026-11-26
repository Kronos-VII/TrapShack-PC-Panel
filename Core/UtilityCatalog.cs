using System.IO;

namespace TrapShack.Core;

/// <summary>One command a utility runs. {sys} becomes the system drive (C:), {temp} the temp folder.</summary>
public sealed record UtilStep(string Label, string File, string Args, bool Unicode = false, bool AnyExitIsFine = false);

/// <summary>
/// One utility on the Utilities page. Either it runs <see cref="Steps"/> in the built-in terminal, or (when
/// <see cref="Open"/> is set and there are no steps) it simply opens a Windows tool.
/// </summary>
public sealed record UtilTool(string Id, string Section, string Title, string Desc, string Glyph, UtilStep[] Steps,
    string? Confirm = null, bool Restart = false, string? Open = null, string? OpenArgs = null, string? OpenAfter = null, bool LaptopOnly = false);

/// <summary>A shortcut to a built-in Windows tool.</summary>
public sealed record WinTool(string Name, string Glyph, string Target, string? Args = null);

/// <summary>
/// Everything on the Utilities page. All commands are Windows' own tools with their documented switches; nothing is
/// downloaded. Order inside each utility matters (for example DISM repairs the image before SFC uses it).
/// </summary>
public static class UtilityCatalog
{
    const string PS = "powershell.exe";
    static string Ps(string script) => "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "\\\"") + "\"";

    public static readonly (string Name, string Glyph, string Blurb)[] Sections =
    {
        ("Repair Windows", "", "Fix damaged system files and Windows Update"),
        ("Network", "", "Fix slow, dropped or broken connections"),
        ("DNS", "\uE774", "Pick a faster or more private DNS server"),
        ("Drives & storage", "", "Keep drives fast and healthy"),
        ("Gaming & audio", "", "Xbox, Game Pass and sound fixes"),
        ("Checks & reports", "", "See what your PC is doing"),
    };

    // sets one DNS provider (IPv4 and IPv6) on every connected adapter, then clears the cache
    static UtilTool Dns(string id, string title, string desc, string servers) => new(id, "DNS", title, desc, "\uE774", new[]
    {
        new UtilStep("Set DNS on connected adapters", PS, Ps(
            "Get-NetAdapter | Where-Object Status -eq 'Up' | ForEach-Object { " +
            "Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses (" + servers + "); " +
            "Write-Output ($_.Name + ': ' + ((" + servers + ") -join ', ')) }")),
        new UtilStep("Clear the DNS cache", "ipconfig.exe", "/flushdns"),
    }, Confirm: "This sets " + title + " as the DNS server on every connected network adapter. \"Automatic\" puts Windows' default back.");

    public static readonly UtilTool[] Tools =
    {
        // ------------------------------------------------------------------ Repair Windows
        new("repair-system", "Repair Windows", "Full system repair",
            "Checks the Windows image, repairs it from Windows Update, then repairs system files. Microsoft's recommended order. 10 to 30 minutes.",
            "", new[]
            {
                new UtilStep("Check the Windows image", "DISM.exe", "/Online /Cleanup-Image /ScanHealth"),
                new UtilStep("Repair the Windows image", "DISM.exe", "/Online /Cleanup-Image /RestoreHealth"),
                new UtilStep("Repair system files", "sfc.exe", "/scannow", Unicode: true),
            },
            Confirm: "This takes 10 to 30 minutes and needs an internet connection for the repair step. You can keep using the PC, but leave TrapShack open."),
        new("sfc", "Repair Windows", "Quick system file check",
            "Runs System File Checker on its own. Finds and replaces damaged Windows files. About 5 to 15 minutes.",
            "", new[] { new UtilStep("Repair system files", "sfc.exe", "/scannow", Unicode: true) }),
        new("component-cleanup", "Repair Windows", "Clean up old update files",
            "Removes superseded Windows update components to free space. Installed updates keep working.",
            "", new[] { new UtilStep("Clean up the component store", "DISM.exe", "/Online /Cleanup-Image /StartComponentCleanup") }),
        new("wu-reset", "Repair Windows", "Reset Windows Update",
            "For updates that fail or get stuck. Stops the update services, sets aside the download cache, then starts them again.",
            "", new[]
            {
                new UtilStep("Stop Windows Update", "net.exe", "stop wuauserv /y", AnyExitIsFine: true),
                new UtilStep("Stop background transfers", "net.exe", "stop bits /y", AnyExitIsFine: true),
                new UtilStep("Stop cryptographic services", "net.exe", "stop cryptsvc /y", AnyExitIsFine: true),
                new UtilStep("Set aside the update cache", PS, Ps(
                    "$t = Get-Date -Format yyyyMMddHHmmss; " +
                    "Rename-Item -Path (Join-Path $env:SystemRoot 'SoftwareDistribution') -NewName ('SoftwareDistribution.old' + $t) -ErrorAction Stop; " +
                    "Rename-Item -Path (Join-Path $env:SystemRoot 'System32\\catroot2') -NewName ('catroot2.old' + $t) -ErrorAction Stop; " +
                    "Write-Output 'Update cache folders renamed. Windows creates fresh ones.'")),
                new UtilStep("Start cryptographic services", "net.exe", "start cryptsvc", AnyExitIsFine: true),
                new UtilStep("Start background transfers", "net.exe", "start bits", AnyExitIsFine: true),
                new UtilStep("Start Windows Update", "net.exe", "start wuauserv", AnyExitIsFine: true),
            },
            Confirm: "Windows Update will download its updates again from scratch. Your installed updates and update history stay.", Restart: true),
        new("store-reset", "Repair Windows", "Reset Microsoft Store",
            "Clears the Store's cache, Microsoft's fix for a Store that won't open, load or download.",
            "", new[] { new UtilStep("Clear the Store cache", "wsreset.exe", "", AnyExitIsFine: true) }),
        new("restart-explorer", "Repair Windows", "Restart File Explorer",
            "Fixes a frozen taskbar, Start menu or desktop without restarting the PC.",
            "", new[]
            {
                new UtilStep("Close File Explorer", "taskkill.exe", "/f /im explorer.exe", AnyExitIsFine: true),
                new UtilStep("Start File Explorer", "cmd.exe", "/c start \"\" explorer.exe", AnyExitIsFine: true),
            }),

        // ------------------------------------------------------------------ Network
        new("net-quick", "Network", "Quick network fix",
            "Clears the DNS cache and gets a fresh address from your router. Your connection drops for a few seconds.",
            "", new[]
            {
                new UtilStep("Clear the DNS cache", "ipconfig.exe", "/flushdns"),
                new UtilStep("Release the address", "ipconfig.exe", "/release", AnyExitIsFine: true),
                new UtilStep("Get a new address", "ipconfig.exe", "/renew", AnyExitIsFine: true),
            }),
        new("net-reset", "Network", "Full network reset",
            "Resets Winsock and TCP/IP to Windows' defaults and clears DNS. For connections nothing else fixes. Restart afterwards.",
            "", new[]
            {
                new UtilStep("Reset Winsock", "netsh.exe", "winsock reset"),
                new UtilStep("Reset TCP/IP", "netsh.exe", "int ip reset", AnyExitIsFine: true),
                new UtilStep("Clear the DNS cache", "ipconfig.exe", "/flushdns"),
            },
            Confirm: "This resets Windows' network stack to its defaults. Adapters set to a static IP address or custom DNS may need setting again, and some VPN programs need reinstalling. A restart is needed to finish.",
            Restart: true),
        new("dns-flush", "Network", "Clear DNS cache",
            "Fixes sites that won't load or load the old version after a change.",
            "", new[] { new UtilStep("Clear the DNS cache", "ipconfig.exe", "/flushdns") }),
        new("net-test", "Network", "Test my connection",
            "Pings a public server and a website, so you can see whether the problem is the internet or DNS.",
            "", new[]
            {
                new UtilStep("Reach the internet (1.1.1.1)", "ping.exe", "-n 4 1.1.1.1", AnyExitIsFine: true),
                new UtilStep("Reach a website by name", "ping.exe", "-n 4 www.microsoft.com", AnyExitIsFine: true),
            }),
        new("net-info", "Network", "Show network details",
            "Lists every adapter with its address, gateway and DNS servers.",
            "", new[] { new UtilStep("Network details", "ipconfig.exe", "/all") }),

        // ------------------------------------------------------------------ DNS
        Dns("dns-cloudflare", "Cloudflare (1.1.1.1)", "Usually the fastest. Doesn't sell or log your lookups long term.",
            "'1.1.1.1','1.0.0.1','2606:4700:4700::1111','2606:4700:4700::1001'"),
        Dns("dns-google", "Google (8.8.8.8)", "Fast and very reliable everywhere.",
            "'8.8.8.8','8.8.4.4','2001:4860:4860::8888','2001:4860:4860::8844'"),
        Dns("dns-quad9", "Quad9 (9.9.9.9)", "Blocks known malware and phishing sites. Run by a non-profit.",
            "'9.9.9.9','149.112.112.112','2620:fe::fe','2620:fe::9'"),
        new("dns-auto", "DNS", "Automatic (Windows default)",
            "Goes back to the DNS servers your router or network hands out. This is how Windows comes.",
            "\uE777", new[]
            {
                new UtilStep("Reset DNS on connected adapters", PS, Ps(
                    "Get-NetAdapter | Where-Object Status -eq 'Up' | ForEach-Object { " +
                    "Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses; " +
                    "Write-Output ($_.Name + ': automatic DNS') }")),
                new UtilStep("Clear the DNS cache", "ipconfig.exe", "/flushdns"),
            }),
        new("dns-speed", "DNS", "Test DNS speed",
            "Times each DNS server answering the same lookups from this PC, so you can pick the fastest one for you.",
            "\uEC4A", new[]
            {
                new UtilStep("Time each DNS server", PS, Ps(
                    "$names = 'www.microsoft.com','www.youtube.com','www.discord.com','store.steampowered.com','www.reddit.com'; " +
                    "$servers = [ordered]@{ 'Cloudflare' = '1.1.1.1'; 'Google' = '8.8.8.8'; 'Quad9' = '9.9.9.9' }; " +
                    "$now = (Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object { $_.ServerAddresses } | Select-Object -First 1).ServerAddresses | Select-Object -First 1; " +
                    "if ($now) { $servers['Current (' + $now + ')'] = $now }; " +
                    "foreach ($k in $servers.Keys) { $ms = @(); foreach ($n in $names) { " +
                    "try { $ms += (Measure-Command { Resolve-DnsName $n -Server $servers[$k] -DnsOnly -NoHostsFile -QuickTimeout -ErrorAction Stop | Out-Null }).TotalMilliseconds } catch { } }; " +
                    "if ($ms.Count) { Write-Output ('{0,-28} {1,6:N0} ms average  ({2}/{3} answered)' -f $k, ($ms | Measure-Object -Average).Average, $ms.Count, $names.Count) } " +
                    "else { Write-Output ('{0,-28} no answer' -f $k) } }")),
            }),

        // ------------------------------------------------------------------ Drives
        new("optimize-all", "Drives & storage", "Optimise all drives",
            "Does what Windows' Optimise Drives does: TRIM for SSDs and defragmenting for hard drives.",
            "", new[] { new UtilStep("Optimise every drive", "defrag.exe", "/C /O /U") }),
        new("chkdsk", "Drives & storage", "Check the system drive",
            "Scans the Windows drive for file system errors while you keep working. Nothing is changed.",
            "", new[] { new UtilStep("Scan for errors", "chkdsk.exe", "{sys} /scan", AnyExitIsFine: true) }),
        new("open-defrag", "Drives & storage", "Optimise Drives window", "Windows' own Optimise Drives tool, with the schedule.",
            "", Array.Empty<UtilStep>(), Open: "dfrgui.exe"),
        new("open-cleanmgr", "Drives & storage", "Disk Cleanup", "Windows' classic Disk Cleanup, including system files.",
            "", Array.Empty<UtilStep>(), Open: "cleanmgr.exe"),

        // ------------------------------------------------------------------ Gaming & audio
        new("xbox-fix", "Gaming & audio", "Fix Xbox and Game Pass services",
            "Puts the Xbox services back to their Windows defaults and starts Gaming Services. Fixes sign-in, cloud saves and game installs.",
            "", new[]
            {
                new UtilStep("Xbox services back to Windows defaults", PS, Ps(
                    "foreach ($s in 'XblAuthManager','XblGameSave','XboxGipSvc','XboxNetApiSvc') { " +
                    "if (Get-Service -Name $s -ErrorAction SilentlyContinue) { Set-Service -Name $s -StartupType Manual; Write-Output ($s + ' set to Manual (Windows default)') } " +
                    "else { Write-Output ($s + ' is not on this PC') } }")),
                new UtilStep("Start Gaming Services", PS, Ps(
                    "foreach ($s in 'GamingServices','GamingServicesNet') { " +
                    "if (Get-Service -Name $s -ErrorAction SilentlyContinue) { Start-Service -Name $s -ErrorAction SilentlyContinue; Write-Output ($s + ': ' + (Get-Service -Name $s).Status) } " +
                    "else { Write-Output ($s + ' is not installed. Use Reinstall Gaming Services.') } }"), AnyExitIsFine: true),
            }),
        new("gaming-services", "Gaming & audio", "Reinstall Gaming Services",
            "Microsoft's fix when Game Pass games won't install or launch: removes Gaming Services, then opens the Store to install it again.",
            "", new[]
            {
                new UtilStep("Remove Gaming Services", PS, Ps(
                    "Get-AppxPackage -AllUsers -Name 'Microsoft.GamingServices' | Remove-AppxPackage -AllUsers -ErrorAction Continue; " +
                    "Write-Output 'Gaming Services removed. Install it again from the Store page that opens.'"), AnyExitIsFine: true),
            },
            Confirm: "Gaming Services is removed, then the Microsoft Store opens on its page so you can install it again. Your games are not removed.",
            OpenAfter: "ms-windows-store://pdp/?productid=9MWPM2CQNLHN"),
        new("audio-restart", "Gaming & audio", "Restart Windows audio",
            "Fixes no sound, crackling or a missing device without restarting the PC.",
            "", new[]
            {
                new UtilStep("Restart the audio services", PS, Ps(
                    "Restart-Service -Name AudioEndpointBuilder -Force; Start-Service -Name Audiosrv; " +
                    "Write-Output ('Windows Audio: ' + (Get-Service -Name Audiosrv).Status)")),
            }),
        new("open-sound", "Gaming & audio", "Sound control panel", "The classic sound panel (mmsys.cpl): default devices, formats and enhancements.",
            "", Array.Empty<UtilStep>(), Open: "control.exe", OpenArgs: "mmsys.cpl"),
        new("open-gamebar", "Gaming & audio", "Game Bar and capture settings", "Xbox Game Bar, captures and Game Mode in Settings.",
            "", Array.Empty<UtilStep>(), Open: "ms-settings:gaming-gamebar"),

        // ------------------------------------------------------------------ Checks & reports
        new("battery-report", "Checks & reports", "Battery health report",
            "Shows your battery's design capacity against what it holds now, and its usage history.",
            "", new[] { new UtilStep("Create the battery report", "powercfg.exe", "/batteryreport /output \"{temp}\\battery-report.html\"") },
            OpenAfter: "{temp}\\battery-report.html", LaptopOnly: true),
        new("open-reliability", "Checks & reports", "Reliability history", "A timeline of crashes, failed updates and app errors.",
            "", Array.Empty<UtilStep>(), Open: "perfmon.exe", OpenArgs: "/rel"),
        new("open-memtest", "Checks & reports", "Memory test", "Windows Memory Diagnostic checks your RAM on the next restart.",
            "", Array.Empty<UtilStep>(), Open: "mdsched.exe"),
        new("open-dxdiag", "Checks & reports", "DirectX diagnostics", "Graphics, sound and DirectX details, handy for game problems.",
            "", Array.Empty<UtilStep>(), Open: "dxdiag.exe"),
    };

    public static readonly WinTool[] WindowsTools =
    {
        new("Device Manager", "", "devmgmt.msc"),
        new("Disk Management", "", "diskmgmt.msc"),
        new("Services", "", "services.msc"),
        new("Task Manager", "", "taskmgr.exe"),
        new("Event Viewer", "", "eventvwr.msc"),
        new("Resource Monitor", "", "resmon.exe"),
        new("System Configuration", "", "msconfig.exe"),
        new("System Properties", "", "sysdm.cpl"),
        new("Network Connections", "", "ncpa.cpl"),
        new("Power Options", "", "powercfg.cpl"),
        new("Programs and Features", "", "appwiz.cpl"),
        new("Registry Editor", "", "regedit.exe"),
        new("Computer Management", "", "compmgmt.msc"),
        new("Optional Features", "", "optionalfeatures.exe"),
        new("Windows Security", "", "windowsdefender:"),
        new("Windows Update", "", "ms-settings:windowsupdate"),
        new("Startup Apps", "", "ms-settings:startupapps"),
        new("Display", "", "ms-settings:display"),
    };

    /// <summary>Fills in {sys} and {temp}.</summary>
    public static string Expand(string s) => s
        .Replace("{sys}", Environment.GetEnvironmentVariable("SystemDrive") ?? "C:")
        .Replace("{temp}", Path.GetTempPath().TrimEnd('\\'));
}
