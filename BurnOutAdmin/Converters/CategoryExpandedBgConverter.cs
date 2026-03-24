using System.Globalization;

namespace BurnOutAdmin.Converters;

public class CategoryExpandedBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded && isExpanded)
            return Color.FromArgb("#EFF6FF"); // blue-50
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
