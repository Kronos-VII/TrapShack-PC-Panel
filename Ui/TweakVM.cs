using System.ComponentModel;
using System.Text;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>What one tweak card in the UI binds to.</summary>
public sealed class TweakVM : INotifyPropertyChanged
{
    public Tweak T { get; }
    public Verdict V { get; }

    bool _isOn;
    bool _busy;

    public TweakVM(Tweak tweak, Verdict verdict)
    {
        T = tweak;
        V = verdict;
        Tip = BuildTip();
        PageKey = PageDefs.KeyOf(tweak);
    }

    public string Title => T.Title;
    public string Summary => T.Summary;
    public string Group => T.Group;
    public string PageKey { get; }
    public bool IsOneShot => T.OneShot;
    public bool IsSwitch => !T.OneShot;
    public string PageTitle => PageDefs.Get(PageKey).Title;
    public bool IsAdvanced => T.Risk == Risk.Advanced;
    string? _glyph;
    public string RowGlyph => _glyph ??= TweakGlyph.For(T);
    public string Tip { get; }

    public string DetailText => T.Detail ?? T.Summary;
    public bool HasDetail => !string.IsNullOrWhiteSpace(T.Detail) || T.Actions.Count > 0;

    public List<string> ChangeLines
    {
        get
        {
            var lines = T.Actions.Take(6).Select(a => "•  " + a.Describe()).ToList();
            if (T.Actions.Count > 6) lines.Add($"•  …and {T.Actions.Count - 6} more");
            return lines;
        }
    }

    public string RestoreNote => T.StoreRestore
        ? "To restore: reinstall the apps from the Microsoft Store."
        : "To restore: switch it off, or use Reset on this page.";

    public bool IsOn => _isOn;

    /// <summary>
    /// What the badge shows. Once something is applied, "recommended" or "not available" no longer matter,
    /// so the card just shows the switch in its on position.
    /// </summary>
    Fit DisplayFit => _isOn && (V.Fit == Fit.Recommended || V.Fit == Fit.Unavailable) ? Fit.Optional : V.Fit;

    public string FitKind => DisplayFit.ToString();

    public string FitText => DisplayFit switch
    {
        Fit.Recommended => "Recommended",
        Fit.Caution => "Caution",
        Fit.Unavailable => "Not available",
        _ => "",
    };

    public string? Note => _isOn && V.Fit == Fit.Unavailable ? null : V.Note;
    public bool HasNote => !string.IsNullOrEmpty(Note);

    /// <summary>Locked only when the tweak can't apply to this PC and isn't already on (so it can always be undone).</summary>
    public bool CanToggle => !_busy && (V.Fit != Fit.Unavailable || _isOn);

    public string Tags
    {
        get
        {
            var tags = new List<string>();
            if (T.RestartRequired) tags.Add("Restart required");
            else if (T.SignOutRequired) tags.Add("Sign-out required");
            if (T.StoreRestore) tags.Add("Restore: reinstall from Microsoft Store");
            return string.Join("   ·   ", tags);
        }
    }

    public bool HasTags => Tags.Length > 0;
    public bool HasFooter => FitText.Length > 0 || HasTags;

    /// <summary>
    /// The one label a row shows, most important first: not available, caution, recommended (while off), advanced,
    /// then what it needs afterwards. "rec" is red, "warn" amber, "info" grey, "" none.
    /// </summary>
    public (string Text, string Kind) RowTag =>
        DisplayFit == Fit.Unavailable ? ("Not available", "info") :
        DisplayFit == Fit.Caution ? ("Caution", "warn") :
        DisplayFit == Fit.Recommended ? ("Recommended", "rec") :
        IsAdvanced ? ("Advanced", "warn") :
        T.RestartRequired ? ("Restart", "info") :
        T.SignOutRequired ? ("Sign-out", "info") :
        T.StoreRestore ? ("Store reinstall", "info") : ("", "");
    public string Label => RowTag.Text;
    public string LabelKind => RowTag.Kind;

    public void SetOn(bool on)
    {
        _isOn = on;
        // Always raise, even if the value is unchanged, so a ToggleButton the user clicked snaps back to the real state.
        foreach (var name in new[] { nameof(IsOn), nameof(CanToggle), nameof(FitKind), nameof(FitText), nameof(Note), nameof(HasNote), nameof(HasFooter), nameof(Label), nameof(LabelKind) })
            Raise(name);
    }

    public void SetBusy(bool busy)
    {
        _busy = busy;
        Raise(nameof(CanToggle));
    }

    string BuildTip()
    {
        var body = (T.Detail ?? T.Summary).Trim();
        if (body.Length > 230) body = body[..227].TrimEnd() + "…";
        var n = T.Actions.Count;
        var foot = new List<string> { n == 1 ? "1 change" : $"{n} changes" };
        foot.Add(T.StoreRestore ? "reinstall from Store to restore" : "fully reversible");
        if (T.RestartRequired) foot.Add("restart needed");
        else if (T.SignOutRequired) foot.Add("sign-out needed");
        return body + "\n\n" + string.Join("  ·  ", foot);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
