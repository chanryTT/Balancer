using System.Windows;
using System.Windows.Media;

namespace BalanceSystem.App.Controls;

public class NumericDisplay : FrameworkElement
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(NumericDisplay),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericDisplay),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(NumericDisplay),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StatusColorProperty =
        DependencyProperty.Register(nameof(StatusColor), typeof(Color), typeof(NumericDisplay),
            new FrameworkPropertyMetadata(Color.FromRgb(0x33, 0x33, 0x33),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.Register(nameof(Format), typeof(string), typeof(NumericDisplay),
            new FrameworkPropertyMetadata("F2", FrameworkPropertyMetadataOptions.AffectsRender));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public Color StatusColor { get => (Color)GetValue(StatusColorProperty); set => SetValue(StatusColorProperty, value); }
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double width = ActualWidth;
        double height = ActualHeight;
        if (width < 10 || height < 10) return;

        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

        var labelText = new FormattedText(Label,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei"), 12, Brushes.Gray, 1.0);
        dc.DrawText(labelText, new Point((width - labelText.Width) / 2, 4));

        string valueStr = Value.ToString(Format);
        var valueText = new FormattedText(valueStr,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Consolas"), 28, new SolidColorBrush(StatusColor), 1.0);
        dc.DrawText(valueText, new Point((width - valueText.Width) / 2, height / 2 - 16));

        var unitText = new FormattedText(Unit,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei"), 11, Brushes.Gray, 1.0);
        dc.DrawText(unitText, new Point((width - unitText.Width) / 2, height - 22));
    }
}
