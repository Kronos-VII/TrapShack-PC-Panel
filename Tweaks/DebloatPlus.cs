using Microsoft.Win32;
using TrapShack.Core;

namespace TrapShack.Tweaks;

/// <summary>Extra debloat settings and the drop-down options on the Debloat page. Values were checked against their .reg files.</summary>
public static partial class Catalog
{
    static IEnumerable<Tweak> DebloatPlus() => new[]
    {
        Mk("rd.suggestions", Category.Debloat, "", @"Tips, tricks and suggestions off", @"Stops suggested apps, tips, ""get even more out of Windows"" prompts and ads across Windows.",
            Always, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SubscribedContent-310093Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"Start_IrisRecommendations", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SoftLandingEnabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SubscribedContent-338393Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SubscribedContent-353694Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SubscribedContent-353696Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SubscribedContent-353698Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\SystemSettings\AccountNotifications", @"EnableAccountNotifications", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement", @"ScoobeSystemSettingEnabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"ShowSyncProviderNotifications", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SilentInstalledAppsEnabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.Suggested", @"Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Mobility", @"OptedIn", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"Start_AccountNotifications", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.BackupReminder", @"Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", @"DisableCopilotPinScreen", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Explorer", @"HideRecommendedPersonalizedSites", 1, RegistryValueKind.DWord, null))),
        Mk("rd.lockscreen-tips", Category.Debloat, "", @"Lock screen tips off", @"No fun facts, tips or tricks on the lock screen.",
            Always, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"SubscribedContent-338387Enabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", @"RotatingLockScreenOverlayEnabled", 0, RegistryValueKind.DWord, 1))),
        Mk("rd.find-my-device", Category.Debloat, "", @"Find My Device off", @"Stops Windows sending this PC's location to Microsoft now and then. You can no longer locate it from your Microsoft account.",
            s => s.IsLaptop ? Verdict.Opt("On a laptop this is how you find it if it is lost or stolen.") : Verdict.Rec(), L(
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\FindMyDevice", @"AllowFindMyDevice", 0, RegistryValueKind.DWord, null))),
        Mk("rd.edge-ads", Category.Debloat, "", @"Edge ads, suggestions and news feed off", @"Turns off the shopping assistant, sponsored tiles, the MSN feed and other suggestions in Microsoft Edge.",
            Always, L(
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"NewTabPageContentEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"NewTabPageHideDefaultTopSites", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"EdgeShoppingAssistantEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"TabServicesEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"AlternateErrorPagesEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"UserFeedbackAllowed", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"ShowRecommendationsEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"WalletDonationEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"HideFirstRunExperience", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"ShowMicrosoftRewards", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"DefaultBrowserSettingEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"DefaultBrowserSettingsCampaignEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"SpotlightExperiencesAndRecommendationsEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"ShowAcrobatSubscriptionButton", 0, RegistryValueKind.DWord, null))),
        // The two Microsoft Edge values in the debloat source's "Disable telemetry" (Disable_Telemetry.reg).
        Mk("rd.edge-diagnostics", Category.Debloat, "", @"Edge diagnostic data off", @"Microsoft Edge stops sending browser diagnostic data and your browsing activity for personalised ads and news.",
            Always, L(
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"PersonalizationReportingEnabled", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", @"DiagnosticData", 0, RegistryValueKind.DWord, null))),
        Mk("rd.click-to-do", Category.Debloat, "", @"Click to Do off", @"Turns off Click to Do, the AI text and image analysis on Copilot+ PCs.",
            s => s.Build >= 22621 ? Verdict.Rec() : Verdict.No("Needs Windows 11 22H2 or newer."), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsAI", @"DisableClickToDo", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", @"DisableClickToDo", 1, RegistryValueKind.DWord, null))),
        Mk("rd.ai-service", Category.Debloat, "", @"AI service starts only when needed", @"Sets the Windows AI Fabric service to Manual so it no longer starts with Windows.",
            s => s.Build >= 22621 ? Verdict.Rec() : Verdict.No("Needs Windows 11 22H2 or newer."), L(
                new RegistryAction(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\WSAIFabricSvc", @"Start", 3, RegistryValueKind.DWord, 2)), restart: true),
        Mk("rd.paint-ai", Category.Debloat, "", @"AI features in Paint off", @"Turns off Cocreator, generative fill, generative erase and Image Creator in Paint.",
            s => s.Build >= 22621 ? Verdict.Opt() : Verdict.No("Needs Windows 11 22H2 or newer."), L(
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", @"DisableCocreator", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", @"DisableGenerativeFill", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", @"DisableImageCreator", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", @"DisableGenerativeErase", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", @"DisableRemoveBackground", 1, RegistryValueKind.DWord, null))),
        Mk("rd.chrome-ai", Category.Debloat, "", @"AI features in Google Chrome off", @"Turns off Gemini, AI mode, ""help me write"", AI history search and other AI features in Chrome. Only matters if Chrome is installed.",
            s => s.Build >= 22621 ? Verdict.Opt() : Verdict.No("Needs Windows 11 22H2 or newer."), L(
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"AIModeSettings", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"AutofillPredictionSettings", 2, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"ChromeSuggestionsSettings", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"CreateThemesSettings", 2, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"DevToolsGenAiSettings", 2, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"FindAndFillWithGeminiSettings", 2, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"GeminiSettings", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"GeminiSparkSettings", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"GenAILocalFoundationalModelSettings", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"HelpMeWriteSettings", 2, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"HistorySearchSettings", 2, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"SearchContentSharingSettings", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"SmartTabSharingSettings", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"TabCompareSettings", 2, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"ThirdPartyAiChatSettings", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Google\Chrome", @"VoiceTypingSettings", 2, RegistryValueKind.DWord, null))),
        Mk("rd.bing-search", Category.Debloat, "", @"Bing and Copilot in Windows Search off", @"Search in the Start menu only looks at this PC: no web results and no Copilot answers.",
            Always, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", @"DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", @"AllowCortana", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", @"CortanaConsent", 0, RegistryValueKind.DWord, null))),
        Mk("rd.start-all-apps", Category.Debloat, "", @"Hide ""All apps"" in Start", @"Removes the All apps list from the Start menu. Apps stay installed; you find them by searching.",
            s => s.Build >= 26200 ? Verdict.Opt() : Verdict.No("Needs Windows 11 25H2 or newer."), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", @"NoStartMenuMorePrograms", 1, RegistryValueKind.DWord, null))),
        Mk("rd.start-phone-link", Category.Debloat, "", @"Phone Link panel in Start off", @"Removes the phone companion panel from the Start menu when a phone is linked.",
            s => s.Build >= 22621 ? Verdict.Opt() : Verdict.No("Needs Windows 11 22H2 or newer."), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Start\Companions\Microsoft.YourPhone_8wekyb3d8bbwe", @"IsEnabled", 0, RegistryValueKind.DWord, 1))),
        Mk("rd.gamebar-integration", Category.Debloat, "", @"Game Bar pop-ups off", @"Stops ""ms-gamebar"" pop-ups when a game starts or a controller connects, once the Game Bar app is removed.",
            Sometimes, L(
                new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\GameBar", @"UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ms-gamebar", @"", @"URL:ms-gamebar", RegistryValueKind.String, @"URL:ms-gamebar"),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ms-gamebar", @"URL Protocol", @"", RegistryValueKind.String, @""),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ms-gamebar", @"NoOpenWith", @"", RegistryValueKind.String, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ms-gamebar\shell\open\command", @"", @"%SystemRoot%/System32/systray.exe", RegistryValueKind.String, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ms-gamebarservices", @"", @"URL:ms-gamebarservices", RegistryValueKind.String, @"URL:ms-gamebarservices"),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ms-gamebarservices", @"URL Protocol", @"", RegistryValueKind.String, @""),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ms-gamebarservices", @"NoOpenWith", @"", RegistryValueKind.String, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ms-gamebarservices\shell\open\command", @"", @"%SystemRoot%/System32/systray.exe", RegistryValueKind.String, null))),
        Mk("rd.fast-startup", Category.Debloat, "", @"Fast startup off", @"Shut down really shuts down. Fixes updates, drivers and dual-boot problems; cold starts take a few seconds longer.",
            Sometimes, L(
                new RegistryAction(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", @"HiberbootEnabled", 0, RegistryValueKind.DWord, 1))),
        Mk("rd.standby-network", Category.Debloat, "", @"No network while asleep", @"Stops laptops that use Modern Standby from staying online (and draining battery) while asleep.",
            s => s.IsLaptop ? Verdict.Rec() : Verdict.Opt("Only matters on laptops and tablets that use Modern Standby."), L(
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Power\PowerSettings\f15576e8-98b7-4186-b944-eafa664402d9", @"ACSettingIndex", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Power\PowerSettings\f15576e8-98b7-4186-b944-eafa664402d9", @"DCSettingIndex", 0, RegistryValueKind.DWord, null))),
        Mk("rd.snap-assist", Category.Debloat, "", @"No app suggestions when snapping", @"Snapping a window no longer fills the rest of the screen with suggested windows.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"SnapAssist", 0, RegistryValueKind.DWord, 1))),
        Mk("rd.snap-layouts", Category.Debloat, "", @"Snap layout pop-up off", @"No layout pop-up when you hover the maximise button or drag a window to the top.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"EnableSnapBar", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"EnableSnapAssistFlyout", 0, RegistryValueKind.DWord, 1))),
        Mk("rd.last-active-click", Category.Debloat, "", @"Taskbar click opens the last window", @"Clicking an app on the taskbar goes straight to its last used window instead of showing previews.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"LastActiveClick", 1, RegistryValueKind.DWord, null))),
        Mk("rd.hide-chat", Category.Debloat, "", @"Hide Chat and Meet Now on the taskbar", @"Removes the Chat (Teams) and Meet Now buttons from the taskbar.",
            Always, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarMn", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", @"HideSCAMeetNow", 1, RegistryValueKind.DWord, 0))),
        Mk("rd.hide-onedrive", Category.Debloat, "", @"Hide OneDrive in File Explorer", @"Removes OneDrive from the File Explorer side panel. OneDrive itself keeps working.",
            Sometimes, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}", @"System.IsPinnedToNameSpaceTree", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"CheckedValue", 1, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"DefaultValue", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"HKeyRoot", unchecked((int)0x80000001), RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"Id", 13, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"RegPath", @"Software\Classes\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}", RegistryValueKind.String, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"Text", @"Show OneDrive", RegistryValueKind.String, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"Type", @"checkbox", RegistryValueKind.String, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"UncheckedValue", 0, RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowOneDrive", @"ValueName", @"System.IsPinnedToNameSpaceTree", RegistryValueKind.String, null))),
        Mk("rd.this-pc-folders", Category.Debloat, "", @"Show Desktop, Documents and other folders in This PC", @"Brings the common folders back onto the This PC page in File Explorer.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryDeleteAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", @"HideIfEnabled", RegistryValueKind.DWord, 36354489),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", @"HiddenByDefault", 0, RegistryValueKind.DWord, 1),
                new RegistryDeleteAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{d3162b92-9365-467a-956b-92703aca08af}", @"HideIfEnabled", RegistryValueKind.DWord, 36354489),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{d3162b92-9365-467a-956b-92703aca08af}", @"HiddenByDefault", 0, RegistryValueKind.DWord, 1),
                new RegistryDeleteAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}", @"HideIfEnabled", RegistryValueKind.DWord, 36354489),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}", @"HiddenByDefault", 0, RegistryValueKind.DWord, 1),
                new RegistryDeleteAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}", @"HideIfEnabled", RegistryValueKind.DWord, 36354489),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{1CF1260C-4DD0-4ebb-811F-33C572699FDE}", @"HiddenByDefault", 0, RegistryValueKind.DWord, 1),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}", @"HiddenByDefault", 0, RegistryValueKind.DWord, 1),
                new RegistryDeleteAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}", @"HideIfEnabled", RegistryValueKind.DWord, 36354489),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}", @"HiddenByDefault", 0, RegistryValueKind.DWord, 1),
                new RegistryDeleteAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}", @"HideIfEnabled", RegistryValueKind.DWord, 36354489),
                new RegistryAction(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}", @"HiddenByDefault", 0, RegistryValueKind.DWord, 1))),
        Choice(@"Desktop spotlight", @"Hide ""Learn about this picture""", Mk("rd.choice.desktop-spotlight.hide-learn-about-this-picture", Category.Debloat, "", @"Desktop spotlight: Hide ""Learn about this picture""", @"Desktop spotlight: Hide ""Learn about this picture"".",
            Sometimes, L(
                new RegistryDeleteAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", @"DisableSpotlightCollectionOnDesktop", RegistryValueKind.DWord, null),
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", @"{2cc5ca98-6485-489a-920e-b3e88a6ccce3}", 1, RegistryValueKind.DWord, null)))),
        Choice(@"Desktop spotlight", @"Spotlight wallpaper off", Mk("rd.choice.desktop-spotlight.spotlight-wallpaper-off", Category.Debloat, "", @"Desktop spotlight: Spotlight wallpaper off", @"Desktop spotlight: Spotlight wallpaper off.",
            Sometimes, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", @"DisableSpotlightCollectionOnDesktop", 1, RegistryValueKind.DWord, null),
                new RegistryDeleteAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", @"{2cc5ca98-6485-489a-920e-b3e88a6ccce3}", RegistryValueKind.DWord, null)))),
        Choice(@"Desktop spotlight", @"Windows default", Mk("rd.choice.desktop-spotlight.windows-default", Category.Debloat, "", @"Desktop spotlight: Windows default", @"Desktop spotlight: Windows default.",
            Sometimes, L(
                new RegistryDeleteAction(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", @"DisableSpotlightCollectionOnDesktop", RegistryValueKind.DWord, null),
                new RegistryDeleteAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", @"{2cc5ca98-6485-489a-920e-b3e88a6ccce3}", RegistryValueKind.DWord, null)))),
        Choice(@"Taskbar search", @"Search box (default)", Mk("rd.choice.taskbar-search.search-box-default", Category.Debloat, "", @"Taskbar search: Search box (default)", @"Taskbar search: Search box (default).",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", @"SearchboxTaskbarMode", 2, RegistryValueKind.DWord, null)))),
        Choice(@"Taskbar search", @"Icon and label", Mk("rd.choice.taskbar-search.icon-and-label", Category.Debloat, "", @"Taskbar search: Icon and label", @"Taskbar search: Icon and label.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", @"SearchboxTaskbarMode", 3, RegistryValueKind.DWord, null)))),
        Choice(@"Taskbar search", @"Icon only", Mk("rd.choice.taskbar-search.icon-only", Category.Debloat, "", @"Taskbar search: Icon only", @"Taskbar search: Icon only.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", @"SearchboxTaskbarMode", 1, RegistryValueKind.DWord, null)))),
        Choice(@"Taskbar search", @"Hidden", Mk("rd.choice.taskbar-search.hidden", Category.Debloat, "", @"Taskbar search: Hidden", @"Taskbar search: Hidden.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", @"SearchboxTaskbarMode", 0, RegistryValueKind.DWord, null)))),
        Choice(@"Combine taskbar buttons", @"Always (default)", Mk("rd.choice.combine-taskbar-buttons.always-default", Category.Debloat, "", @"Combine taskbar buttons: Always (default)", @"Combine taskbar buttons: Always (default).",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarGlomLevel", 0, RegistryValueKind.DWord, null)))),
        Choice(@"Combine taskbar buttons", @"When the taskbar is full", Mk("rd.choice.combine-taskbar-buttons.when-the-taskbar-is-full", Category.Debloat, "", @"Combine taskbar buttons: When the taskbar is full", @"Combine taskbar buttons: When the taskbar is full.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarGlomLevel", 1, RegistryValueKind.DWord, null)))),
        Choice(@"Combine taskbar buttons", @"Never", Mk("rd.choice.combine-taskbar-buttons.never", Category.Debloat, "", @"Combine taskbar buttons: Never", @"Combine taskbar buttons: Never.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"TaskbarGlomLevel", 2, RegistryValueKind.DWord, null)))),
        Choice(@"Combine buttons on other screens", @"Always (default)", Mk("rd.choice.combine-buttons-on-other-screens.always-default", Category.Debloat, "", @"Combine buttons on other screens: Always (default)", @"Combine buttons on other screens: Always (default).",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MMTaskbarGlomLevel", 0, RegistryValueKind.DWord, null)))),
        Choice(@"Combine buttons on other screens", @"When the taskbar is full", Mk("rd.choice.combine-buttons-on-other-screens.when-the-taskbar-is-full", Category.Debloat, "", @"Combine buttons on other screens: When the taskbar is full", @"Combine buttons on other screens: When the taskbar is full.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MMTaskbarGlomLevel", 1, RegistryValueKind.DWord, null)))),
        Choice(@"Combine buttons on other screens", @"Never", Mk("rd.choice.combine-buttons-on-other-screens.never", Category.Debloat, "", @"Combine buttons on other screens: Never", @"Combine buttons on other screens: Never.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MMTaskbarGlomLevel", 2, RegistryValueKind.DWord, null)))),
        Choice(@"Show taskbar apps on", @"All taskbars (default)", Mk("rd.choice.show-taskbar-apps-on.all-taskbars-default", Category.Debloat, "", @"Show taskbar apps on: All taskbars (default)", @"Show taskbar apps on: All taskbars (default).",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MMTaskbarMode", 0, RegistryValueKind.DWord, null)))),
        Choice(@"Show taskbar apps on", @"Main taskbar and where the window is", Mk("rd.choice.show-taskbar-apps-on.main-taskbar-and-where-the-window-is", Category.Debloat, "", @"Show taskbar apps on: Main taskbar and where the window is", @"Show taskbar apps on: Main taskbar and where the window is.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MMTaskbarMode", 1, RegistryValueKind.DWord, null)))),
        Choice(@"Show taskbar apps on", @"Only where the window is", Mk("rd.choice.show-taskbar-apps-on.only-where-the-window-is", Category.Debloat, "", @"Show taskbar apps on: Only where the window is", @"Show taskbar apps on: Only where the window is.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MMTaskbarMode", 2, RegistryValueKind.DWord, null)))),
        Choice(@"Browser tabs in Alt+Tab", @"Don't show tabs", Mk("rd.choice.browser-tabs-in-alt-tab.don-t-show-tabs", Category.Debloat, "", @"Browser tabs in Alt+Tab: Don't show tabs", @"Browser tabs in Alt+Tab: Don't show tabs.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MultiTaskingAltTabFilter", 3, RegistryValueKind.DWord, null)))),
        Choice(@"Browser tabs in Alt+Tab", @"3 most recent (default)", Mk("rd.choice.browser-tabs-in-alt-tab.3-most-recent-default", Category.Debloat, "", @"Browser tabs in Alt+Tab: 3 most recent (default)", @"Browser tabs in Alt+Tab: 3 most recent (default).",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MultiTaskingAltTabFilter", 2, RegistryValueKind.DWord, null)))),
        Choice(@"Browser tabs in Alt+Tab", @"5 most recent", Mk("rd.choice.browser-tabs-in-alt-tab.5-most-recent", Category.Debloat, "", @"Browser tabs in Alt+Tab: 5 most recent", @"Browser tabs in Alt+Tab: 5 most recent.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MultiTaskingAltTabFilter", 1, RegistryValueKind.DWord, null)))),
        Choice(@"Browser tabs in Alt+Tab", @"20 most recent", Mk("rd.choice.browser-tabs-in-alt-tab.20-most-recent", Category.Debloat, "", @"Browser tabs in Alt+Tab: 20 most recent", @"Browser tabs in Alt+Tab: 20 most recent.",
            s => W11(s, Verdict.Opt()), L(
                new RegistryAction(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", @"MultiTaskingAltTabFilter", 0, RegistryValueKind.DWord, null)))),
        Choice(@"Drive letters", @"After the name (default)", Mk("rd.choice.drive-letters.after-the-name-default", Category.Debloat, "", @"Drive letters: After the name (default)", @"Drive letters: After the name (default).",
            Sometimes, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShowDriveLettersFirst", 0, RegistryValueKind.DWord, null)))),
        Choice(@"Drive letters", @"Before the name", Mk("rd.choice.drive-letters.before-the-name", Category.Debloat, "", @"Drive letters: Before the name", @"Drive letters: Before the name.",
            Sometimes, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShowDriveLettersFirst", 4, RegistryValueKind.DWord, 0)))),
        Choice(@"Drive letters", @"Network drives first", Mk("rd.choice.drive-letters.network-drives-first", Category.Debloat, "", @"Drive letters: Network drives first", @"Drive letters: Network drives first.",
            Sometimes, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShowDriveLettersFirst", 1, RegistryValueKind.DWord, 0)))),
        Choice(@"Drive letters", @"Hidden", Mk("rd.choice.drive-letters.hidden", Category.Debloat, "", @"Drive letters: Hidden", @"Drive letters: Hidden.",
            Sometimes, L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", @"ShowDriveLettersFirst", 2, RegistryValueKind.DWord, 0)))),
        Choice(@"All apps view in Start", @"By category (default)", Mk("rd.choice.all-apps-view-in-start.by-category-default", Category.Debloat, "", @"All apps view in Start: By category (default)", @"All apps view in Start: By category (default).",
            s => s.Build >= 26200 ? Verdict.Opt() : Verdict.No("Needs a newer version of Windows 11."), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Start", @"AllAppsViewMode", 0, RegistryValueKind.DWord, null)))),
        Choice(@"All apps view in Start", @"Grid", Mk("rd.choice.all-apps-view-in-start.grid", Category.Debloat, "", @"All apps view in Start: Grid", @"All apps view in Start: Grid.",
            s => s.Build >= 26200 ? Verdict.Opt() : Verdict.No("Needs a newer version of Windows 11."), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Start", @"AllAppsViewMode", 1, RegistryValueKind.DWord, null)))),
        Choice(@"All apps view in Start", @"List", Mk("rd.choice.all-apps-view-in-start.list", Category.Debloat, "", @"All apps view in Start: List", @"All apps view in Start: List.",
            s => s.Build >= 26200 ? Verdict.Opt() : Verdict.No("Needs a newer version of Windows 11."), L(
                new RegistryAction(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Start", @"AllAppsViewMode", 2, RegistryValueKind.DWord, null)))),
    };

    static Tweak Choice(string group, string label, Tweak t) { t.ChoiceGroup = group; t.ChoiceLabel = label; return t; }
}
