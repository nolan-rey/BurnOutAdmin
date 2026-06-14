using BurnOutAdmin.Services.Api;
using BurnOutAdmin.Services.Api.Dto;
using BurnOutAdmin.Views.Auth;
using BurnOutAdmin.Views.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.ViewModels.Auth;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IApiAuthService  _authService;
    private readonly IServiceProvider _serviceProvider;

    // ── Navigation / Étapes ─────────────────────────────────────
    [ObservableProperty] private int    _currentStep    = 1;
    [ObservableProperty] private string _selectedRole   = string.Empty; // "Coach" | "Administrateur"

    // ── Champs formulaire ────────────────────────────────────────
    [ObservableProperty] private string _firstName         = string.Empty;
    [ObservableProperty] private string _lastName          = string.Empty;
    [ObservableProperty] private string _email             = string.Empty;
    [ObservableProperty] private string _password          = string.Empty;
    [ObservableProperty] private string _confirmPassword   = string.Empty;
    [ObservableProperty] private string _specialty         = string.Empty;
    [ObservableProperty] private string _verificationCode  = string.Empty;

    // ── États UI ─────────────────────────────────────────────────
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool   _hasError     = false;
    [ObservableProperty] private bool   _isLoading    = false;

    // ── Spécialités coach ─────────────────────────────────────────
    public List<string> Specialties { get; } = new()
    {
        "Musculation", "Cardio / Running", "CrossFit", "Yoga",
        "Pilates", "Boxe / Arts martiaux", "Natation", "Nutrition",
        "Préparation physique", "Autre"
    };

    // ── Computed — étapes ────────────────────────────────────────
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public string StepLabel    => $"Étape {CurrentStep} / 2";
    public string StepSubtitle => CurrentStep == 1
        ? "Choisissez votre rôle dans la salle"
        : "Renseignez vos informations personnelles";

    // ── Computed — rôle ─────────────────────────────────────────
    public bool IsCoachSelected => SelectedRole == "Coach";
    public bool IsAdminSelected => SelectedRole == "Administrateur";
    public bool CanContinue     => !string.IsNullOrEmpty(SelectedRole);
    public bool IsCoachRole     => SelectedRole == "Coach";

    public string RoleInfoText => SelectedRole switch
    {
        "Coach"          => "Accès à la gestion des clients, programmes et suivi des performances.",
        "Administrateur" => "Accès complet : gestion de la salle, des comptes et des paramètres.",
        _                => string.Empty
    };

    // ── Computed — badge rôle (étape 2) ─────────────────────────
    public string RoleIcon => IsCoachRole ? "\uE3A9" : "\uE8D3";

    public Color RoleBadgeBg   => IsCoachRole
        ? Color.FromArgb("#EEF2FF") : Color.FromArgb("#FFF7ED");
    public Color RoleBadgeText => IsCoachRole
        ? Color.FromArgb("#4338CA") : Color.FromArgb("#C2410C");

    // ── Computed — cartes de rôle ────────────────────────────────
    public Color CoachCardBorder => IsCoachSelected ? Color.FromArgb("#512BD4") : Color.FromArgb("#E2E8F0");
    public Color AdminCardBorder => IsAdminSelected ? Color.FromArgb("#512BD4") : Color.FromArgb("#E2E8F0");
    public Color CoachCardBg     => IsCoachSelected ? Color.FromArgb("#EEF2FF") : Colors.White;
    public Color AdminCardBg     => IsAdminSelected ? Color.FromArgb("#EEF2FF") : Colors.White;
    public Color CoachIconColor  => IsCoachSelected ? Color.FromArgb("#512BD4") : Color.FromArgb("#94A3B8");
    public Color AdminIconColor  => IsAdminSelected ? Color.FromArgb("#512BD4") : Color.FromArgb("#94A3B8");

    // ── Computed — code de vérification ─────────────────────────
    public string VerificationCodeLabel => IsCoachRole
        ? "Code d'invitation"
        : "Code administrateur";

    public string VerificationCodePlaceholder => IsCoachRole
        ? "Code fourni par votre administrateur"
        : "Code secret de la salle";

    public string VerificationCodeDescription => IsCoachRole
        ? "Ce code vous est fourni par l'administrateur de la salle."
        : "Code confidentiel autorisant la création d'un compte admin.";

    // ── Constructeur ─────────────────────────────────────────────
    public RegisterViewModel(IApiAuthService authService, IServiceProvider serviceProvider)
    {
        _authService     = authService;
        _serviceProvider = serviceProvider;
    }

    // ── Commandes — sélection de rôle ───────────────────────────
    [RelayCommand]
    private void SelectCoach()
    {
        SelectedRole = "Coach";
        NotifyRoleChanged();
    }

    [RelayCommand]
    private void SelectAdmin()
    {
        SelectedRole = "Administrateur";
        NotifyRoleChanged();
    }

    private void NotifyRoleChanged()
    {
        OnPropertyChanged(nameof(IsCoachSelected));
        OnPropertyChanged(nameof(IsAdminSelected));
        OnPropertyChanged(nameof(CanContinue));
        OnPropertyChanged(nameof(IsCoachRole));
        OnPropertyChanged(nameof(RoleInfoText));
        OnPropertyChanged(nameof(RoleIcon));
        OnPropertyChanged(nameof(RoleBadgeBg));
        OnPropertyChanged(nameof(RoleBadgeText));
        OnPropertyChanged(nameof(CoachCardBorder));
        OnPropertyChanged(nameof(AdminCardBorder));
        OnPropertyChanged(nameof(CoachCardBg));
        OnPropertyChanged(nameof(AdminCardBg));
        OnPropertyChanged(nameof(CoachIconColor));
        OnPropertyChanged(nameof(AdminIconColor));
        OnPropertyChanged(nameof(VerificationCodeLabel));
        OnPropertyChanged(nameof(VerificationCodePlaceholder));
        OnPropertyChanged(nameof(VerificationCodeDescription));
    }

    // ── Commandes — navigation entre étapes ─────────────────────
    [RelayCommand]
    private void NextStep()
    {
        if (!CanContinue) return;
        HasError = false;
        CurrentStep = 2;
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(StepLabel));
        OnPropertyChanged(nameof(StepSubtitle));
    }

    [RelayCommand]
    private void Back()
    {
        HasError = false;
        CurrentStep = 1;
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(StepLabel));
        OnPropertyChanged(nameof(StepSubtitle));
    }

    // ── Commande — inscription ────────────────────────────────────
    [RelayCommand]
    private async Task RegisterAsync()
    {
        HasError = false;

        // Validation locale
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        { SetError("Veuillez renseigner votre prénom et votre nom."); return; }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        { SetError("Adresse e-mail invalide."); return; }

        if (Password.Length < 8)
        { SetError("Le mot de passe doit contenir au moins 8 caractères."); return; }

        if (Password != ConfirmPassword)
        { SetError("Les mots de passe ne correspondent pas."); return; }

        if (string.IsNullOrWhiteSpace(VerificationCode))
        { SetError($"Veuillez saisir votre {VerificationCodeLabel.ToLower()}."); return; }

        if (IsCoachRole && string.IsNullOrWhiteSpace(Specialty))
        { SetError("Veuillez sélectionner votre spécialité."); return; }

        IsLoading = true;

        var dto = new RegisterRequestDto
        {
            Email            = Email.Trim(),
            Password         = Password,
            FirstName        = FirstName.Trim(),
            LastName         = LastName.Trim(),
            Role             = IsCoachRole ? "coach" : "admin",
            Specialty        = IsCoachRole ? Specialty : null,
            VerificationCode = VerificationCode.Trim()
        };

        try
        {
            var error = await _authService.RegisterAsync(dto);

            if (error is not null)
            {
                SetError(error);
                return;
            }

            // ── Succès : navigation vers MainShell ─────────────────
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Démarrer l'orchestrateur NFC (non bloquant)
                try
                {
                    var app = _serviceProvider.GetRequiredService<App>();
                    app.StartNfcOrchestrator();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Register] NFC orchestrator start error: {ex.Message}");
                    // Non bloquant — on continue vers le shell
                }

                // Remplacer la page courante par MainShell
                var mainShell = _serviceProvider.GetRequiredService<MainShell>();
                if (Application.Current?.Windows is { Count: > 0 } windows)
                    windows[0].Page = mainShell;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Register] Unexpected error: {ex.GetType().Name}: {ex.Message}");
            SetError($"Erreur inattendue : {ex.Message}");
        }
        finally
        {
            // Garantit que le spinner s'arrête toujours
            IsLoading = false;
        }
    }

    // ── Commande — retour login ───────────────────────────────────
    [RelayCommand]
    private void GoToLogin()
    {
        var login = _serviceProvider.GetRequiredService<LoginView>();
        if (Application.Current?.Windows.Count > 0)
            Application.Current.Windows[0].Page = login;
    }

    private void SetError(string message) { ErrorMessage = message; HasError = true; }
}
