using System.Windows;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>One card on the Optimizations hub: a page with its icon, progress and Default / Custom counts.</summary>
public sealed record HubRow(string Key, string Title, string Groups, string Glyph,
    string Rec, bool HasRec, string Def, string Custom, bool HasCustom, double RecFill = 0);

/// <summary>One live card on the System info page (CPU, GPU, memory). Pct is 0-100, or -1 when there is no usage figure.</summary>
public sealed record LiveGroup(string Title, string Headline, double Pct, bool Hot, List<SensorReading> Rows)
{
    public double Fill => Pct < 0 ? 0 : Math.Min(1, Pct / 100.0);
    public Visibility BarVisible => Pct < 0 ? Visibility.Collapsed : Visibility.Visible;
}
