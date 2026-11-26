using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

// GENERATED from published optimization definitions (settings, recommended values and defaults).
// Rewritten as TrapShack tweaks. Every setting with a recommended value is part of the recommended set, and a few that
// weaken security or break common features are marked Caution and never applied by "Apply recommended".
public static partial class Catalog
{
    static IEnumerable<Tweak> OptimizationSettings()
    {
        yield return Mk("opt.gaming-game-mode", Category.Gaming, "Game experience",
            "Game Mode",
            "Optimize your PC for play by turning things off in the background",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\GameBar", "AllowAutoGameMode", 1, RegistryValueKind.DWord, null)),
            detail: "Optimize your PC for play by turning things off in the background. Recommended for most PCs.");

        yield return Mk("opt.gaming-performance-search-webview2", Category.Performance, "Performance tweaks",
            "Turn off WebView2 in Windows Search",
            "Allow Windows Search to use WebView2 (Edge) for rendering search results. Disabling removes Edge processes spawned by SearchHost.exe, reducing…",
            _ => Verdict.Rec("Uses an undocumented Windows feature override that a Windows update may change."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FeatureManagement\\Overrides\\8\\1694661260", "EnabledState", 1, RegistryValueKind.DWord, 2),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FeatureManagement\\Overrides\\8\\1694661260", "EnabledStateOptions", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FeatureManagement\\Overrides\\8\\1694661260", "Variant", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FeatureManagement\\Overrides\\8\\1694661260", "VariantPayload", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FeatureManagement\\Overrides\\8\\1694661260", "VariantPayloadKind", 0, RegistryValueKind.DWord, null)),
            detail: "Allow Windows Search to use WebView2 (Edge) for rendering search results. Disabling removes Edge processes spawned by SearchHost.exe, reducing resource usage. Uses an undocumented Windows Feature Management override (feature ID 37926450) that may change in future Windows updates. Recommended for most PCs.");

        yield return Mk("opt.gaming-nvidia-sharpening", Category.Gaming, "Graphics",
            "Legacy NVIDIA Sharpening",
            "Enable legacy NVIDIA image sharpening filter for enhanced visual clarity. Only works on older NVIDIA drivers; newer drivers should use NVIDIA…",
            s => s.HasNvidiaGpu ? Verdict.Rec() : Verdict.No("Needs an NVIDIA graphics card."),
            L(new RegistryAction(RegistryHive.LocalMachine, "Software\\NVIDIA Corporation\\Global\\FTS", "EnableGR535", 0, RegistryValueKind.DWord, 1)),
            detail: "Enable legacy NVIDIA image sharpening filter for enhanced visual clarity. Only works on older NVIDIA drivers; newer drivers should use NVIDIA Control Panel sharpening instead. Part of the recommended set.");

        yield return Mk("opt.gaming-virtualization-based-security", Category.Privacy, "Security",
            "Turn off Virtualization Based Security (VBS)",
            "Isolates parts of memory to protect the system from vulnerabilities. Disabling can improve gaming performance but reduces system security",
            _ => Verdict.Warn("Turns off a Windows security feature (VBS). Not worth it unless you know your game needs it."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "RequirePlatformSecurityFeatures", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "Locked", 0, RegistryValueKind.DWord, 0)),
            detail: "Isolates parts of memory to protect the system from vulnerabilities. Disabling can improve gaming performance but reduces system security. TrapShack never applies this automatically.", restart: true, risk: Risk.Advanced);

        yield return Mk("opt.gaming-memory-integrity", Category.Privacy, "Security",
            "Turn off Memory Integrity (HVCI)",
            "Prevents malicious code from being inserted into high-security processes. Disabling can improve gaming performance but reduces system security",
            _ => Verdict.Warn("Turns off Memory Integrity, a Windows security feature. Leave on unless a driver forces you to."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Enabled", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Locked", 0, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "WasEnabledBy", 0, RegistryValueKind.DWord, 2)),
            detail: "Prevents malicious code from being inserted into high-security processes. Disabling can improve gaming performance but reduces system security. TrapShack never applies this automatically.", restart: true, risk: Risk.Advanced);

        yield return Mk("opt.gaming-performance-background-services", Category.Performance, "System services",
            "Optimize Background Services",
            "Reduce the startup timeout for Windows services from 60 to 30 seconds. This can speed up boot time slightly",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control", "ServicesPipeTimeout", 30000, RegistryValueKind.DWord, 60000)),
            detail: "Reduce the startup timeout for Windows services from 60 to 30 seconds. This can speed up boot time slightly. Recommended for most PCs.");

        yield return Mk("opt.gaming-performance-prefetch", Category.Performance, "System services",
            "Turn off Prefetch",
            "Preload frequently used applications and boot files into memory to speed up launches. Generally recommended for HDDs not SSDs",
            s => s.SystemDriveIsSsd switch
            {
                true => Verdict.Rec("Windows is on an SSD, where Prefetch gives almost nothing."),
                false => Verdict.Warn("Slows app launches on a hard disk."),
                _ => Verdict.Opt(),
            },
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnablePrefetcher", 0, RegistryValueKind.DWord, 3)),
            detail: "Preload frequently used applications and boot files into memory to speed up launches. Generally recommended for HDDs not SSDs. Recommended only when Windows is on an SSD.", risk: Risk.Advanced);

