using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using TrapShack.Core;

namespace TrapShack.Ui;

public abstract class Notify : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? n = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; Raise(n); return true;
    }
}

/// <summary>Draws the logo tile for an app: the brand mark on the brand colour, or a clean monogram when there is no mark.</summary>
public sealed class TileArt
{
    public Geometry? Icon { get; }
    public Brush Tile { get; }
    public Brush Glyph { get; }
    public string Mono { get; }
    public System.Windows.Media.ImageSource? Photo { get; }
    public bool HasIcon => Icon != null;
    public Visibility PhotoVisible => Photo != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IconVisible => Photo == null && HasIcon ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MonoVisible => Photo == null && !HasIcon ? Visibility.Visible : Visibility.Collapsed;

    static double Lum(Color c) => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;

    public TileArt(string id, string name, string? mono = null)
    {
        Mono = mono ?? MakeMono(name);
        // A logo file dropped into Assets/AppLogos (named after the app id, as a .png) wins over everything else.
        try
        {
            var uri = new Uri($"pack://application:,,,/Assets/AppLogos/{id}.png");
            if (Application.GetResourceStream(uri) != null)
            {
                var bi = new System.Windows.Media.Imaging.BitmapImage();
                bi.BeginInit(); bi.UriSource = uri; bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bi.EndInit(); bi.Freeze();
                Photo = bi;
                Tile = Brushes.Transparent;   // the logo file is already a rounded square and fills the whole tile
                Glyph = Brushes.White;
                return;
            }
        }
        catch { /* no logo file: fall through to the built-in mark or monogram */ }
        if (AppIcons.Map.TryGetValue(id, out var ic))
        {
            try
            {
                var g = Geometry.Parse(ic.Path); g.Freeze(); Icon = g;
                var c = (Color)ColorConverter.ConvertFromString("#" + ic.Hex.TrimStart('#'));
                var l = Lum(c);
                if (l < 0.1)
                    Tile = new LinearGradientBrush(Color.FromRgb(0x2A, 0x2A, 0x33), Color.FromRgb(0x1B, 0x1B, 0x21), 90);
                else Tile = new SolidColorBrush(c);
                Glyph = new SolidColorBrush(l > 0.72 ? Color.FromRgb(0x14, 0x14, 0x18) : Colors.White);
                Tile.Freeze(); Glyph.Freeze();
                return;
            }
            catch { Icon = null; }
        }
        // Monogram: a calm dark tile with a hue taken from the name, so every app keeps the same look each time.
        var h = 0; foreach (var ch in name) h = (h * 31 + ch) & 0xFFFFFF;
        var hue = h % 360;
        Tile = new SolidColorBrush(FromHsl(hue, 0.32, 0.26)); Tile.Freeze();
        Glyph = new SolidColorBrush(FromHsl(hue, 0.55, 0.80)); Glyph.Freeze();
    }

    static string MakeMono(string name)
    {
        var words = Regex.Matches(name, @"[A-Za-z0-9]+").Select(m => m.Value).Where(w => w.Length > 0).ToList();
        if (words.Count == 0) return "?";
        return words.Count == 1 ? words[0][..Math.Min(2, words[0].Length)] : (words[0][0].ToString() + words[1][0]);
    }

    static Color FromHsl(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = l - c / 2;
        double r, g, b;
        if (h < 60) (r, g, b) = (c, x, 0); else if (h < 120) (r, g, b) = (x, c, 0); else if (h < 180) (r, g, b) = (0, c, x);
        else if (h < 240) (r, g, b) = (0, x, c); else if (h < 300) (r, g, b) = (x, 0, c); else (r, g, b) = (c, 0, x);
        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }
}

/// <summary>One installable app (or one architecture of a runtime).</summary>
public sealed class AppCardVM : Notify
{
    public AppDef Def { get; }
    public TileArt Art { get; }
    public Action<AppCardVM>? Changed;

    bool _installed, _selected, _busy, _failed;
    string _phase = "";
    double _cardW = 300;

    public AppCardVM(AppDef d) { Def = d; Art = new TileArt(d.Id, d.Name); }

