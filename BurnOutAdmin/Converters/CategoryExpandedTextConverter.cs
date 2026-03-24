using System.Globalization;

namespace BurnOutAdmin.Converters;

public class CategoryExpandedTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded && isExpanded)
            return Color.FromArgb("#1D4ED8"); // blue-700
        return Color.FromArgb("#374151"); // gray-700
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
