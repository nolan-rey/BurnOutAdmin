#if WINDOWS
using BurnOutAdmin.Platforms.Windows.RFID;
#endif
using System.Diagnostics;

namespace BurnOutAdmin.Services.Rfid;

/// <summary>
/// Implémentation du lecteur RFID StrongLink SL500-USB via les DLL constructeur (P/Invoke).
///
/// Utilise MasterRD.dll + MasterCOM.dll pour communiquer avec le lecteur.
/// Protocole ISO14443A (MIFARE Classic / Ultralight / NTAG).
///
/// Séquence de polling :
///   1. rf_init_com(port, 9600) → obtenir handle icdev
///   2. rf_antenna_sta(icdev, 1) → activer antenne
///   3. Boucle toutes les 200ms :
///      a. rf_request(icdev, 0x52, tagtype) → détecter carte
///      b. rf_anticoll(icdev, 0x04, snr) → obtenir UID 4 octets
///      c. rf_select(icdev, snr, size) → sélectionner carte
///      d. rf_halt(icdev) → libérer carte pour prochaine détection
///      e. Émettre UidReceived si anti-rebond OK
///
/// Anti-rebond : même UID scanné dans les 1500ms → ignoré.
/// Thread-safe. Compatible Windows uniquement (Dummy sur autres plateformes).
/// </summary>
public class SL500NativeRfidReaderService : IRfidReaderService
{
    /// <inheritdoc />
    public event EventHandler<string>? UidReceived;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    private readonly int _comPort;
    private readonly int _baudRate;

    // --- Anti-rebond ---
    private string? _lastUid;
    private DateTime _lastScanTime = DateTime.MinValue;
    private const int DebounceMs = 1500;

    // --- Polling ---
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private readonly object _lock = new();

    // --- Intervalle de polling (ms) ---
    private const int PollIntervalMs = 200;

#if WINDOWS
    // Handle du device retourné par rf_init_com
    private int _icdev = -1;
#endif

    /// <summary>
    /// Crée une instance du service RFID SL500 via DLL native.
    /// </summary>
    /// <param name="comPort">Numéro de port COM : 0=COM1, 1=COM2, 2=COM3, etc.</param>
    /// <param name="baudRate">Vitesse de communication. Par défaut 9600.</param>
    public SL500NativeRfidReaderService(int comPort = 4, int baudRate = 9600)
    {
        _comPort = comPort;
        _baudRate = baudRate;
        Log($"Service créé: comPort={comPort} (COM{comPort + 1}), baudRate={baudRate}");
    }

    private static void Log(string message)
    {
        var msg = $"[SL500-DLL] {message}";
        Console.WriteLine(msg);
        Debug.WriteLine(msg);
    }

