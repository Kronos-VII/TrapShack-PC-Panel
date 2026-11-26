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
        var kids = InternalChildren.Cast<UIElement>().ToList();
        foreach (var k in kids) k.Measure(k.Visibility == Visibility.Collapsed ? new Size(0, 0) : new Size(colW, double.PositiveInfinity));

        // Balance the columns: biggest cards first, each into the shortest column. Inside a column the cards keep
        // their original order, so the page still reads top to bottom.
        var columns = Enumerable.Range(0, cols).Select(_ => new List<int>()).ToArray();
        var sums = new double[cols];
        foreach (var i in Enumerable.Range(0, kids.Count).Where(i => kids[i].Visibility != Visibility.Collapsed)
                                    .OrderByDescending(i => kids[i].DesiredSize.Height))
        {
            var c = Array.IndexOf(sums, sums.Min());
            columns[c].Add(i);
            sums[c] += kids[i].DesiredSize.Height + Gap;
        }
        // keep the first card (and the general order) on the left
        columns = columns.Where(c => c.Count > 0).OrderBy(c => c.Min()).Concat(columns.Where(c => c.Count == 0)).ToArray();

        for (var i = 0; i < kids.Count; i++) _slots.Add(Rect.Empty);
        var tallest = columns.Select(c => c.Sum(i => kids[i].DesiredSize.Height) + Gap * Math.Max(0, c.Count - 1)).DefaultIfEmpty(0).Max();
        for (var c = 0; c < columns.Length; c++)
        {
            var list = columns[c].OrderBy(i => i).ToList();
            double y = 0;
            for (var n = 0; n < list.Count; n++)
            {
                var h = kids[list[n]].DesiredSize.Height;
                // the last card in each column grows so every column ends on the same line
                if (n == list.Count - 1) h = Math.Max(h, tallest - y);
                _slots[list[n]] = new Rect(c * (colW + Gap), y, colW, h);
                y += h + Gap;
            }
        }
        return new Size(width, tallest);
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
    public string Glyph => Options.Count > 0 ? TweakGlyph.For(Options[0].Vm.T) : "\uE713";
    /// <summary>The line under the name: how many choices, and which one Windows uses on its own.</summary>
    public string Sub
    {
        get
        {
            var def = Options.FirstOrDefault(o => o.IsDefault)?.Label;
            return $"{Options.Count} choices" + (def != null ? $"  ·  Windows default: {def.Replace(" (default)", "")}" : "");
        }
    }

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
