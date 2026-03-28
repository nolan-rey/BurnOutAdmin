using BurnOutAdmin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IAlertService _alertService;

    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private bool _darkModeEnabled = false;

    [ObservableProperty]
    private bool _autoSyncEnabled = true;

    [ObservableProperty]
    private string _syncInterval = "15 minutes";

    [ObservableProperty]
    private string _appVersion = "1.0.0 MVP";

    public SettingsViewModel(IAlertService alertService)
    {
        _alertService = alertService;
        Title = "Paramètres";
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _alertService.AlertAsync("Info", "Paramètres sauvegardés (simulation)");
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        NotificationsEnabled = true;
        DarkModeEnabled = false;
        AutoSyncEnabled = true;
        SyncInterval = "15 minutes";

        await _alertService.AlertAsync("Info", "Paramètres réinitialisés");
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        await _alertService.AlertAsync("Info", "Cache vidé (simulation)");
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        await _alertService.AlertAsync("Info", "Export des données - Fonctionnalité à venir");
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        await _alertService.AlertAsync(
            "À propos",
            $"BurnOut Admin\nVersion: {AppVersion}\n\nApplication d'administration pour salle de sport.\nDéveloppé avec .NET MAUI.");
    }
}
