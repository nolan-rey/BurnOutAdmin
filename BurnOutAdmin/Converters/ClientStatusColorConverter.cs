using System.Globalization;

namespace BurnOutAdmin.Converters;

public class ClientStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isLight = parameter?.ToString()?.ToLower() == "light";
        
        if (value is string status)
        {
            return status.ToLower() switch
            {
                "actif" => isLight ? Color.FromArgb("#D1FAE5") : Color.FromArgb("#10B981"),
                "expiré" => isLight ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#EF4444"),
                "en attente" => isLight ? Color.FromArgb("#FEF3C7") : Color.FromArgb("#F59E0B"),
                "suspendu" => isLight ? Color.FromArgb("#F1F5F9") : Color.FromArgb("#64748B"),
                _ => isLight ? Color.FromArgb("#F1F5F9") : Color.FromArgb("#64748B")
            };
        }
        return isLight ? Color.FromArgb("#F1F5F9") : Color.FromArgb("#64748B");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
