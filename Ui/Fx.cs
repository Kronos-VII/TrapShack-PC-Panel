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

    static void OnGlowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border b) return;
        b.MouseEnter -= Enter; b.MouseMove -= Move; b.MouseLeave -= Leave;
        if (e.NewValue is true) { b.MouseEnter += Enter; b.MouseMove += Move; b.MouseLeave += Leave; }
    }

    static readonly DependencyProperty GlowLayerProperty =
        DependencyProperty.RegisterAttached("GlowLayer", typeof(Canvas), typeof(Fx));

    const double GlowSize = 360;

    /// <summary>
    /// The light layer for a card, made the first time the mouse reaches it: a soft round light drawn once into a
    /// cached bitmap. Moving it only shifts that bitmap on the graphics card, so it follows the mouse without
    /// repainting the card (this is what keeps it smooth).
    /// </summary>
    static Canvas? Layer(Border b)
    {
        if (b.GetValue(GlowLayerProperty) is Canvas have) return have;
        if (b.Child is not Grid panel) return null;   // the light sits behind the card's own grid
        var dot = new System.Windows.Shapes.Ellipse
        {
            Width = GlowSize, Height = GlowSize, IsHitTestVisible = false,
            CacheMode = new BitmapCache { RenderAtScale = 1 },
            RenderTransform = new TranslateTransform(),
            Fill = new RadialGradientBrush
            {
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0x40, 0xE5, 0x21, 0x3B), 0),
                    new GradientStop(Color.FromArgb(0x00, 0xE5, 0x21, 0x3B), 1),
                },
            },
        };
        var layer = new Canvas
        {
            IsHitTestVisible = false, ClipToBounds = true, Opacity = 0,
            Margin = new Thickness(-b.Padding.Left, -b.Padding.Top, -b.Padding.Right, -b.Padding.Bottom),
        };
        layer.Children.Add(dot);
        // round the clip to the card's corners
        layer.SizeChanged += (_, e) =>
        {
            var r = Math.Max(0, b.CornerRadius.TopLeft - b.BorderThickness.Left);
            layer.Clip = new RectangleGeometry(new Rect(e.NewSize), r, r);
        };
        Grid.SetColumnSpan(layer, 32); Grid.SetRowSpan(layer, 32);
        Panel.SetZIndex(layer, -1);
        panel.Children.Insert(0, layer);
        b.SetValue(GlowLayerProperty, layer);
        return layer;
    }

    public static void Enter(object sender, MouseEventArgs e)
    {
        if (!Active || sender is not Border b || Layer(b) is not { } layer) return;
        var dot = (System.Windows.Shapes.Ellipse)layer.Children[0];
        if (Application.Current.TryFindResource("AccentColor") is Color c)
        {
            var brush = (RadialGradientBrush)dot.Fill;
            if (brush.GradientStops[0].Color.R != c.R || brush.GradientStops[0].Color.G != c.G || brush.GradientStops[0].Color.B != c.B)
                dot.Fill = new RadialGradientBrush(Color.FromArgb(0x40, c.R, c.G, c.B), Color.FromArgb(0x00, c.R, c.G, c.B));
        }
        Place(b, layer, e);
        layer.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(160)));
    }

    public static void Move(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b.GetValue(GlowLayerProperty) is Canvas layer) Place(b, layer, e);
    }

    public static void Leave(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b.GetValue(GlowLayerProperty) is Canvas layer)
            layer.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(220)));
    }

    static void Place(Border b, Canvas layer, MouseEventArgs ev)
    {
        var p = ev.GetPosition(b);
        var t = (TranslateTransform)((UIElement)layer.Children[0]).RenderTransform;
        t.X = p.X + b.Padding.Left - b.BorderThickness.Left - GlowSize / 2;
        t.Y = p.Y + b.Padding.Top - b.BorderThickness.Top - GlowSize / 2;
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

        // Cards that appear together get a tiny stagger. Only the first few animate; the rest are simply there, so a
        // page with many tiles opens instantly instead of animating dozens of cards at once.
        var step = _wave++;
        if (!_waveReset)
        {
            _waveReset = true;
            fe.Dispatcher.BeginInvoke(new Action(() => { _wave = 0; _waveReset = false; }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
        if (step >= 9) return;
        var delay = TimeSpan.FromMilliseconds(step * 18);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        if (fe.RenderTransform is not TranslateTransform tt || tt.IsFrozen)
        {
            tt = new TranslateTransform();
            fe.RenderTransform = tt;
        }
        tt.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(6, 0, TimeSpan.FromMilliseconds(200)) { BeginTime = delay, EasingFunction = ease, FillBehavior = FillBehavior.Stop });
        fe.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { BeginTime = delay, FillBehavior = FillBehavior.Stop });
        fe.Opacity = 1;
    }

    // ---------------------------------------------------------------------------------- haze
    /// <summary>
    /// Shows or hides the soft accent glow behind the pages. It no longer drifts: moving it forced the whole page to be
    /// redrawn every frame, which is what made the app feel laggy.
    /// </summary>
    public static void Haze(Canvas haze, bool run)
    {
        haze.Visibility = run && Active ? Visibility.Visible : Visibility.Collapsed;
        foreach (var child in haze.Children.OfType<Ellipse>())
            if (child.RenderTransform is TranslateTransform tt) { tt.BeginAnimation(TranslateTransform.XProperty, null); tt.BeginAnimation(TranslateTransform.YProperty, null); }
    }

    static void Anim(TranslateTransform tt, DependencyProperty p, double from, double to, double seconds) =>
        tt.BeginAnimation(p, new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        });
}
