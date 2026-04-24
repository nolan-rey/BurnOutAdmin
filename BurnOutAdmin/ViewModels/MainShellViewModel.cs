using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api;
using BurnOutAdmin.Views.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class MainShellViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IAlertService      _alertService;
    private readonly IApiAuthService    _authService;
    private readonly IServiceProvider   _serviceProvider;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private BaseViewModel? _currentViewModel;

    [ObservableProperty]
    private SidebarMenuItem? _selectedMenuItem;

    [ObservableProperty]
    private ObservableCollection<SidebarMenuItem> _menuItems = new();

    /// <summary>Email de l'utilisateur connecté (affiché dans la sidebar et les paramètres).</summary>
    public string CurrentUserEmail =>
        _authService.CurrentUserEmail ?? "utilisateur@app.fr";

    /// <summary>Initiales extraites de l'email (ex. "test@test.com" → "T").</summary>
    public string CurrentUserInitials
    {
        get
        {
            var email = _authService.CurrentUserEmail ?? string.Empty;
            var local = email.Contains('@') ? email[..email.IndexOf('@')] : email;
            return local.Length > 0 ? local[0].ToString().ToUpperInvariant() : "?";
        }
    }

    public MainShellViewModel(
        INavigationService navigationService,
        IAlertService      alertService,
        IApiAuthService    authService,
        IServiceProvider   serviceProvider)
    {
        _navigationService = navigationService;
        _alertService      = alertService;
        _authService       = authService;
        _serviceProvider   = serviceProvider;
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
            new SidebarMenuItem { Title = "Créateur de Séance", IconPath = "IconCalendar", PageKey = "ProgramBuilder" },
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
        var confirmed = await _alertService.ConfirmAsync(
            "Déconnexion",
            "Voulez-vous vraiment vous déconnecter ?",
            "Se déconnecter",
            "Annuler");

        if (!confirmed) return;

        _authService.Logout();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var loginView = _serviceProvider.GetRequiredService<LoginView>();
            if (Application.Current?.Windows is { Count: > 0 } windows)
                windows[0].Page = loginView;
        });
    }
}