    /// <inheritdoc />
    public Task StartAsync()
    {
#if WINDOWS
        lock (_lock)
        {
            try
            {
                if (IsConnected)
                {
                    Log("Lecteur déjà connecté — ignoré.");
                    return Task.CompletedTask;
                }

                Log("========== DÉMARRAGE SL500 ==========");

                // Diagnostic : vérifier que les DLL existent
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Log($"Répertoire de base: {baseDir}");
                var masterRdPath = Path.Combine(baseDir, "MasterRD.dll");
                var masterComPath = Path.Combine(baseDir, "MasterCOM.dll");
                Log($"MasterRD.dll existe: {File.Exists(masterRdPath)} → {masterRdPath}");
                Log($"MasterCOM.dll existe: {File.Exists(masterComPath)} → {masterComPath}");

                if (!File.Exists(masterRdPath) || !File.Exists(masterComPath))
                {
                    Log("⚠ DLL MANQUANTE ! Vérifiez que les DLLs sont copiées dans le répertoire de sortie.");
                }

                // Étape 1 : Initialiser le port COM via la DLL
                Log($"Appel rf_init_com(port={_comPort}, baud={_baudRate})...");
                _icdev = SL500Native.rf_init_com(_comPort, _baudRate);
                Log($"rf_init_com retour = {_icdev} (>0 = OK, <=0 = erreur)");

                if (_icdev <= 0)
                {
                    Log($"ERREUR: rf_init_com a échoué pour COM{_comPort + 1}. Retour={_icdev}");
                    Log("Vérifiez: (1) Le SL500 est branché (2) COM5 est le bon port (3) Pas d'autre app qui utilise le port");
                    IsConnected = false;
                    return Task.CompletedTask;
                }

                Log($"OK: COM{_comPort + 1} ouvert, icdev={_icdev}");

                // Étape 2 : Activer l'antenne RF
                Log("Appel rf_antenna_sta(icdev, 0x01)...");
                var antennaResult = SL500Native.rf_antenna_sta(_icdev, 0x01);
                Log($"rf_antenna_sta retour = {antennaResult} (0=OK)");

                // Bip de confirmation (court)
                Log("Appel rf_beep...");
                try
                {
                    var beepResult = SL500Native.rf_beep(_icdev, 10);
                    Log($"rf_beep retour = {beepResult}");
                }
                catch (Exception beepEx)
                {
                    Log($"rf_beep exception: {beepEx.Message}");
                }

                IsConnected = true;

                // Étape 3 : Démarrer la boucle de polling
                _pollingCts = new CancellationTokenSource();
                _pollingTask = Task.Run(() => PollingLoopAsync(_pollingCts.Token));

                Log("Polling démarré. Approchez une carte...");
                Log("========== SL500 PRÊT ==========");
            }
            catch (DllNotFoundException ex)
            {
                IsConnected = false;
                Log($"ERREUR DllNotFoundException: {ex.Message}");
                Log($"Stack: {ex.StackTrace}");
                Log("Les DLLs MasterRD.dll et MasterCOM.dll doivent être dans le répertoire de sortie!");
            }
            catch (BadImageFormatException ex)
            {
                IsConnected = false;
                Log($"ERREUR BadImageFormatException: {ex.Message}");
                Log("La DLL est probablement 32-bit et votre app 64-bit (ou inversement).");
                Log($"Stack: {ex.StackTrace}");
            }
            catch (EntryPointNotFoundException ex)
            {
                IsConnected = false;
                Log($"ERREUR EntryPointNotFoundException: {ex.Message}");
                Log("La fonction P/Invoke n'existe pas dans la DLL. Vérifiez les noms de fonctions.");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Log($"ERREUR inattendue: {ex.GetType().Name}: {ex.Message}");
                Log($"Stack: {ex.StackTrace}");
            }
        }
#else
        Log("Non supporté sur cette plateforme (non-Windows).");
#endif
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
#if WINDOWS
        CancellationTokenSource? cts;
        Task? pollingTask;

        lock (_lock)
        {
            cts = _pollingCts;
            pollingTask = _pollingTask;
            _pollingCts = null;
            _pollingTask = null;
        }

        // Annuler le polling et attendre sa fin
        if (cts is not null)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        if (pollingTask is not null)
        {
            try
            {
                await pollingTask;
            }
            catch (OperationCanceledException)
            {
                // Attendu lors de l'annulation
            }
        }

        lock (_lock)
        {
            try
            {
                if (_icdev > 0)
                {
                    Log("Fermeture: désactivation antenne...");
                    try { SL500Native.rf_antenna_sta(_icdev, 0x00); } catch { /* ignore */ }

                    Log($"Fermeture: rf_closeport(icdev={_icdev})...");
                    var result = SL500Native.rf_closeport(_icdev);
                    Log($"rf_closeport retour = {result}");
                    _icdev = -1;
                }
            }
            catch (Exception ex)
            {
                Log($"Erreur fermeture: {ex.Message}");
            }
            finally
            {
                IsConnected = false;
                Log("Port fermé, IsConnected=false.");
            }
        }
#else
        IsConnected = false;
        await Task.CompletedTask;
#endif
    }

#if WINDOWS

    // ═══════════════════════════════════════════════════════════════
    //  BOUCLE DE POLLING
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Boucle de polling exécutée en background.
    /// REQUEST → ANTICOLL → SELECT → HALT → émet UID.
    /// </summary>
    private int _pollCount;

    private async Task PollingLoopAsync(CancellationToken ct)
    {
        Log("Boucle de polling démarrée (thread background).");
        _pollCount = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_icdev <= 0)
                {
                    IsConnected = false;
                    Log("ERREUR: icdev invalide dans la boucle, arrêt.");
                    break;
                }

                _pollCount++;

                // Log toutes les 25 itérations (~5s) pour montrer que le polling tourne
                if (_pollCount % 25 == 1)
                {
                    Log($"Polling actif... (itération #{_pollCount}, icdev={_icdev})");
                }

