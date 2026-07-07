using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BalanceSystem.App.Converters;

public class BoolToPassFailColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45))
            : new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
