using System.Collections.ObjectModel;
using System.Text.Json;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Mqtt;
using BurnOutAdmin.Services.Nfc;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

/// <summary>
/// ViewModel du journal NFC / flux MQTT live.
/// Écoute directement IMqttService sur le topic rfid/access —
/// aucun lecteur RFID local requis.
/// </summary>
public partial class NfcLogsViewModel : BaseViewModel
{
    private readonly INfcService  _nfcService;
    private readonly IMqttService _mqttService;

    private const string TopicRfidAccess = "rfid/access";

    // ── Collections ───────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<NfcLog> _nfcLogs      = new();
    [ObservableProperty] private ObservableCollection<NfcLog> _liveFeedLogs = new();
    [ObservableProperty] private NfcLog? _selectedLog;

    // ── Filtres historique ────────────────────────────────────────
    [ObservableProperty] private bool   _showTodayOnly = true;
    [ObservableProperty] private string _searchText    = string.Empty;

    // ── État connexion ────────────────────────────────────────────
    [ObservableProperty] private bool   _isLiveActive;
    [ObservableProperty] private bool   _isMqttConnected;
    [ObservableProperty] private bool   _isConnecting;
    [ObservableProperty] private string _connectionStatusText = "Déconnecté";

    // ── Stats session live ────────────────────────────────────────
    [ObservableProperty] private int _sessionTotal;
    [ObservableProperty] private int _sessionAuthorized;
    [ObservableProperty] private int _sessionDenied;

    public NfcLogsViewModel(INfcService nfcService, IMqttService mqttService)
    {
        _nfcService  = nfcService;
        _mqttService = mqttService;
        Title = "Journal NFC";

        LoadNfcLogsCommand.ExecuteAsync(null);
    }

    // ── Commandes ─────────────────────────────────────────────────

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
            Console.WriteLine($"[NfcLogsVM] Erreur chargement historique: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    /// <summary>Connecte au broker MQTT et démarre l'écoute live sur rfid/access.</summary>
    [RelayCommand]
    private async Task StartLiveAsync()
    {
        if (IsLiveActive || IsConnecting) return;

        try
        {
            IsConnecting = true;
            ConnectionStatusText = "Connexion…";

            await _mqttService.ConnectAsync();

            if (_mqttService.IsConnected)
            {
                await _mqttService.SubscribeAsync(TopicRfidAccess);
                _mqttService.MessageReceived += OnMqttMessageReceived;

                IsLiveActive     = true;
                IsMqttConnected  = true;
                ConnectionStatusText = $"Connecté — {AppConfiguration.MqttBrokerHost}:{AppConfiguration.MqttBrokerPort}";

                // Reset stats de session
                SessionTotal      = 0;
                SessionAuthorized = 0;
                SessionDenied     = 0;

                Console.WriteLine("[NfcLogsVM] Live MQTT démarré.");
            }
            else
            {
                ConnectionStatusText = "Connexion échouée";
                IsMqttConnected = false;
            }
        }
        catch (Exception ex)
        {
            ConnectionStatusText = $"Erreur : {ex.Message}";
            IsMqttConnected = false;
            Console.WriteLine($"[NfcLogsVM] Erreur démarrage live: {ex.Message}");
        }
        finally { IsConnecting = false; }
    }

    /// <summary>Arrête l'écoute live et déconnecte du broker.</summary>
    [RelayCommand]
    private async Task StopLiveAsync()
    {
        if (!IsLiveActive) return;

        try
        {
            _mqttService.MessageReceived -= OnMqttMessageReceived;
            await _mqttService.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NfcLogsVM] Erreur arrêt live: {ex.Message}");
        }
        finally
        {
            IsLiveActive         = false;
            IsMqttConnected      = false;
            ConnectionStatusText = "Déconnecté";
        }
    }

    [RelayCommand]
    private void ClearLiveFeed()
    {
        LiveFeedLogs.Clear();
        SessionTotal = SessionAuthorized = SessionDenied = 0;
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
        IsMqttConnected = _mqttService.IsConnected;
        await LoadNfcLogsAsync();
    }

    // ── Filtrage historique ───────────────────────────────────────

    partial void OnSearchTextChanged(string value) =>
        LoadNfcLogsCommand.ExecuteAsync(null);

