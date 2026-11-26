using System.Windows;
using System.Windows.Media;

namespace TrapShack.Ui;

/// <summary>
/// Dark or light look. Every neutral colour in the app is a resource (the base brushes plus the Ink_/InkC_ tokens in
/// Theme.xaml) read through DynamicResource, so switching swaps those resources and the whole window follows at once.
/// The light theme is a soft grey, never bright white. The terminal keeps its dark look in both themes.
/// </summary>
public static class ThemeMode
{
    static readonly string[] BaseKeys =
    {
        "BgBrush", "SidebarBrush", "CardBrush", "CardHoverBrush", "BorderBrushSoft",
        "TextBrush", "MutedBrush", "FaintBrush", "AmberBrush", "GreenBrush", "TrackBrush",
    };

    static Dictionary<string, Color>? _dark;   // the original (dark) value of every themed key
    public static bool IsLight { get; private set; }

    public static void Apply(bool light)
    {
        var res = Application.Current.Resources;
        _dark ??= Capture(res);
        IsLight = light;
        foreach (var (key, dark) in _dark)
        {
            var c = light ? Map(dark) : dark;
            if (key.StartsWith("InkC_")) res[key] = c;
            else { var b = new SolidColorBrush(c); b.Freeze(); res[key] = b; }
        }
        AccentTheme.Apply(AccentTheme.Current);   // redo the accent tints for this background
    }

    static Dictionary<string, Color> Capture(ResourceDictionary res)
    {
        var map = new Dictionary<string, Color>();
        void Take(object key, object value)
        {
            if (key is not string k) return;
            var themed = BaseKeys.Contains(k) || k.StartsWith("Ink_") || k.StartsWith("InkC_");
            if (!themed || map.ContainsKey(k)) return;
            if (value is SolidColorBrush b) map[k] = b.Color;
            else if (value is Color c) map[k] = c;
        }
        void Walk(ResourceDictionary d)
        {
            foreach (var key in d.Keys) Take(key, d[key]);
            foreach (var m in d.MergedDictionaries) Walk(m);
        }
        Walk(res);
        return map;
    }

    /// <summary>
    /// The light-theme partner of a dark-theme colour. Keeps the hue and transparency; turns dark surfaces into soft greys
    /// (the page a little darker than the cards, borders darker than both) and light text into dark text.
    /// </summary>
    public static Color Map(Color c)
    {
        var (h, s, l) = ToHsl(c);
        double nl;
        if (l < 0.12) nl = 0.82 + l * 0.9;                 // page, sidebar, cards: 0.82 .. 0.93
        else if (l < 0.35) nl = 0.80 - (l - 0.12) * 1.1;   // borders and tracks: a step darker than the cards
        else nl = Math.Clamp(1.04 - l, 0.08, 0.62);        // text and icons: light becomes dark
        var sat = l < 0.35 ? Math.Min(s, 0.35) : s;
        var o = FromHsl(h, sat, Math.Clamp(nl, 0, 1));
        return Color.FromArgb(c.A, o.R, o.G, o.B);
    }

    static (double H, double S, double L) ToHsl(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b)); var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2; double h = 0, s = 0;
        var d = max - min;
        if (d > 0)
        {
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (max == r) h = ((g - b) / d + (g < b ? 6 : 0)) * 60;
            else if (max == g) h = ((b - r) / d + 2) * 60;
            else h = ((r - g) / d + 4) * 60;
        }
        return (h, s, l);
    }

    static Color FromHsl(double h, double s, double l)
    {
        if (s <= 0) { var v = (byte)Math.Round(l * 255); return Color.FromRgb(v, v, v); }
        double Hue(double p, double q, double t)
        {
            if (t < 0) t += 1; if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 0.5) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }
        var q2 = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p2 = 2 * l - q2;
        var hh = h / 360;
        return Color.FromRgb((byte)Math.Round(Hue(p2, q2, hh + 1.0 / 3) * 255), (byte)Math.Round(Hue(p2, q2, hh) * 255), (byte)Math.Round(Hue(p2, q2, hh - 1.0 / 3) * 255));
    }
}
