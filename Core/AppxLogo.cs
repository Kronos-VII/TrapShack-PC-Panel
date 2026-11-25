using System.IO;
using System.Xml.Linq;

namespace TrapShack.Core;

/// <summary>
/// Finds the real icon of an installed Windows app inside its own package folder (the same picture Windows shows in
/// Start). Nothing is downloaded: the file is read from the app that is already on this PC.
/// </summary>
public static class AppxLogo
{
    /// <summary>Full path of the best icon file for the package in <paramref name="home"/>, or null when there is none.</summary>
    public static string? Find(string? home)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(home)) return null;
            var manifest = Path.Combine(home, "AppxManifest.xml");
            if (!File.Exists(manifest)) return null;
            var doc = XDocument.Load(manifest);

            // Square44x44Logo is the app-list icon; its "unplated" variants are the full-colour versions.
            var candidates = doc.Descendants()
                .Where(e => e.Name.LocalName == "VisualElements")
                .SelectMany(e => new[] { e.Attribute("Square44x44Logo")?.Value, e.Attribute("Square150x150Logo")?.Value })
                .Concat(doc.Descendants().Where(e => e.Name.LocalName == "Logo" && e.Parent?.Name.LocalName == "Properties").Select(e => e.Value))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .ToList();

            foreach (var rel in candidates)
            {
                var hit = Best(Path.Combine(home, rel.Replace('/', '\\')));
                if (hit != null) return hit;
            }
        }
        catch { /* unreadable manifest or folder: no logo */ }
        return null;
    }

    /// <summary>Picks the sharpest file among the scale/size variants Windows stores next to the named file.</summary>
    static string? Best(string named)
    {
        var dir = Path.GetDirectoryName(named);
        if (dir == null || !Directory.Exists(dir)) return null;
        var stem = Path.GetFileNameWithoutExtension(named);
        var ext = Path.GetExtension(named);
        var files = Directory.GetFiles(dir, stem + "*" + ext)
            .Where(f => !f.Contains("contrast-", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains("lightunplated", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (files.Count == 0) return File.Exists(named) ? named : null;

        int Score(string f)
        {
            var n = Path.GetFileName(f).ToLowerInvariant();
            var s = 0;
            if (n.Contains("altform-unplated")) s += 100;   // full colour, no plate behind it
            if (n.Contains("targetsize-48")) s += 40;
            else if (n.Contains("targetsize-64")) s += 38;
            else if (n.Contains("targetsize-96")) s += 34;
            else if (n.Contains("targetsize-32")) s += 30;
            else if (n.Contains("targetsize-256")) s += 26;
            else if (n.Contains("targetsize-")) s += 20;
            if (n.Contains("scale-200")) s += 18;
            else if (n.Contains("scale-150")) s += 16;
            else if (n.Contains("scale-100")) s += 14;
            else if (n.Contains("scale-400")) s += 12;
            return s;
        }
        return files.OrderByDescending(Score).First();
    }
}
