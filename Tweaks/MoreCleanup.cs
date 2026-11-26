using System.IO;
using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

/// <summary>Remaining clean-up and personalisation items, using the same exact registry values and commands as the reference tools.</summary>
public static partial class Catalog
{
    const string Deep = "Read each card first";

    static IEnumerable<Tweak> MoreCleanup()
    {
        yield return Once(Mk("clean.oosu", Category.Debloat, "Privacy tool",
            "Open O&O ShutUp10++",
            "Downloads the free privacy tool from its publisher and opens it so you can review its settings.",
            Sometimes,
            L(new CustomAction(
                "Make a restore point and policy backup, download O&O ShutUp10++, check its signature and open it",
                isApplied: () => false,
                apply: () => PrivacyTool.Run())),
            detail: "Before opening it TrapShack makes a restore point and saves a copy of your policy registry keys under ProgramData\\TrapShack\\backups. " +
                    "The download comes from the publisher's own server and only runs if it carries a valid O&O Software signature. " +
                    "If the download is blocked, the last verified copy is used; if there is none, the official page opens instead. " +
                    "Inside the tool, its own Actions menu can undo its changes."));

        yield return Mk("clean.lockscreen", Category.Customize, Appearance,
            "Skip the lock screen",
            "Goes straight to the sign-in screen without the lock screen picture.",
            Sometimes,
            L(new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreen", 1, RegistryValueKind.DWord, null)),
            detail: "Sets the NoLockScreen policy. Restore removes it.");

        yield return Mk("clean.start-recommended", Category.Customize, Appearance,
            "Hide the Start menu recommendations",
            "Removes the Recommended section from Start (Windows 11).",
            s => W11(s, Verdict.Opt()),
            L(LM(@"SOFTWARE\Microsoft\PolicyManager\current\device\Start", "HideRecommendedSection", 1, null),
              LM(@"SOFTWARE\Microsoft\PolicyManager\current\device\Education", "IsEducationEnvironment", 1, null),
              LM(@"SOFTWARE\Policies\Microsoft\Windows\Explorer", "HideRecommendedSection", 1, null)),
            detail: "Works fully on Pro, Enterprise and Education. On Home the setting may be ignored. " +
                    "Side effect: Windows treats the PC as an education device, which also turns off Windows Spotlight pictures on the lock screen.",
            signOut: true);

        yield return Mk("clean.new-outlook", Category.Customize, Appearance,
            "Stay on classic Outlook",
            "Hides the New Outlook switch and stops automatic migration to it.",
            Sometimes,
            L(CU(@"SOFTWARE\Microsoft\Office\16.0\Outlook\Preferences", "UseNewOutlook", 0, null),
              CU(@"Software\Microsoft\Office\16.0\Outlook\Options\General", "HideNewOutlookToggle", 1, null),
              CU(@"Software\Policies\Microsoft\Office\16.0\Outlook\Options\General", "DoNewOutlookAutoMigration", 0, null),
              CU(@"Software\Policies\Microsoft\Office\16.0\Outlook\Preferences", "NewOutlookMigrationUserSetting", 0, null)),
            detail: "Only matters if you use the desktop Outlook from Microsoft 365.");

        yield return Mk("clean.razer-block", Category.Debloat, Deep,
            "Block Razer software auto-install",
            "Stops Windows installing Razer Synapse when a Razer device is plugged in.",
            _ => Verdict.Warn("Also stops Windows downloading drivers automatically for any new device. Install drivers yourself while this is on."),
            L(LM(@"SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", "SearchOrderConfig", 0, 1),
              LM(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Device Installer", "DisableCoInstallers", 1, 0),
              BlockFolder("Razer block", () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer", "Razer"))),
            detail: "Also pins Windows driver search off. Restore re-enables driver search and removes the write-deny rule from the Razer installer folder.",
            risk: Risk.Advanced);

        yield return Mk("clean.logi-block", Category.Debloat, Deep,
            "Block Logitech download assistant",
            "Stops the Logitech download assistant from installing and running at start-up.",
            Sometimes,
            L(BlockFolder("Logitech block", () => Path.Combine(
                Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "LogiDownloadAssistant"), "logi_download_assistant")),
            detail: "Creates the LogiDownloadAssistant folder and denies write access so it cannot reinstall. Restore removes the rule.",
            risk: Risk.Advanced);

        yield return Mk("clean.adobe-block", Category.Debloat, Deep,
            "Block Adobe background connections",
            "Adds the community Adobe block list to the hosts file to cut Adobe's background pop-ups and tracking.",
            Sometimes,
            L(AdobeHostsBlock()),
            detail: "Downloads the Ruddernation-Designs Adobe URL block list when you switch it on and adds it to the hosts file " +
                    "between TrapShack markers. Only plain block lines are kept. Restore removes exactly that block. " +
                    "Adobe apps may then show sign-in or update problems: switch it off if they do.",
            risk: Risk.Advanced);

        yield return Mk("clean.reserved-storage", Category.Debloat, Deep,
            "Turn off reserved storage",
            "Gives back the few GB Windows holds back for updates.",
            Sometimes,
            L(new CustomAction(
                "Run DISM /Online /Set-ReservedStorageState /State:Disabled (restore sets it to Enabled)",
                // Windows' own answer (the same state DISM /Get-ReservedStorageState reports). ShippedWithReserves only
                // records how Windows was set up, and DISM doesn't change it, so it can't tell whether this is on.
                isApplied: () => SlowCheck.Get("reserved-storage.off", () =>
                    Shell.PowerShell("(Get-WindowsReservedStorageState).ReservedStorageState", 60_000).Output.Trim().Equals("Disabled", StringComparison.OrdinalIgnoreCase)),
                apply: () => { SlowCheck.Forget("reserved-storage.off"); Dism("/Online /Set-ReservedStorageState /State:Disabled"); },
                revert: _ => { SlowCheck.Forget("reserved-storage.off"); Dism("/Online /Set-ReservedStorageState /State:Enabled"); })),
            detail: "Updates may fail if the disk is almost full afterwards. Keep some free space.",
            risk: Risk.Advanced, restart: true);

        yield return Mk("clean.widgets", Category.Debloat, Deep,
            "Remove Widgets",
            "Removes the Widgets runtime and web experience pack.",
            s => W11(s, Verdict.Opt()),
            L(new AppxAction("Microsoft.WidgetsPlatformRuntime", "MicrosoftWindows.Client.WebExperience")),
            detail: "Reinstall from the Microsoft Store if you want them back.", store: true);

        yield return Mk("clean.store-search", Category.Debloat, Deep,
            "Stop Store results in Start search",
            "Denies access to the Store search database so Store suggestions stop appearing.",
            Sometimes,
            L(new CustomAction(
                "Deny Everyone access to the Store search database (restore removes the rule)",
                isApplied: () => StoreDbPaths().Any() && StoreDbPaths().All(StoreDbBlocked),
                apply: () => StoreDb(true),
                revert: _ => StoreDb(false))),
            detail: "Uses icacls on Microsoft.WindowsStore's store.db. Restore removes the deny rule.",
            risk: Risk.Advanced);

        yield return Mk("clean.bitlocker", Category.Debloat, Deep,
            "Turn off BitLocker on the system drive",
            "Decrypts the system drive. Do not use without your recovery key.",
            s => Verdict.Warn("Decrypting removes disk encryption protection. Laptops are exposed if lost or stolen."),
            L(new CustomAction(
                "Run Disable-BitLocker on the system drive (restore runs Enable-BitLocker)",
                isApplied: () => SlowCheck.Get("bitlocker.off", () =>
                    Shell.PowerShell("(Get-BitLockerVolume -MountPoint $env:SystemDrive).ProtectionStatus -eq 'On'").Output.Trim().Equals("False", StringComparison.OrdinalIgnoreCase)),
                apply: () => { SlowCheck.Forget("bitlocker.off"); Ps("Disable-BitLocker -MountPoint $env:SystemDrive"); },
                revert: _ => { SlowCheck.Forget("bitlocker.off"); Ps("Enable-BitLocker -MountPoint $env:SystemDrive -EncryptionMethod XtsAes128 -RecoveryPasswordProtector"); })),
            detail: "Decryption takes a while in the background. Restore re-encrypts with a new recovery password you must save.",
            risk: Risk.Advanced,
            confirm: "This decrypts your system drive. Continue?");
    }

    static void Dism(string args)
    {
        var r = Shell.Run("dism.exe", args, 300_000);
        if (!r.Ok) throw new InvalidOperationException($"dism {args} failed: {(r.Output + r.Error).Trim()}");
    }

    static void Ps(string script)
    {
        var r = Shell.PowerShell(script);
        if (!r.Ok) throw new InvalidOperationException((r.Output + r.Error).Trim());
    }

    // ---------------------------------------------------------------------------------------------- hosts file block lists
    const string AdobeListUrl = "https://github.com/Ruddernation-Designs/Adobe-URL-Block-List/raw/refs/heads/master/hosts";
    const string AdobeStart = "# >>> TrapShack Adobe block list (switch off in TrapShack to remove)";
    const string AdobeEnd = "# <<< TrapShack Adobe block list";

    static string HostsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");

    static CustomAction AdobeHostsBlock() => new(
        "Add the Adobe block list to the hosts file between markers (restore removes the block)",
        isApplied: () => File.Exists(HostsPath) && File.ReadAllText(HostsPath).Contains(AdobeStart),
        apply: () =>
        {
            string list;
            using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(20) })
            {
                http.DefaultRequestHeaders.UserAgent.ParseAdd("TrapShack");
                list = http.GetStringAsync(AdobeListUrl).GetAwaiter().GetResult();
            }
            // keep only plain "0.0.0.0 host" / "127.0.0.1 host" lines, nothing else from the download
            var rx = new System.Text.RegularExpressions.Regex(@"^(0\.0\.0\.0|127\.0\.0\.1)\s+[A-Za-z0-9][A-Za-z0-9.\-]*$");
            var lines = list.Split('\n').Select(l => l.Trim()).Where(l => rx.IsMatch(l))
                            .Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"\s+", " ")).Distinct().ToList();
            if (lines.Count < 5) throw new InvalidOperationException("The Adobe block list couldn't be downloaded. Check the internet connection and try again.");
            var text = RemoveBlock(File.Exists(HostsPath) ? File.ReadAllText(HostsPath) : "", AdobeStart, AdobeEnd).TrimEnd();
            text += "\r\n\r\n" + AdobeStart + "\r\n" + string.Join("\r\n", lines) + "\r\n" + AdobeEnd + "\r\n";
            WriteHosts(text);
            Log.Info($"Added {lines.Count} Adobe block entries to the hosts file.");
        },
        revert: _ =>
        {
            if (!File.Exists(HostsPath)) return;
            var old = File.ReadAllText(HostsPath);
            var text = RemoveBlock(old, AdobeStart, AdobeEnd);
            if (text != old) WriteHosts(text.TrimEnd() + "\r\n");
        });

