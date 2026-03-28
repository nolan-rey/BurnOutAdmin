using System.Collections.ObjectModel;
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

    [ObservableProperty]
    private int _expiringSubscriptionsCount;

    [ObservableProperty]
    private bool _isOnline;

    public ObservableCollection<DashboardNfcEntry> RecentNfcAccesses { get; } = new();

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        Title = "Tableau de bord";
    }

    public override async Task OnActivatedAsync()
    {
        await LoadDashboardDataAsync();
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
            ExpiringSubscriptionsCount = stats.ExpiringSubscriptionsCount;
            IsOnline = stats.IsOnline;

            RecentNfcAccesses.Clear();
            foreach (var entry in stats.RecentNfcAccesses)
                RecentNfcAccesses.Add(entry);
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
