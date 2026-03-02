using System.Globalization;

namespace BurnOutAdmin.Converters;

public class NfcResultColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAuthorized)
        {
            return isAuthorized ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
        }
        return Color.FromArgb("#9E9E9E");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
