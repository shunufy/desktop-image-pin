using System.Globalization;
using System.Windows.Data;

namespace DesktopImagePin.Converters;

public sealed class OpacityPercentConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is double opacity ? opacity * 100 : 100.0;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is double percent ? percent / 100 : 1.0;
    }
}
