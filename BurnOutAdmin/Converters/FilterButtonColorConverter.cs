using System.Globalization;

namespace BurnOutAdmin.Converters;

public class FilterButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return Color.FromArgb("#E3F2FD");
        }
        return Color.FromArgb("#F5F5F5");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
