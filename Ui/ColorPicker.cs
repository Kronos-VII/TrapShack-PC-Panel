using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TrapShack.Ui;

/// <summary>Shared colour state for the wheel and its brightness bar.</summary>
public sealed class HsvState
{
    public double H { get; set; }
    public double S { get; set; } = 1;
    public double V { get; set; } = 1;
    public Color Color => AccentTheme.FromHsv(H, S, V);
    public void Set(Color c) { var (h, s, v) = AccentTheme.ToHsv(c); if (s > 0.001 || v < 0.001) H = h; S = s; V = v; }
}

/// <summary>A round hue and saturation picker. Click or drag anywhere on the wheel.</summary>
public sealed class ColorWheel : FrameworkElement
{
    public HsvState State { get; set; } = new();
    public event Action? Picking;     // while dragging
    public event Action? Picked;      // mouse released

    WriteableBitmap? _bmp;
    double _bmpV = -1;
    const int Px = 256;
    bool _drag;

    public ColorWheel() { Cursor = Cursors.Cross; SnapsToDevicePixels = false; }

    public void Redraw() => InvalidateVisual();

    void EnsureBitmap()
    {
        if (_bmp != null && Math.Abs(_bmpV - State.V) < 0.002) return;
        _bmp ??= new WriteableBitmap(Px, Px, 96, 96, PixelFormats.Pbgra32, null);
        var buf = new byte[Px * Px * 4];
        double r = Px / 2.0;
        for (var y = 0; y < Px; y++)
            for (var x = 0; x < Px; x++)
            {
                double dx = (x + 0.5 - r) / r, dy = (y + 0.5 - r) / r;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                var i = (y * Px + x) * 4;
                if (dist > 1.0) continue;
                var hue = Math.Atan2(dy, dx) * 180 / Math.PI;
                var c = AccentTheme.FromHsv(hue, dist, State.V);
                var edge = (1.0 - dist) * r;                    // pixels from the rim: soften the last pixel
                var a = (byte)(edge >= 1 ? 255 : Math.Max(0, edge) * 255);
                // premultiplied alpha
                buf[i] = (byte)(c.B * a / 255); buf[i + 1] = (byte)(c.G * a / 255); buf[i + 2] = (byte)(c.R * a / 255); buf[i + 3] = a;
            }
        _bmp.WritePixels(new Int32Rect(0, 0, Px, Px), buf, Px * 4, 0);
        _bmpV = State.V;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var d = Math.Min(ActualWidth, ActualHeight);
        if (d < 8) return;
        EnsureBitmap();
        var ox = (ActualWidth - d) / 2; var oy = (ActualHeight - d) / 2;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        dc.DrawImage(_bmp, new Rect(ox, oy, d, d));
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(0x55, 255, 255, 255)), 1), new Point(ox + d / 2, oy + d / 2), d / 2 - 0.5, d / 2 - 0.5);

        // marker
        var ang = State.H * Math.PI / 180;
        var p = new Point(ox + d / 2 + Math.Cos(ang) * State.S * d / 2, oy + d / 2 + Math.Sin(ang) * State.S * d / 2);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0)), 4), p, 9, 9);
        dc.DrawEllipse(new SolidColorBrush(State.Color), new Pen(Brushes.White, 2.5), p, 9, 9);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _drag = true; CaptureMouse(); Pick(e.GetPosition(this)); e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_drag) Pick(e.GetPosition(this));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!_drag) return;
        _drag = false; ReleaseMouseCapture(); Picked?.Invoke();
    }

    void Pick(Point pt)
    {
        var d = Math.Min(ActualWidth, ActualHeight);
        double dx = pt.X - ActualWidth / 2, dy = pt.Y - ActualHeight / 2;
        State.S = Math.Min(1, Math.Sqrt(dx * dx + dy * dy) / (d / 2));
        State.H = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
        InvalidateVisual();
        Picking?.Invoke();
    }
}

/// <summary>A horizontal bar for brightness: dark on the left, the full colour on the right.</summary>
public sealed class BrightnessBar : FrameworkElement
{
    public HsvState State { get; set; } = new();
    public event Action? Picking;
    public event Action? Picked;
    bool _drag;

    public BrightnessBar() { Cursor = Cursors.Hand; Height = 26; }

    public void Redraw() => InvalidateVisual();

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth; var h = ActualHeight;
        if (w < 20) return;
        var track = new Rect(0, h / 2 - 7, w, 14);
        var grad = new LinearGradientBrush(Colors.Black, AccentTheme.FromHsv(State.H, State.S, 1), 0);
        dc.DrawRoundedRectangle(grad, new Pen(new SolidColorBrush(Color.FromArgb(0x44, 255, 255, 255)), 1), track, 7, 7);
        var x = 8 + State.V * (w - 16);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0)), 4), new Point(x, h / 2), 10, 10);
        dc.DrawEllipse(new SolidColorBrush(State.Color), new Pen(Brushes.White, 2.5), new Point(x, h / 2), 10, 10);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _drag = true; CaptureMouse(); Pick(e.GetPosition(this)); e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_drag) Pick(e.GetPosition(this));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!_drag) return;
        _drag = false; ReleaseMouseCapture(); Picked?.Invoke();
    }

    void Pick(Point pt)
    {
        State.V = Math.Clamp((pt.X - 8) / Math.Max(1, ActualWidth - 16), 0.05, 1);   // never fully black: the app would vanish
        InvalidateVisual();
        Picking?.Invoke();
    }
}
