using System.Globalization;

namespace BurnOutAdmin.Converters;

public class NfcCardIconColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasNfcCard)
        {
            return hasNfcCard ? Color.FromArgb("#4CAF50") : Color.FromArgb("#9E9E9E");
        }
        return Color.FromArgb("#9E9E9E");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
