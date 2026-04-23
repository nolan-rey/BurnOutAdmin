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
    public ObservableCollection<ChallengeCardViewModel>         FilteredChallenges { get; } = new();
    public ObservableCollection<ChallengeParticipantRowViewModel> Participants      { get; } = new();

    // ── Selected challenge ────────────────────────────────────────
    [ObservableProperty] private ChallengeCardViewModel? _selectedCard;

    // ── Filters ───────────────────────────────────────────────────
    [ObservableProperty] private string _activeFilter = "Tous";
    [ObservableProperty] private string _searchText   = string.Empty;

    public bool IsFilterAll       => ActiveFilter == "Tous";
    public bool IsFilterActive    => ActiveFilter == "Actifs";
    public bool IsFilterUpcoming  => ActiveFilter == "À venir";
    public bool IsFilterCompleted => ActiveFilter == "Terminés";

    // ── Empty states ─────────────────────────────────────────────
    public bool IsListEmpty   => FilteredChallenges.Count == 0 && !IsBusy;
    public bool IsDetailEmpty => SelectedCard == null;

    // ── Create / Edit modal ───────────────────────────────────────
    [ObservableProperty] private bool   _isEditModalOpen;
    [ObservableProperty] private bool   _isEditMode;
    [ObservableProperty] private string _modalName        = string.Empty;
    [ObservableProperty] private string _modalDescription = string.Empty;
    [ObservableProperty] private int    _modalTypeIndex;
    [ObservableProperty] private int    _modalStatusIndex;
    [ObservableProperty] private DateTime _modalStartDate = DateTime.Today;
    [ObservableProperty] private DateTime _modalEndDate   = DateTime.Today.AddDays(30);
    [ObservableProperty] private string _modalTargetGoal  = "30";
    [ObservableProperty] private string _modalGoalUnit    = string.Empty;
    [ObservableProperty] private string _modalReward      = string.Empty;

    public string EditModalTitle => IsEditMode ? "Modifier le challenge" : "Nouveau challenge";

    public List<string> ChallengeTypeItems { get; } = new()
        { "Général", "Cardio", "Force", "Endurance", "Poids", "Flexibilité" };

    public List<string> ChallengeStatusItems { get; } = new()
        { "À venir", "En cours", "Terminé", "Annulé" };

    private Challenge? _editingChallenge;

    // ── Assign participants modal ─────────────────────────────────
    [ObservableProperty] private bool _isAssignModalOpen;
    public ObservableCollection<ClientSelectionViewModel> SelectableClients { get; } = new();
    public bool HasSelectableClients => SelectableClients.Any(c => c.IsSelected);

    // ── Progress update modal ─────────────────────────────────────
    [ObservableProperty] private bool   _isProgressModalOpen;
    [ObservableProperty] private string _newProgressValue = string.Empty;

    private ChallengeParticipantRowViewModel? _participantToUpdate;
    public string ProgressModalTitle  => _participantToUpdate != null
        ? $"Progression — {_participantToUpdate.ClientName}"
        : string.Empty;
    public string ProgressModalSubtitle => _participantToUpdate != null
        ? $"Objectif : {_participantToUpdate.ProgressText}"
        : string.Empty;

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

    // ── Load ──────────────────────────────────────────────────────

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
            var participants = await _challengeService.GetParticipantsAsync(c.Id);
            _allCards.Add(new ChallengeCardViewModel(
                c, participants.Count,
                OnSelectCard, OnEditCard, OnDeleteCard));
        }

        ApplyFilter();
    }

    // ── Filters ───────────────────────────────────────────────────

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
            "Actifs"    => filtered.Where(c => c.Challenge.Status == ChallengeStatus.Active),
            "À venir"   => filtered.Where(c => c.Challenge.Status == ChallengeStatus.Upcoming),
            "Terminés"  => filtered.Where(c => c.Challenge.Status == ChallengeStatus.Completed),
            _           => filtered
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

    // ── Selection ─────────────────────────────────────────────────

    private async void OnSelectCard(ChallengeCardViewModel card)
    {
        // Toggle : deselect si déjà sélectionné
        if (SelectedCard == card)
        {
            card.IsSelected = false;
            SelectedCard = null;
            Participants.Clear();
            OnPropertyChanged(nameof(IsDetailEmpty));
            return;
        }

        // Deselect previous
        if (SelectedCard != null)
            SelectedCard.IsSelected = false;

        card.IsSelected = true;
        SelectedCard = card;
        OnPropertyChanged(nameof(IsDetailEmpty));

        await LoadParticipantsAsync(card.Challenge.Id);
    }

    private async Task LoadParticipantsAsync(int challengeId)
    {
        var card = _allCards.FirstOrDefault(c => c.Challenge.Id == challengeId);
        if (card == null) return;

        var list = await _challengeService.GetParticipantsAsync(challengeId);
        Participants.Clear();
        foreach (var p in list)
        {
            Participants.Add(new ChallengeParticipantRowViewModel(
                p,
                card.Challenge.TargetGoal,
                card.Challenge.GoalUnit,
                OnUpdateProgress,
                OnRemoveParticipant));
        }
    }

    // ── Create / Edit ─────────────────────────────────────────────

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
        OnPropertyChanged(nameof(IsDetailEmpty));
        await RefreshChallengesAsync();
    }

    // ── Delete ────────────────────────────────────────────────────

    private async void OnDeleteCard(ChallengeCardViewModel card)
    {
        await _challengeService.DeleteChallengeAsync(card.Challenge.Id);
        _allCards.Remove(card);

        if (SelectedCard == card)
        {
            SelectedCard = null;
            Participants.Clear();
            OnPropertyChanged(nameof(IsDetailEmpty));
        }

        ApplyFilter();
    }

    // ── Assign participants ────────────────────────────────────────

    [RelayCommand]
    private async Task OpenAssignAsync()
    {
        if (SelectedCard == null) return;
        try
        {
            var clients  = await _clientService.GetClientsAsync();
            var existing = await _challengeService.GetParticipantsAsync(SelectedCard.Challenge.Id);
            var existingIds = existing.Select(p => p.ClientId).ToHashSet();

            SelectableClients.Clear();
            foreach (var client in clients.Where(c => c.Status == "Actif" && !existingIds.Contains(c.Id)))
            {
                var vm = new ClientSelectionViewModel(client);
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ClientSelectionViewModel.IsSelected))
                        OnPropertyChanged(nameof(HasSelectableClients));
                };
                SelectableClients.Add(vm);
            }

            OnPropertyChanged(nameof(HasSelectableClients));
            IsAssignModalOpen = true;
        }
        catch
        {
            await _alertService.AlertAsync("Erreur", "Impossible de charger la liste des clients.");
        }
    }

    [RelayCommand]
    private void CancelAssign() => IsAssignModalOpen = false;

    [RelayCommand]
    private async Task ConfirmAssignAsync()
    {
        if (SelectedCard == null) return;

        var selected = SelectableClients.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            await _alertService.AlertAsync("Attention", "Sélectionnez au moins un client.");
            return;
        }

        foreach (var clientVm in selected)
            await _challengeService.AddParticipantAsync(
                SelectedCard.Challenge.Id,
                clientVm.Client.Id,
                clientVm.FullName);

        IsAssignModalOpen = false;
        await LoadParticipantsAsync(SelectedCard.Challenge.Id);
        SelectedCard.ParticipantCount = Participants.Count;
    }

    // ── Update progress ────────────────────────────────────────────

    private void OnUpdateProgress(ChallengeParticipantRowViewModel row)
    {
        _participantToUpdate = row;
        NewProgressValue     = row.CurrentValue.ToString("G");
        OnPropertyChanged(nameof(ProgressModalTitle));
        OnPropertyChanged(nameof(ProgressModalSubtitle));
        IsProgressModalOpen = true;
    }

    [RelayCommand]
    private void CancelProgress() => IsProgressModalOpen = false;

    [RelayCommand]
    private async Task ConfirmProgressAsync()
    {
        if (_participantToUpdate == null) return;

        if (!double.TryParse(NewProgressValue.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double val) || val < 0)
        {
            await _alertService.AlertAsync("Attention", "Entrez une valeur numérique valide (≥ 0).");
            return;
        }

        await _challengeService.UpdateProgressAsync(_participantToUpdate.Id, val);
        IsProgressModalOpen = false;

        if (SelectedCard != null)
            await LoadParticipantsAsync(SelectedCard.Challenge.Id);
    }

    // ── Remove participant ─────────────────────────────────────────

    private async void OnRemoveParticipant(ChallengeParticipantRowViewModel row)
    {
        await _challengeService.RemoveParticipantAsync(row.Id);
        Participants.Remove(row);

        if (SelectedCard != null)
            SelectedCard.ParticipantCount = Participants.Count;
    }
}
