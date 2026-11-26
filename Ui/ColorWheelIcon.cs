using System.Windows;
using System.Windows.Media;

namespace TrapShack.Ui;

/// <summary>
/// The small colour-wheel icon in the title bar: a ring of every hue, drawn as vectors so it stays sharp at any
/// screen scale. The current accent colour sits in the middle (an Ellipse in the XAML), so it follows the theme.
/// </summary>
public static class ColorWheelIcon
{
    public static DrawingImage Create()
    {
        const int slices = 60;
        const double outer = 9, inner = 4.6, c = 9;   // 18 x 18 box
        var group = new DrawingGroup();
        for (var i = 0; i < slices; i++)
        {
            // each slice overlaps the next a little, so no hairline gaps show between them
            var a0 = (i * 360.0 / slices - 90) * Math.PI / 180;
            var a1 = ((i + 1.35) * 360.0 / slices - 90) * Math.PI / 180;
            Point P(double r, double a) => new(c + r * Math.Cos(a), c + r * Math.Sin(a));
            var fig = new PathFigure { StartPoint = P(outer, a0), IsClosed = true, IsFilled = true };
            fig.Segments.Add(new ArcSegment(P(outer, a1), new Size(outer, outer), 0, false, SweepDirection.Clockwise, false));
            fig.Segments.Add(new LineSegment(P(inner, a1), false));
            fig.Segments.Add(new ArcSegment(P(inner, a0), new Size(inner, inner), 0, false, SweepDirection.Counterclockwise, false));
            var geo = new PathGeometry(new[] { fig });
            geo.Freeze();
            var brush = new SolidColorBrush(AccentTheme.FromHsv(i * 360.0 / slices, 0.72, 0.95));
            brush.Freeze();
            group.Children.Add(new GeometryDrawing(brush, null, geo));
        }
        // a faint rim so the wheel reads cleanly on both the dark and the light theme
        var rim = new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0, 0, 0)), 0.6);
        rim.Freeze();
        group.Children.Add(new GeometryDrawing(null, rim, new EllipseGeometry(new Point(c, c), outer - 0.3, outer - 0.3)));
        group.Freeze();
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
