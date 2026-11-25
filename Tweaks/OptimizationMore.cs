using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

// Further optimization settings from the published definitions, each with its recommended and default value.
public static partial class Catalog
{
    static IEnumerable<Tweak> OptimizationMore()
    {
        yield return Mk("opt.gaming-gpu-priority", Category.Gaming, "Graphics",
            "GPU priority for games",
            "Gives games a higher GPU scheduling priority.",
            Always,
            L(new RegistryAction(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "GPU Priority", 8, RegistryValueKind.DWord, 2)),
            detail: "Gives games a higher GPU scheduling priority. Restore puts your previous values back.");

        yield return Mk("opt.privacy-advertising-id", Category.Privacy, "Privacy & security",
            "Turn off the advertising ID",
            "Stops apps using a unique advertising ID to personalize ads.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CPSS\\Store\\AdvertisingInfo", "Value", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo", "DisabledByGroupPolicy", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo", "DisabledByGroupPolicy", 1, RegistryValueKind.DWord, null)),
            detail: "Stops apps using a unique advertising ID to personalize ads. Restore puts your previous values back.");

        yield return Mk("opt.privacy-inking-typing-dictionary", Category.Privacy, "Speech & inking",
            "Stop the custom inking and typing dictionary",
            "Stops Windows building a personal dictionary from what you type and write.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CPSS\\Store\\InkingAndTypingPersonalization", "Value", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Personalization\\Settings", "AcceptedPrivacyPolicy", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\InputPersonalization", "RestrictImplicitTextCollection", 1, RegistryValueKind.DWord, 0),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\InputPersonalization\\TrainedDataStore", "HarvestContacts", 0, RegistryValueKind.DWord, 1)),
            detail: "Stops Windows building a personal dictionary from what you type and write. Restore puts your previous values back.");

        yield return Mk("opt.privacy-diagnostics", Category.Privacy, "Diagnostics & feedback",
            "Set diagnostic data to the minimum",
            "Sets the diagnostic data level to the lowest Windows allows and turns off app telemetry.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Diagnostics\\DiagTrack", "ShowedToastAtLevel", 1, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection", "MaxTelemetryAllowed", 0, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection", "MaxTelemetryAllowed", 0, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord, 3),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "AITEnable", 0, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "AITEnable", 0, RegistryValueKind.DWord, null)),
            detail: "Sets the diagnostic data level to the lowest Windows allows and turns off app telemetry. Restore puts your previous values back.");

        yield return Mk("opt.privacy-improve-inking-typing", Category.Privacy, "Diagnostics & feedback",
            "Don't send inking and typing data",
            "Stops inking and typing samples being sent to improve Windows.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Input\\TIPC", "Enabled", 0, RegistryValueKind.DWord, 1),
              new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CPSS\\Store\\ImproveInkingAndTyping", "Value", 0, RegistryValueKind.DWord, 1)),
            detail: "Stops inking and typing samples being sent to improve Windows. Restore puts your previous values back.");

        yield return Mk("opt.privacy-feedback-frequency", Category.Privacy, "Diagnostics & feedback",
            "Stop Windows asking for feedback",
            "Turns off the feedback pop-ups.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod", 0, RegistryValueKind.DWord, null)),
            detail: "Turns off the feedback pop-ups. Restore puts your previous values back.");

        yield return Mk("opt.sound-communication-ducking", Category.Customize, "Sound",
            "Don't lower other sounds during calls",
            "Stops Windows turning down your music and games when it detects a call.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "Software\\Microsoft\\Multimedia\\Audio", "UserDuckingPreference", 3, RegistryValueKind.DWord, 1)),
            detail: "Sets the communications ducking preference to \"Do nothing\". Restore goes back to \"Reduce the volume of other sounds by 80%\".");

        yield return Mk("opt.updates-driver-controls", Category.Updates, "Update behaviour",
            "Keep drivers out of Windows Update",
            "Stops Windows Update installing hardware drivers with quality updates.",
            Always,
            L(new RegistryAction(RegistryHive.CurrentUser, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1, RegistryValueKind.DWord, null),
              new RegistryAction(RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1, RegistryValueKind.DWord, null)),
            detail: "Applies the \"Do not include drivers with Windows Updates\" policy. Install graphics and other drivers from the maker's own site or the Apps page instead. Restore removes the policy.");
    }
}
