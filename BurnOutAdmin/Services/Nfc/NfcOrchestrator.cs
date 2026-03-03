using BurnOutAdmin.Models;
using BurnOutAdmin.Services.Mqtt;
using BurnOutAdmin.Services.Rfid;

namespace BurnOutAdmin.Services.Nfc;

/// <summary>
/// Orchestrateur central NFC.
/// Responsabilités :
///   1. Écoute les UIDs du lecteur RFID
///   2. Crée un log Pending + persiste en SQLite
///   3. Publie un événement scan/bind sur MQTT
///   4. Écoute les résultats d'accès MQTT
///   5. Met à jour le log avec le résultat
///   6. Notifie l'UI via LogUpdated
/// </summary>
public class NfcOrchestrator : INfcOrchestrator
{
    /// <inheritdoc />
    public event EventHandler<NfcLog>? LogUpdated;

    /// <inheritdoc />
    public event EventHandler<string>? UidScanned;

    /// <inheritdoc />
    public bool IsReaderConnected => _rfidReader.IsConnected;

    /// <inheritdoc />
    public bool IsMqttConnected => _mqttService.IsConnected;

    /// <inheritdoc />
    public bool IsBindMode => _bindClientId.HasValue;

    /// <inheritdoc />
    public bool IsCaptureMode { get; private set; }

    private readonly IRfidReaderService _rfidReader;
    private readonly IMqttService _mqttService;
    private readonly INfcLogRepository _logRepository;

    // --- État du mode Bind ---
    private int? _bindClientId;
    private string? _bindClientName;
    private readonly object _bindLock = new();

    // --- Configuration ---
    private const string ReaderId = "admin-reader-01";
    private const string TopicRfidAccess = "rfid/access";

    private bool _disposed;

    public NfcOrchestrator(
        IRfidReaderService rfidReader,
        IMqttService mqttService,
        INfcLogRepository logRepository)
    {
        _rfidReader = rfidReader;
        _mqttService = mqttService;
        _logRepository = logRepository;
        OrcLog($"Créé. RfidReader type={rfidReader.GetType().Name}");
    }

    private static void OrcLog(string message)
    {
        var msg = $"[Orchestrator] {message}";
        Console.WriteLine(msg);
        System.Diagnostics.Debug.WriteLine(msg);
    }

    /// <inheritdoc />
    public async Task StartAsync()
    {
        OrcLog("========== DÉMARRAGE ORCHESTRATOR ==========");

        // Initialiser le dépôt SQLite
        OrcLog("Initialisation SQLite...");
        try
        {
            await _logRepository.InitializeAsync();
            OrcLog("SQLite OK.");
        }
        catch (Exception ex)
        {
            OrcLog($"ERREUR SQLite: {ex.Message}");
        }

        // Démarrer le lecteur RFID
        OrcLog($"Abonnement UidReceived sur {_rfidReader.GetType().Name}...");
        _rfidReader.UidReceived += OnUidReceived;
        OrcLog("Démarrage du lecteur RFID (StartAsync)...");
        try
        {
            await _rfidReader.StartAsync();
            OrcLog($"Lecteur RFID démarré. IsConnected={_rfidReader.IsConnected}");
        }
        catch (Exception ex)
        {
            OrcLog($"ERREUR démarrage lecteur RFID: {ex.GetType().Name}: {ex.Message}");
        }

        // Connecter MQTT et s'abonner aux résultats
        OrcLog("Connexion MQTT...");
        try
        {
            await _mqttService.ConnectAsync();
            _mqttService.MessageReceived += OnMqttMessageReceived;
            OrcLog($"MQTT connecté. IsConnected={_mqttService.IsConnected}");
        }
        catch (Exception ex)
        {
            OrcLog($"ERREUR MQTT: {ex.Message}");
        }

        OrcLog($"Reader={IsReaderConnected}, MQTT={IsMqttConnected}");
        OrcLog("========== ORCHESTRATOR PRÊT ==========");
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        _rfidReader.UidReceived -= OnUidReceived;
        _mqttService.MessageReceived -= OnMqttMessageReceived;

        await _rfidReader.StopAsync();
        await _mqttService.DisconnectAsync();

        CancelBindMode();

        System.Diagnostics.Debug.WriteLine("[Orchestrator] Arrêté.");
    }

    /// <inheritdoc />
    public void StartBindMode(int clientId, string clientName)
    {
        lock (_bindLock)
        {
            _bindClientId = clientId;
            _bindClientName = clientName;
        }
        System.Diagnostics.Debug.WriteLine($"[Orchestrator] Mode Bind activé pour client #{clientId} ({clientName})");
    }

    /// <inheritdoc />
    public void CancelBindMode()
    {
        lock (_bindLock)
        {
            _bindClientId = null;
            _bindClientName = null;
        }
        System.Diagnostics.Debug.WriteLine("[Orchestrator] Mode Bind annulé.");
    }

    /// <inheritdoc />
    public void StartCaptureMode()
    {
        IsCaptureMode = true;
        System.Diagnostics.Debug.WriteLine("[Orchestrator] Mode Capture activé (UID intercepté sans MQTT/log).");
    }

    /// <inheritdoc />
    public void StopCaptureMode()
    {
        IsCaptureMode = false;
        System.Diagnostics.Debug.WriteLine("[Orchestrator] Mode Capture désactivé.");
    }

