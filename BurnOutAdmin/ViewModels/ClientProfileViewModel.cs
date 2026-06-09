using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Models.Program;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.SessionLibrary;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

/// <summary>
/// Fiche profil d'un client : informations personnelles, abonnement, historique
/// des séances assignées (réalisées + à venir) avec détail au clic.
/// </summary>
public partial class ClientProfileViewModel : BaseViewModel
{
    private readonly INavigationService     _navigationService;
    private readonly IClientHistoryService  _historyService;
    private readonly ISessionLibraryService _sessionLibrary;
    private readonly IAlertService          _alertService;
    private readonly IClientService         _clientService;
    private readonly IClientStatsService    _statsService;
    private readonly IRankBadgeService      _badgeService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasClient))]
    [NotifyPropertyChangedFor(nameof(Initial))]
    private Client? _client;

    /// <summary>
    /// Totem actuellement affecté au client courant (mirror local pour binding XAML —
    /// la classe <see cref="Client"/> n'implémente pas <c>INotifyPropertyChanged</c>).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTotem))]
    private Totem? _selectedTotem;

    public bool HasSelectedTotem => SelectedTotem is not null;

    /// <summary>
    /// Points (PE) cumulés du client courant, lus dans <c>client_stats.points</c>.
    /// 0 par défaut tant que la stat n'est pas chargée ou si le client n'a
    /// pas encore de ligne stats.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentRankLabel))]
    private int _clientPoints;

    /// <summary>
    /// Rang (tier × division) calculé à partir des points. Mis à jour à
    /// chaque chargement de la fiche.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentRankLabel))]
    private ClientRank _currentRank = ClientRank.Default;

    /// <summary>Libellé court du rang à afficher sous le totem (ex. « Argent II »).</summary>
    public string CurrentRankLabel => CurrentRank.DisplayLabel;

    /// <summary>
    /// URL Cloudinary normalisée du badge composé du client courant
    /// (<c>tier × division × totem_slug</c>). <c>null</c> tant que la
    /// résolution n'a pas eu lieu ou si aucun badge n'est trouvé en base.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentBadge))]
    private string? _currentBadgeUrl;

    public bool HasCurrentBadge => !string.IsNullOrEmpty(CurrentBadgeUrl);

    partial void OnClientChanged(Client? value)
    {
        SelectedTotem = value?.Totem;
        // Reset visuel immédiat ; le pipeline async met à jour ensuite.
        ClientPoints    = 0;
        CurrentRank     = ClientRank.Default;
        CurrentBadgeUrl = null;
        _ = LoadRankBadgeAsync();
    }

    /// <summary>
    /// Recharge le badge du client courant : (1) lit les points → (2)
    /// calcule le rang → (3) résout l'URL via <see cref="IRankBadgeService"/>.
    /// Échec silencieux — la fiche reste utilisable même si la stat est
    /// indisponible.
    /// </summary>
    private async Task LoadRankBadgeAsync()
    {
        var client = Client;
        var totem  = SelectedTotem;
        if (client is null || client.Id <= 0 || totem is null)
        {
            CurrentBadgeUrl = null;
            return;
        }

        try
        {
            var points = await _statsService.GetPointsAsync(client.Id);
            var rank   = RankCalculator.ForPoints(points);
            var url    = await _badgeService.GetBadgeUrlAsync(rank, totem.Slug);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ClientPoints    = points;
                CurrentRank     = rank;
                CurrentBadgeUrl = url;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClientProfileVM] LoadRankBadge error: {ex.Message}");
        }
    }

    partial void OnSelectedTotemChanged(Totem? value) => _ = LoadRankBadgeAsync();

    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<ClientSeanceItem> Seances { get; } = new();

    /// <summary>Sous-ensemble affiché de <see cref="Seances"/> (filtre actif + pagination).</summary>
    public ObservableCollection<ClientSeanceItem> VisibleSeances { get; } = new();

    // ── Filtre + pagination de l'historique séances ───────────────
    private const int InitialVisibleCount = 10;
    private const int IncrementVisibleCount = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAllFilter))]
    [NotifyPropertyChangedFor(nameof(IsPendingFilter))]
    [NotifyPropertyChangedFor(nameof(IsDoneFilter))]
    [NotifyPropertyChangedFor(nameof(EmptyFilterTitle))]
    [NotifyPropertyChangedFor(nameof(EmptyFilterSubtitle))]
    private SeanceFilter _activeFilter = SeanceFilter.All;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLoadMore))]
    [NotifyPropertyChangedFor(nameof(LoadMoreLabel))]
    private int _visibleSeanceCount = InitialVisibleCount;

    public bool IsAllFilter     => ActiveFilter == SeanceFilter.All;
    public bool IsPendingFilter => ActiveFilter == SeanceFilter.Pending;
    public bool IsDoneFilter    => ActiveFilter == SeanceFilter.Done;

    public int TotalCount   => Seances.Count;
    public int PendingCount => Seances.Count(s => !s.IsCompleted);
    public int DoneCount    => Seances.Count(s => s.IsCompleted);
    public int FilteredCount => GetFilteredQuery().Count();
    public bool CanLoadMore => FilteredCount > VisibleSeanceCount;
    public string LoadMoreLabel
    {
        get
        {
            var remaining = FilteredCount - VisibleSeanceCount;
            if (remaining <= 0) return string.Empty;
            return remaining == 1
                ? "Voir 1 séance de plus"
                : $"Voir {Math.Min(remaining, IncrementVisibleCount)} séances de plus";
        }
    }

    public string EmptyFilterTitle => ActiveFilter switch
    {
        SeanceFilter.Pending => "Aucune séance à venir",
        SeanceFilter.Done    => "Aucune séance réalisée",
        _                    => "Aucune séance assignée"
    };
    public string EmptyFilterSubtitle => ActiveFilter switch
    {
        SeanceFilter.Pending => "Toutes les séances assignées ont déjà été marquées comme réalisées.",
        SeanceFilter.Done    => "Le client n'a encore réalisé aucune des séances assignées.",
        _                    => "Assignez un programme à ce client depuis la page Programmes."
    };

    // ── Sous-modal détail séance ──────────────────────────────────
    [ObservableProperty] private bool _isSessionDetailOpen;
    [ObservableProperty] private bool _isLoadingSessionDetail;
    [ObservableProperty] private ClientSeanceItem? _viewingSeance;
    [ObservableProperty] private SessionModel? _viewingSessionModel;
    [ObservableProperty] private ClientSessionFeedback? _viewingFeedback;
    [ObservableProperty] private bool _isRpeDetailsExpanded;

    public ObservableCollection<ClientPerformanceItem> Performances { get; } = new();

    public bool HasFeedback => ViewingFeedback is not null;
    public bool HasNoFeedback => !IsLoadingSessionDetail
                              && ViewingSeance?.IsCompleted == true
                              && ViewingFeedback is null;
    public bool HasAssignationNote => !string.IsNullOrWhiteSpace(ViewingSeance?.Commentaire);

    partial void OnViewingFeedbackChanged(ClientSessionFeedback? value)
    {
        OnPropertyChanged(nameof(HasFeedback));
        OnPropertyChanged(nameof(HasNoFeedback));
    }
    partial void OnIsLoadingSessionDetailChanged(bool value) => OnPropertyChanged(nameof(HasNoFeedback));
    partial void OnViewingSeanceChanged(ClientSeanceItem? value)
    {
        OnPropertyChanged(nameof(HasAssignationNote));
        OnPropertyChanged(nameof(HasNoFeedback));
    }

    public bool HasClient => Client is not null;
    public string Initial => Client is null
        ? "?"
        : (Client.FirstName.Length > 0 ? Client.FirstName[..1].ToUpperInvariant() : "?");

    public bool HasSeances     => Seances.Count > 0;
    public bool HasPerformances => Performances.Count > 0;

    /// <summary>Affiche le bandeau de filtres uniquement quand la liste est chargée et non vide.</summary>
    public bool ShouldShowFilters => !IsLoading && Seances.Count > 0;

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(ShouldShowFilters));

    public ClientProfileViewModel(
        INavigationService     navigationService,
        IClientHistoryService  historyService,
        ISessionLibraryService sessionLibrary,
        IAlertService          alertService,
        IClientService         clientService,
        IClientStatsService    statsService,
        IRankBadgeService      badgeService)
    {
        _navigationService = navigationService;
        _historyService    = historyService;
        _sessionLibrary    = sessionLibrary;
        _alertService      = alertService;
        _clientService     = clientService;
        _statsService      = statsService;
        _badgeService      = badgeService;
        Title = "Fiche client";
    }

    // ── Totem ─────────────────────────────────────────────────────
    [ObservableProperty] private bool _isTotemPickerOpen;
    [ObservableProperty] private bool _isSavingTotem;

    /// <summary>Catalogue des totems disponibles pour le picker (aligné avec
    /// l'app mobile, cf. <see cref="Models.Totem.All"/>).</summary>
    public IReadOnlyList<Totem> AvailableTotems => Totem.All;

    /// <summary>Charge la fiche pour le client demandé.</summary>
    public void SetClient(Client client)
    {
        Client = client;
        Seances.Clear();
        _ = LoadSeancesAsync();
    }

    private async Task LoadSeancesAsync()
    {
        if (Client is null) return;

        try
        {
            IsLoading = true;
            var list = await _historyService.GetClientSeancesAsync(Client.Id);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Seances.Clear();
                foreach (var s in list) Seances.Add(s);
                VisibleSeanceCount = InitialVisibleCount;
                RebuildVisibleSeances();
                OnPropertyChanged(nameof(HasSeances));
                OnPropertyChanged(nameof(ShouldShowFilters));
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(DoneCount));
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClientProfileVM] LoadSeances error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Filtre + pagination ───────────────────────────────────────

    private IEnumerable<ClientSeanceItem> GetFilteredQuery() => ActiveFilter switch
    {
        SeanceFilter.Pending => Seances.Where(s => !s.IsCompleted),
        SeanceFilter.Done    => Seances.Where(s => s.IsCompleted),
        _                    => Seances
    };

    private void RebuildVisibleSeances()
    {
        VisibleSeances.Clear();
        foreach (var s in GetFilteredQuery().Take(VisibleSeanceCount))
            VisibleSeances.Add(s);
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(CanLoadMore));
        OnPropertyChanged(nameof(LoadMoreLabel));
    }

    [RelayCommand]
    private void SetFilter(string? filter)
    {
        var next = filter switch
        {
            "pending" => SeanceFilter.Pending,
            "done"    => SeanceFilter.Done,
            _         => SeanceFilter.All
        };
        if (next == ActiveFilter) return;
        ActiveFilter        = next;
        VisibleSeanceCount  = InitialVisibleCount;
        RebuildVisibleSeances();
    }

    [RelayCommand]
    private void LoadMoreSeances()
    {
        if (!CanLoadMore) return;
        VisibleSeanceCount += IncrementVisibleCount;
        RebuildVisibleSeances();
    }

    // ── Commandes ─────────────────────────────────────────────────

    [RelayCommand]
    private void BackToClients() => _navigationService.NavigateTo("Clients");

    [RelayCommand]
    private async Task OpenSessionDetailAsync(ClientSeanceItem? item)
    {
        if (item is null || Client is null) return;

        ViewingSeance         = item;
        ViewingSessionModel   = null;
        ViewingFeedback       = null;
        IsRpeDetailsExpanded  = false;
        Performances.Clear();
        IsLoadingSessionDetail = true;
        IsSessionDetailOpen    = true;
        OnPropertyChanged(nameof(HasPerformances));

        try
        {
            var loadModel    = _sessionLibrary.LoadSessionModelAsync(item.IdSeanceBuilder);
            var loadPerfs    = _historyService.GetPerformancesForSessionAsync(Client.Id, item.IdSeanceBuilder);
            // Feedback uniquement si la séance a été marquée comme réalisée.
            var loadFeedback = item.DateRealisation is { } d
                ? _historyService.GetSessionFeedbackAsync(Client.Id, d, item.IdAssignation)
                : Task.FromResult<ClientSessionFeedback?>(null);
            await Task.WhenAll(loadModel, loadPerfs, loadFeedback);

            ViewingSessionModel = await loadModel;
            ViewingFeedback     = await loadFeedback;
            foreach (var p in await loadPerfs)
                Performances.Add(p);
            OnPropertyChanged(nameof(HasPerformances));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClientProfileVM] OpenSessionDetail error: {ex.Message}");
        }
        finally
        {
            IsLoadingSessionDetail = false;
        }
    }

    [RelayCommand]
    private void CloseSessionDetail()
    {
        IsSessionDetailOpen   = false;
        ViewingSeance         = null;
        ViewingSessionModel   = null;
        ViewingFeedback       = null;
        IsRpeDetailsExpanded  = false;
        Performances.Clear();
    }

    /// <summary>Bascule l'affichage du détail complet du RPE (3 catégories + commentaires).</summary>
    [RelayCommand]
    private void ToggleRpeDetails() => IsRpeDetailsExpanded = !IsRpeDetailsExpanded;

    // ── Sélection du totem ────────────────────────────────────────

    [RelayCommand]
    private void OpenTotemPicker()
    {
        if (Client is null) return;
        IsTotemPickerOpen = true;
    }

    [RelayCommand]
    private void CloseTotemPicker() => IsTotemPickerOpen = false;

    /// <summary>
    /// Sélectionne un totem pour le client courant. Met à jour la valeur localement
    /// (refresh immédiat de l'UI) puis persiste via PATCH /clients. Revient à l'état
    /// précédent si l'appel API échoue.
    /// </summary>
    [RelayCommand]
    private async Task SelectTotemAsync(Totem? totem)
    {
        if (Client is null || totem is null || IsSavingTotem) return;
        if (Client.TotemRang == totem.Rank)
        {
            IsTotemPickerOpen = false;
            return;
        }

        var previous = SelectedTotem;
        try
        {
            IsSavingTotem = true;
            // Refresh optimiste — VM mirror puis Client.
            SelectedTotem      = totem;
            Client.TotemRang   = totem.Rank;

            var ok = await _clientService.UpdateClientTotemAsync(Client.Id, totem.Rank);
            if (!ok)
            {
                SelectedTotem    = previous;
                Client.TotemRang = previous?.Rank;
                await _alertService.AlertAsync("Erreur",
                    "Impossible d'enregistrer le totem. Réessayez plus tard.");
                return;
            }
            IsTotemPickerOpen = false;
        }
        catch (Exception ex)
        {
            SelectedTotem    = previous;
            Client.TotemRang = previous?.Rank;
            Console.WriteLine($"[ClientProfileVM] SelectTotem error: {ex.Message}");
        }
        finally
        {
            IsSavingTotem = false;
        }
    }
}

/// <summary>Filtre de la liste de séances dans la fiche profil client.</summary>
public enum SeanceFilter
{
    All,
    Pending, // à venir / à faire
    Done     // réalisées
}
