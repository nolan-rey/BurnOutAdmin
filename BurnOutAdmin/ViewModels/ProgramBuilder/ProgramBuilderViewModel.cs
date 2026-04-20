using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Models.Program;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.ExerciseLibrary;
using BurnOutAdmin.Services.ProgramBuilder;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class ProgramBuilderViewModel : BaseViewModel
{
    private readonly IProgramBuilderService _programBuilderService;
    private readonly IClientService _clientService;
    private readonly IProgramAssignmentService _assignmentService;
    private readonly IAlertService _alertService;
    private readonly IExerciseLibraryService _libraryService;

    public ProgramModel? Model { get; private set; }

    // ── Programme ────────────────────────────────────────────────
    [ObservableProperty] private string _programName = string.Empty;
    [ObservableProperty] private string _programDescription = string.Empty;

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
    public ObservableCollection<SessionViewModel> Sessions { get; } = new();
    public ObservableCollection<LibraryCategoryViewModel> LibraryCategories { get; } = new();
    public ObservableCollection<Client> Clients { get; } = new();
    public ObservableCollection<string> KnownCategories { get; } = new();

    public ProgramBuilderViewModel(
        IProgramBuilderService programBuilderService,
        IClientService clientService,
        IProgramAssignmentService assignmentService,
        IAlertService alertService,
        IExerciseLibraryService libraryService)
    {
        _programBuilderService = programBuilderService;
        _clientService = clientService;
        _assignmentService = assignmentService;
        _alertService = alertService;
        _libraryService = libraryService;
        Title = "Program Builder";
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

            if (Model is null)
            {
                var program = await _programBuilderService.GetSampleProgramAsync();
                LoadProgram(program);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

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
        // Catégories prédéfinies non encore en base
        foreach (var c in new[] { "Échauffement", "Musculation", "Cardio", "Récupération" })
            if (!KnownCategories.Contains(c)) KnownCategories.Add(c);
    }

    // ── Filtrage bibliothèque ────────────────────────────────────

    partial void OnLibrarySearchTextChanged(string value) => ApplyLibraryFilter(value);

    private void ApplyLibraryFilter(string search)
    {
        var lower = search.Trim().ToLower();
        foreach (var cat in LibraryCategories)
        {
            cat.ApplyFilter(lower);
        }
    }

    // ── Programme ────────────────────────────────────────────────

    partial void OnProgramNameChanged(string value) { if (Model != null) Model.Name = value; }
    partial void OnProgramDescriptionChanged(string value) { if (Model != null) Model.Description = value; }

    [RelayCommand]
    private void AddSession()
    {
        var session = CreateSession();
        Sessions.Add(session);
    }

    public void LoadProgram(ProgramModel program)
    {
        Model = program;
        ProgramName = program.Name;
        ProgramDescription = program.Description;
        Sessions.Clear();
        foreach (var session in program.Sessions)
            Sessions.Add(new SessionViewModel(session, RemoveSession));
    }

    private SessionViewModel CreateSession()
    {
        var model = new SessionModel
        {
            Id = Guid.NewGuid(),
            Name = "Nouvelle Séance",
            Order = Sessions.Count + 1
        };
        if (Model is null)
        {
            Model = new ProgramModel { Id = Guid.NewGuid(), Name = ProgramName, Description = ProgramDescription };
        }
        Model.Sessions.Add(model);
        return new SessionViewModel(model, RemoveSession);
    }

    private void RemoveSession(SessionViewModel session)
    {
        Sessions.Remove(session);
        Model?.Sessions.Remove(session.Model);
        RecalculateOrders();
    }

    // ── Drag & Drop depuis la bibliothèque ───────────────────────

    /// <summary>
    /// Appelé depuis le code-behind quand un exercice est déposé sur une séance.
    /// </summary>
    public void DropExerciseOnSession(SessionViewModel session, string exerciseName, string category = "")
    {
        var item = new ExerciseLibraryItem { Name = exerciseName, Category = category };
        AddItemToSession(session, item);
    }

    private void AddItemToSession(SessionViewModel session, ExerciseLibraryItem item)
    {
        // Cherche la première sous-catégorie disponible
        foreach (var cat in session.Categories)
        {
            if (cat.SubCategories.Count > 0)
            {
                cat.SubCategories[0].AddExerciseFromLibrary(item);
                cat.IsExpanded = true;
                cat.SubCategories[0].IsExpanded = true;
                return;
            }
        }

        // Aucune structure n'existe encore → créer catégorie + sous-catégorie
        session.AddCategoryCommand.Execute(null);
        var newCat = session.Categories.Last();
        newCat.AddSubCategoryCommand.Execute(null);
        newCat.SubCategories.Last().AddExerciseFromLibrary(item);
        newCat.IsExpanded = true;
        newCat.SubCategories.Last().IsExpanded = true;
    }

    [RelayCommand]
    private void AddLibraryExercise(ExerciseLibraryItem item)
    {
        if (Sessions.Count == 0) AddSession();
        AddItemToSession(Sessions[0], item);
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
    private void CancelAddExercise()
    {
        IsAddExerciseOpen = false;
    }

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
        if (string.IsNullOrWhiteSpace(ProgramName))
        {
            await _alertService.AlertAsync("Attention", "Veuillez donner un nom au programme avant de l'assigner.");
            return;
        }

        var assignment = new ClientProgramAssignment
        {
            ClientId = SelectedClient.Id,
            ProgramName = ProgramName,
            AssignedAt = AssignStartDate,
            Sessions = Sessions.Select(s => new AssignedSession
            {
                Name = s.Name,
                Order = s.Order,
                Categories = s.Categories.Select(c => new AssignedCategory
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
            }).ToList()
        };

        await _assignmentService.AssignProgramAsync(assignment);
        IsAssignPanelOpen = false;
        await _alertService.AlertAsync("Programme assigné",
            $"Le programme \"{ProgramName}\" a été assigné à {SelectedClient.FullName}.");
    }

    [RelayCommand]
    private async Task SaveToLibraryAsync()
    {
        if (string.IsNullOrWhiteSpace(ProgramName))
        {
            await _alertService.AlertAsync("Attention", "Veuillez donner un nom au modèle avant de sauvegarder.");
            return;
        }
        await _alertService.AlertAsync("Sauvegardé", $"Le modèle \"{ProgramName}\" a été sauvegardé dans la bibliothèque.");
    }

    // ── Helpers ──────────────────────────────────────────────────

    private void RecalculateOrders()
    {
        for (var i = 0; i < Sessions.Count; i++)
            Sessions[i].Order = i + 1;
    }

    private static ExerciseLibraryItem ToItem(ExerciseLibraryEntry e) => new()
    {
        DbId = e.Id,
        Id = Guid.NewGuid(),
        Name = e.Name,
        Category = e.CategoryName,
        MuscleGroup = e.MuscleGroup
    };
}
