using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TrapShack.Ui;

/// <summary>
/// The app's small touches of motion: a soft light that follows the mouse across a card, cards that rise in one after
/// another, and a very slow dark haze behind the pages. All of it switches off with one Settings option, and also when
/// Windows is set to reduce animations.
/// </summary>
public static class Fx
{
    /// <summary>Master switch. Off when the user turns it off, or when Windows animations are disabled.</summary>
    public static bool On { get; set; } = true;
    static bool Active => On;   // only the app's own Settings switch decides; Windows' animation setting is not used (it is often off by accident)

    // ---------------------------------------------------------------------------------- mouse glow
    public static readonly DependencyProperty GlowProperty =
        DependencyProperty.RegisterAttached("Glow", typeof(bool), typeof(Fx), new PropertyMetadata(false, OnGlowChanged));
    public static void SetGlow(DependencyObject d, bool v) => d.SetValue(GlowProperty, v);
    public static bool GetGlow(DependencyObject d) => (bool)d.GetValue(GlowProperty);

    static readonly DependencyProperty GlowRectProperty =
        DependencyProperty.RegisterAttached("GlowRect", typeof(Rectangle), typeof(Fx));

    static void OnGlowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border b || e.NewValue is not true) return;
        b.MouseEnter += (_, ev) => Enter(b, ev);
        b.MouseMove += (_, ev) => Move(b, ev);
        b.MouseLeave += (_, ev) => Leave(b, ev);
    }

    /// <summary>The light layer for a card, created the first time the mouse reaches it.</summary>
    static Rectangle? Layer(Border b)
    {
        if (b.GetValue(GlowRectProperty) is Rectangle have) return have;
        if (b.Child is not Panel panel) return null;
        var brush = new RadialGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute, RadiusX = 240, RadiusY = 240,
            GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(0x4A, 0xE5, 0x21, 0x3B), 0),
                new GradientStop(Color.FromArgb(0x00, 0xE5, 0x21, 0x3B), 1),
            },
        };
        var r = b.CornerRadius.TopLeft;
        var glow = new Rectangle
        {
            Fill = brush, Opacity = 0, IsHitTestVisible = false, RadiusX = r, RadiusY = r,
            Margin = new Thickness(-b.Padding.Left, -b.Padding.Top, -b.Padding.Right, -b.Padding.Bottom),
        };
        Grid.SetColumnSpan(glow, 32); Grid.SetRowSpan(glow, 32);
        panel.Children.Add(glow);
        b.SetValue(GlowRectProperty, glow);
        return glow;
    }

    public static void Enter(object sender, MouseEventArgs e)
    {
        if (!Active || sender is not Border b || Layer(b) is not { } glow) return;
        var brush = (RadialGradientBrush)glow.Fill;
        if (Application.Current.TryFindResource("AccentColor") is Color c)
        {
            brush.GradientStops[0].Color = Color.FromArgb(0x4A, c.R, c.G, c.B);
            brush.GradientStops[1].Color = Color.FromArgb(0x00, c.R, c.G, c.B);
        }
        Place(b, brush, e);
        glow.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220)));
    }

    public static void Move(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b.GetValue(GlowRectProperty) is Rectangle { Opacity: > 0 } glow) Place(b, (RadialGradientBrush)glow.Fill, e);
    }

    public static void Leave(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b.GetValue(GlowRectProperty) is Rectangle glow)
            glow.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(260)));
    }

    static void Place(Border b, RadialGradientBrush brush, MouseEventArgs ev)
    {
        var p = ev.GetPosition(b);
        // the glow layer is larger than the border by its padding, so shift into its own space
        var q = new Point(p.X + b.Padding.Left, p.Y + b.Padding.Top);
        brush.Center = q; brush.GradientOrigin = q;
    }

    // ---------------------------------------------------------------------------------- rise in
    public static readonly DependencyProperty RiseProperty =
        DependencyProperty.RegisterAttached("Rise", typeof(bool), typeof(Fx), new PropertyMetadata(false, OnRiseChanged));
    public static void SetRise(DependencyObject d, bool v) => d.SetValue(RiseProperty, v);
    public static bool GetRise(DependencyObject d) => (bool)d.GetValue(RiseProperty);

    static int _wave;
    static bool _waveReset;

    static void OnRiseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement fe && e.NewValue is true) fe.Loaded += RiseIn;
    }

    static void RiseIn(object sender, RoutedEventArgs e)
    {
        var fe = (FrameworkElement)sender;
        fe.Loaded -= RiseIn;
        if (!Active) return;

        // cards that appear together get a short stagger; the counter restarts once the batch has been laid out
        var step = Math.Min(_wave++, 14);
        if (!_waveReset)
        {
            _waveReset = true;
            fe.Dispatcher.BeginInvoke(new Action(() => { _wave = 0; _waveReset = false; }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
        var delay = TimeSpan.FromMilliseconds(step * 30);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        if (fe.RenderTransform is not TranslateTransform tt || tt.IsFrozen)
        {
            tt = new TranslateTransform();
            fe.RenderTransform = tt;
        }
        tt.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(380)) { BeginTime = delay, EasingFunction = ease, FillBehavior = FillBehavior.Stop });
        fe.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)) { BeginTime = delay, FillBehavior = FillBehavior.Stop });
        fe.Opacity = 1;
    }

    // ---------------------------------------------------------------------------------- haze
    /// <summary>Starts (or stops) the two slow, dark glows that drift behind the pages.</summary>
    public static void Haze(Canvas haze, bool run)
    {
        haze.Visibility = run && Active ? Visibility.Visible : Visibility.Collapsed;
        foreach (var child in haze.Children.OfType<Ellipse>())
        {
            if (child.RenderTransform is not TranslateTransform tt) continue;
            if (!run || !Active) { tt.BeginAnimation(TranslateTransform.XProperty, null); tt.BeginAnimation(TranslateTransform.YProperty, null); continue; }
            var dur = child.Tag is string s && double.TryParse(s, out var secs) ? secs : 40;
            Anim(tt, TranslateTransform.XProperty, -90, 110, dur);
            Anim(tt, TranslateTransform.YProperty, 60, -80, dur * 1.25);
        }
    }

    static void Anim(TranslateTransform tt, DependencyProperty p, double from, double to, double seconds) =>
        tt.BeginAnimation(p, new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
}