    public string Id => Def.Id;
    public string Name => Def.Name;
    public string Desc => Def.Desc;
    public string Package => Def.Winget;
    public string Group => Def.Group;
    public bool IsDep => Def.Dep;
    public string ArchLabel => Def.Id.EndsWith("-x64") ? "64-bit" : Def.Id.EndsWith("-x86") ? "32-bit" : "";

    public double CardW { get => _cardW; set => Set(ref _cardW, value); }

    public bool Installed
    {
        get => _installed;
        set
        {
            if (!Set(ref _installed, value)) return;
            if (value && _selected) { _selected = false; Raise(nameof(Selected)); }
            Raise(nameof(CanSelect)); Raise(nameof(ShowInstalled)); Raise(nameof(CanUninstall)); Raise(nameof(NotInstalled));
            Changed?.Invoke(this);
        }
    }

    public bool Selected
    {
        get => _selected;
        set { if (_installed && value) return; if (Set(ref _selected, value)) Changed?.Invoke(this); }
    }

    public bool Busy { get => _busy; set { if (Set(ref _busy, value)) { Raise(nameof(CanSelect)); Raise(nameof(CanUninstall)); } } }
    public bool Failed { get => _failed; set { if (Set(ref _failed, value)) Raise(nameof(FailedVisible)); } }
    public string Phase { get => _phase; set { if (Set(ref _phase, value)) { Raise(nameof(HasPhase)); Raise(nameof(Subline)); } } }

    /// <summary>The small line at the bottom of a card: what it is doing right now, or else its package id.</summary>
    public string Subline => _phase.Length > 0 ? _phase : Def.Winget;

    public bool NotInstalled => !_installed;
    public bool CanSelect => !_installed && !_busy;
    public bool HasPhase => _phase.Length > 0;
    public Visibility ShowInstalled => _installed ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FailedVisible => _failed ? Visibility.Visible : Visibility.Collapsed;
    public bool CanUninstall => _installed && !Def.Dep && !_busy;
    public Visibility UninstallVisible => Def.Dep ? Visibility.Collapsed : Visibility.Visible;

    public bool Matches(string q) =>
        q.Length == 0 || Def.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
        Def.Desc.Contains(q, StringComparison.OrdinalIgnoreCase) || Def.Winget.Contains(q, StringComparison.OrdinalIgnoreCase);
}

/// <summary>A runtime that comes in 64-bit and 32-bit builds, shown as one card with two check boxes.</summary>
public sealed class PairCardVM : Notify
{
    public AppCardVM X64 { get; }
    public AppCardVM X86 { get; }
    public TileArt Art { get; }
    double _cardW = 300;

    public PairCardVM(AppCardVM x64, AppCardVM x86)
    {
        X64 = x64; X86 = x86;
        Title = Regex.Replace(x64.Name, @"\s*\((x64|x86)\)\s*$", "");
        Art = new TileArt(x64.Id, Title, "C++");
        x64.PropertyChanged += (_, _) => Raise(nameof(AllInstalled));
        x86.PropertyChanged += (_, _) => Raise(nameof(AllInstalled));
    }

    public string Title { get; }
    public string Desc => "Needed by many games and programs. Most want both the 64-bit and the 32-bit build.";
    public double CardW { get => _cardW; set => Set(ref _cardW, value); }
    public bool AllInstalled => X64.Installed && X86.Installed;
    public bool Matches(string q) => q.Length == 0 || Title.Contains(q, StringComparison.OrdinalIgnoreCase) || X64.Matches(q);
}

public sealed class AppSectionVM : Notify
{
    string _counter = "";
    public string Title { get; init; } = "";
    public List<object> Items { get; init; } = new();
    public int Total { get; init; }
    public string Counter { get => _counter; set => Set(ref _counter, value); }
}

/// <summary>One removable Windows app on the Debloat apps page.</summary>
public sealed class WinAppVM : Notify
{
    public WinAppDef Def { get; }
    bool _selected, _busy, _removed;
    string _phase = "";

