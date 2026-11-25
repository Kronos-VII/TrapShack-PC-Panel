using System.IO;
using System.Text.Json;

namespace TrapShack.Core;

/// <summary>Small user settings file, kept next to the undo data so it survives updates and restarts.</summary>
public sealed class Settings
{
    public string Accent { get; set; } = "#E5213B";

    /// <summary>GitHub repository (owner/name) the Update button checks for new releases.</summary>
    public string UpdateRepo { get; set; } = "Kronos-VII/TrapShack-PC-Panel";

    /// <summary>True once the one-time "create a restore point first?" question has been answered.</summary>
    public bool RestorePointAsked { get; set; }

    /// <summary>Sections the user folded up, as "page|section" keys.</summary>
    public List<string> ClosedSections { get; set; } = new();

    /// <summary>Apps ticked on the Apps page, kept between runs. Installed apps are dropped from it.</summary>
    public List<string> AppPicks { get; set; } = new();

    /// <summary>The soft glow, rise-in and drifting haze. On by default.</summary>
    public bool LiveEffects { get; set; } = true;
    /// <summary>Show the console under the progress panel (the commands TrapShack runs, live).</summary>
    public bool ShowConsole { get; set; }

    /// <summary>True once the "missing runtimes" note has been dismissed.</summary>
    public bool RuntimeNoticeHidden { get; set; }

    static string FilePath => Path.Combine(Log.Dir, "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch (Exception ex) { Log.Error("Could not read settings: " + ex.Message); }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Log.Dir);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, FilePath, true);
        }
        catch (Exception ex) { Log.Error("Could not save settings: " + ex.Message); }
    }
}
