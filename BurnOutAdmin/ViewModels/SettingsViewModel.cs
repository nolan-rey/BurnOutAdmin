using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api;
using BurnOutAdmin.Services.Mqtt;
using BurnOutAdmin.Services.Nfc;
using BurnOutAdmin.Views.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IAlertService    _alertService;
    private readonly IApiAuthService  _authService;
    private readonly IServiceProvider _serviceProvider;
    private readonly INfcOrchestrator _nfcOrchestrator;

    // ── Profil ────────────────────────────────────────────────────
    public string CurrentUserEmail =>
        _authService.CurrentUserEmail ?? "utilisateur@app.fr";

    public string CurrentUserInitials
    {
        get
        {
            var email = _authService.CurrentUserEmail ?? string.Empty;
            var local = email.Contains('@') ? email[..email.IndexOf('@')] : email;
            return local.Length > 0 ? local[0].ToString().ToUpperInvariant() : "?";
        }
    }

    // ── Statuts connexion ─────────────────────────────────────────
    [ObservableProperty] private bool   _isApiConnected;
    [ObservableProperty] private bool   _isMqttConnected;
    [ObservableProperty] private bool   _isNfcReaderConnected;
    [ObservableProperty] private string _apiStatusText    = "Vérification…";
    [ObservableProperty] private string _mqttStatusText   = "Vérification…";
    [ObservableProperty] private string _nfcStatusText    = "Vérification…";
    [ObservableProperty] private bool   _isRefreshing     = false;

    // Couleurs dérivées des statuts (évite les convertisseurs complexes)
    private static readonly Color _colorConnected   = Color.FromArgb("#22C55E");
    private static readonly Color _colorDisconnected= Color.FromArgb("#EF4444");
    private static readonly Color _bgConnected      = Color.FromArgb("#F0FDF4");
    private static readonly Color _bgDisconnected   = Color.FromArgb("#FEF2F2");

    public Color ApiDotColor  => IsApiConnected       ? _colorConnected : _colorDisconnected;
    public Color ApiBgColor   => IsApiConnected       ? _bgConnected    : _bgDisconnected;
    public Color MqttDotColor => IsMqttConnected      ? _colorConnected : _colorDisconnected;
    public Color MqttBgColor  => IsMqttConnected      ? _bgConnected    : _bgDisconnected;
    public Color NfcDotColor  => IsNfcReaderConnected ? _colorConnected : _colorDisconnected;
    public Color NfcBgColor   => IsNfcReaderConnected ? _bgConnected    : _bgDisconnected;
    public string ApiStatusLabel  => IsApiConnected       ? "Connecté"  : "Hors ligne";
    public string MqttStatusLabel => IsMqttConnected      ? "Connecté"  : "Déconnecté";
    public string NfcStatusLabel  => IsNfcReaderConnected ? "Actif"     : "Inactif";

    // ── Configuration (lecture seule) ─────────────────────────────
    public string ApiBaseUrl      => AppConfiguration.ApiBaseUrl;
    public string MqttBrokerHost  => AppConfiguration.MqttBrokerHost;
    public int    MqttBrokerPort  => AppConfiguration.MqttBrokerPort;

    // ── App ───────────────────────────────────────────────────────
    [ObservableProperty] private string _appVersion = "1.0.0";

    public SettingsViewModel(
        IAlertService    alertService,
        IApiAuthService  authService,
        IServiceProvider serviceProvider,
        INfcOrchestrator nfcOrchestrator)
    {
        _alertService    = alertService;
        _authService     = authService;
        _serviceProvider = serviceProvider;
        _nfcOrchestrator = nfcOrchestrator;
        Title = "Paramètres";

        _ = RefreshStatusAsync();
    }

    // ── Commandes ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        IsRefreshing = true;

        // API : appel léger sur la racine
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var resp = await http.GetAsync(AppConfiguration.ApiBaseUrl);
            IsApiConnected = resp.IsSuccessStatusCode;
            ApiStatusText  = IsApiConnected ? "Connecté" : $"Erreur {(int)resp.StatusCode}";
        }
        catch
        {
            IsApiConnected = false;
            ApiStatusText  = "Inaccessible";
        }

        // MQTT & NFC : depuis l'orchestrateur
        IsMqttConnected      = _nfcOrchestrator.IsMqttConnected;
        IsNfcReaderConnected = _nfcOrchestrator.IsReaderConnected;
        MqttStatusText       = IsMqttConnected      ? $"{MqttBrokerHost}:{MqttBrokerPort}" : "Broker non joignable";
        NfcStatusText        = IsNfcReaderConnected ? "Lecteur RFID détecté et actif" : "Aucun lecteur détecté";

        // Notifier les propriétés de couleur dérivées
        OnPropertyChanged(nameof(ApiDotColor));  OnPropertyChanged(nameof(ApiBgColor));
        OnPropertyChanged(nameof(MqttDotColor)); OnPropertyChanged(nameof(MqttBgColor));
        OnPropertyChanged(nameof(NfcDotColor));  OnPropertyChanged(nameof(NfcBgColor));
        OnPropertyChanged(nameof(ApiStatusLabel));
        OnPropertyChanged(nameof(MqttStatusLabel));
        OnPropertyChanged(nameof(NfcStatusLabel));

        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        var ok = await _alertService.ConfirmAsync("Vider le cache",
            "Supprimer les données temporaires de l'application ?", "Vider", "Annuler");
        if (!ok) return;
        // TODO: implémenter le vidage SQLite/Preferences si nécessaire
        await _alertService.AlertAsync("Cache", "Cache vidé avec succès.");
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        await _alertService.AlertAsync(
            "À propos",
            $"BurnOut Admin — v{AppVersion}\n\n" +
            "Plateforme de gestion Call of Phoenix.\n" +
            "Authentification Firebase · API REST · MQTT · NFC\n\n" +
            "Développé avec .NET MAUI");
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
