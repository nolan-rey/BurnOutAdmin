using System.Globalization;

namespace BurnOutAdmin.Converters;

public class FilterTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool showTodayOnly)
        {
            return showTodayOnly ? "Aujourd'hui" : "Tout l'historique";
        }
        return "Filtrer";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