                // --- Étape 1 : REQUEST (détecter carte) ---
                var tagType = new byte[2];
                var reqResult = SL500Native.rf_request(_icdev, 0x52, tagType);

                if (reqResult != 0)
                {
                    // Pas de carte — normal, on continue silencieusement
                    // Log toutes les 50 itérations pour debug
                    if (_pollCount % 50 == 0)
                    {
                        Log($"rf_request retour={reqResult} (pas de carte) — poll #{_pollCount}");
                    }
                    await Task.Delay(PollIntervalMs, ct);
                    continue;
                }

                // CARTE DÉTECTÉE !
                Log($">>> CARTE DÉTECTÉE ! rf_request=0, tagType=[0x{tagType[0]:X2}, 0x{tagType[1]:X2}]");

                // --- Étape 2 : ANTICOLL (obtenir UID) ---
                var snr = new byte[4];
                var anticollResult = SL500Native.rf_anticoll(_icdev, 0x04, snr);
                Log($"rf_anticoll retour={anticollResult}, snr=[0x{snr[0]:X2}, 0x{snr[1]:X2}, 0x{snr[2]:X2}, 0x{snr[3]:X2}]");

                if (anticollResult != 0)
                {
                    Log($"ERREUR rf_anticoll: retour={anticollResult}");
                    await Task.Delay(PollIntervalMs, ct);
                    continue;
                }

                // --- Étape 3 : SELECT (valider la carte) ---
                var size = new byte[1];
                var selectResult = SL500Native.rf_select(_icdev, snr, size);
                Log($"rf_select retour={selectResult}, SAK=0x{size[0]:X2}");

                // --- Étape 4 : Convertir UID en hex ---
                var uid = FormatUid(snr);
                Log($"UID formaté: {uid}");

                // --- Étape 5 : HALT (libérer la carte pour prochaine détection) ---
                try
                {
                    var haltResult = SL500Native.rf_halt(_icdev);
                    Log($"rf_halt retour={haltResult}");
                }
                catch (Exception haltEx)
                {
                    Log($"rf_halt exception: {haltEx.Message}");
                }

                // --- Étape 6 : Émettre si anti-rebond OK ---
                if (!string.IsNullOrEmpty(uid))
                {
                    TryEmitUid(uid);
                }
                else
                {
                    Log("UID vide après formatage — ignoré.");
                }
            }
            catch (OperationCanceledException)
            {
                Log("Polling annulé (CancellationToken).");
                break;
            }
            catch (DllNotFoundException ex)
            {
                IsConnected = false;
                Log($"ERREUR DllNotFoundException pendant polling: {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                Log($"ERREUR polling: {ex.GetType().Name}: {ex.Message}");
            }

            try
            {
                await Task.Delay(PollIntervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Log($"Boucle de polling terminée après {_pollCount} itérations.");
    }

    // ═══════════════════════════════════════════════════════════════
    //  FORMATAGE UID
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Convertit les 4 octets UID (snr) en chaîne hex uppercase.
    /// Exemple : { 0x04, 0xA3, 0x5B, 0x12 } → "04A35B12"
    /// </summary>
    private static string FormatUid(byte[] snr)
    {
        return BitConverter.ToString(snr).Replace("-", "");
    }

#endif

    // ═══════════════════════════════════════════════════════════════
    //  ANTI-REBOND + ÉMISSION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Émet l'UID si anti-rebond OK (même UID &lt; 1500ms → ignoré).
    /// </summary>
    private void TryEmitUid(string uid)
    {
        var now = DateTime.UtcNow;

        if (uid == _lastUid && (now - _lastScanTime).TotalMilliseconds < DebounceMs)
        {
            Log($"Anti-rebond: {uid} ignoré ({(now - _lastScanTime).TotalMilliseconds:F0}ms < {DebounceMs}ms)");
            return;
        }

        _lastUid = uid;
        _lastScanTime = now;

        Log($">>> UID ÉMIS: {uid} — envoi event UidReceived");
        var handlers = UidReceived;
        if (handlers == null)
        {
            Log("ATTENTION: aucun abonné à UidReceived ! L'orchestrateur n'est peut-être pas démarré.");
        }
        else
        {
            handlers.Invoke(this, uid);
            Log("Event UidReceived invoqué avec succès.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
