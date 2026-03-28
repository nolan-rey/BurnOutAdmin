using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Nfc;
using BurnOutAdmin.Services.Rfid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class ClientsViewModel : BaseViewModel
{
    private readonly IClientService _clientService;
    private readonly IAlertService _alertService;
    private readonly INfcOrchestrator _orchestrator;
    private readonly IRfidReaderService _rfidReader;
    private readonly IProgramAssignmentService _assignmentService;

    // Source complète (non filtrée)
    private List<Client> _allClients = new();

    // --- Collections / états ---

    [ObservableProperty]
    private ObservableCollection<Client> _filteredClients = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedClient))]
    private Client? _selectedClient;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedStatus = "Tous";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private bool _isEditMode;

    // --- Champs formulaire ---

    [ObservableProperty]
    private int? _formId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _formFirstName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _formLastName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _formEmail = string.Empty;

    [ObservableProperty]
    private string? _formNfcUid;

    [ObservableProperty]
    private string _formStatus = "Actif";

    [ObservableProperty]
    private string _formSubscriptionType = "Mensuel";

    [ObservableProperty]
    private DateTime _formStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _formEndDate = DateTime.Today.AddMonths(1);

    [ObservableProperty]
    private bool _formAutoRenewal;

    // --- Scan NFC dans formulaire ---

    [ObservableProperty]
    private bool _isWaitingForRfidScan;

    // --- Programme Assignments Popup ---

    [ObservableProperty]
    private bool _isClientProgramsPanelOpen;

    [ObservableProperty]
    private Client? _viewingProgramsClient;

    public ObservableCollection<ClientProgramAssignment> ClientPrograms { get; } = new();


    // --- Bind NFC Mode ---

    [ObservableProperty]
    private bool _isBindMode;

    [ObservableProperty]
    private int? _bindTargetClientId;

    [ObservableProperty]
    private string _bindStatusText = string.Empty;

    // --- Listes statiques pour les Pickers ---

    public List<string> StatusOptions { get; } = new() { "Tous", "Actif", "Expiré", "En attente", "Suspendu" };
    public List<string> FormStatusOptions { get; } = new() { "Actif", "Expiré", "En attente", "Suspendu" };
    public List<string> SubscriptionTypeOptions { get; } = new() { "Mensuel", "Trimestriel", "Annuel" };

    // --- Propriétés calculées ---

    public bool HasSelectedClient => SelectedClient is not null;

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(FormFirstName) &&
        !string.IsNullOrWhiteSpace(FormLastName) &&
        IsValidEmail(FormEmail) &&
        !IsBusy;

    // --- Constructeur ---

    public ClientsViewModel(
        IClientService clientService,
        IAlertService alertService,
        INfcOrchestrator orchestrator,
        IRfidReaderService rfidReader,
        IProgramAssignmentService assignmentService)
    {
        _clientService = clientService;
        _alertService = alertService;
        _orchestrator = orchestrator;
        _rfidReader = rfidReader;
        _assignmentService = assignmentService;
        Title = "Liste des Clients";

        LoadClientsCommand.ExecuteAsync(null);
    }

    // --- Commandes ---

    [RelayCommand]
    private async Task ShowClientProgramsAsync(Client? client)
    {
        if (client is null) return;

        ViewingProgramsClient = client;
        ClientPrograms.Clear();

        var assignments = await _assignmentService.GetAssignmentsForClientAsync(client.Id);
        foreach (var a in assignments)
            ClientPrograms.Add(a);

        IsClientProgramsPanelOpen = true;
    }

    [RelayCommand]
    private void CloseClientPrograms()
    {
        IsClientProgramsPanelOpen = false;
        ViewingProgramsClient = null;
    }

    [RelayCommand]
    private async Task LoadClientsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var clients = await _clientService.GetClientsAsync();
            _allClients = clients;
            ApplyFilters();

            if (FilteredClients.Any() && SelectedClient is null)
                SelectedClient = FilteredClients.First();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Impossible de charger les clients : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectClient(Client? client)
    {
        if (client is null) return;
        SelectedClient = client;
        // Fermer le formulaire si on sélectionne un autre client
        if (IsFormOpen)
            IsFormOpen = false;
    }

    [RelayCommand]
    private void OpenCreateForm()
    {
        IsEditMode = false;
        FormId = null;
        FormFirstName = string.Empty;
        FormLastName = string.Empty;
        FormEmail = string.Empty;
        FormNfcUid = null;
        FormStatus = "Actif";
        FormSubscriptionType = "Mensuel";
        FormStartDate = DateTime.Today;
        FormEndDate = DateTime.Today.AddMonths(1);
        FormAutoRenewal = false;
        ErrorMessage = null;
        IsFormOpen = true;
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private void OpenEditForm(Client? client)
    {
        var target = client ?? SelectedClient;
        if (target is null) return;

        IsEditMode = true;
        FormId = target.Id;
        FormFirstName = target.FirstName;
        FormLastName = target.LastName;
        FormEmail = target.Email;
        FormNfcUid = target.NfcUid;
        FormStatus = target.Status;
        FormSubscriptionType = target.Subscription?.Type ?? "Mensuel";
        FormStartDate = target.Subscription?.StartDate ?? DateTime.Today;
        FormEndDate = target.Subscription?.EndDate ?? DateTime.Today.AddMonths(1);
        FormAutoRenewal = target.Subscription?.AutoRenewal ?? false;
        ErrorMessage = null;
        IsFormOpen = true;
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private void CancelForm()
    {
        // Arrêter un éventuel scan NFC en cours dans le formulaire
        if (IsWaitingForRfidScan)
            CancelScanForClient();

        IsFormOpen = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(FormFirstName) || string.IsNullOrWhiteSpace(FormLastName))
        {
            ErrorMessage = "Le prénom et le nom sont obligatoires.";
            return;
        }

        if (!IsValidEmail(FormEmail))
        {
            ErrorMessage = "L'email doit être valide (contenir @ et un point).";
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var clientData = new Client
            {
                Id = FormId ?? 0,
                FirstName = FormFirstName.Trim(),
                LastName = FormLastName.Trim(),
                Email = FormEmail.Trim(),
                Status = FormStatus,
                NfcUid = string.IsNullOrWhiteSpace(FormNfcUid) ? null : FormNfcUid.Trim(),
                Subscription = new Subscription
                {
                    Type = FormSubscriptionType,
                    StartDate = FormStartDate,
                    EndDate = FormEndDate,
                    AutoRenewal = FormAutoRenewal
                }
            };

            bool success;
            if (IsEditMode)
            {
                success = await _clientService.UpdateClientAsync(clientData);
            }
            else
            {
                success = await _clientService.AddClientAsync(clientData);
            }

            if (success)
            {
                // Recharger la liste depuis le service
                var clients = await _clientService.GetClientsAsync();
                _allClients = clients;
                ApplyFilters();

                // Sélectionner le client créé/modifié
                var savedClient = FilteredClients.FirstOrDefault(c =>
                    IsEditMode ? c.Id == FormId : c.Email == clientData.Email);
                if (savedClient is not null)
                    SelectedClient = savedClient;

                IsFormOpen = false;
            }
            else
            {
                ErrorMessage = "Échec de la sauvegarde. Veuillez réessayer.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur lors de la sauvegarde : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteClientAsync(Client? client)
    {
        var target = client ?? SelectedClient;
        if (target is null) return;

        var confirmed = await _alertService.ConfirmAsync(
            "Confirmer la suppression",
            $"Voulez-vous vraiment supprimer {target.FirstName} {target.LastName} ?");

        if (!confirmed) return;

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var success = await _clientService.DeleteClientAsync(target.Id);
            if (success)
            {
                _allClients.RemoveAll(c => c.Id == target.Id);
                ApplyFilters();

                if (SelectedClient?.Id == target.Id)
                    SelectedClient = FilteredClients.FirstOrDefault();
            }
            else
            {
                ErrorMessage = "Échec de la suppression.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur lors de la suppression : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Scan NFC dans formulaire Client ---

    /// <summary>Active le mode Capture pour remplir le champ FormNfcUid par scan.</summary>
    [RelayCommand]
    private async Task StartScanForClientAsync()
    {
        try
        {
            // S'assurer que l'orchestrateur est démarré
            if (!_orchestrator.IsReaderConnected)
                await _orchestrator.StartAsync();

            _orchestrator.UidScanned += OnUidScanned;
            _orchestrator.StartCaptureMode();
            IsWaitingForRfidScan = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClientsVM] Erreur StartScanForClient: {ex.Message}");
            IsWaitingForRfidScan = false;
        }
    }

    /// <summary>Annule le scan NFC en cours dans le formulaire.</summary>
    [RelayCommand]
    private void CancelScanForClient()
    {
        _orchestrator.UidScanned -= OnUidScanned;
        _orchestrator.StopCaptureMode();
        IsWaitingForRfidScan = false;
    }

    /// <summary>Callback déclenché par l'orchestrateur quand un UID est capturé en mode formulaire.</summary>
    private void OnUidScanned(object? sender, string uid)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FormNfcUid = uid;
            CancelScanForClient();
            System.Diagnostics.Debug.WriteLine($"[ClientsVM] UID capturé dans formulaire: {uid}");
        });
    }

    // --- Bind NFC Commands ---

    /// <summary>Active le mode Bind pour associer le prochain badge scanné au client sélectionné.</summary>
    [RelayCommand]
    private async Task StartBindAsync(Client? client)
    {
        var target = client ?? SelectedClient;
        if (target is null) return;

        try
        {
            // S'assurer que l'orchestrateur est démarré
            if (!_orchestrator.IsReaderConnected)
                await _orchestrator.StartAsync();

            _orchestrator.LogUpdated += OnBindLogUpdated;
            _orchestrator.StartBindMode(target.Id, target.FullName);

            BindTargetClientId = target.Id;
            IsBindMode = true;
            BindStatusText = $"En attente du badge pour {target.FullName}...";
        }
        catch (Exception ex)
        {
            BindStatusText = $"Erreur: {ex.Message}";
        }
    }

    /// <summary>Annule le mode Bind en cours.</summary>
    [RelayCommand]
    private void CancelBind()
    {
        _orchestrator.LogUpdated -= OnBindLogUpdated;
        _orchestrator.CancelBindMode();

        IsBindMode = false;
        BindTargetClientId = null;
        BindStatusText = string.Empty;
    }

    /// <summary>Callback déclenché par l'orchestrateur pendant le bind.</summary>
    private void OnBindLogUpdated(object? sender, NfcLog log)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var uid = log.Uid;
            var clientId = BindTargetClientId;

            if (log.Result == NfcAccessResult.Pending)
            {
                // Le scan a été déclenché — BADGE:<UID> a été publié sur MQTT
                BindStatusText = $"Badge {uid} scanné — En attente de la Raspberry...";

                // Associer immédiatement l'UID au client (sans attendre la réponse)
                if (clientId.HasValue && !string.IsNullOrWhiteSpace(uid))
                {
                    var clientToUpdate = _allClients.FirstOrDefault(c => c.Id == clientId.Value);
                    if (clientToUpdate is not null && clientToUpdate.NfcUid != uid)
                    {
                        clientToUpdate.NfcUid = uid;
                        await _clientService.UpdateClientAsync(clientToUpdate);

                        var clients = await _clientService.GetClientsAsync();
                        _allClients = clients;
                        ApplyFilters();
                    }
                }
                return;
            }

            // Résultat reçu de la Raspberry (Autorisé ou Refusé)
            var resultText = log.Result == NfcAccessResult.Authorized
                ? $"Badge {uid} — Accès autorisé !"
                : $"Badge {uid} — Accès refusé.";

            BindStatusText = resultText;

            // Désactiver le mode bind après un court délai pour que l'utilisateur voie le résultat
            await Task.Delay(1500);
            CancelBind();

            // Recharger la liste pour refléter les changements
            var updatedClients = await _clientService.GetClientsAsync();
            _allClients = updatedClients;
            ApplyFilters();
        });
    }

    /// <summary>Simule un scan RFID avec l'UID NFC du client ciblé (dev uniquement).</summary>
    [RelayCommand]
    private void SimulateRfidScan()
    {
        if (_rfidReader is not DummyRfidReaderService dummy)
            return;

        // Retrouver l'UID du client ciblé pour publier le bon BADGE:<UID>
        var clientId = BindTargetClientId;
        if (clientId.HasValue)
        {
            var client = _allClients.FirstOrDefault(c => c.Id == clientId.Value);
            if (client is not null && !string.IsNullOrWhiteSpace(client.NfcUid))
            {
                dummy.SimulateScan(client.NfcUid);
                return;
            }
        }

        // Fallback : UID générique si le client n'a pas encore d'UID
        dummy.SimulateScan("029EC135");
    }

    // --- Filtrage ---

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedStatusChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var query = _allClients.AsEnumerable();

        // Filtre par statut
        if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Tous")
        {
            query = query.Where(c =>
                string.Equals(c.Status, SelectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        // Filtre par recherche textuelle
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (c.NfcUid is not null && c.NfcUid.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var result = query.ToList();
        FilteredClients.Clear();
        foreach (var c in result)
            FilteredClients.Add(c);
    }

    // --- Helpers ---

    /// <summary>Notification IsBusy → rafraîchir CanSave.</summary>
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy))
            OnPropertyChanged(nameof(CanSave));
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return email.Contains('@') && email.Contains('.');
    }
}
