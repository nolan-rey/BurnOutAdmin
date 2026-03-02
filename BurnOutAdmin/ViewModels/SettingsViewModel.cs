using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
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

    public SettingsViewModel()
    {
        Title = "Paramètres";
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Info", 
            "Paramètres sauvegardés (simulation)", 
            "OK");
    }

    [RelayCommand]
    private async Task ResetSettingsAsync()
    {
        NotificationsEnabled = true;
        DarkModeEnabled = false;
        AutoSyncEnabled = true;
        SyncInterval = "15 minutes";
        
        await Application.Current!.MainPage!.DisplayAlert(
            "Info", 
            "Paramètres réinitialisés", 
            "OK");
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Info", 
            "Cache vidé (simulation)", 
            "OK");
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Info", 
            "Export des données - Fonctionnalité à venir", 
            "OK");
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "À propos", 
            $"BurnOut Admin\nVersion: {AppVersion}\n\nApplication d'administration pour salle de sport.\nDéveloppé avec .NET MAUI.", 
            "OK");
    }
}
