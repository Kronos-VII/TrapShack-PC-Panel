using System.Collections.Concurrent;

namespace TrapShack.Core;

/// <summary>
/// Remembers the answer of a check that has to start PowerShell (BitLocker, Defender), so it is asked at most every few
/// minutes instead of on every re-check. Anything that changes the answer calls <see cref="Forget"/> first, so the
/// read-back right after an apply or an undo is always fresh.
/// </summary>
public static class SlowCheck
{
    static readonly ConcurrentDictionary<string, (DateTime At, object? Value)> Answers = new();
    static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(10);

    public static T Get<T>(string key, Func<T> read)
    {
        if (Answers.TryGetValue(key, out var a) && DateTime.UtcNow - a.At < MaxAge) return (T)a.Value!;
        var value = read();
        Answers[key] = (DateTime.UtcNow, value);
        return value;
    }

    public static void Forget(string key) => Answers.TryRemove(key, out _);

    /// <summary>Drops every remembered answer, so the next read asks Windows again (used by the PC scan).</summary>
    public static void ForgetAll() => Answers.Clear();
}
