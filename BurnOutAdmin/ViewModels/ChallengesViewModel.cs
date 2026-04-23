using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.ViewModels.Challenges;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class ChallengesViewModel : BaseViewModel
{
    private readonly IChallengeService _challengeService;
    private readonly IClientService    _clientService;
    private readonly IAlertService     _alertService;

    // ── Collections ───────────────────────────────────────────────
    private readonly List<ChallengeCardViewModel> _allCards = new();
    public ObservableCollection<ChallengeCardViewModel>           FilteredChallenges { get; } = new();
    public ObservableCollection<ChallengeParticipantRowViewModel> Participants        { get; } = new();

    // ── Selected challenge ────────────────────────────────────────
    [ObservableProperty] private ChallengeCardViewModel? _selectedCard;

    // ── Filtres ───────────────────────────────────────────────────
    [ObservableProperty] private string _activeFilter = "Tous";
    [ObservableProperty] private string _searchText   = string.Empty;

    public bool IsFilterAll       => ActiveFilter == "Tous";
    public bool IsFilterActive    => ActiveFilter == "Actifs";
    public bool IsFilterUpcoming  => ActiveFilter == "À venir";
    public bool IsFilterCompleted => ActiveFilter == "Terminés";

    // ── États vides ───────────────────────────────────────────────
    public bool IsListEmpty   => FilteredChallenges.Count == 0 && !IsBusy;
    public bool IsDetailEmpty => SelectedCard == null;

    // ── Statistiques classement (panel droit) ────────────────────
    public int    ParticipantCount  => Participants.Count;
    public string LeaderName        => Participants.Count > 0 ? Participants[0].ClientName : "—";
    public int    CompletedCount    => Participants.Count(p => p.ProgressRatio >= 1.0);
    public double AveragePercent    =>
        Participants.Count > 0 ? Participants.Average(p => p.ProgressRatio) * 100 : 0;
    public string AveragePercentText => $"{(int)AveragePercent} %";

    // ── Créer / Modifier challenge ────────────────────────────────
    [ObservableProperty] private bool     _isEditModalOpen;
    [ObservableProperty] private bool     _isEditMode;
    [ObservableProperty] private string   _modalName        = string.Empty;
    [ObservableProperty] private string   _modalDescription = string.Empty;
    [ObservableProperty] private int      _modalTypeIndex;
    [ObservableProperty] private int      _modalStatusIndex;
    [ObservableProperty] private DateTime _modalStartDate   = DateTime.Today;
    [ObservableProperty] private DateTime _modalEndDate     = DateTime.Today.AddDays(30);
    [ObservableProperty] private string   _modalTargetGoal  = "30";
    [ObservableProperty] private string   _modalGoalUnit    = string.Empty;
    [ObservableProperty] private string   _modalReward      = string.Empty;

    public string EditModalTitle => IsEditMode ? "Modifier le challenge" : "Nouveau challenge";

    public List<string> ChallengeTypeItems { get; } = new()
        { "Général", "Cardio", "Force", "Endurance", "Poids", "Flexibilité" };

    public List<string> ChallengeStatusItems { get; } = new()
        { "À venir", "En cours", "Terminé", "Annulé" };

    private Challenge? _editingChallenge;

    // ── Modal : Enregistrer une performance ───────────────────────
    [ObservableProperty] private bool   _isRecordModalOpen;
    [ObservableProperty] private int    _recordClientIndex = -1;
    [ObservableProperty] private string _recordNewValue    = string.Empty;
    [ObservableProperty] private string _recordCurrentHint = string.Empty;
    [ObservableProperty] private bool   _recordIsUpdate;

    public string RecordGoalUnit   => SelectedCard?.Challenge.GoalUnit ?? string.Empty;
    public string RecordButtonText => RecordIsUpdate ? "Mettre à jour" : "Enregistrer";

    public List<string> RecordClientNames { get; private set; } = new();

    // Références internes pour la modale
    private List<Client>             _recordAllClients   = new();
    private List<ChallengeParticipant> _recordExisting   = new();

    // ─────────────────────────────────────────────────────────────

    public ChallengesViewModel(
        IChallengeService challengeService,
        IClientService    clientService,
        IAlertService     alertService)
    {
        _challengeService = challengeService;
        _clientService    = clientService;
        _alertService     = alertService;
        Title = "Challenges";
    }

    public override Task OnActivatedAsync() => LoadAsync();

    // ── Chargement ────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            await RefreshChallengesAsync();
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsListEmpty));
        }
    }

    private async Task RefreshChallengesAsync()
    {
        var challenges = await _challengeService.GetChallengesAsync();
        _allCards.Clear();

        foreach (var c in challenges)
        {
            var pts = await _challengeService.GetParticipantsAsync(c.Id);
            _allCards.Add(new ChallengeCardViewModel(
                c, pts.Count, OnSelectCard, OnEditCard, OnDeleteCard));
        }

        ApplyFilter();
    }

    // ── Filtres ───────────────────────────────────────────────────

    partial void OnActiveFilterChanged(string value)
    {
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterActive));
        OnPropertyChanged(nameof(IsFilterUpcoming));
        OnPropertyChanged(nameof(IsFilterCompleted));
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = _allCards.AsEnumerable();

        filtered = ActiveFilter switch
        {
            "Actifs"   => filtered.Where(c => c.Challenge.Status == ChallengeStatus.Active),
            "À venir"  => filtered.Where(c => c.Challenge.Status == ChallengeStatus.Upcoming),
            "Terminés" => filtered.Where(c => c.Challenge.Status == ChallengeStatus.Completed),
            _          => filtered
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(c =>
                c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        FilteredChallenges.Clear();
        foreach (var c in filtered)
            FilteredChallenges.Add(c);

        OnPropertyChanged(nameof(IsListEmpty));
    }

    [RelayCommand] private void SetFilterAll()       => ActiveFilter = "Tous";
    [RelayCommand] private void SetFilterActive()    => ActiveFilter = "Actifs";
    [RelayCommand] private void SetFilterUpcoming()  => ActiveFilter = "À venir";
    [RelayCommand] private void SetFilterCompleted() => ActiveFilter = "Terminés";

    // ── Sélection ─────────────────────────────────────────────────

    private async void OnSelectCard(ChallengeCardViewModel card)
    {
        if (SelectedCard == card)
        {
            card.IsSelected = false;
            SelectedCard = null;
            Participants.Clear();
            RefreshLeaderboardStats();
            OnPropertyChanged(nameof(IsDetailEmpty));
            return;
        }

        if (SelectedCard != null) SelectedCard.IsSelected = false;
        card.IsSelected = true;
        SelectedCard = card;
        OnPropertyChanged(nameof(IsDetailEmpty));
        await LoadLeaderboardAsync(card.Challenge.Id);
    }

    private async Task LoadLeaderboardAsync(int challengeId)
    {
        var card = _allCards.FirstOrDefault(c => c.Challenge.Id == challengeId);
        if (card == null) return;

        var list = await _challengeService.GetParticipantsAsync(challengeId);

        // Trier par score décroissant → classement
        var sorted = list.OrderByDescending(p => p.CurrentValue).ToList();

        Participants.Clear();
        for (var i = 0; i < sorted.Count; i++)
        {
            Participants.Add(new ChallengeParticipantRowViewModel(
                sorted[i],
                i + 1,                       // rang
                card.Challenge.TargetGoal,
                card.Challenge.GoalUnit,
                OnRemoveParticipant));
        }

        RefreshLeaderboardStats();
    }

    private void RefreshLeaderboardStats()
    {
        OnPropertyChanged(nameof(ParticipantCount));
        OnPropertyChanged(nameof(LeaderName));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(AveragePercent));
        OnPropertyChanged(nameof(AveragePercentText));
    }

    // ── Créer / Modifier challenge ────────────────────────────────

    [RelayCommand]
    private void OpenCreate()
    {
        _editingChallenge = null;
        IsEditMode        = false;
        ModalName         = string.Empty;
        ModalDescription  = string.Empty;
        ModalTypeIndex    = 0;
        ModalStatusIndex  = 0;
        ModalStartDate    = DateTime.Today;
        ModalEndDate      = DateTime.Today.AddDays(30);
        ModalTargetGoal   = "30";
        ModalGoalUnit     = string.Empty;
        ModalReward       = string.Empty;
        OnPropertyChanged(nameof(EditModalTitle));
        IsEditModalOpen = true;
    }

    private void OnEditCard(ChallengeCardViewModel card)
    {
        _editingChallenge = card.Challenge;
        IsEditMode        = true;
        ModalName         = card.Challenge.Name;
        ModalDescription  = card.Challenge.Description;
        ModalTypeIndex    = (int)card.Challenge.Type;
        ModalStatusIndex  = card.Challenge.Status switch
        {
            ChallengeStatus.Upcoming  => 0,
            ChallengeStatus.Active    => 1,
            ChallengeStatus.Completed => 2,
            ChallengeStatus.Cancelled => 3,
            _                         => 0
        };
        ModalStartDate  = card.Challenge.StartDate;
        ModalEndDate    = card.Challenge.EndDate;
        ModalTargetGoal = card.Challenge.TargetGoal.ToString();
        ModalGoalUnit   = card.Challenge.GoalUnit;
        ModalReward     = card.Challenge.RewardDescription;
        OnPropertyChanged(nameof(EditModalTitle));
        IsEditModalOpen = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingChallenge = null;
        IsEditModalOpen   = false;
    }

    [RelayCommand]
    private async Task ConfirmEditAsync()
    {
        if (string.IsNullOrWhiteSpace(ModalName))
        {
            await _alertService.AlertAsync("Attention", "Le nom du challenge est obligatoire.");
            return;
        }
        if (!int.TryParse(ModalTargetGoal, out int goal) || goal <= 0)
        {
            await _alertService.AlertAsync("Attention", "L'objectif doit être un nombre entier positif.");
            return;
        }
        if (ModalEndDate <= ModalStartDate)
        {
            await _alertService.AlertAsync("Attention", "La date de fin doit être postérieure à la date de début.");
            return;
        }

        var status = ModalStatusIndex switch
        {
            1 => ChallengeStatus.Active,
            2 => ChallengeStatus.Completed,
            3 => ChallengeStatus.Cancelled,
            _ => ChallengeStatus.Upcoming
        };

        if (IsEditMode && _editingChallenge != null)
        {
            _editingChallenge.Name              = ModalName.Trim();
            _editingChallenge.Description       = ModalDescription.Trim();
            _editingChallenge.Type              = (ChallengeType)ModalTypeIndex;
            _editingChallenge.Status            = status;
            _editingChallenge.StartDate         = ModalStartDate;
            _editingChallenge.EndDate           = ModalEndDate;
            _editingChallenge.TargetGoal        = goal;
            _editingChallenge.GoalUnit          = ModalGoalUnit.Trim();
            _editingChallenge.RewardDescription = ModalReward.Trim();
            await _challengeService.UpdateChallengeAsync(_editingChallenge);
        }
        else
        {
            var challenge = new Challenge
            {
                Name              = ModalName.Trim(),
                Description       = ModalDescription.Trim(),
                Type              = (ChallengeType)ModalTypeIndex,
                Status            = status,
                StartDate         = ModalStartDate,
                EndDate           = ModalEndDate,
                TargetGoal        = goal,
                GoalUnit          = ModalGoalUnit.Trim(),
                RewardDescription = ModalReward.Trim()
            };
            await _challengeService.CreateChallengeAsync(challenge);
        }

        IsEditModalOpen = false;
        SelectedCard    = null;
        Participants.Clear();
        RefreshLeaderboardStats();
        OnPropertyChanged(nameof(IsDetailEmpty));
        await RefreshChallengesAsync();
    }

    // ── Supprimer challenge ───────────────────────────────────────

    private async void OnDeleteCard(ChallengeCardViewModel card)
    {
        await _challengeService.DeleteChallengeAsync(card.Challenge.Id);
        _allCards.Remove(card);

        if (SelectedCard == card)
        {
            SelectedCard = null;
            Participants.Clear();
            RefreshLeaderboardStats();
            OnPropertyChanged(nameof(IsDetailEmpty));
        }

        ApplyFilter();
    }

    // ── Enregistrer une performance ───────────────────────────────

    [RelayCommand]
    private async Task OpenRecordAsync()
    {
        if (SelectedCard == null) return;
        try
        {
            var clients = await _clientService.GetClientsAsync();
            _recordAllClients = clients.Where(c => c.Status == "Actif")
                                       .OrderBy(c => c.LastName)
                                       .ThenBy(c => c.FirstName)
                                       .ToList();

            _recordExisting = await _challengeService.GetParticipantsAsync(SelectedCard.Challenge.Id);

            RecordClientNames = _recordAllClients
                .Select(c => $"{c.FirstName} {c.LastName}")
                .ToList();

            OnPropertyChanged(nameof(RecordClientNames));
            OnPropertyChanged(nameof(RecordGoalUnit));
            OnPropertyChanged(nameof(RecordButtonText));

            RecordClientIndex  = -1;
            RecordNewValue     = string.Empty;
            RecordCurrentHint  = string.Empty;
            RecordIsUpdate     = false;
            IsRecordModalOpen  = true;
        }
        catch
        {
            await _alertService.AlertAsync("Erreur", "Impossible de charger la liste des clients.");
        }
    }

    partial void OnRecordClientIndexChanged(int value)
    {
        if (value < 0 || value >= _recordAllClients.Count)
        {
            RecordCurrentHint = string.Empty;
            RecordNewValue    = string.Empty;
            RecordIsUpdate    = false;
            OnPropertyChanged(nameof(RecordButtonText));
            return;
        }

        var client   = _recordAllClients[value];
        var existing = _recordExisting.FirstOrDefault(p => p.ClientId == client.Id);

        RecordIsUpdate = existing != null;
        OnPropertyChanged(nameof(RecordButtonText));

        if (existing != null)
        {
            RecordCurrentHint = $"Valeur actuelle : {existing.CurrentValue} {RecordGoalUnit}";
            RecordNewValue    = existing.CurrentValue.ToString("G");
        }
        else
        {
            RecordCurrentHint = string.Empty;
            RecordNewValue    = string.Empty;
        }
    }

    [RelayCommand]
    private void CancelRecord() => IsRecordModalOpen = false;

    [RelayCommand]
    private async Task ConfirmRecordAsync()
    {
        if (SelectedCard == null) return;

        if (RecordClientIndex < 0 || RecordClientIndex >= _recordAllClients.Count)
        {
            await _alertService.AlertAsync("Attention", "Sélectionnez un athlète.");
            return;
        }

        if (!double.TryParse(RecordNewValue.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double val) || val < 0)
        {
            await _alertService.AlertAsync("Attention", "Entrez une valeur numérique valide (≥ 0).");
            return;
        }

        var client    = _recordAllClients[RecordClientIndex];
        var challengeId = SelectedCard.Challenge.Id;
        var fullName  = $"{client.FirstName} {client.LastName}";

        // Ajouter si nouveau, puis mettre à jour la valeur
        await _challengeService.AddParticipantAsync(challengeId, client.Id, fullName);

        // Récupérer l'entrée (nouvelle ou existante) et mettre à jour
        var participants = await _challengeService.GetParticipantsAsync(challengeId);
        var entry = participants.FirstOrDefault(p => p.ClientId == client.Id);
        if (entry != null)
            await _challengeService.UpdateProgressAsync(entry.Id, val);

        IsRecordModalOpen = false;
        await LoadLeaderboardAsync(challengeId);

        // Mettre à jour le compteur sur la carte
        SelectedCard.ParticipantCount = Participants.Count;
    }

    // ── Retirer un participant ────────────────────────────────────

    private async void OnRemoveParticipant(ChallengeParticipantRowViewModel row)
    {
        await _challengeService.RemoveParticipantAsync(row.Id);
        await LoadLeaderboardAsync(SelectedCard!.Challenge.Id);
        SelectedCard.ParticipantCount = Participants.Count;
    }
}
