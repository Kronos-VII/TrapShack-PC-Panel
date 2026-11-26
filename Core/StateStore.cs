using System.IO;
using System.Text.Json;

namespace TrapShack.Core;

/// <summary>
/// Remembers the ORIGINAL value of everything TrapShack changes, so every toggle can be put back exactly
/// as it was. Stored in %ProgramData%\TrapShack\state.json. Keys look like "tweak.id/actionIndex".
/// </summary>
public sealed class StateStore
{
    readonly string _file = Path.Combine(Log.Dir, "state.json");
    readonly object _gate = new();
    Dictionary<string, string> _data = new();

    public StateStore()
    {
        try
        {
            if (File.Exists(_file))
                _data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_file)) ?? new();
        }
        catch (Exception ex)
        {
            Log.Error("Could not read state file: " + ex.Message);
            _data = new();
        }
    }

    public bool Has(string key) { lock (_gate) return _data.ContainsKey(key); }

    public string? Get(string key) { lock (_gate) return _data.TryGetValue(key, out var v) ? v : null; }

    public void Set(string key, string value)
    {
        lock (_gate) { _data[key] = value; Save(); }
    }

    /// <summary>Several keys at once with a single save (first run after an update can mark dozens).</summary>
    public void SetMany(IEnumerable<string> keys, string value)
    {
        lock (_gate)
        {
            var changed = false;
            foreach (var k in keys) if (!_data.TryGetValue(k, out var old) || old != value) { _data[k] = value; changed = true; }
            if (changed) Save();
        }
    }

    public void Remove(string key)
    {
        lock (_gate) { if (_data.Remove(key)) Save(); }
    }

    public bool HasPrefix(string prefix)
    {
        lock (_gate) return _data.Keys.Any(k => k.StartsWith(prefix, StringComparison.Ordinal));
    }

    public void RemovePrefix(string prefix)
    {
        lock (_gate)
        {
            foreach (var k in _data.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
                _data.Remove(k);
            Save();
        }
    }

    void Save()
    {
        try
        {
            Directory.CreateDirectory(Log.Dir);
            File.WriteAllText(_file, JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { Log.Error("Could not save state file: " + ex.Message); }
    }
}
