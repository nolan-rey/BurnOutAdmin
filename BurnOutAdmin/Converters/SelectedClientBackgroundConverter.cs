using System.Globalization;
using BurnOutAdmin.Models;

namespace BurnOutAdmin.Converters;

public class SelectedClientBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This is a simplified version - in real implementation you'd compare the current item
        // For now, we'll use transparent as default
        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
