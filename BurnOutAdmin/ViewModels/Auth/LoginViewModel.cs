using BurnOutAdmin.Services.Api;
using BurnOutAdmin.Views.Auth;
using BurnOutAdmin.Views.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.Auth;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiAuthService  _authService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty] private string _email        = string.Empty;
    [ObservableProperty] private string _password     = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool   _isLoading    = false;
    [ObservableProperty] private bool   _hasError     = false;

    public LoginViewModel(IApiAuthService authService, IServiceProvider serviceProvider)
    {
        _authService     = authService;
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Veuillez renseigner votre email et votre mot de passe.");
            return;
        }

        IsLoading    = true;
        HasError     = false;
        ErrorMessage = string.Empty;

        var error = await _authService.LoginAsync(Email.Trim(), Password);

        IsLoading = false;

        if (error is not null) { SetError(error); return; }

        var app = _serviceProvider.GetRequiredService<App>();
        app.StartNfcOrchestrator();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var mainShell = _serviceProvider.GetRequiredService<MainShell>();
            if (Application.Current?.Windows.Count > 0)
                Application.Current.Windows[0].Page = mainShell;
        });
    }

    [RelayCommand]
    private void GoToRegister()
    {
        var register = _serviceProvider.GetRequiredService<RegisterView>();
        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = register;
    }

    private void SetError(string message) { ErrorMessage = message; HasError = true; }
    partial void OnEmailChanged(string value)    => HasError = false;
    partial void OnPasswordChanged(string value) => HasError = false;
}