        yield return Mk("opt.gaming-print-spooler-service", Category.Performance, "System services",
            "Print Spooler Service",
            "Manages print jobs sent to printers. If you don't use a printer, set to Manual or Disabled to free up system resources",
            _ => Verdict.Warn("Printing stops working until the spooler is started. Only use if you never print."),
            L(new ServiceAction("Spooler", "demand", "auto")),
            detail: "Manages print jobs sent to printers. If you don't use a printer, set to Manual or Disabled to free up system resources. TrapShack never applies this automatically.", restart: true, risk: Risk.Advanced);

        yield return Mk("opt.gaming-connected-devices-platform-service", Category.Performance, "System services",
            "Connected Devices Platform Service",
            "Enables cross-device experiences like phone linking and nearby sharing. Disabling reduces background activity and device interaction logging",
            Always,
            L(new ServiceAction("CDPSvc", "demand", "auto"),
              new ServiceAction("CDPUserSvc", "demand", "auto")),
            detail: "Enables cross-device experiences like phone linking and nearby sharing. Disabling reduces background activity and device interaction logging. Part of the recommended set.", restart: true, confirm: "Manual or Disabled startup can break Windows Night Light and delay cross-device features (Phone Link, Nearby Sharing, clipboard sync). Choose Automatic if you use Night Light.");

        yield return Mk("opt.gaming-mixed-reality-service", Category.Performance, "System services",
            "Windows Mixed Reality OpenXR Service",
            "Runs OpenXR applications on Windows Mixed Reality devices. Safe to disable if you don't use VR or AR headsets",
            Always,
            L(new ServiceAction("MixedRealityOpenXRSvc", "demand", "disabled")),
            detail: "Runs OpenXR applications on Windows Mixed Reality devices. Safe to disable if you don't use VR or AR headsets. Recommended for most PCs.", restart: true);

        yield return Mk("opt.gaming-ai-fabric-service", Category.Performance, "System services",
            "Windows AI Fabric Service",
            "Windows AI Fabric Service (WSAIFabricSvc) manages AI workloads. Disable if you don't use Windows AI features",
            Always,
            L(new ServiceAction("WSAIFabricSvc", ServiceAction.Disabled, "auto")),
            detail: "Windows AI Fabric Service (WSAIFabricSvc) manages AI workloads. Disable if you don't use Windows AI features. Recommended for most PCs.", restart: true);

        yield return Mk("opt.gaming-task-disk-diagnostic", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Disk Diagnostic data collector task",
            "Collects disk diagnostic information and S.M.A.R.T. data for Microsoft. Disable to reduce background disk activity and telemetry",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector")),
            detail: "Collects disk diagnostic information and S.M.A.R.T. data for Microsoft. Disable to reduce background disk activity and telemetry. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-error-reporting-queue", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Error Reporting queue task",
            "Queues crash reports and error data to send to Microsoft. Works alongside the Windows Error Reporting Service. Disable both to prevent crash data…",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\Windows Error Reporting\\QueueReporting")),
            detail: "Queues crash reports and error data to send to Microsoft. Works alongside the Windows Error Reporting Service. Disable both to prevent crash data collection. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-sqm", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Software Quality Metrics task",
            "Collects software quality metrics and reliability data for Microsoft telemetry. Disable to improve privacy",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\PI\\Sqm-Tasks")),
            detail: "Collects software quality metrics and reliability data for Microsoft telemetry. Disable to improve privacy. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-mare-backup", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the MAR backup task",
            "Backs up Microsoft Assisted Recovery data. Disable to reduce background system activity",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\Application Experience\\MareBackup")),
            detail: "Backs up Microsoft Assisted Recovery data. Disable to reduce background system activity. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-startup-app", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Startup App task",
            "Tracks and monitors startup applications for telemetry and diagnostics. Disable to reduce telemetry",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\Application Experience\\StartupAppTask")),
            detail: "Tracks and monitors startup applications for telemetry and diagnostics. Disable to reduce telemetry. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-maps-update", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Maps update task",
            "Updates offline maps data for the Windows Maps app. Disable if you don't use the Maps app to save bandwidth and storage",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\Maps\\MapsUpdateTask")),
            detail: "Updates offline maps data for the Windows Maps app. Disable if you don't use the Maps app to save bandwidth and storage. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-autochk-proxy", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the AutoChk Proxy task",
            "Performs disk checking operations and collects diagnostic data. Consider keeping enabled for disk health monitoring",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\Autochk\\Proxy")),
            detail: "Performs disk checking operations and collects diagnostic data. Consider keeping enabled for disk health monitoring. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-family-safety", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Family Safety monitor task",
            "Monitors family safety settings and usage. Disable if you don't use family safety features",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\Shell\\FamilySafetyMonitor")),
            detail: "Monitors family safety settings and usage. Disable if you don't use family safety features. Part of the recommended set.");

