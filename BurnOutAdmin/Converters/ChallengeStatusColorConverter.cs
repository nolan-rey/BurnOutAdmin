using System.Globalization;
using BurnOutAdmin.Models;

namespace BurnOutAdmin.Converters;

public class ChallengeStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChallengeStatus status)
        {
            return status switch
            {
                ChallengeStatus.Active => Color.FromArgb("#4CAF50"),
                ChallengeStatus.Upcoming => Color.FromArgb("#2196F3"),
                ChallengeStatus.Completed => Color.FromArgb("#9E9E9E"),
                ChallengeStatus.Cancelled => Color.FromArgb("#F44336"),
                _ => Color.FromArgb("#9E9E9E")
            };
        }
        return Color.FromArgb("#9E9E9E");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
