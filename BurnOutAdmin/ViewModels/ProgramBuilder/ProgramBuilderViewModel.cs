using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Models.Program;
using BurnOutAdmin.Services;
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

    public ProgramModel? Model { get; private set; }

    [ObservableProperty]
    private string _programName = string.Empty;

    [ObservableProperty]
    private string _programDescription = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isAssignPanelOpen;

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private string _assignSearchText = string.Empty;

    [ObservableProperty]
    private DateTime _assignStartDate = DateTime.Today;

    public ObservableCollection<SessionViewModel> Sessions { get; } = new();

    public ObservableCollection<LibraryCategoryViewModel> LibraryCategories { get; } = new();

    public ObservableCollection<Client> Clients { get; } = new();

    public ProgramBuilderViewModel(IProgramBuilderService programBuilderService, IClientService clientService, IProgramAssignmentService assignmentService, IAlertService alertService)
    {
        _programBuilderService = programBuilderService;
        _clientService = clientService;
        _assignmentService = assignmentService;
        _alertService = alertService;
        Title = "Program Builder";
        InitializeLibrary();
    }

    private void InitializeLibrary()
    {
        LibraryCategories.Add(new LibraryCategoryViewModel("Échauffement", new List<ExerciseLibraryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Rameur 5 min", Category = "Échauffement", Tags = new List<string> { "Cardio" } },
            new() { Id = Guid.NewGuid(), Name = "Mobilité Épaules", Category = "Échauffement", Tags = new List<string> { "Mobilité" } },
            new() { Id = Guid.NewGuid(), Name = "Band Pull Apart", Category = "Échauffement", Tags = new List<string> { "Activation" } }
        }));

        LibraryCategories.Add(new LibraryCategoryViewModel("Musculation", new List<ExerciseLibraryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Squat Barre", Category = "Musculation", Tags = new List<string> { "Jambes", "Force" } },
            new() { Id = Guid.NewGuid(), Name = "Développé Couché", Category = "Musculation", Tags = new List<string> { "Pectoraux", "Force" } },
            new() { Id = Guid.NewGuid(), Name = "Soulevé de Terre", Category = "Musculation", Tags = new List<string> { "Dos", "Force" } },
            new() { Id = Guid.NewGuid(), Name = "Shoulder Press", Category = "Musculation", Tags = new List<string> { "Épaules", "Force" } },
            new() { Id = Guid.NewGuid(), Name = "Rowing Barre", Category = "Musculation", Tags = new List<string> { "Dos", "Force" } }
        }) { IsExpanded = true });

        LibraryCategories.Add(new LibraryCategoryViewModel("Cardio", new List<ExerciseLibraryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Bike Erg", Category = "Cardio", Tags = new List<string> { "Endurance" } },
            new() { Id = Guid.NewGuid(), Name = "Course 400m", Category = "Cardio", Tags = new List<string> { "Sprint" } }
        }));

        LibraryCategories.Add(new LibraryCategoryViewModel("Récupération", new List<ExerciseLibraryItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Étirements statiques", Category = "Récupération", Tags = new List<string> { "Flexibilité" } },
            new() { Id = Guid.NewGuid(), Name = "Foam Rolling", Category = "Récupération", Tags = new List<string> { "Myofascial" } }
        }));
    }

    partial void OnProgramNameChanged(string value)
    {
        if (Model != null)
            Model.Name = value;
    }

    partial void OnProgramDescriptionChanged(string value)
    {
        if (Model != null)
            Model.Description = value;
    }

    [RelayCommand]
    private async Task LoadProgramAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var program = await _programBuilderService.GetSampleProgramAsync();
            LoadProgram(program);
        }
        finally
        {
            IsBusy = false;
        }
    }

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
        {
            Sessions.Add(new SessionViewModel(session, RemoveSession));
        }
    }

    private SessionViewModel CreateSession()
    {
        var model = new SessionModel
        {
            Id = Guid.NewGuid(),
            Name = "Nouvelle Session",
            Order = Sessions.Count + 1
        };

        Model?.Sessions.Add(model);
        return new SessionViewModel(model, RemoveSession);
    }

    private void RemoveSession(SessionViewModel session)
    {
        Sessions.Remove(session);
        Model?.Sessions.Remove(session.Model);
        RecalculateOrders();
    }

    [RelayCommand]
    private void AddLibraryExercise(ExerciseLibraryItem item)
    {
        // Find the first session that has at least one category with a subcategory
        foreach (var session in Sessions)
        {
            foreach (var category in session.Categories)
            {
                foreach (var subCategory in category.SubCategories)
                {
                    subCategory.AddExerciseFromLibrary(item);
                    return;
                }
            }
        }

        // No session/category/subcategory exists yet — create one automatically
        AddSession();
        var newSession = Sessions.Last();
        newSession.AddCategoryCommand.Execute(null);
        var newCategory = newSession.Categories.Last();
        newCategory.AddSubCategoryCommand.Execute(null);
        var newSubCategory = newCategory.SubCategories.Last();
        newSubCategory.AddExerciseFromLibrary(item);
    }

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
    private void CancelAssign()
    {
        IsAssignPanelOpen = false;
    }

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

        await _alertService.AlertAsync(
            "Programme assigné",
            $"Le programme \"{ProgramName}\" a été assigné à {SelectedClient.FirstName} {SelectedClient.LastName}.");
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

    private void RecalculateOrders()
    {
        for (var i = 0; i < Sessions.Count; i++)
        {
            Sessions[i].Order = i + 1;
        }
    }
}
