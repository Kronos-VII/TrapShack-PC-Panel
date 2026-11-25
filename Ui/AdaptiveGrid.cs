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

    protected override Size MeasureOverride(Size available)
    {
        var visible = InternalChildren.Cast<UIElement>().Where(c => c.Visibility != Visibility.Collapsed).ToList();
        if (visible.Count == 0) return new Size(0, 0);

        var cols = ColumnsFor(available.Width);
        var width = double.IsInfinity(available.Width) ? cols * MinItemWidth + (cols - 1) * Gap : available.Width;
        var cell = Math.Max(0, (width - Gap * (cols - 1)) / cols);

        double total = 0;
        for (var i = 0; i < visible.Count; i += cols)
        {
            double rowH = 0;
            for (var j = i; j < Math.Min(i + cols, visible.Count); j++)
            {
                visible[j].Measure(new Size(cell, RowHeight > 0 ? RowHeight : double.PositiveInfinity));
                rowH = Math.Max(rowH, visible[j].DesiredSize.Height);
            }
            if (RowHeight > 0) rowH = RowHeight;
            total += rowH + (i + cols < visible.Count ? Gap : 0);
        }
        return new Size(width, total);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = InternalChildren.Cast<UIElement>().Where(c => c.Visibility != Visibility.Collapsed).ToList();
        if (visible.Count == 0) return finalSize;

        var cols = ColumnsFor(finalSize.Width);
        var cell = Math.Max(0, (finalSize.Width - Gap * (cols - 1)) / cols);

        double y = 0;
        for (var i = 0; i < visible.Count; i += cols)
        {
            double rowH = 0;
            for (var j = i; j < Math.Min(i + cols, visible.Count); j++) rowH = Math.Max(rowH, visible[j].DesiredSize.Height);
            if (RowHeight > 0) rowH = RowHeight;
            for (var j = i; j < Math.Min(i + cols, visible.Count); j++)
                visible[j].Arrange(new Rect((j - i) * (cell + Gap), y, cell, rowH));
            y += rowH + Gap;
        }
        return finalSize;
    }
}