    private void ApplySearchFilter(List<NfcLog> logs)
    {
        IEnumerable<NfcLog> filtered = logs;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            filtered = filtered.Where(l =>
                l.Uid.Contains(s, StringComparison.OrdinalIgnoreCase)         ||
                l.ClientName.Contains(s, StringComparison.OrdinalIgnoreCase)  ||
                (l.Reason?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                l.ResultText.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        NfcLogs.Clear();
        foreach (var log in filtered) NfcLogs.Add(log);
    }

    // ── Réception MQTT ────────────────────────────────────────────

    private void OnMqttMessageReceived(object? sender, MqttMessageEventArgs e)
    {
        if (!e.Topic.Equals(TopicRfidAccess, StringComparison.OrdinalIgnoreCase))
            return;

        var log = ParseMqttPayload(e.Payload);
        if (log is null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Insérer en tête (plus récent en premier)
            LiveFeedLogs.Insert(0, log);

            // Limiter le feed à 200 entrées en mémoire
            while (LiveFeedLogs.Count > 200)
                LiveFeedLogs.RemoveAt(LiveFeedLogs.Count - 1);

            // Stats
            SessionTotal++;
            if (log.Result == NfcAccessResult.Authorized) SessionAuthorized++;
            else if (log.Result == NfcAccessResult.Denied) SessionDenied++;
        });
    }

    /// <summary>
    /// Parse un message MQTT du topic rfid/access.
    /// Gère 3 formats :
    ///   1. JSON  : {"uid":"...","access":"granted/denied","nom_client":"...","raison":"..."}
    ///   2. Texte : "BADGE:04:A3:5B:12"
    ///   3. Texte : "Accès autorisé" / "Accès refusé"
    /// </summary>
    private static NfcLog? ParseMqttPayload(string payload)
    {
        payload = payload.Trim();
        if (string.IsNullOrEmpty(payload)) return null;

        // ── 1. Tentative JSON ──────────────────────────────────────
        if (payload.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                var uid    = GetJsonStr(root, "uid", "uid_nfc", "badge_uid", "card_uid");
                var access = GetJsonStr(root, "access", "resultat", "result", "status", "acces");
                var client = GetJsonStr(root, "nom_client", "client", "client_name", "name");
                var reason = GetJsonStr(root, "raison", "reason", "message", "details");

                return new NfcLog
                {
                    EventId      = Guid.NewGuid().ToString("N"),
                    Uid          = uid ?? "—",
                    ClientName   = client ?? string.Empty,
                    Result       = ParseAccessResult(access),
                    Reason       = reason ?? access ?? payload,
                    TimestampUtc = DateTime.UtcNow,
                    Source       = "mqtt"
                };
            }
            catch { /* pas du JSON valide, on continue */ }
        }

        // ── 2. Format BADGE:<UID> ──────────────────────────────────
        if (payload.StartsWith("BADGE:", StringComparison.OrdinalIgnoreCase))
        {
            var uid = payload[6..].Trim();
            return new NfcLog
            {
                EventId      = Guid.NewGuid().ToString("N"),
                Uid          = uid,
                ClientName   = string.Empty,
                Result       = NfcAccessResult.Pending,
                Reason       = "Scan reçu",
                TimestampUtc = DateTime.UtcNow,
                Source       = "mqtt"
            };
        }

        // ── 3. Texte résultat de la Raspberry Pi ──────────────────
        var isGranted = payload.Contains("autoris", StringComparison.OrdinalIgnoreCase);
        var isDenied  = payload.Contains("refus",   StringComparison.OrdinalIgnoreCase);

        if (isGranted || isDenied)
        {
            return new NfcLog
            {
                EventId      = Guid.NewGuid().ToString("N"),
                Uid          = "—",
                ClientName   = string.Empty,
                Result       = isGranted ? NfcAccessResult.Authorized : NfcAccessResult.Denied,
                Reason       = payload,
                TimestampUtc = DateTime.UtcNow,
                Source       = "mqtt"
            };
        }

        // ── 4. Message brut non reconnu ────────────────────────────
        return new NfcLog
        {
            EventId      = Guid.NewGuid().ToString("N"),
            Uid          = "—",
            ClientName   = string.Empty,
            Result       = NfcAccessResult.Pending,
            Reason       = payload,
            TimestampUtc = DateTime.UtcNow,
            Source       = "mqtt-raw"
        };
    }

    private static NfcAccessResult ParseAccessResult(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "granted" or "autorise" or "autorisé" or "authorized" or "ok" or "1" or "true" => NfcAccessResult.Authorized,
            "denied"  or "refuse"  or "refusé"   or "forbidden"  or "ko" or "0" or "false" => NfcAccessResult.Denied,
            _ => NfcAccessResult.Pending
        };

    /// <summary>Cherche une valeur string parmi plusieurs noms de propriétés JSON candidats.</summary>
    private static string? GetJsonStr(JsonElement root, params string[] names)
    {
        foreach (var name in names)
            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        return null;
    }
}