    static string RemoveBlock(string text, string start, string end)
    {
        var i = text.IndexOf(start, StringComparison.Ordinal);
        if (i < 0) return text;
        var j = text.IndexOf(end, i, StringComparison.Ordinal);
        j = j < 0 ? text.Length : j + end.Length;
        return text.Remove(i, j - i);
    }

    static void WriteHosts(string text)
    {
        // write next to it first, then swap in, so a failed write never leaves a half-written hosts file
        var tmp = HostsPath + ".trapshack.tmp";
        File.WriteAllText(tmp, text, new System.Text.UTF8Encoding(false));
        File.Copy(tmp, HostsPath, true);
        File.Delete(tmp);
        Shell.Run("ipconfig.exe", "/flushdns", 15_000);
    }

    static void Icacls(string args)
    {
        var r = Shell.Run("icacls.exe", args, 60_000);
        Log.Info($"icacls {args} -> exit {r.ExitCode}: {(r.Output + r.Error).Trim()}");
        if (!r.Ok) throw new InvalidOperationException($"icacls failed: {(r.Output + r.Error).Trim()}");
    }

    static CustomAction BlockFolder(string name, Func<string> path, string? killProcess = null) => new(
        $"{name}: deny write access to the installer folder (restore removes the rule)",
        isApplied: () => IsDenied(path()),
        apply: () =>
        {
            if (killProcess != null) Shell.Run("taskkill.exe", $"/f /im {killProcess}.exe", 15_000);
            var p = path();
            if (Directory.Exists(p))
            {
                foreach (var f in Directory.GetFiles(p)) { try { File.Delete(f); } catch { } }
                foreach (var d in Directory.GetDirectories(p)) { try { Directory.Delete(d, true); } catch { } }
            }
            else Directory.CreateDirectory(p);
            Icacls($"\"{p}\" /deny \"*S-1-1-0:(W)\"");
        },
        revert: _ =>
        {
            var p = path();
            if (Directory.Exists(p)) Icacls($"\"{p}\" /remove:d \"*S-1-1-0\"");
        });

