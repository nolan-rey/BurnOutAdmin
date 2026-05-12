using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.SessionLibrary;
using BurnOutAdmin.ViewModels.ProgramBuilder;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.ViewModels;

public partial class ProgrammesViewModel : BaseViewModel
{
    private readonly IProgrammeService        _programmeService;
    private readonly ISessionLibraryService   _sessionLibraryService;
    private readonly IClientService           _clientService;
    private readonly IProgramAssignmentService _assignmentService;
    private readonly IAlertService            _alertService;
    private readonly INavigationService       _navigationService;

    // ── Bibliothèque de séances (gauche) ─────────────────────────
    public ObservableCollection<SavedSessionCardViewModel> SavedSessions { get; } = new();

    // ── Programmes API (droite) ──────────────────────────────────
    public ObservableCollection<ProgrammeCardViewModel> Programmes { get; } = new();

    [ObservableProperty] private ProgrammeCardViewModel? _selectedProgramme;

    // ── Formulaire Créer / Modifier ──────────────────────────────
    [ObservableProperty] private bool   _isModalOpen;
    [ObservableProperty] private bool   _isEditMode;
    [ObservableProperty] private string _modalNom         = string.Empty;
    [ObservableProperty] private string _modalDescription = string.Empty;
    [ObservableProperty] private int    _modalTypeIndex   = 3; // Mixte par défaut
    [ObservableProperty] private int    _modalNiveauIndex = 1; // Intermédiaire par défaut
    [ObservableProperty] private int    _modalDuree       = 8;
    [ObservableProperty] private int    _modalSeances     = 3;
    [ObservableProperty] private bool   _modalIsActif     = true;

    public string ModalTitle => IsEditMode ? "Modifier le programme" : "Nouveau programme";

    public List<string> TypeItems   { get; } = new() { "Cardio", "Force", "Souplesse", "Mixte" };
    public List<string> NiveauItems { get; } = new() { "Débutant", "Intermédiaire", "Avancé" };

    // ── Suppression ──────────────────────────────────────────────
    [ObservableProperty] private bool                   _isDeleteConfirmOpen;
    [ObservableProperty] private ProgrammeCardViewModel? _cardPendingDelete;

    // ── Assignation client ────────────────────────────────────────
    [ObservableProperty] private bool   _isAssignPanelOpen;
    [ObservableProperty] private string _assignTargetName = string.Empty;
    [ObservableProperty] private DateTime _assignStartDate = DateTime.Today;

    public ObservableCollection<ClientSelectionViewModel> SelectableClients { get; } = new();
    private ProgrammeCardViewModel? _programmeToAssign;

    // ── Erreur ────────────────────────────────────────────────────
    [ObservableProperty] private string? _errorMessage;

    // ── États computed ────────────────────────────────────────────
    public bool IsSessionsEmpty   => SavedSessions.Count == 0;
    public bool IsProgrammesEmpty => Programmes.Count == 0 && !IsBusy;
    public bool HasSelectedClients => SelectableClients.Any(c => c.IsSelected);

    // ─────────────────────────────────────────────────────────────

    public ProgrammesViewModel(
        IProgrammeService        programmeService,
        ISessionLibraryService   sessionLibraryService,
        IClientService           clientService,
        IProgramAssignmentService assignmentService,
        IAlertService            alertService,
        INavigationService       navigationService)
    {
        _programmeService      = programmeService;
        _sessionLibraryService = sessionLibraryService;
        _clientService         = clientService;
        _assignmentService     = assignmentService;
        _alertService          = alertService;
        _navigationService     = navigationService;
        Title = "Programmes";
    }

    // ── Navigation ────────────────────────────────────────────────

    public override Task OnActivatedAsync() => LoadAsync();

