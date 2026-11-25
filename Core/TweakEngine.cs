namespace TrapShack.Core;

/// <summary>Applies, detects and reverts tweaks. All methods are blocking: call them off the UI thread.</summary>
public sealed class TweakEngine
{
    public StateStore State { get; } = new();

    static string Key(Tweak t, int index) => $"{t.Id}/{index}";

    /// <summary>
    /// A tweak is "on" when every action that applies to this PC is currently in effect.
    /// This is read straight from Windows, so settings applied earlier (or by other tools) are detected too.
    /// </summary>
    public bool IsApplied(Tweak t)
    {
        var applicable = t.Actions.Where(a => a.IsApplicable()).ToList();
        return applicable.Count > 0 && applicable.All(a => a.IsApplied());
    }

    public bool IsAvailable(Tweak t) => t.Actions.Any(a => a.IsApplicable());

    /// <summary>True when TrapShack itself changed this tweak (so there is a backup to restore from).</summary>
    public bool HasBackup(Tweak t) => State.HasPrefix(t.Id + "/");

    /// <summary>
    /// Applies every action. If anything fails, everything this call already touched is put back,
    /// so a tweak is never left half-applied.
    /// </summary>
    public void Apply(Tweak t, Action<int>? onStep = null)
    {
        Log.Info($"Apply {t.Id}");
        var touched = new List<(int Index, TweakAction Action)>();
        var ordinal = 0;
        try
        {
            for (var i = 0; i < t.Actions.Count; i++)
            {
                var action = t.Actions[i];
                if (!action.IsApplicable()) continue;
                onStep?.Invoke(ordinal++);

                var key = Key(t, i);
                if (!State.Has(key))
                {
                    // If this target is already in the tweaked state (set earlier by another TrapShack tweak that
                    // shares the same registry value, or by another tool), the live value is not an "original".
                    // Store nothing so Revert falls back to the Windows default instead of a stale value.
                    State.Set(key, action.IsApplied() ? "" : (action.Capture() ?? ""));
                }
                touched.Add((i, action));
                action.Apply();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Apply {t.Id} failed: {ex.Message}. Rolling back.");
            for (var n = touched.Count - 1; n >= 0; n--)
            {
                var (index, action) = touched[n];
                try
                {
                    var saved = State.Get(Key(t, index));
                    action.Revert(string.IsNullOrEmpty(saved) ? null : saved);
                }
                catch (Exception rex) { Log.Error($"Rollback of {t.Id}/{index} failed: {rex.Message}"); }
                State.Remove(Key(t, index));
            }
            throw;
        }
    }

    /// <summary>Restores the original state. Returns a hint to show the user when manual steps remain.</summary>
    public string? Revert(Tweak t, Action<int>? onStep = null)
    {
        Log.Info($"Revert {t.Id}");
        var ordinal = 0;
        for (var i = 0; i < t.Actions.Count; i++)
        {
            var action = t.Actions[i];
            if (!action.IsApplicable()) continue;
            onStep?.Invoke(ordinal++);

            var saved = State.Get(Key(t, i));
            action.Revert(string.IsNullOrEmpty(saved) ? null : saved);
        }
        State.RemovePrefix(t.Id + "/");

        return t.StoreRestore
            ? "These apps were removed from Windows itself. To get them back, reinstall them from the Microsoft Store."
            : null;
    }
}
