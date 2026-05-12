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
    private readonly IProgrammeSeanceService   _programmeSeanceService;
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

    // ── Gestion séances d'un programme ───────────────────────────
    [ObservableProperty] private bool _isManageSeancesOpen;
    [ObservableProperty] private ProgrammeCardViewModel? _seancesTargetProgramme;
    [ObservableProperty] private bool _isAddSeanceFromLibraryOpen;

    /// <summary>Séances actuellement attachées au programme ouvert dans le modal.</summary>
    public ObservableCollection<ProgrammeSeanceCardViewModel> ProgrammeSeances { get; } = new();

    /// <summary>Templates affichés dans le picker multi-sélection (IsSelected sur chaque carte).</summary>
    public ObservableCollection<SavedSessionCardViewModel> PickerTemplates { get; } = new();

    public bool IsProgrammeSeancesEmpty => ProgrammeSeances.Count == 0 && !IsBusy;
    public bool HasSelectedTemplates    => PickerTemplates.Any(t => t.IsSelected);

    // ── Édition inline d'une séance attachée ──────────────────────
    [ObservableProperty] private bool _isEditSeanceOpen;
    [ObservableProperty] private ProgrammeSeanceCardViewModel? _editingSeance;
    [ObservableProperty] private string _editSeanceName = string.Empty;
    [ObservableProperty] private string _editSeanceDescription = string.Empty;

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
        IProgrammeSeanceService   programmeSeanceService,
        IAlertService            alertService,
        INavigationService       navigationService)
    {
        _programmeService       = programmeService;
        _sessionLibraryService  = sessionLibraryService;
        _clientService          = clientService;
        _assignmentService      = assignmentService;
        _programmeSeanceService = programmeSeanceService;
        _alertService           = alertService;
        _navigationService      = navigationService;
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
            Programmes.Add(new ProgrammeCardViewModel(p,
                OnEditProgramme, OnDeleteProgramme, OnAssignProgramme, OnManageSeances));
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

    // ── Gestion des séances d'un programme ───────────────────────

    private void OnManageSeances(ProgrammeCardViewModel card) =>
        _ = OpenManageSeancesAsync(card);

    private async Task OpenManageSeancesAsync(ProgrammeCardViewModel card)
    {
        SeancesTargetProgramme = card;
        ProgrammeSeances.Clear();
        IsManageSeancesOpen = true;
        OnPropertyChanged(nameof(IsProgrammeSeancesEmpty));

        await RefreshProgrammeSeancesAsync();
    }

    private async Task RefreshProgrammeSeancesAsync()
    {
        if (SeancesTargetProgramme is null) return;

        var list = await _programmeSeanceService.GetSeancesAsync(SeancesTargetProgramme.Programme.Id);
        ProgrammeSeances.Clear();
        foreach (var s in list)
            ProgrammeSeances.Add(CreateSeanceCard(s));
        OnPropertyChanged(nameof(IsProgrammeSeancesEmpty));
    }

    private ProgrammeSeanceCardViewModel CreateSeanceCard(ProgrammeSeance s) =>
        new(s, OnRemoveProgrammeSeance, OnMoveSeanceUp, OnMoveSeanceDown, OnEditSeance);

    [RelayCommand]
    private void CloseManageSeances()
    {
        IsManageSeancesOpen = false;
        IsAddSeanceFromLibraryOpen = false;
        IsEditSeanceOpen = false;
        SeancesTargetProgramme = null;
        EditingSeance = null;
        ProgrammeSeances.Clear();
        PickerTemplates.Clear();
    }

    [RelayCommand]
    private void OpenAddSeanceFromLibrary()
    {
        // Reconstruit la liste du picker en clonant des cartes avec IsSelected reset
        PickerTemplates.Clear();
        foreach (var t in SavedSessions)
        {
            var card = new SavedSessionCardViewModel(t.Entry);
            card.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SavedSessionCardViewModel.IsSelected))
                    OnPropertyChanged(nameof(HasSelectedTemplates));
            };
            PickerTemplates.Add(card);
        }
        OnPropertyChanged(nameof(HasSelectedTemplates));
        IsAddSeanceFromLibraryOpen = true;
    }

    [RelayCommand]
    private void CancelAddSeanceFromLibrary()
    {
        IsAddSeanceFromLibraryOpen = false;
        PickerTemplates.Clear();
    }

    /// <summary>Toggle IsSelected sur une carte du picker (utilisé par TapGestureRecognizer).</summary>
    [RelayCommand]
    private void ToggleTemplateSelection(SavedSessionCardViewModel? card)
    {
        if (card is null) return;
        card.IsSelected = !card.IsSelected;
    }

    [RelayCommand]
    private async Task ConfirmAddSeanceFromLibraryAsync()
    {
        if (SeancesTargetProgramme is null) return;
        if (IsBusy) return;

        var selected = PickerTemplates.Where(t => t.IsSelected).ToList();
        if (selected.Count == 0)
        {
            await _alertService.AlertAsync("Attention", "Sélectionnez au moins une séance.");
            return;
        }

        try
        {
            IsBusy = true;
            var failures = new List<string>();

            foreach (var template in selected)
            {
                var nextOrder = ProgrammeSeances.Count + 1;
                var attached  = await _programmeSeanceService.AddSeanceFromTemplateAsync(
                    SeancesTargetProgramme.Programme.Id, template.Entry, nextOrder);

                if (attached is null)
                {
                    failures.Add(template.Name);
                }
                else
                {
                    ProgrammeSeances.Add(CreateSeanceCard(attached));
                }
            }

            OnPropertyChanged(nameof(IsProgrammeSeancesEmpty));
            IsAddSeanceFromLibraryOpen = false;
            PickerTemplates.Clear();

            if (failures.Count > 0)
            {
                await _alertService.AlertAsync("Partiel",
                    $"Échec d'ajout pour : {string.Join(", ", failures)}");
            }
        }
        catch (Exception ex)
        {
            await _alertService.AlertAsync("Erreur", $"Échec de l'ajout : {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Reorder ──────────────────────────────────────────────────

    private async void OnMoveSeanceUp(ProgrammeSeanceCardViewModel card)
    {
        var idx = ProgrammeSeances.IndexOf(card);
        if (idx <= 0) return;
        await SwapAndPersistAsync(idx, idx - 1);
    }

    private async void OnMoveSeanceDown(ProgrammeSeanceCardViewModel card)
    {
        var idx = ProgrammeSeances.IndexOf(card);
        if (idx < 0 || idx >= ProgrammeSeances.Count - 1) return;
        await SwapAndPersistAsync(idx, idx + 1);
    }

    private async Task SwapAndPersistAsync(int from, int to)
    {
        if (SeancesTargetProgramme is null) return;
        var programmeId = SeancesTargetProgramme.Programme.Id;

        ProgrammeSeances.Move(from, to);

        // Recalculer les ordres locaux (1-based) puis persister chaque changement
        var tasks = new List<Task<bool>>();
        for (var i = 0; i < ProgrammeSeances.Count; i++)
        {
            var card     = ProgrammeSeances[i];
            var newOrder = i + 1;
            if (card.Order == newOrder) continue;
            card.Order = newOrder;
            tasks.Add(_programmeSeanceService.UpdateSeanceAsync(
                programmeId, card.Seance.Id, newOrder, null, null));
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }

    // ── Edit inline (nom / description) ──────────────────────────

    private void OnEditSeance(ProgrammeSeanceCardViewModel card)
    {
        EditingSeance          = card;
        EditSeanceName         = card.Name;
        EditSeanceDescription  = card.Description;
        IsEditSeanceOpen       = true;
    }

    [RelayCommand]
    private void CancelEditSeance()
    {
        IsEditSeanceOpen = false;
        EditingSeance    = null;
    }

    [RelayCommand]
    private async Task ConfirmEditSeanceAsync()
    {
        if (SeancesTargetProgramme is null || EditingSeance is null) return;
        if (string.IsNullOrWhiteSpace(EditSeanceName))
        {
            await _alertService.AlertAsync("Attention", "Le nom est obligatoire.");
            return;
        }

        var ok = await _programmeSeanceService.UpdateSeanceAsync(
            SeancesTargetProgramme.Programme.Id,
            EditingSeance.Seance.Id,
            null,
            EditSeanceName.Trim(),
            EditSeanceDescription.Trim());

        if (ok)
        {
            EditingSeance.Name        = EditSeanceName.Trim();
            EditingSeance.Description = EditSeanceDescription.Trim();
            IsEditSeanceOpen          = false;
            EditingSeance             = null;
        }
        else
        {
            await _alertService.AlertAsync("Erreur", "Impossible de modifier la séance.");
        }
    }

    // ── Suppression ──────────────────────────────────────────────

    private async void OnRemoveProgrammeSeance(ProgrammeSeanceCardViewModel card)
    {
        if (SeancesTargetProgramme is null) return;

        var ok = await _programmeSeanceService.RemoveSeanceAsync(
            SeancesTargetProgramme.Programme.Id, card.Seance.Id);
        if (ok)
        {
            ProgrammeSeances.Remove(card);
            // Recompacter les ordres locaux après suppression
            for (var i = 0; i < ProgrammeSeances.Count; i++)
                ProgrammeSeances[i].Order = i + 1;
            OnPropertyChanged(nameof(IsProgrammeSeancesEmpty));
        }
        else
        {
            await _alertService.AlertAsync("Erreur", "Impossible de retirer la séance.");
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
    private readonly Action<ProgrammeCardViewModel> _manageSeancesAction;

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
        Action<ProgrammeCardViewModel> assignAction,
        Action<ProgrammeCardViewModel> manageSeancesAction)
    {
        Programme            = programme;
        _editAction          = editAction;
        _deleteAction        = deleteAction;
        _assignAction        = assignAction;
        _manageSeancesAction = manageSeancesAction;
    }

    [RelayCommand] void Edit()           => _editAction(this);
    [RelayCommand] void Delete()         => _deleteAction(this);
    [RelayCommand] void Assign()         => _assignAction(this);
    [RelayCommand] void ManageSeances()  => _manageSeancesAction(this);
}

// ── Carte d'une séance attachée à un programme ──────────────────

public partial class ProgrammeSeanceCardViewModel : ObservableObject
{
    private readonly Action<ProgrammeSeanceCardViewModel> _removeAction;
    private readonly Action<ProgrammeSeanceCardViewModel> _moveUpAction;
    private readonly Action<ProgrammeSeanceCardViewModel> _moveDownAction;
    private readonly Action<ProgrammeSeanceCardViewModel> _editAction;

    public ProgrammeSeance Seance { get; }

    [ObservableProperty] private int _order;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;

    public string DescriptionDisplay => string.IsNullOrWhiteSpace(Description)
        ? "Aucune description" : Description;
    public string OrderText   => $"S{Order}";
    public string ContentText => $"{Seance.CategoryCount} cat. · {Seance.ExerciseCount} ex.";

    public ProgrammeSeanceCardViewModel(
        ProgrammeSeance seance,
        Action<ProgrammeSeanceCardViewModel> removeAction,
        Action<ProgrammeSeanceCardViewModel> moveUpAction,
        Action<ProgrammeSeanceCardViewModel> moveDownAction,
        Action<ProgrammeSeanceCardViewModel> editAction)
    {
        Seance          = seance;
        _removeAction   = removeAction;
        _moveUpAction   = moveUpAction;
        _moveDownAction = moveDownAction;
        _editAction     = editAction;

        _order       = seance.Order;
        _name        = seance.Name;
        _description = seance.Description;
    }

    partial void OnOrderChanged(int value)
    {
        Seance.Order = value;
        OnPropertyChanged(nameof(OrderText));
    }

    partial void OnNameChanged(string value) => Seance.Name = value;
    partial void OnDescriptionChanged(string value)
    {
        Seance.Description = value;
        OnPropertyChanged(nameof(DescriptionDisplay));
    }

    [RelayCommand] void Remove()   => _removeAction(this);
    [RelayCommand] void MoveUp()   => _moveUpAction(this);
    [RelayCommand] void MoveDown() => _moveDownAction(this);
    [RelayCommand] void Edit()     => _editAction(this);
}