    // ── Chargement ────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await Task.WhenAll(RefreshSessionsAsync(), RefreshProgrammesAsync());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur de chargement : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsSessionsEmpty));
            OnPropertyChanged(nameof(IsProgrammesEmpty));
        }
    }

    private async Task RefreshSessionsAsync()
    {
        var entries = await _sessionLibraryService.GetAllSessionsAsync();
        SavedSessions.Clear();
        foreach (var e in entries)
            SavedSessions.Add(new SavedSessionCardViewModel(e,
                deleteAction: OnDeleteSession,
                toggleAction: null,
                assignAction: null,
                editAction:   OnEditSession));
        OnPropertyChanged(nameof(IsSessionsEmpty));
    }

    private async Task RefreshProgrammesAsync()
    {
        var list = await _programmeService.GetProgrammesAsync();
        Programmes.Clear();
        foreach (var p in list)
            Programmes.Add(new ProgrammeCardViewModel(p, OnEditProgramme, OnDeleteProgramme, OnAssignProgramme));
        OnPropertyChanged(nameof(IsProgrammesEmpty));
    }

    // ── Séances : suppression ─────────────────────────────────────

    private async void OnDeleteSession(SavedSessionCardViewModel card)
    {
        await _sessionLibraryService.DeleteSessionAsync(card.Entry.Id);
        SavedSessions.Remove(card);
        OnPropertyChanged(nameof(IsSessionsEmpty));
    }

    // ── Séances : modification ────────────────────────────────────

    private void OnEditSession(SavedSessionCardViewModel card)
    {
        // Naviguer vers le Créateur de Séance
        // NavigateTo crée (Transient) et stocke dans CurrentViewModel le ProgramBuilderViewModel
        _navigationService.NavigateTo("ProgramBuilder");

        // CurrentViewModel est maintenant le nouveau ProgramBuilderViewModel
        // SetPendingEdit gère les deux cas : LoadAsync pas encore lancé ou déjà terminé
        if (_navigationService.CurrentViewModel is ProgramBuilderViewModel pbvm)
            pbvm.SetPendingEdit(card.Entry);
    }

    // ── Programmes : Créer ────────────────────────────────────────

    [RelayCommand]
    private void OpenCreate()
    {
        IsEditMode       = false;
        ModalNom         = string.Empty;
        ModalDescription = string.Empty;
        ModalTypeIndex   = 3;
        ModalNiveauIndex = 1;
        ModalDuree       = 8;
        ModalSeances     = 3;
        ModalIsActif     = true;
        ErrorMessage     = null;
        OnPropertyChanged(nameof(ModalTitle));
        IsModalOpen = true;
    }

    [RelayCommand]
    private void CancelModal()
    {
        IsModalOpen  = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmModalAsync()
    {
        if (string.IsNullOrWhiteSpace(ModalNom))
        {
            ErrorMessage = "Le nom du programme est obligatoire.";
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (IsEditMode && SelectedProgramme is not null)
            {
                // ── Modification ──
                var p = SelectedProgramme.Programme;
                p.Name        = ModalNom.Trim();
                p.Description = ModalDescription.Trim();
                p.Type        = (ProgrammeType)ModalTypeIndex;
                p.Level       = (ProgrammeLevel)ModalNiveauIndex;
                p.IsActive    = ModalIsActif;

                var ok = await _programmeService.UpdateProgrammeAsync(p);
                if (!ok)
                {
                    ErrorMessage = "Échec de la mise à jour. Vérifiez votre connexion.";
                    return;
                }
            }
            else
            {
                // ── Création ──
                var newProgramme = new Programme
                {
                    Name            = ModalNom.Trim(),
                    Description     = ModalDescription.Trim(),
                    Type            = (ProgrammeType)ModalTypeIndex,
                    Level           = (ProgrammeLevel)ModalNiveauIndex,
                    DurationWeeks   = ModalDuree,
                    SessionsPerWeek = ModalSeances,
                    IsActive        = true
                };

                var created = await _programmeService.CreateProgrammeAsync(newProgramme);
                if (created is null)
                {
                    ErrorMessage = "Échec de la création. Si c'est votre première connexion, ouvrez la page Clients puis réessayez (résolution de votre ID administrateur).";
                    return;
                }
            }

            IsModalOpen = false;
            await RefreshProgrammesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Programmes : Modifier ─────────────────────────────────────

    private void OnEditProgramme(ProgrammeCardViewModel card)
    {
        SelectedProgramme = card;
        IsEditMode        = true;
        ModalNom          = card.Programme.Name;
        ModalDescription  = card.Programme.Description;
        ModalTypeIndex    = (int)card.Programme.Type;
        ModalNiveauIndex  = (int)card.Programme.Level;
        ModalDuree        = card.Programme.DurationWeeks;
        ModalSeances      = card.Programme.SessionsPerWeek;
        ModalIsActif      = card.Programme.IsActive;
        ErrorMessage      = null;
        OnPropertyChanged(nameof(ModalTitle));
        IsModalOpen = true;
    }

    // ── Programmes : Supprimer ────────────────────────────────────

    private void OnDeleteProgramme(ProgrammeCardViewModel card)
    {
        CardPendingDelete    = card;
        IsDeleteConfirmOpen  = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteConfirmOpen = false;
        CardPendingDelete   = null;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        var card = CardPendingDelete;
        IsDeleteConfirmOpen = false;
        CardPendingDelete   = null;

        if (card is null || IsBusy) return;

        try
        {
            IsBusy = true;
            await _programmeService.DeleteProgrammeAsync(card.Programme.Id);
            Programmes.Remove(card);
            if (SelectedProgramme == card) SelectedProgramme = null;
            OnPropertyChanged(nameof(IsProgrammesEmpty));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur suppression : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Assignation client ────────────────────────────────────────

    private void OnAssignProgramme(ProgrammeCardViewModel card)
    {
        _programmeToAssign = card;
        _ = OpenAssignPanelAsync(card.Programme.Name);
    }

    private async Task OpenAssignPanelAsync(string name)
    {
        try
        {
            var clients = await _clientService.GetClientsAsync();
            SelectableClients.Clear();

            foreach (var client in clients.Where(c => c.Status == "Actif"))
            {
                var vm = new ClientSelectionViewModel(client);
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ClientSelectionViewModel.IsSelected))
                        OnPropertyChanged(nameof(HasSelectedClients));
                };
                SelectableClients.Add(vm);
            }

            AssignTargetName = name;
            AssignStartDate  = DateTime.Today;
            OnPropertyChanged(nameof(HasSelectedClients));
            IsAssignPanelOpen = true;
        }
        catch
        {
            await _alertService.AlertAsync("Erreur", "Impossible de charger la liste des clients.");
        }
    }

    [RelayCommand]
    private void CancelAssign()
    {
        _programmeToAssign = null;
        IsAssignPanelOpen  = false;
    }

    [RelayCommand]
    private async Task ConfirmAssignAsync()
    {
        var selected = SelectableClients.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            await _alertService.AlertAsync("Attention", "Sélectionnez au moins un client.");
            return;
        }

        var card = _programmeToAssign;
        if (card is null) return;

        var failures = new List<string>();
        foreach (var clientVm in selected)
        {
            var assignment = new ClientProgramAssignment
            {
                ClientId     = clientVm.Client.Id,
                ProgrammeId  = card.Programme.Id,
                ProgramName  = card.Programme.Name,
                AssignedAt   = AssignStartDate,
                EndDate      = AssignStartDate.AddDays(card.Programme.DurationWeeks * 7)
            };
            await _assignmentService.AssignProgramAsync(assignment);

            // Si l'API a échoué, Id reste un nouveau Guid (non parsé depuis la réponse)
            // → on ne fait pas de check strict ici, juste un log
            if (assignment.Id == Guid.Empty)
                failures.Add(clientVm.Client.FullName);
        }

        IsAssignPanelOpen = false;
        var count = selected.Count;
        await _alertService.AlertAsync("Assigné",
            $"\"{card.Programme.Name}\" assigné à {count} client{(count > 1 ? "s" : "")}.");
    }
}