    /// <summary>
    /// Callback déclenché par le lecteur RFID lorsqu'un UID est lu.
    /// Si mode Capture actif : émet UidScanned puis retourne (pas de MQTT/log).
    /// Sinon : crée un log Pending, persiste, publie sur MQTT.
    /// </summary>
    private async void OnUidReceived(object? sender, string uid)
    {
        OrcLog($">>> OnUidReceived déclenché ! UID={uid}, CaptureMode={IsCaptureMode}, BindMode={IsBindMode}");
        try
        {
            // Toujours émettre l'événement UidScanned (brut)
            OrcLog("Emission UidScanned...");
            UidScanned?.Invoke(this, uid);

            // Mode Capture : intercepter l'UID sans traitement
            if (IsCaptureMode)
            {
                IsCaptureMode = false;
                System.Diagnostics.Debug.WriteLine($"[Orchestrator] UID capturé (formulaire): {uid}");
                return;
            }

            // Déterminer le mode (access ou bind)
            string mode;
            int? clientId;
            string? clientName;

            lock (_bindLock)
            {
                if (_bindClientId.HasValue)
                {
                    mode = "bind";
                    clientId = _bindClientId;
                    clientName = _bindClientName;
                }
                else
                {
                    mode = "access";
                    clientId = null;
                    clientName = null;
                }
            }

            // Générer un EventId unique
            var eventId = Guid.NewGuid().ToString("N");

            // Créer le log en état Pending
            var log = new NfcLog
            {
                EventId = eventId,
                Uid = uid,
                ClientId = clientId,
                ClientName = clientName ?? string.Empty,
                Result = NfcAccessResult.Pending,
                Reason = mode == "bind" ? "Association en cours..." : "Vérification en cours...",
                TimestampUtc = DateTime.UtcNow,
                Door = null,
                Source = ReaderId
            };

            // Persister en SQLite
            await _logRepository.AddAsync(log);

            // Notifier l'UI immédiatement (état Pending)
            LogUpdated?.Invoke(this, log);

            // Publier sur MQTT au format BADGE:<UID> (protocole Raspberry Pi)
            var payload = $"BADGE:{uid}";
            await _mqttService.PublishAsync(TopicRfidAccess, payload);

            // Si mode Bind, désactiver le mode après le scan
            if (mode == "bind")
            {
                CancelBindMode();
            }

            System.Diagnostics.Debug.WriteLine($"[Orchestrator] Scan traité: EventId={eventId}, Mode={mode}, Uid={uid}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Orchestrator] Erreur OnUidReceived: {ex.Message}");
        }
    }

    /// <summary>
    /// Callback déclenché à la réception d'un message MQTT.
    /// Gère deux formats :
    ///   - "BADGE:&lt;UID&gt;" depuis la Raspberry (scan reçu/confirmé)
    ///   - Texte simple de résultat (Accès autorisé / Accès refusé)
    /// </summary>
    private async void OnMqttMessageReceived(object? sender, MqttMessageEventArgs e)
    {
        if (e.Topic != TopicRfidAccess)
            return;

        try
        {
            var payload = e.Payload.Trim();

            // Ignorer les messages BADGE: qu'on a nous-mêmes publiés
            // (on les traite déjà dans OnUidReceived)
            // Traiter uniquement les réponses de la Raspberry
            if (payload.StartsWith("BADGE:", StringComparison.OrdinalIgnoreCase))
            {
                // Message BADGE reçu de la Raspberry — c'est un écho ou un scan externe
                var uid = payload[6..].Trim();
                if (string.IsNullOrEmpty(uid))
                    return;

                // Chercher le log Pending le plus récent pour cet UID
                var logs = await _logRepository.GetTodayLogsAsync();
                var pendingLog = logs.FirstOrDefault(l =>
                    l.Uid.Equals(uid, StringComparison.OrdinalIgnoreCase) &&
                    l.Result == NfcAccessResult.Pending);

                if (pendingLog is not null)
                {
                    // La Raspberry a confirmé le scan — marquer comme autorisé
                    pendingLog.Result = NfcAccessResult.Authorized;
                    pendingLog.Reason = "Accès autorisé";
                    await _logRepository.UpdateAsync(pendingLog);
                    LogUpdated?.Invoke(this, pendingLog);

                    System.Diagnostics.Debug.WriteLine($"[Orchestrator] BADGE confirmé par Raspberry: {uid}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Orchestrator] BADGE reçu sans log Pending: {uid}");
                }

                return;
            }

            // Traiter les réponses texte de la Raspberry ("Accès autorisé" / "Accès refusé")
            var isAuthorized = payload.Contains("autorisé", StringComparison.OrdinalIgnoreCase);
            var isDenied = payload.Contains("refusé", StringComparison.OrdinalIgnoreCase);

            if (!isAuthorized && !isDenied)
            {
                System.Diagnostics.Debug.WriteLine($"[Orchestrator] Message MQTT non reconnu: {payload}");
                return;
            }

            // Trouver le log Pending le plus récent
            var todayLogs = await _logRepository.GetTodayLogsAsync();
            var latestPending = todayLogs.FirstOrDefault(l => l.Result == NfcAccessResult.Pending);

            if (latestPending is null)
            {
                System.Diagnostics.Debug.WriteLine("[Orchestrator] Aucun log Pending pour le résultat reçu.");
                return;
            }

            latestPending.Result = isAuthorized ? NfcAccessResult.Authorized : NfcAccessResult.Denied;
            latestPending.Reason = isAuthorized ? "Accès autorisé" : "Accès refusé";

            await _logRepository.UpdateAsync(latestPending);
            LogUpdated?.Invoke(this, latestPending);

            System.Diagnostics.Debug.WriteLine(
                $"[Orchestrator] Résultat Raspberry: {payload} → {latestPending.Result} pour UID={latestPending.Uid}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Orchestrator] Erreur traitement résultat MQTT: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _rfidReader.UidReceived -= OnUidReceived;
        _mqttService.MessageReceived -= OnMqttMessageReceived;

        GC.SuppressFinalize(this);
    }
}

