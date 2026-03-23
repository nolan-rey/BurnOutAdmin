using System.Collections.ObjectModel;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Nfc;
using BurnOutAdmin.Services.Rfid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

/// <summary>
/// ViewModel du journal NFC.
/// Gère l'affichage des logs, le live feed via l'orchestrateur,
/// les indicateurs de statut (Reader/MQTT), et la simulation de scan.
/// </summary>
public partial class NfcLogsViewModel : BaseViewModel
{
    private readonly INfcService _nfcService;
    private readonly INfcOrchestrator _orchestrator;
    private readonly IRfidReaderService _rfidReader;

    // --- Collections et sélection ---

    [ObservableProperty]
    private ObservableCollection<NfcLog> _nfcLogs = new();

    [ObservableProperty]
    private NfcLog? _selectedLog;

    // --- Filtres ---

    [ObservableProperty]
    private bool _showTodayOnly = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // --- Statuts live ---

    [ObservableProperty]
    private bool _isLiveActive;

    [ObservableProperty]
    private bool _isReaderConnected;

    [ObservableProperty]
    private bool _isMqttConnected;

    // --- Simulation (dev) ---

    [ObservableProperty]
    private string _simulateUid = "04:A3:5B:12";

    public NfcLogsViewModel(
        INfcService nfcService,
        INfcOrchestrator orchestrator,
        IRfidReaderService rfidReader)
    {
        _nfcService = nfcService;
        _orchestrator = orchestrator;
        _rfidReader = rfidReader;
        Title = "Journal NFC";

        LoadNfcLogsCommand.ExecuteAsync(null);
    }

    // --- Commandes ---

    [RelayCommand]
    private async Task LoadNfcLogsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var logs = ShowTodayOnly
                ? await _nfcService.GetTodayLogsAsync()
                : await _nfcService.GetNfcLogsAsync();

            ApplySearchFilter(logs);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NfcLogsVM] Erreur chargement: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Démarre le live feed : lance l'orchestrateur et écoute les nouveaux logs.</summary>
    [RelayCommand]
    private async Task StartLiveAsync()
    {
        if (IsLiveActive) return;

        try
        {
            await _orchestrator.StartAsync();
            _orchestrator.LogUpdated += OnLogUpdated;

            IsLiveActive = true;
            RefreshStatuses();

            System.Diagnostics.Debug.WriteLine("[NfcLogsVM] Live démarré.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NfcLogsVM] Erreur démarrage live: {ex.Message}");
        }
    }

    /// <summary>Arrête le live feed.</summary>
    [RelayCommand]
    private async Task StopLiveAsync()
    {
        if (!IsLiveActive) return;

        try
        {
            _orchestrator.LogUpdated -= OnLogUpdated;
            await _orchestrator.StopAsync();

            IsLiveActive = false;
            RefreshStatuses();

            System.Diagnostics.Debug.WriteLine("[NfcLogsVM] Live arrêté.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NfcLogsVM] Erreur arrêt live: {ex.Message}");
        }
    }

    /// <summary>Vide la liste affichée (ne supprime pas les données SQLite).</summary>
    [RelayCommand]
    private void ClearLogs()
    {
        NfcLogs.Clear();
    }

    [RelayCommand]
    private async Task ToggleFilterAsync()
    {
        ShowTodayOnly = !ShowTodayOnly;
        await LoadNfcLogsAsync();
    }

    [RelayCommand]
    private async Task RefreshLogsAsync()
    {
        RefreshStatuses();
        await LoadNfcLogsAsync();
    }

    /// <summary>Simule un scan RFID (dev uniquement, utilise le DummyRfidReaderService).</summary>
    [RelayCommand]
    private void SimulateScan()
    {
        if (string.IsNullOrWhiteSpace(SimulateUid)) return;

        if (_rfidReader is DummyRfidReaderService dummy)
        {
            dummy.SimulateScan(SimulateUid.Trim());
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[NfcLogsVM] Simulation impossible : lecteur réel connecté.");
        }
    }

    // --- Filtrage ---

    partial void OnSearchTextChanged(string value)
    {
        // Recharger avec le filtre
        LoadNfcLogsCommand.ExecuteAsync(null);
    }

    /// <summary>Applique le filtre de recherche textuelle et met à jour la collection affichée.</summary>
    private void ApplySearchFilter(List<NfcLog> logs)
    {
        IEnumerable<NfcLog> filtered = logs;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(l =>
                l.Uid.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                l.ClientName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (l.Reason?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                l.ResultText.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        NfcLogs.Clear();
        foreach (var log in filtered)
            NfcLogs.Add(log);
    }

    // --- Callbacks orchestrateur ---

    /// <summary>Callback déclenché par l'orchestrateur à chaque nouveau log ou mise à jour.</summary>
    private void OnLogUpdated(object? sender, NfcLog log)
    {
        // Dispatcher sur le thread UI
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Chercher si ce log existe déjà (mise à jour)
            var existing = NfcLogs.FirstOrDefault(l => l.EventId == log.EventId);
            if (existing is not null)
            {
                var index = NfcLogs.IndexOf(existing);
                NfcLogs[index] = log;
            }
            else
            {
                // Insérer en tête (plus récent en premier)
                NfcLogs.Insert(0, log);
            }

            RefreshStatuses();
        });
    }

    /// <summary>Met à jour les indicateurs de statut.</summary>
    private void RefreshStatuses()
    {
        IsReaderConnected = _orchestrator.IsReaderConnected;
        IsMqttConnected = _orchestrator.IsMqttConnected;
    }
}