    static string EveryoneName()
    {
        try
        {
            var name = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null)
                .Translate(typeof(System.Security.Principal.NTAccount)).Value;
            return name.Contains('\\') ? name[(name.LastIndexOf('\\') + 1)..] : name;
        }
        catch { return "Everyone"; }
    }

    static bool IsDenied(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (!Directory.Exists(path) && !(parent != null && Directory.Exists(parent) && Directory.GetFileSystemEntries(parent, Path.GetFileName(path)).Length > 0)) return false;
        var r = Shell.Run("icacls.exe", $"\"{path}\"", 30_000);
        // "Everyone" has another name on non-English Windows ("Jeder", "Tout le monde"): ask Windows for it
        var everyone = EveryoneName() + ":";
        return r.Ok && r.Output.Split('\n').Any(l => l.Contains(everyone, StringComparison.OrdinalIgnoreCase) && l.Contains("(DENY)"));
    }

    /// <summary>
    /// Every Store search database for this account. Found by listing the folder: once the block is in place, Windows
    /// refuses even "does this file exist?", so File.Exists would wrongly answer no.
    /// </summary>
    static IEnumerable<string> StoreDbPaths()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
        if (!Directory.Exists(root)) yield break;
        foreach (var pkg in Directory.GetDirectories(root, "Microsoft.WindowsStore_*"))
        {
            var dir = Path.Combine(pkg, "LocalState");
            if (!Directory.Exists(dir)) continue;
            string[] hits;
            try { hits = Directory.GetFiles(dir, "store.db"); } catch { continue; }
            foreach (var db in hits) yield return db;
        }
    }

    /// <summary>
    /// The block works when Windows refuses to open the file, so that is the test: try to open it for reading.
    /// "Access denied" means it's blocked. (A file that another program has open is not blocked.)
    /// </summary>
    static bool StoreDbBlocked(string db)
    {
        try
        {
            using var f = new FileStream(db, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return false;
        }
        catch (UnauthorizedAccessException) { return true; }
        catch (Exception ex)
        {
            Log.Info($"Store search block check: {db}: {ex.GetType().Name}: {ex.Message}. Asking icacls instead.");
            return IsDenied(db);
        }
    }

    static void StoreDb(bool deny)
    {
        // Like Win11Debloat: where the database doesn't exist yet (fresh accounts, EU PCs), create it empty first so
        // Windows can't create it later without the block.
        if (deny && !StoreDbPaths().Any())
        {
            var db = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", "Microsoft.WindowsStore_8wekyb3d8bbwe", "LocalState", "store.db");
            Directory.CreateDirectory(Path.GetDirectoryName(db)!);
            if (!File.Exists(db)) File.Create(db).Dispose();
        }
        foreach (var db in StoreDbPaths())
        {
            Icacls(deny ? $"\"{db}\" /deny \"*S-1-1-0:F\"" : $"\"{db}\" /remove:d \"*S-1-1-0\"");
        }
    }
}
