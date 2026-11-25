using System.Windows;
using System.Windows.Controls;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>
/// Lays cards out in columns like a pin board: each card goes into the shortest column, so cards of different
/// heights pack tightly with no gaps. Uses <see cref="Columns"/> columns, fewer when a column would be narrower
/// than <see cref="MinColumnWidth"/>.
/// </summary>
public sealed class MasonryPanel : Panel
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns), typeof(int), typeof(MasonryPanel),
        new FrameworkPropertyMetadata(3, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty MinColumnWidthProperty = DependencyProperty.Register(
        nameof(MinColumnWidth), typeof(double), typeof(MasonryPanel),
        new FrameworkPropertyMetadata(280.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(MasonryPanel),
        new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double MinColumnWidth { get => (double)GetValue(MinColumnWidthProperty); set => SetValue(MinColumnWidthProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }

    readonly List<Rect> _slots = new();

    int ColumnsFor(double width)
    {
        var want = Math.Max(1, Columns);
        if (double.IsInfinity(width) || double.IsNaN(width)) return want;
        return Math.Max(1, Math.Min(want, (int)Math.Floor((width + Gap) / (MinColumnWidth + Gap))));
    }

    protected override Size MeasureOverride(Size available)
    {
        _slots.Clear();
        var width = double.IsInfinity(available.Width) ? Columns * MinColumnWidth + (Columns - 1) * Gap : available.Width;
        var cols = ColumnsFor(width);
        var colW = Math.Max(0, (width - Gap * (cols - 1)) / cols);
        var heights = new double[cols];
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) { _slots.Add(Rect.Empty); continue; }
            child.Measure(new Size(colW, double.PositiveInfinity));
            var c = Array.IndexOf(heights, heights.Min());
            var y = heights[c];
            _slots.Add(new Rect(c * (colW + Gap), y, colW, child.DesiredSize.Height));
            heights[c] = y + child.DesiredSize.Height + Gap;
        }
        var total = heights.Length == 0 ? 0 : Math.Max(0, heights.Max() - Gap);
        return new Size(width, total);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var i = 0;
        foreach (UIElement child in InternalChildren)
        {
            var r = i < _slots.Count ? _slots[i] : Rect.Empty;
            child.Arrange(r.IsEmpty ? new Rect(0, 0, 0, 0) : r);
            i++;
        }
        return finalSize;
    }
}

/// <summary>Picks the row template for a Debloat card: a switch row for a setting, or a drop-down row for a choice.</summary>
public sealed class DebloatRowSelector : DataTemplateSelector
{
    public DataTemplate? TweakTemplate { get; set; }
    public DataTemplate? ChoiceTemplate { get; set; }
    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item is ChoiceVM ? ChoiceTemplate : TweakTemplate;
}

/// <summary>One option inside a drop-down.</summary>
public sealed class ChoiceOptionVM : Notify
{
    public ChoiceOptionVM(ChoiceVM parent, TweakVM vm) { Parent = parent; Vm = vm; }
    public ChoiceVM Parent { get; }
    public TweakVM Vm { get; }
    public string Label => Vm.T.ChoiceLabel ?? Vm.Title;
    bool _current;
    public bool IsCurrent { get => _current; set => Set(ref _current, value); }
    public bool IsDefault => Label.Contains("default", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A drop-down made of several settings that write the same value (only one can be in effect).</summary>
public sealed class ChoiceVM : Notify
{
    public ChoiceVM(string title, IEnumerable<TweakVM> options)
    {
        Title = title;
        Options = options.Select(o => new ChoiceOptionVM(this, o)).ToList();
        Refresh();
    }

    public string Title { get; }
    public List<ChoiceOptionVM> Options { get; }
    public bool CanPick => Options.Any(o => o.Vm.CanToggle);
    public string Tip => Options.FirstOrDefault()?.Vm.T.Detail ?? "";

    string _selected = "";
    public string SelectedLabel { get => _selected; private set => Set(ref _selected, value); }

    /// <summary>Shows the option that is in effect now. When no option's value is set, Windows is using its default.</summary>
    public void Refresh()
    {
        var on = Options.FirstOrDefault(o => o.Vm.IsOn && !o.IsDefault) ?? Options.FirstOrDefault(o => o.Vm.IsOn)
                 ?? Options.FirstOrDefault(o => o.IsDefault);
        foreach (var o in Options) o.IsCurrent = ReferenceEquals(o, on);
        SelectedLabel = on?.Label ?? "No change";
        Raise(nameof(CanPick));
    }
}

/// <summary>One card on the Debloat page: a category with its switches and drop-downs.</summary>
public sealed class DebloatCategoryVM : Notify
{
    public string Title { get; init; } = "";
    public string Glyph { get; init; } = "";
    public List<object> Rows { get; init; } = new();

    string _count = "";
    public string Count { get => _count; private set => Set(ref _count, value); }

    public void Refresh()
    {
        foreach (var c in Rows.OfType<ChoiceVM>()) c.Refresh();
        var switches = Rows.OfType<TweakVM>().Where(v => !v.T.OneShot).ToList();
        Count = switches.Count == 0 ? "" : $"{switches.Count(v => v.IsOn)}/{switches.Count}";
    }
}
