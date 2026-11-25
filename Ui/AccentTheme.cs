using System.Windows;
using System.Windows.Media;

namespace TrapShack.Ui;

/// <summary>
/// Changes the accent colour of the whole app while it is running. Every accent-coloured thing in the app reads one of the
/// keys set here through DynamicResource, so a single call recolours buttons, switches, glows, borders and backgrounds at once.
/// </summary>
public static class AccentTheme
{
    public static readonly (string Name, string Hex)[] Swatches =
    {
        ("Red", "#E5213B"), ("Crimson", "#C8102E"), ("Orange", "#F97316"), ("Amber", "#F5A524"),
        ("Green", "#22C55E"), ("Teal", "#14B8A6"), ("Blue", "#3B82F6"), ("Violet", "#8B5CF6"), ("Pink", "#EC4899"),
    };

    public static bool TryParse(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        var h = hex.Trim();
        if (!h.StartsWith('#')) h = "#" + h;
        if (h.Length != 7 && h.Length != 4 && h.Length != 9) return false;
        try { color = (Color)ColorConverter.ConvertFromString(h); color.A = 255; return true; }
        catch { return false; }
    }

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // ------------------------------------------------------------------------------------------ HSV helpers
    public static Color FromHsv(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360; s = Math.Clamp(s, 0, 1); v = Math.Clamp(v, 0, 1);
        var c = v * s; var x = c * (1 - Math.Abs(h / 60 % 2 - 1)); var m = v - c;
        double r, g, b;
        if (h < 60) (r, g, b) = (c, x, 0);
        else if (h < 120) (r, g, b) = (x, c, 0);
        else if (h < 180) (r, g, b) = (0, c, x);
        else if (h < 240) (r, g, b) = (0, x, c);
        else if (h < 300) (r, g, b) = (x, 0, c);
        else (r, g, b) = (c, 0, x);
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }

    public static (double H, double S, double V) ToHsv(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b)); var min = Math.Min(r, Math.Min(g, b)); var d = max - min;
        double h = 0;
        if (d > 0)
        {
            if (max == r) h = 60 * (((g - b) / d) % 6);
            else if (max == g) h = 60 * ((b - r) / d + 2);
            else h = 60 * ((r - g) / d + 4);
        }
        if (h < 0) h += 360;
        return (h, max == 0 ? 0 : d / max, max);
    }

    // ------------------------------------------------------------------------------------------ apply
    static double Lum(Color c) => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;

    static Color Mix(Color over, Color under, double amount) => Color.FromRgb(
        (byte)(under.R + (over.R - under.R) * amount),
        (byte)(under.G + (over.G - under.G) * amount),
        (byte)(under.B + (over.B - under.B) * amount));

    static Color Lighten(Color c, double amount) => Mix(Colors.White, c, amount);

    public static void Apply(Color c)
    {
        var res = Application.Current.Resources;
        var hot = Lighten(c, 0.28);
        var page = Color.FromRgb(0x0B, 0x0B, 0x0E);
        var side = Color.FromRgb(0x08, 0x08, 0x0A);
        var card = Color.FromRgb(0x14, 0x14, 0x18);

        Put(res, "AccentColor", c);
        Put(res, "AccentHotColor", hot);
        Brush(res, "AccentBrush", c);
        Brush(res, "AccentHotBrush", hot);
        Brush(res, "AccentSoftBrush", Color.FromArgb(0x33, c.R, c.G, c.B));
        Brush(res, "AccentEdgeBrush", Color.FromArgb(0x66, c.R, c.G, c.B));

        // Dark tints of the accent: page glow, selected rows, borders of popups and hovered cards.
        Put(res, "AccentDeepColor", Mix(c, page, 0.14));
        Put(res, "AccentDeeperColor", Mix(c, page, 0.08));
        Put(res, "AccentGlowColor", Color.FromArgb(0x24, c.R, c.G, c.B));
        Put(res, "AccentGlowFadeColor", Color.FromArgb(0x00, c.R, c.G, c.B));
        Put(res, "AccentHazeColor", Color.FromArgb(0x40, c.R, c.G, c.B));
        Put(res, "AccentHazeFadeColor", Color.FromArgb(0x00, c.R, c.G, c.B));
        Brush(res, "AccentWashBrush", Mix(c, side, 0.12));
        Brush(res, "AccentCardWashBrush", Mix(c, card, 0.10));
        Brush(res, "AccentPanelBorderBrush", Mix(c, page, 0.30));
        Brush(res, "AccentHoverBorderBrush", Mix(c, page, 0.45));

        // Text and ticks that sit ON the accent: white, unless the accent is so light that white would vanish.
        Brush(res, "OnAccentBrush", Lum(c) > 0.62 ? Color.FromRgb(0x10, 0x10, 0x14) : Colors.White);

        res["LogoBrush"] = new LinearGradientBrush(hot, Mix(c, Colors.Black, 0.7), new Point(0, 0), new Point(1, 1));
    }

    static void Put(ResourceDictionary r, string key, Color c) => r[key] = c;
    static void Brush(ResourceDictionary r, string key, Color c) => r[key] = new SolidColorBrush(c);
}
