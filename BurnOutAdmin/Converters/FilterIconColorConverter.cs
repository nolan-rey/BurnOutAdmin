using System.Globalization;

namespace BurnOutAdmin.Converters;

public class FilterIconColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return Color.FromArgb("#2196F3");
        }
        return Color.FromArgb("#666666");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
