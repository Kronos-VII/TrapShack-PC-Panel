using TrapShack.Core;

namespace TrapShack.Tweaks;

// Optional service switches. Each card switches one Windows service to Disabled (or Manual)
// and puts it back to its original startup type on restore. All are personal preferences, so none is applied automatically.
public static partial class Catalog
{
    static IEnumerable<Tweak> OptionalServices()
    {
        yield return Mk("opt.gaming-compatibility-assistant-service", Category.Performance, "System services",
            "Set Program Compatibility Assistant Service to Disabled",
            "Monitors programs for compatibility issues and suggests fixes. Disabling prevents compatibility prompts and saves minor system resources",
            Sometimes,
            L(new ServiceAction("PcaSvc", "disabled", "auto")),
            detail: "Monitors programs for compatibility issues and suggests fixes. Disabling prevents compatibility prompts and saves minor system resources. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-retail-demo-service", Category.Performance, "System services",
            "Set Retail Demo Service to Disabled",
            "Controls device activity when in retail demo mode. Safe to disable for personal computers as it only serves retail display purposes",
            Sometimes,
            L(new ServiceAction("RetailDemo", "disabled", "demand")),
            detail: "Controls device activity when in retail demo mode. Safe to disable for personal computers as it only serves retail display purposes. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-insider-service", Category.Performance, "System services",
            "Set Windows Insider Service to Disabled",
            "Manages Windows Insider Program features and preview builds. Safe to disable if you're not participating in the Windows Insider Program",
            Sometimes,
            L(new ServiceAction("wisvc", "disabled", "demand")),
            detail: "Manages Windows Insider Program features and preview builds. Safe to disable if you're not participating in the Windows Insider Program. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-phone-service", Category.Performance, "System services",
            "Set Phone Service to Disabled",
            "Manages telephony state on the device. Safe to disable if you don't use phone connectivity features or make calls from your PC",
            Sometimes,
            L(new ServiceAction("PhoneSvc", "disabled", "demand")),
            detail: "Manages telephony state on the device. Safe to disable if you don't use phone connectivity features or make calls from your PC. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-wallet-service", Category.Performance, "System services",
            "Set Wallet Service to Disabled",
            "Provides wallet functionality for payment and NFC scenarios. Safe to disable if you don't use Microsoft Wallet features",
            Sometimes,
            L(new ServiceAction("WalletService", "disabled", "demand")),
            detail: "Provides wallet functionality for payment and NFC scenarios. Safe to disable if you don't use Microsoft Wallet features. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-wmp-network-service", Category.Performance, "System services",
            "Set Windows Media Player Network Sharing Service to Disabled",
            "Shares Windows Media Player libraries to other networked players and media devices. Safe to disable if you don't share media over your network",
            Always,
            L(new ServiceAction("WMPNetworkSvc", "disabled", "demand")),
            detail: "Shares Windows Media Player libraries to other networked players and media devices. Safe to disable if you don't share media over your network. Part of the recommended set. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-mobile-hotspot-service", Category.Performance, "System services",
            "Set Windows Mobile Hotspot Service to Disabled",
            "Provides ability to share internet connection with other devices. Set to Manual to keep functionality available while preventing unnecessary…",
            Sometimes,
            L(new ServiceAction("icssvc", "disabled", "demand")),
            detail: "Provides ability to share internet connection with other devices. Set to Manual to keep functionality available while preventing unnecessary background activity. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-sms-router-service", Category.Performance, "System services",
            "Set Microsoft Windows SMS Router Service to Disabled",
            "Routes SMS messages according to rules. Safe to disable if you don't use SMS features on your PC",
            Sometimes,
            L(new ServiceAction("SmsRouter", "disabled", "demand")),
            detail: "Routes SMS messages according to rules. Safe to disable if you don't use SMS features on your PC. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-parental-controls-service", Category.Performance, "System services",
            "Set Parental Controls Service to Disabled",
            "Enables parental controls and family safety features. Safe to disable if you don't use parental control features",
            Sometimes,
            L(new ServiceAction("WpcMonSvc", "disabled", "demand")),
            detail: "Enables parental controls and family safety features. Safe to disable if you don't use parental control features. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-payments-nfc-service", Category.Performance, "System services",
            "Set Payments and NFC/SE Manager to Disabled",
            "Manages payments and Near Field Communication secure elements. Safe to disable if you don't use NFC payment features",
            Sometimes,
            L(new ServiceAction("SEMgrSvc", "disabled", "demand")),
            detail: "Manages payments and Near Field Communication secure elements. Safe to disable if you don't use NFC payment features. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-spot-verifier-service", Category.Performance, "System services",
            "Set Spot Verifier Service to Disabled",
            "Verifies potential file system corruptions. Set to Manual to allow verification when needed while reducing background activity",
            Sometimes,
            L(new ServiceAction("svsvc", "disabled", "demand")),
            detail: "Verifies potential file system corruptions. Set to Manual to allow verification when needed while reducing background activity. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-remote-access-manager", Category.Performance, "System services",
            "Set Remote Access Connection Manager to Disabled",
            "Manages VPN and dial-up connections. Set to Manual to reduce background activity while keeping VPN functionality available when needed.",
            Sometimes,
            L(new ServiceAction("RasMan", "disabled", "demand")),
            detail: "Manages VPN and dial-up connections. Set to Manual to reduce background activity while keeping VPN functionality available when needed. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-remote-access-auto", Category.Performance, "System services",
            "Set Remote Access Auto Connection Manager to Disabled",
            "Automatically connects to remote networks when programs reference remote resources. Safe to disable if you don't use auto-connect VPN features",
            Sometimes,
            L(new ServiceAction("RasAuto", "disabled", "demand")),
            detail: "Automatically connects to remote networks when programs reference remote resources. Safe to disable if you don't use auto-connect VPN features. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-remote-desktop-services", Category.Performance, "System services",
            "Set Remote Desktop Services to Disabled",
            "Allows users to connect interactively to a remote computer. Set to Manual to reduce background activity while keeping Remote Desktop available.",
            Sometimes,
            L(new ServiceAction("TermService", "disabled", "demand")),
            detail: "Allows users to connect interactively to a remote computer. Set to Manual to reduce background activity while keeping Remote Desktop available. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-remote-desktop-configuration", Category.Performance, "System services",
            "Set Remote Desktop Configuration to Disabled",
            "Manages Remote Desktop Services and Remote Desktop related configurations. Set to Manual to reduce background activity while keeping Remote…",
            Sometimes,
            L(new ServiceAction("SessionEnv", "disabled", "demand")),
            detail: "Manages Remote Desktop Services and Remote Desktop related configurations. Set to Manual to reduce background activity while keeping Remote Desktop available. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-remote-desktop-port-redirector", Category.Performance, "System services",
            "Set Remote Desktop Services UserMode Port Redirector to Disabled",
            "Allows local device redirection for Remote Desktop connections. Safe to disable if you don't need to share local devices during Remote Desktop…",
            Sometimes,
            L(new ServiceAction("UmRdpService", "disabled", "demand")),
            detail: "Allows local device redirection for Remote Desktop connections. Safe to disable if you don't need to share local devices during Remote Desktop sessions. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-xbox-auth-manager", Category.Performance, "System services",
            "Set Xbox Live Auth Manager to Disabled",
            "Provides authentication and authorization services for Xbox Live. Safe to disable if you don't use Xbox Game Pass, Microsoft Store games, or Xbox…",
            _ => Verdict.Warn("Disabling will prevent Xbox Game Pass and Microsoft Store games from working"),
            L(new ServiceAction("XblAuthManager", "disabled", "demand")),
            detail: "Provides authentication and authorization services for Xbox Live. Safe to disable if you don't use Xbox Game Pass, Microsoft Store games, or Xbox features. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true, risk: Risk.Advanced);

        yield return Mk("opt.gaming-xbox-game-save", Category.Performance, "System services",
            "Set Xbox Live Game Save to Disabled",
            "Syncs game saves to Xbox Live cloud. Only needed for Xbox Game Pass and Microsoft Store games with cloud save features",
            Sometimes,
            L(new ServiceAction("XblGameSave", "disabled", "demand")),
            detail: "Syncs game saves to Xbox Live cloud. Only needed for Xbox Game Pass and Microsoft Store games with cloud save features. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-xbox-networking", Category.Performance, "System services",
            "Set Xbox Live Networking Service to Disabled",
            "Supports Xbox Live multiplayer networking. Required for Xbox multiplayer gaming but not needed for Steam/Epic/other gaming platforms",
            Sometimes,
            L(new ServiceAction("XboxNetApiSvc", "disabled", "demand")),
            detail: "Supports Xbox Live multiplayer networking. Required for Xbox multiplayer gaming but not needed for Steam/Epic/other gaming platforms. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-biometric-service", Category.Performance, "System services",
            "Set Windows Biometric Service to Disabled",
            "Enables fingerprint and facial recognition login via Windows Hello. Safe to disable on desktop systems without biometric hardware",
            Sometimes,
            L(new ServiceAction("WbioSrvc", "disabled", "demand")),
            detail: "Enables fingerprint and facial recognition login via Windows Hello. Safe to disable on desktop systems without biometric hardware. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-touch-keyboard-service", Category.Performance, "System services",
            "Set Touch Keyboard and Handwriting Panel Service to Disabled",
            "Manages the Windows Input Experience including touch keyboard, pen/stylus input, handwriting panel, emoji panel (Win+.), and Xbox controller…",
            s => s.IsLaptop ? Verdict.Warn("On a laptop or tablet this can break the touch keyboard, handwriting and the emoji panel (Win+.).") : Verdict.Rec(),
            L(new ServiceAction("TabletInputService", "disabled", "demand")),
            detail: "Manages the Windows Input Experience including touch keyboard, pen/stylus input, handwriting panel, emoji panel (Win+.), and Xbox controller keyboard. Disabling will break all virtual/software keyboard input but is safe on desktop systems without touchscreen, pen, or gamepad. Part of the recommended set. Restore puts the service back to its original startup type.");

        yield return Mk("opt.gaming-telephony-service", Category.Performance, "System services",
            "Set Telephony Service to Disabled",
            "Manages telephony (TAPI) for Phone Link audio relay, modems, fax, and VoIP softphones. Leave at Manual (Windows default) unless you use no…",
            _ => Verdict.Warn("Disabling Telephony breaks Phone Link audio relay, fax software, dial-up modems, and VoIP softphones (e.g. 3CX, Cisco Jabber)."),
            L(new ServiceAction("TapiSrv", "disabled", "demand")),
            detail: "Manages telephony (TAPI) for Phone Link audio relay, modems, fax, and VoIP softphones. Leave at Manual (Windows default) unless you use no telephony software. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true, risk: Risk.Advanced);

        yield return Mk("opt.gaming-sensor-monitoring-service", Category.Performance, "System services",
            "Set Sensor Monitoring Service to Disabled",
            "Monitors various sensors like ambient light and orientation. Safe to disable on desktop systems without sensor hardware",
            Sometimes,
            L(new ServiceAction("SensrSvc", "disabled", "demand")),
            detail: "Monitors various sensors like ambient light and orientation. Safe to disable on desktop systems without sensor hardware. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);

        yield return Mk("opt.gaming-sensor-data-service", Category.Performance, "System services",
            "Set Sensor Data Service to Disabled",
            "Delivers data from a variety of sensors to applications. Safe to disable on desktop systems without sensor hardware",
            Sometimes,
            L(new ServiceAction("SensorDataService", "disabled", "demand")),
            detail: "Delivers data from a variety of sensors to applications. Safe to disable on desktop systems without sensor hardware. Never part of \"Apply recommended\": the recommended startup for this service is Manual, which is how Windows already ships it. Restore puts the service back to its original startup type.", restart: true);
    }
}
