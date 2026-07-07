using System.Globalization;
using System.Windows.Data;

namespace BalanceSystem.App.Converters;

public class BoolToPassFailConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "PASS" : "FAIL";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
