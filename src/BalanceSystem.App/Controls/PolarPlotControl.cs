using System.Windows;
using System.Windows.Media;

namespace BalanceSystem.App.Controls;

public class PolarPlotControl : FrameworkElement
{
    private readonly VisualCollection _visuals;
    private DrawingVisual? _gridVisual;
    private DrawingVisual? _vectorVisual;

    public static readonly DependencyProperty LeftVectorAngleProperty =
        DependencyProperty.Register(nameof(LeftVectorAngle), typeof(double), typeof(PolarPlotControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorChanged));

    public static readonly DependencyProperty LeftVectorMagnitudeProperty =
        DependencyProperty.Register(nameof(LeftVectorMagnitude), typeof(double), typeof(PolarPlotControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorChanged));

    public static readonly DependencyProperty RightVectorAngleProperty =
        DependencyProperty.Register(nameof(RightVectorAngle), typeof(double), typeof(PolarPlotControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorChanged));

    public static readonly DependencyProperty RightVectorMagnitudeProperty =
        DependencyProperty.Register(nameof(RightVectorMagnitude), typeof(double), typeof(PolarPlotControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorChanged));

    public double LeftVectorAngle { get => (double)GetValue(LeftVectorAngleProperty); set => SetValue(LeftVectorAngleProperty, value); }
    public double LeftVectorMagnitude { get => (double)GetValue(LeftVectorMagnitudeProperty); set => SetValue(LeftVectorMagnitudeProperty, value); }
    public double RightVectorAngle { get => (double)GetValue(RightVectorAngleProperty); set => SetValue(RightVectorAngleProperty, value); }
    public double RightVectorMagnitude { get => (double)GetValue(RightVectorMagnitudeProperty); set => SetValue(RightVectorMagnitudeProperty, value); }

    private static void OnVectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PolarPlotControl)d).RenderVectors();
    }

    public PolarPlotControl()
    {
        _visuals = new VisualCollection(this);
        RenderGrid();
        RenderVectors();
    }

    protected override Visual GetVisualChild(int index) => _visuals[index];
    protected override int VisualChildrenCount => _visuals.Count;

    private void RenderGrid()
    {
        _gridVisual = new DrawingVisual();
        using var dc = _gridVisual.RenderOpen();
        double width = ActualWidth > 0 ? ActualWidth : 300;
        double height = ActualHeight > 0 ? ActualHeight : 300;
        double cx = width / 2, cy = height / 2;
        double radius = Math.Min(cx, cy) - 30;

        var grayPen = new Pen(Brushes.LightGray, 0.5);
        var axisPen = new Pen(Brushes.Gray, 1);

        for (int i = 1; i <= 4; i++)
        {
            double r = radius * i / 4;
            dc.DrawEllipse(null, grayPen, new Point(cx, cy), r, r);
        }

        for (int angle = 0; angle < 360; angle += 30)
        {
            double rad = angle * Math.PI / 180.0;
            dc.DrawLine(grayPen, new Point(cx, cy),
                new Point(cx + radius * Math.Cos(rad), cy - radius * Math.Sin(rad)));
        }

        dc.DrawLine(axisPen, new Point(cx - radius, cy), new Point(cx + radius, cy));
        dc.DrawLine(axisPen, new Point(cx, cy - radius), new Point(cx, cy + radius));

        var typeface = new Typeface("Microsoft YaHei");
        for (int angle = 0; angle < 360; angle += 30)
        {
            double rad = angle * Math.PI / 180.0;
            double lx = cx + (radius + 15) * Math.Cos(rad);
            double ly = cy - (radius + 15) * Math.Sin(rad);
            var text = new FormattedText($"{angle}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, 9, Brushes.Gray, 1.0);
            dc.DrawText(text, new Point(lx - text.Width / 2, ly - text.Height / 2));
        }

        dc.Close();
        _visuals.Clear();
        _visuals.Add(_gridVisual);
        if (_vectorVisual != null) _visuals.Add(_vectorVisual);
    }

    private void RenderVectors()
    {
        _vectorVisual = new DrawingVisual();
        using var dc = _vectorVisual.RenderOpen();
        double width = ActualWidth > 0 ? ActualWidth : 300;
        double height = ActualHeight > 0 ? ActualHeight : 300;
        double cx = width / 2, cy = height / 2;
        double radius = Math.Min(cx, cy) - 30;

        DrawVectorLine(dc, cx, cy, radius, LeftVectorMagnitude, LeftVectorAngle,
            Color.FromRgb(0x2C, 0x5A, 0xA0), "L");
        DrawVectorLine(dc, cx, cy, radius, RightVectorMagnitude, RightVectorAngle,
            Color.FromRgb(0x28, 0xA7, 0x45), "R");

        dc.Close();

        if (_visuals.Count >= 2)
            _visuals[1] = _vectorVisual;
        else
            _visuals.Add(_vectorVisual);
    }

    private static void DrawVectorLine(DrawingContext dc, double cx, double cy,
        double maxRadius, double magnitude, double angleDeg, Color color, string label)
    {
        double angleRad = angleDeg * Math.PI / 180.0;
        double scaledRadius = magnitude * maxRadius / 100.0;
        if (scaledRadius > maxRadius) scaledRadius = maxRadius;
        if (scaledRadius < 2) scaledRadius = 2;

        double ex = cx + scaledRadius * Math.Cos(angleRad);
        double ey = cy - scaledRadius * Math.Sin(angleRad);

        var brush = new SolidColorBrush(color);
        var pen = new Pen(brush, 2.5);
        dc.DrawLine(pen, new Point(cx, cy), new Point(ex, ey));

        double arrowLen = 8;
        double arrowAngle1 = angleRad + Math.PI - 0.4;
        double arrowAngle2 = angleRad + Math.PI + 0.4;
        var arrowPen = new Pen(brush, 1.5);
        dc.DrawLine(arrowPen, new Point(ex, ey),
            new Point(ex + arrowLen * Math.Cos(arrowAngle1), ey - arrowLen * Math.Sin(arrowAngle1)));
        dc.DrawLine(arrowPen, new Point(ex, ey),
            new Point(ex + arrowLen * Math.Cos(arrowAngle2), ey - arrowLen * Math.Sin(arrowAngle2)));
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RenderGrid();
        RenderVectors();
    }
}
