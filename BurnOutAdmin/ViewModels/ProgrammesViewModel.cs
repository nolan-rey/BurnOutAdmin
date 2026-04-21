using System.Collections.ObjectModel;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.SessionLibrary;
using BurnOutAdmin.ViewModels.ProgramBuilder;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class ProgrammesViewModel : BaseViewModel
{
    private readonly ISessionLibraryService _sessionLibraryService;
    private readonly IAlertService _alertService;

    // ── Bibliothèque de séances ───────────────────────────────────
    public ObservableCollection<SavedSessionCardViewModel> SavedSessions { get; } = new();

    // ── Programmes créés (session en cours) ───────────────────────
    public ObservableCollection<SavedProgrammeViewModel> SavedProgrammes { get; } = new();

    // ── Création programme ────────────────────────────────────────
    [ObservableProperty] private bool _isCreateProgramOpen;
    [ObservableProperty] private string _newProgramName = string.Empty;
    [ObservableProperty] private string _newProgramDescription = string.Empty;

    public ObservableCollection<SavedSessionCardViewModel> SelectedSessions { get; } = new();

    // ── États computed ────────────────────────────────────────────
    public bool IsSessionsEmpty => SavedSessions.Count == 0;
    public bool IsProgrammesEmpty => SavedProgrammes.Count == 0;
    public bool HasSelectedSessions => SelectedSessions.Count > 0;

    public ProgrammesViewModel(ISessionLibraryService sessionLibraryService, IAlertService alertService)
    {
        _sessionLibraryService = sessionLibraryService;
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
            SavedSessions.Add(new SavedSessionCardViewModel(e, OnDeleteSession, OnToggleSession));

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
        foreach (var s in SavedSessions)
            s.IsSelected = false;
        SelectedSessions.Clear();
        IsCreateProgramOpen = true;
    }

    [RelayCommand]
    private void CancelCreateProgram()
    {
        foreach (var s in SavedSessions)
            s.IsSelected = false;
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

        var programme = new SavedProgrammeViewModel(
            NewProgramName.Trim(),
            NewProgramDescription.Trim(),
            SelectedSessions.Select(s => s.Name).ToList(),
            DateTime.Now);

        SavedProgrammes.Insert(0, programme);
        OnPropertyChanged(nameof(IsProgrammesEmpty));

        foreach (var s in SavedSessions) s.IsSelected = false;
        SelectedSessions.Clear();
        IsCreateProgramOpen = false;

        await _alertService.AlertAsync("Programme créé",
            $"Le programme \"{programme.Name}\" avec {programme.SessionCount} séance(s) a été créé.");
    }
}

// ── Représentation d'un programme créé ───────────────────────────
public class SavedProgrammeViewModel
{
    public string Name { get; }
    public string Description { get; }
    public List<string> SessionNames { get; }
    public int SessionCount => SessionNames.Count;
    public string CreatedAt { get; }
    public string SubTitle => $"{SessionCount} séance{(SessionCount > 1 ? "s" : "")}";
    public string SessionsPreview =>
        string.Join(" · ", SessionNames.Take(3)) + (SessionNames.Count > 3 ? " …" : "");

    public SavedProgrammeViewModel(string name, string description, List<string> sessionNames, DateTime createdAt)
    {
        Name = name;
        Description = description;
        SessionNames = sessionNames;
        CreatedAt = createdAt.ToString("dd/MM/yyyy");
    }
}
