using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Models.Program;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.ExerciseLibrary;
using BurnOutAdmin.Services.SessionLibrary;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class ProgramBuilderViewModel : BaseViewModel
{
    private readonly IClientService _clientService;
    private readonly IProgramAssignmentService _assignmentService;
    private readonly IAlertService _alertService;
    private readonly IExerciseLibraryService _libraryService;
    private readonly ISessionLibraryService _sessionLibraryService;

    // ── Séance courante (unique) ──────────────────────────────────
    [ObservableProperty] private SessionViewModel? _currentSession;
    [ObservableProperty] private string _seanceName = string.Empty;
    [ObservableProperty] private string _seanceDescription = string.Empty;

    // ── Bibliothèque ─────────────────────────────────────────────
    [ObservableProperty] private string _librarySearchText = string.Empty;

    // ── Assignation client ────────────────────────────────────────
    [ObservableProperty] private bool _isAssignPanelOpen;
    [ObservableProperty] private Client? _selectedClient;
    [ObservableProperty] private string _assignSearchText = string.Empty;
    [ObservableProperty] private DateTime _assignStartDate = DateTime.Today;

    // ── Popup ajout exercice à la bibliothèque ────────────────────
    [ObservableProperty] private bool _isAddExerciseOpen;
    [ObservableProperty] private string _newExerciseName = string.Empty;
    [ObservableProperty] private string _newExerciseCategory = string.Empty;
    [ObservableProperty] private string _newExerciseMuscleGroup = string.Empty;

    // ── Collections ───────────────────────────────────────────────
    public ObservableCollection<LibraryCategoryViewModel> LibraryCategories { get; } = new();
    public ObservableCollection<Client> Clients { get; } = new();
    public ObservableCollection<string> KnownCategories { get; } = new();

    public ProgramBuilderViewModel(
        IClientService clientService,
        IProgramAssignmentService assignmentService,
        IAlertService alertService,
        IExerciseLibraryService libraryService,
        ISessionLibraryService sessionLibraryService)
    {
        _clientService = clientService;
        _assignmentService = assignmentService;
        _alertService = alertService;
        _libraryService = libraryService;
        _sessionLibraryService = sessionLibraryService;
        Title = "Créateur de Séance";
    }

    // ── Initialisation ───────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await _libraryService.InitializeAsync();
            await RefreshLibraryAsync();

            if (CurrentSession is null)
                InitNewSeance();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void InitNewSeance()
    {
        var model = new SessionModel
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            Order = 1
        };
        CurrentSession = new SessionViewModel(model, removeAction: null);
        SeanceName = string.Empty;
        SeanceDescription = string.Empty;
    }

    // ── Synchronisation nom ──────────────────────────────────────

    partial void OnSeanceNameChanged(string value)
    {
        if (CurrentSession is not null)
            CurrentSession.Name = value;
    }

    // ── Bibliothèque ─────────────────────────────────────────────

    private async Task RefreshLibraryAsync()
    {
        var entries = await _libraryService.GetAllAsync();
        var grouped = entries
            .GroupBy(e => e.CategoryName)
            .Select(g => new LibraryCategoryViewModel(g.Key, g.Select(ToItem).ToList()))
            .ToList();

        LibraryCategories.Clear();
        foreach (var cat in grouped)
        {
            cat.IsExpanded = true;
            LibraryCategories.Add(cat);
        }

        var categories = await _libraryService.GetCategoriesAsync();
        KnownCategories.Clear();
        foreach (var c in categories) KnownCategories.Add(c);
        foreach (var c in new[] { "Échauffement", "Musculation", "Cardio", "Récupération" })
            if (!KnownCategories.Contains(c)) KnownCategories.Add(c);
    }

    partial void OnLibrarySearchTextChanged(string value) => ApplyLibraryFilter(value);

    private void ApplyLibraryFilter(string search)
    {
        var lower = search.Trim().ToLower();
        foreach (var cat in LibraryCategories)
            cat.ApplyFilter(lower);
    }

    // ── Nouvelle séance ──────────────────────────────────────────

    [RelayCommand]
    private void NewSeance()
    {
        InitNewSeance();
    }

    // ── Popup ajout exercice à la bibliothèque ────────────────────

    [RelayCommand]
    private void OpenAddExercise()
    {
        NewExerciseName = string.Empty;
        NewExerciseCategory = KnownCategories.FirstOrDefault() ?? "Musculation";
        NewExerciseMuscleGroup = string.Empty;
        IsAddExerciseOpen = true;
    }

    [RelayCommand]
    private void CancelAddExercise() => IsAddExerciseOpen = false;

    [RelayCommand]
    private async Task ConfirmAddExerciseAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExerciseName))
        {
            await _alertService.AlertAsync("Attention", "Le nom de l'exercice est obligatoire.");
            return;
        }
        if (string.IsNullOrWhiteSpace(NewExerciseCategory))
        {
            await _alertService.AlertAsync("Attention", "Veuillez choisir une catégorie.");
            return;
        }

        await _libraryService.AddAsync(NewExerciseName.Trim(), NewExerciseCategory.Trim(), NewExerciseMuscleGroup.Trim());
        IsAddExerciseOpen = false;
        await RefreshLibraryAsync();
    }

    // ── Assignation client ────────────────────────────────────────

    [RelayCommand]
    private async Task OpenAssignPanelAsync()
    {
        if (CurrentSession is null || !CurrentSession.Categories.Any())
        {
            await _alertService.AlertAsync("Séance vide", "Ajoutez au moins une catégorie avant d'assigner.");
            return;
        }

        try
        {
            var clients = await _clientService.GetClientsAsync();
            Clients.Clear();
            foreach (var client in clients.Where(c => c.Status == "Actif"))
                Clients.Add(client);

            SelectedClient = null;
            AssignSearchText = string.Empty;
            AssignStartDate = DateTime.Today;
            IsAssignPanelOpen = true;
        }
        catch
        {
            await _alertService.AlertAsync("Erreur", "Impossible de charger la liste des clients.");
        }
    }

    [RelayCommand]
    private void CancelAssign() => IsAssignPanelOpen = false;

    [RelayCommand]
    private async Task ConfirmAssignAsync()
    {
        if (SelectedClient == null)
        {
            await _alertService.AlertAsync("Attention", "Veuillez sélectionner un client.");
            return;
        }
        if (string.IsNullOrWhiteSpace(SeanceName))
        {
            await _alertService.AlertAsync("Attention", "Donnez un nom à la séance avant de l'assigner.");
            return;
        }

        var assignment = new ClientProgramAssignment
        {
            ClientId = SelectedClient.Id,
            ProgramName = SeanceName,
            AssignedAt = AssignStartDate,
            Sessions = new List<AssignedSession>
            {
                new AssignedSession
                {
                    Name = SeanceName,
                    Order = 1,
                    Categories = CurrentSession!.Categories.Select(c => new AssignedCategory
                    {
                        Name = c.Name,
                        Exercises = c.SubCategories
                            .SelectMany(sc => sc.Exercises)
                            .Select(e => new AssignedExercise
                            {
                                Name = e.Name,
                                Sets = e.Sets,
                                Reps = e.Reps,
                                Weight = e.Weight,
                                Rpe = e.Rpe
                            }).ToList()
                    }).ToList()
                }
            }
        };

        await _assignmentService.AssignProgramAsync(assignment);
        IsAssignPanelOpen = false;
        await _alertService.AlertAsync("Séance assignée",
            $"La séance \"{SeanceName}\" a été assignée à {SelectedClient.FullName}.");
    }

    [RelayCommand]
    private async Task SaveSeanceAsync()
    {
        if (string.IsNullOrWhiteSpace(SeanceName))
        {
            await _alertService.AlertAsync("Attention", "Donnez un nom à la séance avant de sauvegarder.");
            return;
        }
        if (CurrentSession is null || !CurrentSession.Categories.Any())
        {
            await _alertService.AlertAsync("Attention", "Ajoutez au moins une catégorie avant de sauvegarder.");
            return;
        }

        await _sessionLibraryService.InitializeAsync();
        await _sessionLibraryService.SaveSessionAsync(
            SeanceName.Trim(),
            SeanceDescription.Trim(),
            CurrentSession.Model);

        await _alertService.AlertAsync("Séance sauvegardée",
            $"La séance \"{SeanceName}\" est disponible dans la bibliothèque des Programmes.");

        InitNewSeance(); // Réinitialise pour une nouvelle séance
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static ExerciseLibraryItem ToItem(ExerciseLibraryEntry e) => new()
    {
        DbId = e.Id,
        Id = Guid.NewGuid(),
        Name = e.Name,
        Category = e.CategoryName,
        MuscleGroup = e.MuscleGroup
    };
}
