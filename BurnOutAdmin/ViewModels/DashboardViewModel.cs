using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IDashboardService _dashboardService;

    [ObservableProperty]
    private int _activeClientsCount;

    [ObservableProperty]
    private int _todayNfcAccessCount;

    [ObservableProperty]
    private int _alertsCount;

    [ObservableProperty]
    private string _systemStatusText = string.Empty;

    [ObservableProperty]
    private SystemStatus _systemStatus;

    [ObservableProperty]
    private int _activeChallengesCount;

    [ObservableProperty]
    private int _activeProgrammesCount;

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        Title = "Tableau de bord";
        
        // Load data on initialization
        LoadDashboardDataCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var stats = await _dashboardService.GetDashboardStatsAsync();
            
            ActiveClientsCount = stats.ActiveClientsCount;
            TodayNfcAccessCount = stats.TodayNfcAccessCount;
            AlertsCount = stats.AlertsCount;
            SystemStatus = stats.SystemStatus;
            SystemStatusText = stats.SystemStatusText;
            ActiveChallengesCount = stats.ActiveChallengesCount;
            ActiveProgrammesCount = stats.ActiveProgrammesCount;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
