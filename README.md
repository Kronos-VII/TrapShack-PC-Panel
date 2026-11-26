# TrapShack Tweak Utility

A native Windows 10/11 tweak and debloat utility (WPF, .NET 8). Black and red theme, sidebar pages, every setting is a toggle with a
proper restore, and everything recommended is chosen from what it detects on your PC.

## Build
Unzip into an empty folder and double-click `build.bat`. It checks for the .NET 8 SDK (offers to install it), restores packages
(internet needed the first time), builds a single `TrapShack Tweak Utility.exe` into `dist\` and opens that folder.
Options: `build.bat -Run`, `-Clean`, `-Debug`. If a build fails, send `build.log`.

## Pushing to GitHub
Double-click `push.bat`. First run it sets up git here and points it at https://github.com/Kronos-VII/TrapShack-PC-Panel, then asks for a one-line description, commits and pushes.

## Updating the app
Settings > Updates > Check for updates. TrapShack reads the newest GitHub release, and if its version is higher than the running one it downloads the attached .exe, closes, swaps itself and reopens. To ship an update: run `push.bat` (saves the code), then double-click `release.bat`. It raises the version number, builds, asks what changed and publishes the release. It installs and signs in to the GitHub CLI the first time.

## Layout
```
Assets/    logo, icon, app logos (AppLogos), cleaner rules (Cleaner), optional brand marks (Brand, e.g. github.png)
Core/      engine: actions, apply/revert, backups (state.json), hardware detection, live sensors
Theme/     colours, buttons, tooltips
Tweaks/    every setting, by area (edit or add settings here)
Ui/        page list (PageDefs.cs), what "recommended" means (Recommended.cs), view models
```

## Pages
* **Home** (a small start page), **System info** (hardware details), **Temperatures** (live CPU, GPU and memory with a minute of history) and **Search settings**.
* **List pages** (Debloat, Optimizations, Apps, Debloat apps, PC Cleaner, Utilities, Startup apps, Restore points, Settings): the categories are listed on the left with their counts; the chosen category's settings show on the right as 64 px rows in two columns. Search looks through every category. Optimizations is one page: its areas are listed gaming first, updates last; Debloat lists Gaming first and Windows AI and Windows Update last.
* **Utilities**: Windows' own repair tools (DISM then SFC, Windows Update reset, network fixes, drive optimise, Xbox and audio fixes, reports) run in a pop-up terminal that only opens while a tool runs, plus shortcuts to Windows tools. Edit `Core/UtilityCatalog.cs` to add more.
* **PC Cleaner**: scans for temporary files and caches with the safe winapp2.ini rules, then cleans what you keep ticked (see Assets/Cleaner/LICENSES.txt).
* **Settings** has a colour wheel for the accent colour (the whole app recolours as you drag), a **Light theme** switch (soft grey, the terminal stays dark),
  and **Profiles**: *Save profile* writes the settings that are on to a `.tsprofile` file; *Load profile* applies that file on another PC
  (restore point first, settings that don't suit that PC are skipped).
* **Utilities → DNS**: switch every connected adapter to Cloudflare, Google or Quad9 (IPv4 and IPv6), back to Automatic, or time each server from this PC.
* **Debloat**: one page of category cards (privacy, Windows AI, Start menu, taskbar, File Explorer, system ...) with switches and drop-downs, plus **Debloat apps** and **Apps**. Edit `Tweaks/DebloatLayout.cs` to change the cards.
* **Optimizations**: a hub page that opens Gaming & Performance, Privacy & Security, Notifications, Power, Sound, Update and Customization.
Edit `Ui/PageDefs.cs` to rename, reorder or move pages.

## Apply recommended, Undo, Revert
Each page and hub has a **Quick actions** menu: *Apply recommended settings* and *Revert all to Windows defaults*. Applying creates a
restore point first, is all-or-nothing per setting, and survives restarts (settings are written to the registry, services and tasks).
Debloat's recommended set is the list in `Ui/Recommended.cs` (the standard debloat set plus the second source's safe-for-everyone defaults). Disk Cleanup and temp files are one-time "Run now" actions. On Optimizations pages, "recommended" follows the published per-setting recommended values: settings with id `opt.*` carry their own
verdict, and `Recommended.OptimizationRecommended` lists the others. Every setting that has a recommended value counts, including
ones the source marks as a personal preference. The exceptions are Caution items (security features, printing, UAC and similar), which are never applied automatically.

## Safety
* Settings that don't suit your hardware (wrong CPU or GPU vendor, laptop, virtual machine) are warned about or locked.
* TrapShack saves the original value of everything it changes in `%ProgramData%\TrapShack\state.json`, and logs to
  `%ProgramData%\TrapShack\trapshack.log`.
* The Store, App Installer and the Xbox sign-in pieces are never removed.
* **O&O ShutUp10++** (Debloat → Privacy) is downloaded from O&O's own server, run only if its O&O Software signature checks out, and opened after a restore point and a backup of your policy keys.
* Single switches and Run now buttons work quietly (the switch greys out while it works and only problems pop up). The progress panel only appears for batches: Apply recommended, Revert all, loading a profile.
* The Adobe block list (Debloat → Browsers & apps) is written between marker lines in the hosts file; switching it off removes exactly that block.

## System info and temperatures
Uses the `LibreHardwareMonitorLib` package. CPU sensors need its driver, which Windows may block with Memory Integrity on; the page
says so and everything else keeps working.

System info shows one card each for Windows, the processor, every graphics card, memory, the motherboard, storage, the network
connection in use (link speed and DNS) and security (Secure Boot, TPM, virtualization security and Memory integrity). It only reads; nothing
on it changes the PC. **Copy specs** copies the hardware as a short text block for Discord, without the PC name or network details.
Temperatures reads memory straight from Windows, so its total matches System info and Task Manager.

## Debloat apps
Lists the Windows apps found on this PC (from `Core/WinAppCatalog.cs`) with a label on each: *Reinstall from Store*, *Core app*, *Can't be reinstalled* (red) or *Keep* (locked: the Store, App Installer and the Xbox sign-in pieces are never removable). Removal asks first, names anything that can't come back, removes the app for all users and from new accounts, and then offers a restart.

## Apps
Installs game launchers, game runtimes (Visual C++ 2005-2022 in 64 and 32-bit, DirectX, .NET Desktop, WebView2, Java) and everyday apps through **winget** (`Core/WingetService.cs`, catalog in `Core/AppCatalog.cs`).
* Already-installed apps are detected (winget list, program files and the uninstall list) and show an *Installed* mark instead of a check box. Picks are remembered between runs, and cleared once installed.
* Each install shows name, "3 of 19", size and percent. A failed install is retried once automatically; anything that still fails stays picked with a *Retry* button.
* If winget is missing, installing is switched off with a note; detection still works.
* Logos come from the CC0 Simple Icons set (`Core/AppIcons.cs`); apps without one get a monogram tile. All brand names and logos belong to their owners.


## Settings Windows changed back
Big Windows updates sometimes reset settings. When a setting TrapShack applied (and saw working) reads as off again, Home shows one
small card listing them, with *Turn back on* and *Keep as is*. It never pops up, and it stays hidden when nothing was reset.

## Speed
* The program is compiled ahead of time (ReadyToRun) and isn't compressed, so nothing is unpacked or compiled when it opens.
  The .exe is larger because of this.
* Startup asks the Task Scheduler and the registry directly instead of PowerShell/DISM where it can (scheduled tasks,
  provisioned apps). The slow hardware checks (drive type, Recall, graphics names) are remembered per Windows build and
  re-read quietly after the window is up.
* After the window shows, background work starts one piece at a time: installed apps, then the temperature sensors.
* While the app isn't the focused window (a game is running, or it's minimised) it does no re-checks at all, and no
  hidden animation keeps drawing frames. Checks that need PowerShell (BitLocker, Defender sample submission) are kept
  for ten minutes and refreshed straight after any change to them.

## PC Cleaner and shader caches
GPU and game shader caches (NVIDIA, AMD, Intel, DirectX, Steam's shader pre-cache and games' own shader folders) are
listed as their own rules and are **not ticked by default**: deleting them makes games rebuild their shaders, which
stutters for a while. They are still measured, so you can see their size and tick them when you want them gone.

## Testing everything
**Test everything.bat** (stays on your PC, never pushed) is the one test. Double-click it and leave its window open:

1. It builds the app.
2. **This PC, reads only.** Windows asks for permission (Yes). The app reads every setting's state, the hardware,
   the sensors, startup apps, restore points, what the cleaner would find (nothing is deleted), winget and the
   Utilities' programs, then opens every page once and saves a picture of it. Every value the app can change is read
   before and after, and the report shows they're identical. Nothing is switched, removed, cleaned or installed.
3. **Windows Sandbox, everything for real** (a throwaway copy of Windows, wiped when it closes): every page, every
   setting switched on, checked, switched off and checked to be exactly as before, then every feature, every utility,
   the one-time actions and the app removals. It refuses to run anywhere but the Sandbox or a GitHub build machine.
4. Everything is packed into **TrapShack test results.zip** next to the .bat. Send that one file.

Needs Windows Sandbox turned on for part 3 (Windows 11 Pro: "Turn Windows features on or off" > Windows Sandbox);
without it, parts 1, 2 and 4 still run. Only left out, everywhere: decrypting BitLocker, opening O&O ShutUp10++'s own
window (it's downloaded and its signature checked), and the 10 to 30 minute Windows repairs (DISM and SFC, and Disk
Cleanup, which ends with the same DISM cleanup), whose programs are checked to exist.

**GitHub**: every push builds the program and runs the settings test on a Windows build machine
(`.github/workflows/build-and-test.yml`). The finished program and the report are attached to each run under Actions.
