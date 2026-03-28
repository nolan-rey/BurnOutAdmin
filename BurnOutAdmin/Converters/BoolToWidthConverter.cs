using System.Globalization;

namespace BurnOutAdmin.Converters;

public class BoolToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOpen && isOpen)
        {
            if (parameter is string widthStr && double.TryParse(widthStr, out var width))
                return width;
            return 400d;
        }
        return 0d;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
