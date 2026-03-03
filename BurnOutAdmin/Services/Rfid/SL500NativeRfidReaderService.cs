#if WINDOWS
using BurnOutAdmin.Platforms.Windows.RFID;
#endif

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
                    System.Diagnostics.Debug.WriteLine("[SL500-DLL] Lecteur déjà connecté.");
                    return Task.CompletedTask;
                }

                // Étape 1 : Initialiser le port COM via la DLL
                _icdev = SL500Native.rf_init_com(_comPort, _baudRate);

                if (_icdev <= 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SL500-DLL] Erreur rf_init_com: port COM{_comPort + 1}, retour={_icdev}");
                    IsConnected = false;
                    return Task.CompletedTask;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[SL500-DLL] rf_init_com OK: COM{_comPort + 1}, icdev={_icdev}");

                // Étape 2 : Activer l'antenne RF
                var antennaResult = SL500Native.rf_antenna_sta(_icdev, 0x01);
                if (antennaResult != 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SL500-DLL] Avertissement rf_antenna_sta: retour={antennaResult}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[SL500-DLL] Antenne RF activée.");
                }

                // Bip de confirmation (court)
                try { SL500Native.rf_beep(_icdev, 10); } catch { /* optionnel */ }

                IsConnected = true;

                // Étape 3 : Démarrer la boucle de polling
                _pollingCts = new CancellationTokenSource();
                _pollingTask = Task.Run(() => PollingLoopAsync(_pollingCts.Token));

                System.Diagnostics.Debug.WriteLine("[SL500-DLL] Polling démarré.");
            }
            catch (DllNotFoundException ex)
            {
                IsConnected = false;
                System.Diagnostics.Debug.WriteLine(
                    $"[SL500-DLL] DLL introuvable: {ex.Message}. " +
                    "Vérifiez que MasterRD.dll et MasterCOM.dll sont dans le répertoire de sortie.");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                System.Diagnostics.Debug.WriteLine($"[SL500-DLL] Erreur démarrage: {ex.Message}");
            }
        }
#else
        System.Diagnostics.Debug.WriteLine("[SL500-DLL] Non supporté sur cette plateforme.");
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
                    // Désactiver l'antenne avant de fermer
                    try { SL500Native.rf_antenna_sta(_icdev, 0x00); } catch { /* ignore */ }

                    var result = SL500Native.rf_closeport(_icdev);
                    System.Diagnostics.Debug.WriteLine(
                        $"[SL500-DLL] rf_closeport: retour={result}");
                    _icdev = -1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SL500-DLL] Erreur fermeture: {ex.Message}");
            }
            finally
            {
                IsConnected = false;
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
    private async Task PollingLoopAsync(CancellationToken ct)
    {
        System.Diagnostics.Debug.WriteLine("[SL500-DLL] Boucle de polling démarrée.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_icdev <= 0)
                {
                    IsConnected = false;
                    System.Diagnostics.Debug.WriteLine("[SL500-DLL] Device invalide, arrêt du polling.");
                    break;
                }

                // --- Étape 1 : REQUEST (détecter carte) ---
                var tagType = new byte[2];
                var reqResult = SL500Native.rf_request(_icdev, 0x52, tagType);

                if (reqResult != 0)
                {
                    // Pas de carte à proximité — normal, on continue
                    await Task.Delay(PollIntervalMs, ct);
                    continue;
                }

                // --- Étape 2 : ANTICOLL (obtenir UID) ---
                var snr = new byte[4];
                var anticollResult = SL500Native.rf_anticoll(_icdev, 0x04, snr);

                if (anticollResult != 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SL500-DLL] rf_anticoll échoué: retour={anticollResult}");
                    await Task.Delay(PollIntervalMs, ct);
                    continue;
                }

                // --- Étape 3 : SELECT (valider la carte) ---
                var size = new byte[1];
                var selectResult = SL500Native.rf_select(_icdev, snr, size);

                if (selectResult != 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SL500-DLL] rf_select échoué: retour={selectResult}");
                }

                // --- Étape 4 : Convertir UID en hex ---
                var uid = FormatUid(snr);

                // --- Étape 5 : HALT (libérer la carte pour prochaine détection) ---
                try { SL500Native.rf_halt(_icdev); } catch { /* ignore */ }

                // --- Étape 6 : Émettre si anti-rebond OK ---
                if (!string.IsNullOrEmpty(uid))
                {
                    TryEmitUid(uid);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (DllNotFoundException ex)
            {
                IsConnected = false;
                System.Diagnostics.Debug.WriteLine(
                    $"[SL500-DLL] DLL introuvable pendant polling: {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SL500-DLL] Erreur polling: {ex.Message}");
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

        System.Diagnostics.Debug.WriteLine("[SL500-DLL] Boucle de polling terminée.");
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
            return; // Anti-rebond : même carte encore sur le lecteur
        }

        _lastUid = uid;
        _lastScanTime = now;

        System.Diagnostics.Debug.WriteLine($"[SL500-DLL] UID détecté: {uid}");
        UidReceived?.Invoke(this, uid);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
