using System.Collections.ObjectModel;
using System.Text.Json;
using BurnOutAdmin.Models;
using BurnOutAdmin.Models.Program;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.SessionLibrary;
using BurnOutAdmin.ViewModels.ProgramBuilder;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class ProgrammesViewModel : BaseViewModel
{
    private readonly ISessionLibraryService _sessionLibraryService;
    private readonly IClientService _clientService;
    private readonly IProgramAssignmentService _assignmentService;
    private readonly IAlertService _alertService;

    // ── Bibliothèque de séances ───────────────────────────────────
    public ObservableCollection<SavedSessionCardViewModel> SavedSessions { get; } = new();

    // ── Programmes créés ─────────────────────────────────────────
    public ObservableCollection<SavedProgrammeViewModel> SavedProgrammes { get; } = new();

    // ── Création programme ────────────────────────────────────────
    [ObservableProperty] private bool _isCreateProgramOpen;
    [ObservableProperty] private string _newProgramName = string.Empty;
    [ObservableProperty] private string _newProgramDescription = string.Empty;

    public ObservableCollection<SavedSessionCardViewModel> SelectedSessions { get; } = new();

    // ── Assignation multi-client ──────────────────────────────────
    [ObservableProperty] private bool _isAssignPanelOpen;
    [ObservableProperty] private string _assignTargetName = string.Empty;
    [ObservableProperty] private string _assignTargetType = string.Empty; // "Séance" ou "Programme"
    [ObservableProperty] private DateTime _assignStartDate = DateTime.Today;

    public ObservableCollection<ClientSelectionViewModel> SelectableClients { get; } = new();

    // Référence à ce qui est en cours d'assignation
    private SavedSessionCardViewModel? _sessionToAssign;
    private SavedProgrammeViewModel? _programmeToAssign;

    // ── États computed ────────────────────────────────────────────
    public bool IsSessionsEmpty => SavedSessions.Count == 0;
    public bool IsProgrammesEmpty => SavedProgrammes.Count == 0;
    public bool HasSelectedSessions => SelectedSessions.Count > 0;
    public bool HasSelectedClients => SelectableClients.Any(c => c.IsSelected);

    public ProgrammesViewModel(
        ISessionLibraryService sessionLibraryService,
        IClientService clientService,
        IProgramAssignmentService assignmentService,
        IAlertService alertService)
    {
        _sessionLibraryService = sessionLibraryService;
        _clientService = clientService;
        _assignmentService = assignmentService;
        _alertService = alertService;
        Title = "Programmes";

        SelectedSessions.CollectionChanged += (_, _) =>
            OnPropertyChanged(nameof(HasSelectedSessions));
    }

    // ── Chargement ────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await _sessionLibraryService.InitializeAsync();
            await RefreshSessionsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshSessionsAsync()
    {
        var entries = await _sessionLibraryService.GetAllSessionsAsync();
        SavedSessions.Clear();
        foreach (var e in entries)
            SavedSessions.Add(new SavedSessionCardViewModel(e, OnDeleteSession, OnToggleSession, OnAssignSession));

        OnPropertyChanged(nameof(IsSessionsEmpty));
    }

    // ── Suppression séance ────────────────────────────────────────

    private async void OnDeleteSession(SavedSessionCardViewModel card)
    {
        await _sessionLibraryService.DeleteSessionAsync(card.Entry.Id);
        SavedSessions.Remove(card);
        SelectedSessions.Remove(card);
        OnPropertyChanged(nameof(IsSessionsEmpty));
    }

    // ── Sélection pour création de programme ─────────────────────

    private void OnToggleSession(SavedSessionCardViewModel card)
    {
        if (!IsCreateProgramOpen) return;

        card.IsSelected = !card.IsSelected;
        if (card.IsSelected)
            SelectedSessions.Add(card);
        else
            SelectedSessions.Remove(card);
    }

    // ── Création de programme ─────────────────────────────────────

    [RelayCommand]
    private void OpenCreateProgram()
    {
        NewProgramName = string.Empty;
        NewProgramDescription = string.Empty;
        foreach (var s in SavedSessions) s.IsSelected = false;
        SelectedSessions.Clear();
        IsCreateProgramOpen = true;
    }

    [RelayCommand]
    private void CancelCreateProgram()
    {
        foreach (var s in SavedSessions) s.IsSelected = false;
        SelectedSessions.Clear();
        IsCreateProgramOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmCreateProgramAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProgramName))
        {
            await _alertService.AlertAsync("Attention", "Donnez un nom au programme.");
            return;
        }
        if (SelectedSessions.Count == 0)
        {
            await _alertService.AlertAsync("Attention", "Sélectionnez au moins une séance dans la bibliothèque.");
            return;
        }

        // On conserve les entrées complètes pour l'assignation ultérieure
        var programme = new SavedProgrammeViewModel(
            NewProgramName.Trim(),
            NewProgramDescription.Trim(),
            SelectedSessions.Select(s => s.Entry).ToList(),
            DateTime.Now,
            OnAssignProgramme);

        SavedProgrammes.Insert(0, programme);
        OnPropertyChanged(nameof(IsProgrammesEmpty));

        foreach (var s in SavedSessions) s.IsSelected = false;
        SelectedSessions.Clear();
        IsCreateProgramOpen = false;

        await _alertService.AlertAsync("Programme créé",
            $"Le programme \"{programme.Name}\" avec {programme.SessionCount} séance(s) a été créé.");
    }

    // ── Ouverture panneau d'assignation ──────────────────────────

    private void OnAssignSession(SavedSessionCardViewModel card)
    {
        _sessionToAssign = card;
        _programmeToAssign = null;
        _ = OpenAssignPanelAsync(card.Name, "Séance");
    }

    private void OnAssignProgramme(SavedProgrammeViewModel programme)
    {
        _programmeToAssign = programme;
        _sessionToAssign = null;
        _ = OpenAssignPanelAsync(programme.Name, "Programme");
    }

    private async Task OpenAssignPanelAsync(string name, string type)
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
            AssignTargetType = type;
            AssignStartDate = DateTime.Today;
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
        _sessionToAssign = null;
        _programmeToAssign = null;
        IsAssignPanelOpen = false;
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

        foreach (var clientVm in selected)
        {
            ClientProgramAssignment assignment;

            if (_sessionToAssign is not null)
            {
                // ── Séance unique ────────────────────────────────
                var sessionModel = TryDeserializeSession(_sessionToAssign.Entry.DataJson);
                assignment = new ClientProgramAssignment
                {
                    ClientId = clientVm.Client.Id,
                    ProgramName = AssignTargetName,
                    AssignedAt = AssignStartDate,
                    Sessions = new List<AssignedSession>
                    {
                        new()
                        {
                            Name = AssignTargetName,
                            Order = 1,
                            Categories = BuildCategories(sessionModel)
                        }
                    }
                };
            }
            else if (_programmeToAssign is not null)
            {
                // ── Programme multi-séances ──────────────────────
                assignment = new ClientProgramAssignment
                {
                    ClientId = clientVm.Client.Id,
                    ProgramName = AssignTargetName,
                    AssignedAt = AssignStartDate,
                    Sessions = _programmeToAssign.Sessions.Select((entry, i) =>
                    {
                        var model = TryDeserializeSession(entry.DataJson);
                        return new AssignedSession
                        {
                            Name = entry.Name,
                            Order = i + 1,
                            Categories = BuildCategories(model)
                        };
                    }).ToList()
                };
            }
            else continue;

            await _assignmentService.AssignProgramAsync(assignment);
        }

        IsAssignPanelOpen = false;
        var count = selected.Count;
        await _alertService.AlertAsync("Assigné avec succès",
            $"\"{AssignTargetName}\" a été assigné à {count} client{(count > 1 ? "s" : "")} avec succès.");
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static SessionModel? TryDeserializeSession(string json)
    {
        try { return JsonSerializer.Deserialize<SessionModel>(json); }
        catch { return null; }
    }

    private static List<AssignedCategory> BuildCategories(SessionModel? session)
    {
        if (session is null) return new();

        return session.Categories.Select(c => new AssignedCategory
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
        }).ToList();
    }
}

// ── ViewModel d'un programme créé ────────────────────────────────
public partial class SavedProgrammeViewModel : ObservableObject
{
    private readonly Action<SavedProgrammeViewModel>? _assignAction;

    public string Name { get; }
    public string Description { get; }
    public List<SavedSessionEntry> Sessions { get; }   // entrées complètes avec DataJson
    public int SessionCount => Sessions.Count;
    public string CreatedAt { get; }
    public string SubTitle => $"{SessionCount} séance{(SessionCount > 1 ? "s" : "")}";
    public string SessionsPreview =>
        string.Join(" · ", Sessions.Take(3).Select(s => s.Name)) + (Sessions.Count > 3 ? " …" : "");

    public SavedProgrammeViewModel(
        string name,
        string description,
        List<SavedSessionEntry> sessions,
        DateTime createdAt,
        Action<SavedProgrammeViewModel>? assignAction = null)
    {
        Name = name;
        Description = description;
        Sessions = sessions;
        CreatedAt = createdAt.ToString("dd/MM/yyyy");
        _assignAction = assignAction;
    }

    [RelayCommand]
    private void Assign() => _assignAction?.Invoke(this);
}