    public WinAppVM(WinAppDef d)
    {
        Def = d;
        var (glyph, hex) = WinAppArt.For(d);
        Glyph = glyph;
        var c = (Color)ColorConverter.ConvertFromString(hex);
        Tile = new LinearGradientBrush(Mix(c, Colors.White, 0.10), Mix(c, Colors.Black, 0.30), 90); Tile.Freeze();
        GlyphFg = Brushes.White;
    }

    static Color Mix(Color a, Color b, double t) => Color.FromRgb((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    /// <summary>Icon character (Segoe Fluent Icons) and the tile colour behind it.</summary>
    public string Glyph { get; }

    static readonly Brush PhotoTile = Frozen(Color.FromRgb(0x1D, 0x1D, 0x24));
    static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

    ImageSource? _photo;
    /// <summary>The app's own icon, read from its package folder on this PC. Null until found (the tile shows the icon character meanwhile).</summary>
    public ImageSource? Photo
    {
        get => _photo;
        set { if (Set(ref _photo, value)) { Raise(nameof(HasPhoto)); Raise(nameof(NoPhoto)); Raise(nameof(TileBg)); } }
    }
    TileArt? _art;
    /// <summary>The same brand logo the Apps page uses, for apps that are on both pages (Spotify, Teams ...). Wins over the package icon.</summary>
    public TileArt? Art
    {
        get => _art;
        set { if (Set(ref _art, value)) { Raise(nameof(HasPhoto)); Raise(nameof(NoPhoto)); Raise(nameof(TileBg)); Raise(nameof(HasArt)); } }
    }
    public Visibility HasArt => _art != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility HasPhoto => _photo != null && _art == null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NoPhoto => _photo == null && _art == null ? Visibility.Visible : Visibility.Collapsed;
    public Brush TileBg => _art != null ? Brushes.Transparent : _photo != null ? PhotoTile : Tile;
    public Brush Tile { get; }
    public Brush GlyphFg { get; }
    /// <summary>The little label under the tick box is only shown when it matters: core apps, apps that can't come back, locked apps.</summary>
    public bool ShowTag => _removed || Def.Tier != RemoveTier.Ok;

    public string Name => Def.Name;
    public string Desc => Def.Desc;
    public string Group => Def.Group;
    public bool Locked => Def.Tier == RemoveTier.Protected;
    public bool CanSelect => !Locked && !_busy && !_removed;
    public Action<WinAppVM>? Changed;

    public bool Selected
    {
        get => _selected;
        set { if (Locked && value) return; if (Set(ref _selected, value)) Changed?.Invoke(this); }
    }
    public bool Busy { get => _busy; set { if (Set(ref _busy, value)) Raise(nameof(CanSelect)); } }
    public bool Removed
    {
        get => _removed;
        set { if (Set(ref _removed, value)) { if (value) { _selected = false; Raise(nameof(Selected)); } Raise(nameof(CanSelect)); Raise(nameof(TagText)); Raise(nameof(TagKind)); Raise(nameof(ShortTag)); Raise(nameof(ShowTag)); } }
    }
    public string Phase { get => _phase; set { if (Set(ref _phase, value)) Raise(nameof(HasPhase)); } }
    public bool HasPhase => _phase.Length > 0;

    /// <summary>"reinstall", "core", "perm" or "keep": decides the colour of the tag.</summary>
    public string TagKind => _removed ? "done" : Def.Tier switch
    {
        RemoveTier.Ok => "reinstall", RemoveTier.Core => "core", RemoveTier.Perm => "perm", RemoveTier.Optional => "optional", _ => "keep",
    };

    public string TagText => _removed ? "Removed" : Def.Tier switch
    {
        RemoveTier.Ok => "Reinstall from Store",
        RemoveTier.Core => "Core app",
        RemoveTier.Perm => "Can't be reinstalled",
        RemoveTier.Optional => "Optional",
        _ => "Keep",
    };

    /// <summary>Short label for the tile; the full wording is in the tooltip.</summary>
    public string ShortTag => _removed ? "Removed" : Def.Tier switch
    {
        RemoveTier.Ok => "",
        RemoveTier.Core => "Core app",
        RemoveTier.Perm => "No reinstall",
        RemoveTier.Optional => "Optional",
        _ => "Locked",
    };

    public string Hint => Def.Tier switch
    {
        RemoveTier.Perm => "Microsoft no longer offers this app, so once it is removed it cannot be brought back.",
        RemoveTier.Core => "Part of everyday Windows. You can reinstall it from the Store, but most people want it.",
        RemoveTier.Protected => "Windows and other apps depend on this. TrapShack never removes it.",
        RemoveTier.Optional => "Only remove it if you don't use it. It isn't picked by \u201cPick the safe ones\u201d.",
        _ => "You can get this back from the Microsoft Store whenever you want.",
    };

    public bool Matches(string q) =>
        q.Length == 0 || Def.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || Def.Desc.Contains(q, StringComparison.OrdinalIgnoreCase);
}

public sealed class WinSectionVM
{
    public string Title { get; init; } = "";
    public List<WinAppVM> Items { get; init; } = new();
    public string Counter => Items.Count == 1 ? "1 app" : Items.Count + " apps";
}

/// <summary>Picks an icon and a colour for each Windows app. The icons are plain Windows icon characters, not logos.</summary>
public static class WinAppArt
{
    static readonly Dictionary<string, (string Glyph, string Hex)> ById = new()
    {
        ["app-microsoft-windowscalculator"] = ("\uE8EF", "#3A4A5C"),
        ["app-microsoft-windowsalarms"] = ("\uE916", "#2F6D8A"),
        ["app-microsoft-windowscamera"] = ("\uE722", "#3B5B8C"),
        ["app-clipchamp-clipchamp"] = ("\uE714", "#6E3EC1"),
        ["app-microsoft-windowsmaps"] = ("\uE707", "#1F8A70"),
        ["app-microsoft-windowsterminal"] = ("\uE756", "#2B2B33"),
        ["app-microsoft-windowsfeedbackhub"] = ("\uE946", "#8A4B2F"),
        ["app-microsoft-windows-photos"] = ("\uE8B9", "#0F6CBD"),
        ["app-microsoft-paint"] = ("\uE70F", "#3B7DD8"),
        ["app-microsoft-mspaint"] = ("\uE70F", "#7A3BD8"),
        ["app-microsoft-screensketch"] = ("\uE70F", "#0F6CBD"),
        ["app-microsoft-windowsnotepad"] = ("\uE70F", "#2F8AB5"),
        ["app-microsoft-microsoftstickynotes"] = ("\uE70B", "#B8941F"),
        ["app-microsoft-todos"] = ("\uE73E", "#2564CF"),
        ["app-microsoft-getstarted"] = ("\uE946", "#B87A1F"),
        ["app-microsoft-gethelp"] = ("\uE946", "#2F6D8A"),
        ["app-microsoft-yourphone"] = ("\uE717", "#0F6CBD"),
        ["app-microsoftcorporationii-quickassist"] = ("\uE77B", "#0F6CBD"),
        ["app-microsoft-windowscommunicationsapps"] = ("\uE715", "#0F6CBD"),
        ["app-microsoft-outlookforwindows"] = ("\uE715", "#0A64AD"),
        ["app-microsoft-skypeapp"] = ("\uE717", "#00A0D8"),
        ["app-msteams"] = ("\uE716", "#5059C9"),
        ["app-microsoftcorporationii-microsoftfamily"] = ("\uE77B", "#2F8A5B"),
        ["app-microsoft-people"] = ("\uE716", "#2F6D8A"),
        ["app-microsoft-microsoftofficehub"] = ("\uE71D", "#C4401F"),
        ["app-microsoft-office-onenote"] = ("\uE70F", "#7719AA"),
        ["app-microsoft-powerautomatedesktop"] = ("\uE768", "#2564CF"),
        ["app-microsoft-microsoftsolitairecollection"] = ("\uE7FC", "#2F7A4B"),
        ["app-microsoft-gamingapp"] = ("\uE7FC", "#107C10"),
        ["app-microsoft-xboxgamingoverlay"] = ("\uE7FC", "#107C10"),
        ["app-microsoft-xboxgameoverlay"] = ("\uE7FC", "#107C10"),
        ["app-microsoft-xboxidentityprovider"] = ("\uE77B", "#107C10"),
        ["app-microsoft-xbox-tcui"] = ("\uE7FC", "#107C10"),
        ["app-microsoft-windowsstore"] = ("\uE719", "#0F6CBD"),
        ["app-microsoft-desktopappinstaller"] = ("\uE719", "#0F6CBD"),
        ["app-microsoft-zunemusic"] = ("\uE768", "#C4401F"),
        ["app-microsoft-zunevideo"] = ("\uE714", "#7A2FB5"),
        ["app-microsoft-windowssoundrecorder"] = ("\uE720", "#8A2F3B"),
        ["app-microsoft-bingsearch"] = ("\uE721", "#0F6CBD"),
        ["app-microsoft-bingnews"] = ("\uE8A5", "#8A2F3B"),
        ["app-microsoft-bingweather"] = ("\uE706", "#2F6DB5"),
        ["app-microsoft-549981c3f5f10"] = ("\uE720", "#2F6DB5"),
        ["app-microsoft-microsoft3dviewer"] = ("\uE71D", "#6B3FA0"),
        ["app-microsoft-mixedreality-portal"] = ("\uE71D", "#6B3FA0"),
        ["app-microsoft-windows-devhome"] = ("\uE713", "#3A4A5C"),
    };

    static readonly Dictionary<string, string> GroupHex = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AI"] = "#5B4BB5", ["System"] = "#3A4A5C", ["System Tools"] = "#3A4A5C", ["System Utilities"] = "#3A4A5C",
        ["Games"] = "#107C10", ["Media"] = "#7A2FB5", ["Office"] = "#C4401F", ["Communication"] = "#2564CF",
        ["PC maker apps"] = "#3A4A5C", ["Microsoft apps"] = "#0F6CBD", ["Third-party apps"] = "#5B4BB5",
    };

    // Third-party apps: a fitting icon from the kind of app (these are Windows icon characters, not brand logos)
    static readonly (string[] Words, string Glyph, string Hex)[] Kinds =
    {
        (new[] { "netflix", "hulu", "primevideo", "disney.", "slingtv", "spotify", "pandora", "iheart", "tunein", "acgmediaplayer", "cyberlink" }, "\uE768", "#8A2F3B"),
        (new[] { "candycrush", "bubblewitch", "asphalt", "farmville", "hiddencity", "marchofempires", "royalrevolt", "cookingfever", "caesars", "disneymagic", "nytcrossword" }, "\uE7FC", "#2F7A4B"),
        (new[] { "facebook", "instagram", "tiktok", "linkedin", "flipboard" }, "\uE716", "#2564CF"),
        (new[] { "photoshop", "picsart", "polarr", "phototastic", "sketchbook", "drawboard" }, "\uE8B9", "#6E3EC1"),
        (new[] { "copilot", "aihub" }, "\uE945", "#5B4BB5"),
        (new[] { "teams", "messaging", "oneconnect" }, "\uE8BD", "#5059C9"),
        (new[] { "whiteboard", "journal", "sway", "onecalendar" }, "\uE70F", "#2F6D8A"),
        (new[] { "widgets", "webexperience", "startexperiences" }, "\uE71D", "#2F6DB5"),
        (new[] { "remotedesktop", "windows365", "crossdevice", "glidex", "quickshare", "multicontrol", "secondscreen", "flux" }, "\uE772", "#2F6D8A"),
        (new[] { "amazon.com", "winzip", "duolingo", "actipro" }, "\uE719", "#B87A1F"),
    };

    public static (string Glyph, string Hex) For(WinAppDef d)
    {
        if (ById.TryGetValue(d.Id, out var v)) return v;
        var key = d.Appx.ToLowerInvariant();
        foreach (var k in Kinds)
            if (k.Words.Any(w => key.Contains(w))) return (k.Glyph, k.Hex);
        if (d.Group == "PC maker apps") return ("\uE772", "#3A4A5C");
        var hex = GroupHex.TryGetValue(d.Group, out var g) ? g : "#3A4A5C";
        var glyph = d.Group.Equals("AI", StringComparison.OrdinalIgnoreCase) ? "\uE945" : "\uE71D";
        return (glyph, hex);
    }
}
