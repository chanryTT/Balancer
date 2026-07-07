using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BalanceSystem.App.Converters;

public class TabToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int tabIndex && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
        {
            return tabIndex == targetIndex ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