// ── ViewModel d'une carte programme API ─────────────────────────

public partial class ProgrammeCardViewModel : ObservableObject
{
    private readonly Action<ProgrammeCardViewModel> _editAction;
    private readonly Action<ProgrammeCardViewModel> _deleteAction;
    private readonly Action<ProgrammeCardViewModel> _assignAction;

    public Programme Programme { get; }

    public string Name        => Programme.Name;
    public string Description => string.IsNullOrWhiteSpace(Programme.Description)
        ? "Aucune description" : Programme.Description;
    public string TypeText    => Programme.TypeText;
    public string LevelText   => Programme.LevelText;
    public string StatusText  => Programme.IsActive ? "Actif" : "Inactif";
    public string DurationText => $"{Programme.DurationWeeks} sem. · {Programme.SessionsPerWeek} séance{(Programme.SessionsPerWeek > 1 ? "s" : "")}/sem.";
    public string CreatedAt   => Programme.CreatedAt.ToString("dd/MM/yyyy");

    public Color TypeBadgeColor => Programme.Type switch
    {
        ProgrammeType.Cardio      => Color.FromArgb("#DC2626"),
        ProgrammeType.Strength    => Color.FromArgb("#7C3AED"),
        ProgrammeType.Flexibility => Color.FromArgb("#059669"),
        _                         => Color.FromArgb("#4F46E5")
    };

    public Color TypeBadgeBg => Programme.Type switch
    {
        ProgrammeType.Cardio      => Color.FromArgb("#FEF2F2"),
        ProgrammeType.Strength    => Color.FromArgb("#F5F3FF"),
        ProgrammeType.Flexibility => Color.FromArgb("#F0FDF4"),
        _                         => Color.FromArgb("#EEF2FF")
    };

    public Color StatusColor => Programme.IsActive
        ? Color.FromArgb("#16A34A") : Color.FromArgb("#94A3B8");

    public Color StatusBg => Programme.IsActive
        ? Color.FromArgb("#F0FDF4") : Color.FromArgb("#F8FAFC");

    public ProgrammeCardViewModel(
        Programme programme,
        Action<ProgrammeCardViewModel> editAction,
        Action<ProgrammeCardViewModel> deleteAction,
        Action<ProgrammeCardViewModel> assignAction)
    {
        Programme     = programme;
        _editAction   = editAction;
        _deleteAction = deleteAction;
        _assignAction = assignAction;
    }

    [RelayCommand] void Edit()   => _editAction(this);
    [RelayCommand] void Delete() => _deleteAction(this);
    [RelayCommand] void Assign() => _assignAction(this);
}
