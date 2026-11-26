using TrapShack.Core;
using Microsoft.Win32;

namespace TrapShack.Tweaks;

/// <summary>
/// The list of everything TrapShack can change. Each page lives in its own file (DebloatTweaks.cs,
/// PrivacyTweaks.cs ...). To add a tweak, copy an existing one with Mk(...) into the right page.
/// </summary>
public static partial class Catalog
{
    public static List<Tweak> Build() =>
        ApplyDebloatLayout(Debloat()
            .Concat(Essentials())
            .Concat(AdvancedDebloat())
            .Concat(MoreEssentials())
            .Concat(MoreSettings())
            .Concat(OptimizationSettings())
            .Concat(OptimizationMore())
            .Concat(DebloatExtras())
            .Concat(EssentialSet())
            .Concat(MoreCleanup())
            .Concat(MoreOptions())
            .Concat(PowerSettings())
            .Concat(Customize())
            .Concat(Privacy())
            .Concat(MorePrivacy())
            .Concat(Performance())
            .Concat(MorePerformance())
            .Concat(Gaming())
            .Concat(MoreGaming())
            .Concat(Network())
            .Concat(Updates())
            .Concat(Browsers())
            .Concat(DebloatMissing()).Concat(OptionalServices())
            .Concat(DebloatPlus())
            .Concat(SourceSettings())
            .Concat(MoreSourceSettings())
            .ToList());

    // ---- builders -------------------------------------------------------------------------------

    static Tweak Mk(
        string id, Category category, string group, string title, string summary,
        Func<SystemInfo, Verdict> evaluate, IEnumerable<TweakAction> actions,
        string? detail = null, Risk risk = Risk.Safe,
        bool restart = false, bool signOut = false, bool store = false, string? confirm = null)
    {
        var t = new Tweak
        {
            Id = id, Category = category, Group = group, Title = title, Summary = summary,
            Detail = detail, Risk = risk, RestartRequired = restart, SignOutRequired = signOut,
            StoreRestore = store, Confirm = confirm, Evaluate = evaluate,
        };
        t.Actions.AddRange(actions);
        return t;
    }

    /// <summary>Flattens single actions and lists of actions into one list.</summary>
    static IEnumerable<TweakAction> L(params object[] parts)
    {
        foreach (var p in parts)
        {
            if (p is TweakAction a) yield return a;
            else if (p is IEnumerable<TweakAction> many)
                foreach (var x in many) yield return x;
        }
    }

    /// <summary>HKLM DWORD. <paramref name="windowsDefault"/> is used only if no backup exists when reverting.</summary>
    static RegistryAction LM(string path, string name, int value, object? windowsDefault = null) =>
        new(RegistryHive.LocalMachine, path, name, value, RegistryValueKind.DWord, windowsDefault);

    /// <summary>HKLM string.</summary>
    static RegistryAction LMs(string path, string name, string value, object? windowsDefault = null) =>
        new(RegistryHive.LocalMachine, path, name, value, RegistryValueKind.String, windowsDefault);

    /// <summary>HKCU DWORD.</summary>
    static RegistryAction CU(string path, string name, int value, object? windowsDefault = null) =>
        new(RegistryHive.CurrentUser, path, name, value, RegistryValueKind.DWord, windowsDefault);

    /// <summary>HKCU string.</summary>
    static RegistryAction CUs(string path, string name, string value, object? windowsDefault = null) =>
        new(RegistryHive.CurrentUser, path, name, value, RegistryValueKind.String, windowsDefault);

    /// <summary>Shared gate for settings that only exist on Windows 11.</summary>
    static Verdict W11(SystemInfo s, Verdict ok) => s.IsWindows11 ? ok : Verdict.No("This setting only exists on Windows 11.");

    const string Adv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

    static Verdict Always(SystemInfo _) => Verdict.Rec();
    static Verdict Sometimes(SystemInfo _) => Verdict.Opt();
}
