using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class MainShellViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private BaseViewModel? _currentViewModel;

    [ObservableProperty]
    private SidebarMenuItem? _selectedMenuItem;

    [ObservableProperty]
    private ObservableCollection<SidebarMenuItem> _menuItems = new();

    public MainShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.CurrentViewModelChanged += HandleCurrentViewModelChanged;
        
        InitializeMenuItems();
        
        // Navigate to Dashboard by default
        NavigateToPage("Dashboard");
    }

    private void InitializeMenuItems()
    {
        MenuItems = new ObservableCollection<SidebarMenuItem>
        {
            new SidebarMenuItem { Title = "Tableau de bord", IconPath = "IconDashboard", PageKey = "Dashboard" },
            new SidebarMenuItem { Title = "Clients", IconPath = "IconAccount", PageKey = "Clients" },
            new SidebarMenuItem { Title = "Programmes", IconPath = "IconCalendar", PageKey = "Programmes" },
            new SidebarMenuItem { Title = "Program Builder", IconPath = "IconCalendar", PageKey = "ProgramBuilder" },
            new SidebarMenuItem { Title = "Challenges", IconPath = "IconLogoBox", PageKey = "Challenges" },
            new SidebarMenuItem { Title = "Journal NFC", IconPath = "IconNfc", PageKey = "NfcLogs" },
            new SidebarMenuItem { Title = "Paramètres", IconPath = "IconSettings", PageKey = "Settings" }
        };

        SelectedMenuItem = MenuItems.FirstOrDefault();
    }

    private void HandleCurrentViewModelChanged(BaseViewModel viewModel)
    {
        CurrentViewModel = viewModel;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void NavigateToPage(string pageKey)
    {
        _navigationService.NavigateTo(pageKey);
        
        // Update selected menu item
        foreach (var item in MenuItems)
        {
            item.IsSelected = item.PageKey == pageKey;
        }
        
        SelectedMenuItem = MenuItems.FirstOrDefault(m => m.PageKey == pageKey);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        // UI only - placeholder for future implementation
        await Application.Current!.MainPage!.DisplayAlert(
            "Déconnexion", 
            "Fonctionnalité de déconnexion à implémenter", 
            "OK");
    }
}
