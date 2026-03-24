using System.Globalization;

namespace BurnOutAdmin.Converters;

public class CategoryChevronConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded && isExpanded)
            return "▲";
        return "▼";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