        yield return Mk("opt.gaming-task-power-efficiency", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Power Efficiency Diagnostics task",
            "Analyzes system power consumption and collects energy efficiency data. Disable to reduce telemetry and background analysis",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem")),
            detail: "Analyzes system power consumption and collects energy efficiency data. Disable to reduce telemetry and background analysis. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-windows-ai", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Windows AI (Recall) tasks",
            "Windows AI scheduled tasks including Recall configuration. Disable to prevent AI features from running in the background",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Windows\\WindowsAI\\RecallConfiguration"),
              new ScheduledTaskAction("\\Microsoft\\Windows\\WindowsAI\\RecallPipeline")),
            detail: "Windows AI scheduled tasks including Recall configuration. Disable to prevent AI features from running in the background. Recommended for most PCs.");

        yield return Mk("opt.gaming-task-office-actions-server", Category.Privacy, "Telemetry scheduled tasks",
            "Turn off the Office Actions Server task",
            "Office AI Actions Server scheduled task. Disable to prevent Office AI from running in the background",
            Always,
            L(new ScheduledTaskAction("\\Microsoft\\Office\\Office Actions Server")),
            detail: "Office AI Actions Server scheduled task. Disable to prevent Office AI from running in the background. Recommended for most PCs.");

        yield return Mk("opt.font-smoothing", Category.Performance, "Visual effects",
            "Smooth edges of screen fonts",
            "Apply anti-aliasing to text for smoother, more readable fonts on screen",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Desktop", "FontSmoothing", "2", RegistryValueKind.String, "0")),
            detail: "Apply anti-aliasing to text for smoother, more readable fonts on screen. Part of the recommended set.", restart: true);

        yield return Mk("opt.gaming-narrator-hotkey", Category.Customize, "Accessibility shortcuts",
            "Turn off the Narrator shortcut (Win+Ctrl+Enter)",
            "Enable the Win+Ctrl+Enter keyboard shortcut to quickly launch Windows Narrator screen reader",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Narrator\\NoRoam", "WinEnterLaunchEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Enable the Win+Ctrl+Enter keyboard shortcut to quickly launch Windows Narrator screen reader. Part of the recommended set.");

        yield return Mk("opt.accessibility-filterkeys-hotkey", Category.Customize, "Accessibility shortcuts",
            "Turn off the Filter Keys shortcut (hold Right Shift 8 s)",
            "Enable the keyboard shortcut to activate FilterKeys by holding the right Shift key for 8 seconds",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Accessibility\\Keyboard Response", "Flags", "2", RegistryValueKind.String, "126")),
            detail: "Enable the keyboard shortcut to activate FilterKeys by holding the right Shift key for 8 seconds. Part of the recommended set.");

        yield return Mk("opt.accessibility-togglekeys-hotkey", Category.Customize, "Accessibility shortcuts",
            "Turn off the Toggle Keys shortcut (hold Num Lock 5 s)",
            "Enable the keyboard shortcut to activate ToggleKeys by holding Num Lock for 5 seconds, which plays sounds when Caps/Num/Scroll Lock are pressed",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Accessibility\\ToggleKeys", "Flags", "34", RegistryValueKind.String, "62")),
            detail: "Enable the keyboard shortcut to activate ToggleKeys by holding Num Lock for 5 seconds, which plays sounds when Caps/Num/Scroll Lock are pressed. Part of the recommended set.");

        yield return Mk("opt.accessibility-mousekeys-hotkey", Category.Customize, "Accessibility shortcuts",
            "Turn off the Mouse Keys shortcut (Alt+Shift+Num Lock)",
            "Enable the keyboard shortcut to activate MouseKeys, which allows using the numeric keypad to control the mouse pointer",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Accessibility\\MouseKeys", "Flags", "130", RegistryValueKind.String, "126")),
            detail: "Enable the keyboard shortcut to activate MouseKeys, which allows using the numeric keypad to control the mouse pointer. Part of the recommended set.");

        yield return Mk("opt.security-uac-level", Category.Privacy, "Security",
            "User Account Control Level",
            "Controls UAC notification level and secure desktop behavior",
            _ => Verdict.Warn("A security setting: Windows would stop asking before programs make admin-level changes, which lowers protection from malware. Never applied automatically."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin", 0, RegistryValueKind.DWord, 5),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "PromptOnSecureDesktop", 0, RegistryValueKind.DWord, 1)),
            detail: "Controls UAC notification level and secure desktop behavior. Part of the recommended set. Windows stops asking for your permission before admin-level changes.", risk: Risk.Advanced);

        yield return Mk("opt.security-bitlocker-auto-encryption", Category.Privacy, "Security",
            "Turn off BitLocker automatic encryption",
            "Controls whether Windows can automatically encrypt drives with BitLocker. Has no effect if BitLocker encryption is already active on your device",
            s => s.IsLaptop ? Verdict.Warn("Automatic encryption protects your files if the laptop is lost or stolen. Leave this off.") : Verdict.Opt("Stops Windows encrypting this PC automatically. A security setting, so it's never applied automatically."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\BitLocker", "PreventDeviceEncryption", 1, RegistryValueKind.DWord, 0)),
            detail: "Controls whether Windows can automatically encrypt drives with BitLocker. Has no effect if BitLocker encryption is already active on your device. Part of the recommended set.", risk: Risk.Advanced);

        yield return Mk("opt.security-automatic-maintenance", Category.Privacy, "Security",
            "Turn off Automatic Maintenance",
            "Choose if Windows should run automatic system maintenance tasks during idle time",
            _ => Verdict.Warn("Stops scheduled maintenance (updates, defrag, diagnostics)."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", "MaintenanceDisabled", 1, RegistryValueKind.DWord, 0)),
            detail: "Choose if Windows should run automatic system maintenance tasks during idle time. TrapShack never applies this automatically.", risk: Risk.Advanced);

        yield return Mk("opt.security-smart-app-control", Category.Privacy, "Security",
            "Smart App Control",
            "Controls the Smart App Control feature which blocks untrusted and potentially dangerous applications",
            _ => Verdict.Warn("Turns off Smart App Control for good. Windows cannot switch it back on without a reset."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\CI\\Policy", "VerifiedAndReputablePolicyState", 0, RegistryValueKind.DWord, 2)),
            detail: "Controls the Smart App Control feature which blocks untrusted and potentially dangerous applications. TrapShack never applies this automatically.", risk: Risk.Advanced);

        yield return Mk("opt.security-powershell-execution-policy", Category.Privacy, "Security",
            "PowerShell Execution Policy",
            "Controls whether PowerShell scripts are allowed to run and under what conditions for both the current user and the local machine",
            _ => Verdict.Warn("A security setting: lets local PowerShell scripts run (downloaded ones must be signed). Never applied automatically."),
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\PowerShell\\1\\ShellIds\\Microsoft.PowerShell", "ExecutionPolicy", "RemoteSigned", RegistryValueKind.String, "Restricted"),
              new RegistryAction(RegistryHive.LocalMachine, "Software\\Microsoft\\PowerShell\\1\\ShellIds\\Microsoft.PowerShell", "ExecutionPolicy", "RemoteSigned", RegistryValueKind.String, "Restricted")),
            detail: "Controls whether PowerShell scripts are allowed to run and under what conditions for both the current user and the local machine. Part of the recommended set.", risk: Risk.Advanced);

        yield return Mk("opt.privacy-subscribed-content", Category.Privacy, "Ads & promotional content",
            "Turn off subscribed promotional content",
            "Enables promotional content subscriptions from Microsoft and partners throughout Windows",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContentEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Enables promotional content subscriptions from Microsoft and partners throughout Windows. Recommended for most PCs.");

        yield return Mk("opt.privacy-content-delivery-allowed", Category.Privacy, "Ads & promotional content",
            "Turn off Windows content delivery",
            "Stops Windows delivering promotional content and installing suggested apps on its own.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "ContentDeliveryAllowed", 0, RegistryValueKind.DWord, 1)),
            detail: "Sets ContentDeliveryAllowed to 0 for your account. Windows Spotlight lock-screen pictures can stop changing while this is on. Recommended for most PCs.");

        yield return Mk("opt.privacy-feature-management", Category.Privacy, "Ads & promotional content",
            "Turn off promotional feature management",
            "Stops Windows switching on promotional features and automatic app installs.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "FeatureManagementEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Sets FeatureManagementEnabled to 0 for your account. Recommended for most PCs.");

        yield return Mk("opt.privacy-oem-preinstalled-apps", Category.Privacy, "Ads & promotional content",
            "Stop PC-maker apps installing themselves",
            "Stops your PC's manufacturer from automatically installing its apps.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "OemPreInstalledAppsEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Sets OemPreInstalledAppsEnabled to 0 for your account. Apps already installed stay; remove them on the Debloat apps page. Recommended for most PCs.");

        yield return Mk("opt.privacy-preinstalled-apps", Category.Privacy, "Ads & promotional content",
            "Stop suggested apps installing themselves",
            "Stops Microsoft automatically installing suggested apps.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "PreInstalledAppsEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Sets PreInstalledAppsEnabled to 0 for your account. Apps already installed stay; remove them on the Debloat apps page. Recommended for most PCs.");

        yield return Mk("opt.privacy-preinstalled-apps-ever", Category.Privacy, "Ads & promotional content",
            "Stop tracking pre-installed apps",
            "Stops Windows recording whether pre-installed apps were ever turned on.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "PreInstalledAppsEverEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Sets PreInstalledAppsEverEnabled to 0. Switching it off removes the value, which is Windows' default. Recommended for most PCs.");

        yield return Mk("opt.privacy-lock-screen", Category.Customize, "Lock screen",
            "Stop the PC from being locked (Win+L)",
            "Allows users to lock their computer using Windows+L, Start menu, or Ctrl+Alt+Del screen",
            _ => Verdict.Warn("Blocks Win+L, so you can no longer lock the PC."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DisableLockWorkstation", 1, RegistryValueKind.DWord, 0)),
            detail: "Allows users to lock their computer using Windows+L, Start menu, or Ctrl+Alt+Del screen. TrapShack never applies this automatically.", risk: Risk.Advanced);

        yield return Mk("opt.privacy-rotating-lock-screen", Category.Customize, "Lock screen",
            "Turn off Windows Spotlight on the lock screen",
            "Displays rotating Windows Spotlight images on your lock screen instead of a static background. Windows also sets the Start Menu…",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "RotatingLockScreenEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Displays rotating Windows Spotlight images on your lock screen instead of a static background. Windows also sets the Start Menu Recommended Section to Show when this setting is enabled as it is required. Recommended for most PCs.");

        yield return Mk("opt.privacy-settings-notifications", Category.Privacy, "Privacy & security",
            "Turn off Settings app notifications",
            "Shows account notifications in the Settings app, including prompts to reauthenticate, backup your device, and manage subscriptions",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\SystemSettings\\AccountNotifications", "EnableAccountNotifications", 0, RegistryValueKind.DWord, 1)),
            detail: "Shows account notifications in the Settings app, including prompts to reauthenticate, backup your device, and manage subscriptions. Recommended for most PCs.");

        yield return Mk("opt.privacy-narrator-online-services", Category.Privacy, "Speech & inking",
            "Turn off Narrator online services",
            "Allow Narrator to use Microsoft cloud services for features like intelligent image descriptions and enhanced voice models",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Narrator\\NoRoam", "OnlineServicesEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Allow Narrator to use Microsoft cloud services for features like intelligent image descriptions and enhanced voice models. Recommended for most PCs.");

        yield return Mk("opt.privacy-narrator-scripting", Category.Privacy, "Speech & inking",
            "Turn off Narrator scripting support",
            "Allow Narrator to execute scripts for automation and custom functionality",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Narrator\\NoRoam", "ScriptingEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Allow Narrator to execute scripts for automation and custom functionality. Recommended for most PCs.");

        yield return Mk("opt.privacy-account-info-access", Category.Privacy, "App permissions",
            "Block apps from your account info",
            "Allow apps to have account info access",
            _ => Verdict.Rec("Apps can no longer read your name and account picture."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\userAccountInformation", "Value", "Deny", RegistryValueKind.String, "Allow")),
            detail: "Allow apps to have account info access. TrapShack never applies this automatically.", risk: Risk.Advanced);

        yield return Mk("opt.privacy-app-diagnostic-access", Category.Privacy, "App permissions",
            "Block apps from reading other apps' diagnostic info",
            "Allow apps to have app diagnostic access",
            _ => Verdict.Rec("Task Manager-style apps lose access to other apps' diagnostics."),
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appDiagnostics", "Value", "Deny", RegistryValueKind.String, "Allow")),
            detail: "Allow apps to have app diagnostic access. TrapShack never applies this automatically.", risk: Risk.Advanced);

        yield return Mk("opt.privacy-disable-recall-snapshots", Category.Privacy, "Windows AI",
            "Turn off Recall snapshots",
            "Allows Windows Recall to save screenshots of your activity for later recall",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "TurnOffSavingSnapshots", 1, RegistryValueKind.DWord, null)),
            detail: "Allows Windows Recall to save screenshots of your activity for later recall. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-settings-agent", Category.Privacy, "Windows AI",
            "Turn off the AI agent in Settings",
            "Controls whether the AI-powered Settings Agent is available in Windows",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableSettingsAgent", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether the AI-powered Settings Agent is available in Windows. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-agent-connectors", Category.Privacy, "Windows AI",
            "Turn off AI agent connectors",
            "Controls whether AI agents can use connectors to access external services",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAgentConnectors", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI agents can use connectors to access external services. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-agent-workspaces", Category.Privacy, "Windows AI",
            "Turn off AI agent workspaces",
            "Controls whether AI Agent Workspaces are available in Windows",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAgentWorkspaces", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI Agent Workspaces are available in Windows. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-remote-agent-connectors", Category.Privacy, "Windows AI",
            "Turn off remote AI agent connectors",
            "Controls whether AI agents can use remote connectors to access remote services",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableRemoteAgentConnectors", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI agents can use remote connectors to access remote services. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-copilot-hardware-key", Category.Privacy, "Windows AI",
            "Turn off the Copilot key",
            "Controls whether the dedicated Copilot key on keyboards opens Copilot",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\CopilotKey", "SetCopilotHardwareKey", "", RegistryValueKind.String, null)),
            detail: "Controls whether the dedicated Copilot key on keyboards opens Copilot. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-copilot-runtime", Category.Privacy, "Windows AI",
            "Turn off the Copilot runtime",
            "Controls whether the Copilot runtime is allowed to run via policy",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "AllowCopilotRuntime", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether the Copilot runtime is allowed to run via policy. Recommended for most PCs.");

        yield return Mk("opt.privacy-copilot-unavailable", Category.Privacy, "Windows AI",
            "Make Copilot unavailable in Windows",
            "Controls whether Copilot is available in the Windows Shell",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\Shell\\Copilot", "IsCopilotAvailable", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Copilot is available in the Windows Shell. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-bing-chat", Category.Privacy, "Windows AI",
            "Turn off Bing Chat eligibility",
            "Controls whether the user is eligible for Bing Chat and Copilot in Search",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\Shell\\Copilot\\BingChat", "IsUserEligible", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether the user is eligible for Bing Chat and Copilot in Search. Recommended for most PCs.");

        yield return Mk("opt.privacy-deny-generative-ai-access", Category.Privacy, "Windows AI",
            "Block apps from generative AI",
            "Controls whether apps can access the generative AI capability on your device",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\generativeAI", "Value", "Deny", RegistryValueKind.String, "Allow"),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessGenerativeAI", 2, RegistryValueKind.DWord, null)),
            detail: "Controls whether apps can access the generative AI capability on your device. Recommended for most PCs.");

        yield return Mk("opt.privacy-deny-system-ai-models", Category.Privacy, "Windows AI",
            "Block apps from Windows' AI models",
            "Controls whether apps can access system AI models on your device and collect usage data",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\systemAIModels", "Value", "Deny", RegistryValueKind.String, "Allow"),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessSystemAIModels", 2, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\systemAIModels", "RecordUsageData", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether apps can access system AI models on your device and collect usage data. Recommended for most PCs.");

        yield return Mk("opt.privacy-deny-copilot-microphone", Category.Privacy, "Windows AI",
            "Block Copilot's microphone access",
            "Controls whether Copilot and Office Hub apps have microphone permission",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\Microsoft.Copilot_8wekyb3d8bbwe", "Value", "Deny", RegistryValueKind.String, "Allow"),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe", "Value", "Deny", RegistryValueKind.String, "Allow")),
            detail: "Controls whether Copilot and Office Hub apps have microphone permission. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-paint-ai-image-creator", Category.Privacy, "Windows AI",
            "Turn off Paint Image Creator",
            "Controls whether the AI Image Creator feature is available in Microsoft Paint",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Paint", "DisableImageCreator", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether the AI Image Creator feature is available in Microsoft Paint. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-paint-ai-cocreator", Category.Privacy, "Windows AI",
            "Turn off Paint Cocreator",
            "Controls whether the AI Cocreator feature is available in Microsoft Paint",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Paint", "DisableCocreator", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether the AI Cocreator feature is available in Microsoft Paint. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-paint-generative-fill", Category.Privacy, "Windows AI",
            "Turn off Paint generative fill",
            "Controls whether the AI Generative Fill feature is available in Microsoft Paint",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Paint", "DisableGenerativeFill", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether the AI Generative Fill feature is available in Microsoft Paint. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-paint-generative-erase", Category.Privacy, "Windows AI",
            "Turn off Paint generative erase",
            "Controls whether the AI Generative Erase feature is available in Microsoft Paint",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Paint", "DisableGenerativeErase", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether the AI Generative Erase feature is available in Microsoft Paint. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-paint-remove-background", Category.Privacy, "Windows AI",
            "Turn off Paint background removal",
            "Controls whether the AI Remove Background feature is available in Microsoft Paint",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Paint", "DisableRemoveBackground", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether the AI Remove Background feature is available in Microsoft Paint. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-input-insights", Category.Privacy, "Windows AI",
            "Turn off input insights",
            "Controls whether Windows Input Insights can track typing patterns and provide suggestions",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\input\\Settings", "InsightsEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Controls whether Windows Input Insights can track typing patterns and provide suggestions. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-copilot-nudges", Category.Privacy, "Windows AI",
            "Turn off Copilot nudges",
            "Controls whether Copilot promotional nudges and background task notifications are shown",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowCopilotNudges", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Copilot promotional nudges and background task notifications are shown. Recommended for most PCs.");

        yield return Mk("opt.privacy-disable-consumer-ai-content", Category.Privacy, "Windows AI",
            "Turn off AI consumer content",
            "Controls whether AI-driven consumer account content recommendations are shown",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableConsumerAccountStateContent", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI-driven consumer account content recommendations are shown. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-copilot-cdp-page-context", Category.Privacy, "Microsoft Edge AI",
            "Stop Edge Copilot reading pages (CDP)",
            "Controls whether Copilot can use CDP to access page content in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "CopilotCDPPageContext", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Copilot can use CDP to access page content in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-copilot-page-context", Category.Privacy, "Microsoft Edge AI",
            "Stop Edge Copilot reading pages",
            "Controls whether Copilot can read page content in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "CopilotPageContext", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Copilot can read page content in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-entra-copilot", Category.Privacy, "Microsoft Edge AI",
            "Stop Edge Copilot reading pages (work accounts)",
            "Controls whether Entra Copilot can access page context in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "EdgeEntraCopilotPageContext", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Entra Copilot can access page context in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-m365-copilot-icon", Category.Privacy, "Microsoft Edge AI",
            "Hide the Microsoft 365 Copilot icon in Edge",
            "Controls whether the Microsoft 365 Copilot chat icon is shown in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "Microsoft365CopilotChatIconEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether the Microsoft 365 Copilot chat icon is shown in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-ai-history-search", Category.Privacy, "Microsoft Edge AI",
            "Turn off Edge AI history search",
            "Controls whether AI-powered history search is available in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "EdgeHistoryAISearchEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI-powered history search is available in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-inline-compose", Category.Privacy, "Microsoft Edge AI",
            "Turn off Edge inline AI writing",
            "Controls whether AI-powered inline compose suggestions are available in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "ComposeInlineEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI-powered inline compose suggestions are available in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-local-ai-model", Category.Privacy, "Microsoft Edge AI",
            "Turn off Edge's local AI model",
            "Controls whether local AI model settings are available in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "GenAILocalFoundationalModelSettings", 1, RegistryValueKind.DWord, null)),
            detail: "Controls whether local AI model settings are available in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-builtin-ai-apis", Category.Privacy, "Microsoft Edge AI",
            "Turn off Edge's built-in AI APIs",
            "Controls whether built-in AI APIs are available in Microsoft Edge for websites to use",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "BuiltInAIAPIsEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether built-in AI APIs are available in Microsoft Edge for websites to use. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-ai-themes", Category.Privacy, "Microsoft Edge AI",
            "Turn off Edge AI-generated themes",
            "Controls whether AI-generated themes are available in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "AIGenThemesEnabled", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI-generated themes are available in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-devtools-ai", Category.Privacy, "Microsoft Edge AI",
            "Turn off AI in Edge DevTools",
            "Controls whether AI features are available in Edge DevTools",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "DevToolsGenAiSettings", 2, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI features are available in Edge DevTools. Recommended for most PCs.");

        yield return Mk("opt.privacy-edge-share-history-copilot", Category.Privacy, "Microsoft Edge AI",
            "Stop Edge sharing history with Copilot",
            "Controls whether browsing history is shared with Copilot search in Microsoft Edge",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge", "ShareBrowsingHistoryWithCopilotSearchAllowed", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether browsing history is shared with Copilot search in Microsoft Edge. Recommended for most PCs.");

        yield return Mk("opt.privacy-office-ai-training", Category.Privacy, "Microsoft Office AI",
            "Stop Office AI training on your content",
            "Controls whether Office collects AI training data from your usage",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\office\\16.0\\common\\ai\\training", "optionalconnectedexperiencesenabled", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Office collects AI training data from your usage. Recommended for most PCs.");

        yield return Mk("opt.privacy-office-connected-services", Category.Privacy, "Microsoft Office AI",
            "Turn off Office connected services",
            "Controls whether Office connected experiences and AI-powered services are available",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\office\\16.0\\common\\privacy", "controllerconnectedservicesenabled", 2, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\office\\16.0\\common\\privacy", "usercontentdisabled", 2, RegistryValueKind.DWord, null)),
            detail: "Controls whether Office connected experiences and AI-powered services are available. Recommended for most PCs.");

        yield return Mk("opt.privacy-word-copilot", Category.Privacy, "Microsoft Office AI",
            "Turn off Copilot in Word",
            "Controls whether Copilot AI features are available in Microsoft Word",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Office\\16.0\\Word\\Options", "EnableCopilot", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Copilot AI features are available in Microsoft Word. Recommended for most PCs.");

        yield return Mk("opt.privacy-excel-copilot", Category.Privacy, "Microsoft Office AI",
            "Turn off Copilot in Excel",
            "Controls whether Copilot AI features are available in Microsoft Excel",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Office\\16.0\\Excel\\Options", "EnableCopilot", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Copilot AI features are available in Microsoft Excel. Recommended for most PCs.");

        yield return Mk("opt.privacy-onenote-copilot", Category.Privacy, "Microsoft Office AI",
            "Turn off Copilot in OneNote",
            "Controls whether Copilot AI features, Copilot notebooks, and Copilot skittle are available in Microsoft OneNote",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Office\\16.0\\OneNote\\Options\\Other", "EnableCopilot", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Office\\16.0\\OneNote\\Options\\Other", "EnableCopilotNotebooks", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Office\\16.0\\OneNote\\Options\\Other", "EnableCopilotSkittle", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether Copilot AI features, Copilot notebooks, and Copilot skittle are available in Microsoft OneNote. Recommended for most PCs.");

        yield return Mk("opt.privacy-office-content-safety-ai", Category.Privacy, "Microsoft Office AI",
            "Turn off Office AI content safety",
            "Controls whether AI content safety features for alt text, rewrite, and summarization are available in Office apps",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\office\\16.0\\common\\ai", "contentsafetyserviceenabled", 0, RegistryValueKind.DWord, null)),
            detail: "Controls whether AI content safety features for alt text, rewrite, and summarization are available in Office apps. Recommended for most PCs.");

        yield return Mk("opt.notifications-sound", Category.Customize, "Notifications",
            "Turn off notification sounds",
            "Play audio alerts when notifications arrive from apps and system senders",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings", "NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND", 0, RegistryValueKind.DWord, 1)),
            detail: "Play audio alerts when notifications arrive from apps and system senders. Part of the recommended set.");

        yield return Mk("opt.notifications-toast-above-lock", Category.Customize, "Notifications",
            "Hide notifications on the lock screen",
            "Display toast notifications on the lock screen when your device is locked",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings", "NOC_GLOBAL_SETTING_ALLOW_TOASTS_ABOVE_LOCK", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "LockScreenToastEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Display toast notifications on the lock screen when your device is locked. Part of the recommended set.");

        yield return Mk("opt.notifications-critical-toast-above-lock", Category.Customize, "Notifications",
            "Hide reminders and VoIP calls on the lock screen",
            "Display critical notifications like reminders and VoIP calls when your device is locked",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings", "NOC_GLOBAL_SETTING_ALLOW_CRITICAL_TOASTS_ABOVE_LOCK", 0, RegistryValueKind.DWord, 1)),
            detail: "Display critical notifications like reminders and VoIP calls when your device is locked. Part of the recommended set.");

        yield return Mk("opt.notifications-show-bell-icon", Category.Customize, "Notifications",
            "Hide the notification bell icon",
            "Display the notification bell icon in the system tray",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowNotificationIcon", 0, RegistryValueKind.DWord, 1)),
            detail: "Display the notification bell icon in the system tray. Part of the recommended set.");

        yield return Mk("opt.notifications-capability-access", Category.Customize, "Notifications",
            "Turn off capability access notifications",
            "Show notifications when apps request access to system capabilities and permissions",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Windows.SystemToast.CapabilityAccess", "Enabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Show notifications when apps request access to system capabilities and permissions. Recommended for most PCs.");

        yield return Mk("opt.notifications-startup-app", Category.Customize, "Notifications",
            "Turn off startup app notifications",
            "Show notifications when apps are added to your Windows startup list",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\\Windows.SystemToast.StartupApp", "Enabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Show notifications when apps are added to your Windows startup list. Recommended for most PCs.");

        yield return Mk("opt.notifications-clock-change", Category.Customize, "Notifications",
            "Turn off clock change notifications",
            "Show notifications when daylight saving time changes occur",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Desktop", "DstNotification", 0, RegistryValueKind.DWord, 1)),
            detail: "Show notifications when daylight saving time changes occur. Recommended for most PCs.");

        yield return Mk("opt.sound-narrator-audio-ducking", Category.Customize, "Sound",
            "Turn off Narrator audio ducking",
            "Allow Narrator to automatically lower the volume of other applications when it speaks",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Narrator\\NoRoam", "DuckAudio", 0, RegistryValueKind.DWord, 1)),
            detail: "Allow Narrator to automatically lower the volume of other applications when it speaks. Recommended for most PCs.");

        yield return Mk("opt.sound-voice-activation", Category.Customize, "Sound",
            "Turn off voice activation for apps",
            "Allow apps to listen and respond to voice commands like \"Hey Cortana\"",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\SpeechOneCore\\Settings", "AgentActivationEnabled", 0, RegistryValueKind.DWord, 1)),
            detail: "Allow apps to listen and respond to voice commands like \"Hey Cortana\". Recommended for most PCs.");

        yield return Mk("opt.sound-voice-activation-last-used", Category.Customize, "Sound",
            "Turn off last-used voice activation",
            "Remember and apply the most recently used voice activation configuration",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\SpeechOneCore\\Settings", "AgentActivationLastUsed", 0, RegistryValueKind.DWord, 1)),
            detail: "Remember and apply the most recently used voice activation configuration. Recommended for most PCs.");

        yield return Mk("opt.sound-accessibility-activation", Category.Customize, "Sound",
            "Turn off accessibility activation sounds",
            "Play sounds when accessibility features like StickyKeys or FilterKeys are activated",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Accessibility", "Sound on Activation", 0, RegistryValueKind.DWord, 1)),
            detail: "Play sounds when accessibility features like StickyKeys or FilterKeys are activated. Recommended for most PCs.");

        yield return Mk("opt.sound-accessibility-warnings", Category.Customize, "Sound",
            "Turn off accessibility warning sounds",
            "Play warning sounds when attempting to activate accessibility features or when accessibility-related events occur",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Control Panel\\Accessibility", "Warning Sounds", 0, RegistryValueKind.DWord, 1)),
            detail: "Play warning sounds when attempting to activate accessibility features or when accessibility-related events occur. Recommended for most PCs.");

        yield return Mk("opt.updates-latest-updates", Category.Updates, "Update behaviour",
            "Don't get the latest updates as soon as they're out",
            "Be among the first to get the latest non-security updates, fixes, and improvements as they roll out",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "IsContinuousInnovationOptedIn", 0, RegistryValueKind.DWord, null)),
            detail: "Be among the first to get the latest non-security updates, fixes, and improvements as they roll out. Recommended for most PCs.");

        yield return Mk("opt.updates-restart-asap", Category.Updates, "Update behaviour",
            "Don't restart early to finish updates",
            "Restart as soon as possible (even during active hours) to finish updating",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "Software\\Microsoft\\WindowsUpdate\\UX\\Settings", "IsExpedited", 0, RegistryValueKind.DWord, 1)),
            detail: "Restart as soon as possible (even during active hours) to finish updating. Recommended for most PCs.");

        yield return Mk("opt.updates-metered-connection", Category.Updates, "Update behaviour",
            "Don't download updates over metered connections",
            "Allow Windows to download updates when using mobile hotspots or data-limited connections",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "AllowAutoWindowsUpdateDownloadOverMeteredNetwork", 0, RegistryValueKind.DWord, 1)),
            detail: "Allow Windows to download updates when using mobile hotspots or data-limited connections. Recommended for most PCs.");
    }
}
