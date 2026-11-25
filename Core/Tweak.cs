namespace TrapShack.Core;

public enum Category { Debloat, Customize, Privacy, Performance, Gaming, Network, Updates, Browsers }

public enum Risk { Safe, Advanced }

/// <summary>How well a tweak fits THIS computer.</summary>
public enum Fit
{
    /// <summary>Good idea here. Included in "Apply recommended".</summary>
    Recommended,
    /// <summary>Safe, but a matter of taste. Never applied automatically.</summary>
    Optional,
    /// <summary>Works, but has a downside on this PC. Never applied automatically.</summary>
    Caution,
    /// <summary>Does not apply to this PC (wrong Windows version, app not installed...). Switch is locked.</summary>
    Unavailable,
}

public sealed record Verdict(Fit Fit, string? Note = null)
{
    public static Verdict Rec(string? note = null) => new(Fit.Recommended, note);
    public static Verdict Opt(string? note = null) => new(Fit.Optional, note);
    public static Verdict Warn(string note) => new(Fit.Caution, note);
    public static Verdict No(string note) => new(Fit.Unavailable, note);
}

public sealed class Tweak
{
    public required string Id { get; init; }
    public required Category Category { get; set; }
    /// <summary>Sub-heading inside a page.</summary>
    public string Group { get; set; } = "";
    public required string Title { get; init; }
    /// <summary>One line shown on the card.</summary>
    public required string Summary { get; init; }
    /// <summary>Longer explanation shown in the hover tooltip.</summary>
    public string? Detail { get; init; }
    public Risk Risk { get; init; } = Risk.Safe;
    public bool RestartRequired { get; init; }
    public bool SignOutRequired { get; init; }
    /// <summary>True when "off" can only be done by reinstalling from the Microsoft Store.</summary>
    public bool StoreRestore { get; init; }
    /// <summary>Extra warning text shown in a confirmation dialog before applying.</summary>
    public string? Confirm { get; init; }
    /// <summary>Decides whether this tweak suits the detected PC.</summary>
    public Func<SystemInfo, Verdict> Evaluate { get; init; } = _ => Verdict.Opt();
    /// <summary>A one-time action (cleanup) shown with a "Run now" button instead of a switch.</summary>
    public bool OneShot { get; set; }
    /// <summary>Set for settings that are one option of a drop-down (for example "Combine taskbar buttons"): the drop-down's name.</summary>
    public string? ChoiceGroup { get; set; }
    /// <summary>The option's text inside its drop-down.</summary>
    public string? ChoiceLabel { get; set; }
    public List<TweakAction> Actions { get; } = new();

    /// <summary>True when the tweak writes machine policies that a domain or MDM could override.</summary>
    public bool UsesMachinePolicy => Actions.OfType<RegistryAction>().Any(a => a.IsMachinePolicy);
}
