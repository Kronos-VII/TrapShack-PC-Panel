using System.Windows;
using System.Windows.Controls;

namespace TrapShack.Ui;

/// <summary>
/// A tidy grid for cards: the same number of equal columns on every row, whatever the window size. It uses
/// <see cref="Columns"/> columns (3 by default) and only drops to fewer when a column would be narrower than
/// <see cref="MinItemWidth"/>. Every card in a row is as tall as the tallest one in that row.
/// </summary>
public sealed class AdaptiveGrid : Panel
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns), typeof(int), typeof(AdaptiveGrid),
        new FrameworkPropertyMetadata(3, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty MinItemWidthProperty = DependencyProperty.Register(
        nameof(MinItemWidth), typeof(double), typeof(AdaptiveGrid),
        new FrameworkPropertyMetadata(230.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(AdaptiveGrid),
        new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
        nameof(RowHeight), typeof(double), typeof(AdaptiveGrid),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>How many columns to aim for.</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>A column is never narrower than this; the grid uses fewer columns instead.</summary>
    public double MinItemWidth { get => (double)GetValue(MinItemWidthProperty); set => SetValue(MinItemWidthProperty, value); }

    /// <summary>Space between cards, horizontally and vertically.</summary>
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }

    /// <summary>If above zero, every row is exactly this tall (cards then line up perfectly across sections).</summary>
    public double RowHeight { get => (double)GetValue(RowHeightProperty); set => SetValue(RowHeightProperty, value); }

    int ColumnsFor(double width)
    {
        var want = Math.Max(1, Columns);
        if (double.IsInfinity(width) || double.IsNaN(width)) return want;
        var fit = (int)Math.Floor((width + Gap) / (MinItemWidth + Gap));
        return Math.Max(1, Math.Min(want, fit));
    }

    /// <summary>
    /// The cards of one row and how wide each is. A last row that isn't full spreads its cards across the whole width,
    /// so there is never an empty gap at the end of a row.
    /// </summary>
    IEnumerable<(int Start, int Count, double Cell)> Rows(int total, int cols, double width)
    {
        for (var i = 0; i < total; i += cols)
        {
            var n = Math.Min(cols, total - i);
            yield return (i, n, Math.Max(0, (width - Gap * (n - 1)) / n));
        }
    }

    protected override Size MeasureOverride(Size available)
    {
        var visible = InternalChildren.Cast<UIElement>().Where(c => c.Visibility != Visibility.Collapsed).ToList();
        if (visible.Count == 0) return new Size(0, 0);

        var cols = ColumnsFor(available.Width);
        var width = double.IsInfinity(available.Width) ? cols * MinItemWidth + (cols - 1) * Gap : available.Width;

        double total = 0;
        var first = true;
        foreach (var (start, n, cell) in Rows(visible.Count, cols, width))
        {
            double rowH = 0;
            for (var j = start; j < start + n; j++)
            {
                visible[j].Measure(new Size(cell, RowHeight > 0 ? RowHeight : double.PositiveInfinity));
                rowH = Math.Max(rowH, visible[j].DesiredSize.Height);
            }
            if (RowHeight > 0) rowH = RowHeight;
            total += rowH + (first ? 0 : Gap);
            first = false;
        }
        return new Size(width, total);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = InternalChildren.Cast<UIElement>().Where(c => c.Visibility != Visibility.Collapsed).ToList();
        if (visible.Count == 0) return finalSize;

        var cols = ColumnsFor(finalSize.Width);
        var rows = Rows(visible.Count, cols, finalSize.Width).ToList();
        var heights = rows.Select(r =>
        {
            double h = 0;
            for (var j = r.Start; j < r.Start + r.Count; j++) h = Math.Max(h, visible[j].DesiredSize.Height);
            return RowHeight > 0 ? RowHeight : h;
        }).ToList();

        // when the parent gives more height than needed (cards sitting next to a taller group), the rows grow to fill it
        var used = heights.Sum() + Gap * (rows.Count - 1);
        if (RowHeight <= 0 && finalSize.Height > used + 0.5 && !double.IsInfinity(finalSize.Height))
        {
            var extra = (finalSize.Height - used) / rows.Count;
            for (var k = 0; k < heights.Count; k++) heights[k] += extra;
        }

        double y = 0;
        for (var k = 0; k < rows.Count; k++)
        {
            var (start, n, cell) = rows[k];
            for (var j = 0; j < n; j++)
                visible[start + j].Arrange(new Rect(j * (cell + Gap), y, cell, heights[k]));
            y += heights[k] + Gap;
        }
        return finalSize;
    }
}

/// <summary>Something that knows how many cards it holds, so <see cref="GroupPackPanel"/> can size it.</summary>
public interface IPackCount { int PackCount { get; } }

/// <summary>
/// Lays out whole groups (a heading plus its cards) on a shared column grid. A group with many cards takes a full row;
/// small groups sit side by side on one row, each as wide as its cards need. Any columns left over at the end of a row
/// go to the groups in that row, so rows always run edge to edge with no empty gaps, and every group in a row is the
/// same height.
/// </summary>
public sealed class GroupPackPanel : Panel
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns), typeof(int), typeof(GroupPackPanel), new FrameworkPropertyMetadata(4, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty MinColumnWidthProperty = DependencyProperty.Register(
        nameof(MinColumnWidth), typeof(double), typeof(GroupPackPanel), new FrameworkPropertyMetadata(300.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(GroupPackPanel), new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty RowGapProperty = DependencyProperty.Register(
        nameof(RowGap), typeof(double), typeof(GroupPackPanel), new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double MinColumnWidth { get => (double)GetValue(MinColumnWidthProperty); set => SetValue(MinColumnWidthProperty, value); }
    /// <summary>Space between columns (the same gap the cards inside a group use, so columns line up).</summary>
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public double RowGap { get => (double)GetValue(RowGapProperty); set => SetValue(RowGapProperty, value); }

    readonly List<Rect> _slots = new();

    static int CountOf(UIElement e)
    {
        var dc = (e as FrameworkElement)?.DataContext;
        return dc switch
        {
            System.Windows.Data.CollectionViewGroup g => g.ItemCount,
            IPackCount p => p.PackCount,
            _ => int.MaxValue,
        };
    }

    protected override Size MeasureOverride(Size available)
    {
        _slots.Clear();
        var kids = InternalChildren.Cast<UIElement>().ToList();
        var width = double.IsInfinity(available.Width) ? Columns * MinColumnWidth + (Columns - 1) * Gap : available.Width;
        var cols = Math.Max(1, Math.Min(Math.Max(1, Columns), (int)Math.Floor((width + Gap) / (MinColumnWidth + Gap))));
        var colW = Math.Max(0, (width - Gap * (cols - 1)) / cols);
        double W(int span) => span * colW + (span - 1) * Gap;

        // 1) pack the groups into rows, in order
        var rows = new List<List<(int Index, int Span, int Count)>>();
        var row = new List<(int Index, int Span, int Count)>();
        var used = 0;
        for (var i = 0; i < kids.Count; i++)
        {
            if (kids[i].Visibility == Visibility.Collapsed) continue;
            var count = Math.Max(1, CountOf(kids[i]));
            var span = Math.Min(count, cols);
            if (used + span > cols && row.Count > 0) { rows.Add(row); row = new(); used = 0; }
            row.Add((i, span, count));
            used += span;
        }
        if (row.Count > 0) rows.Add(row);

        // 2) hand spare columns to the groups that have the most cards, so every row is full
        foreach (var r in rows)
        {
            var spare = cols - r.Sum(x => x.Span);
            while (spare > 0)
            {
                var k = Enumerable.Range(0, r.Count).OrderByDescending(x => (double)r[x].Count / r[x].Span).First();
                r[k] = (r[k].Index, r[k].Span + 1, r[k].Count);
                spare--;
            }
        }

        // 3) measure and place
        for (var i = 0; i < kids.Count; i++) _slots.Add(Rect.Empty);
        double y = 0;
        foreach (var r in rows)
        {
            double h = 0;
            foreach (var g in r) { kids[g.Index].Measure(new Size(W(g.Span), double.PositiveInfinity)); h = Math.Max(h, kids[g.Index].DesiredSize.Height); }
            double x = 0;
            foreach (var g in r) { _slots[g.Index] = new Rect(x, y, W(g.Span), h); x += W(g.Span) + Gap; }
            y += h + RowGap;
        }
        foreach (var k in kids) if (k.Visibility == Visibility.Collapsed) k.Measure(new Size(0, 0));
        return new Size(width, Math.Max(0, y - RowGap));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var kids = InternalChildren.Cast<UIElement>().ToList();
        for (var i = 0; i < kids.Count; i++)
            kids[i].Arrange(i < _slots.Count && !_slots[i].IsEmpty ? _slots[i] : new Rect(0, 0, 0, 0));
        return finalSize;
    }
}
