using System.Globalization;
using BurnOutAdmin.ViewModels;
using BurnOutAdmin.Views.Challenges;
using BurnOutAdmin.Views.Clients;
using BurnOutAdmin.Views.Dashboard;
using BurnOutAdmin.Views.NfcLogs;
using BurnOutAdmin.Views.Programmes;
using BurnOutAdmin.Views.Settings;

namespace BurnOutAdmin.Converters;

public class ViewModelToViewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        return value switch
        {
            DashboardViewModel vm => new DashboardView { BindingContext = vm },
            ClientsViewModel vm => new ClientsView { BindingContext = vm },
            ProgrammesViewModel vm => new ProgrammesView { BindingContext = vm },
            ChallengesViewModel vm => new ChallengesView { BindingContext = vm },
            NfcLogsViewModel vm => new NfcLogsView { BindingContext = vm },
            SettingsViewModel vm => new SettingsView { BindingContext = vm },
            _ => null
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
