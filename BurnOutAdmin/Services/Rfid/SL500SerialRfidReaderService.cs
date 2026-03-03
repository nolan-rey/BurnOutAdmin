#if WINDOWS
using System.IO.Ports;
#endif

namespace BurnOutAdmin.Services.Rfid;

/// <summary>
/// Implémentation du lecteur RFID StrongLink SL500-USB via port série (Windows uniquement).
///
/// Protocole SL500 (ISO14443A) :
///   Le SL500 ne fonctionne PAS en mode auto-send. Il nécessite un polling actif :
///   1. Ouvrir le port série (9600, 8N1)
///   2. Envoyer la commande RF_REQUEST (0x41) pour détecter une carte ISO14443A
///   3. Si carte détectée → envoyer RF_ANTICOLL (0x42) pour obtenir l'UID
///   4. Parser la réponse pour extraire l'UID (4 ou 7 octets)
///   5. Boucler toutes les 200ms
///
/// Format trame SL500 :
///   TX : [STX=0xAA] [LEN] [CMD] [DATA...] [BCC]
///   RX : [STX=0xAA] [LEN] [STATUS] [CMD] [DATA...] [BCC]
///   BCC = XOR de tous les octets sauf STX
///   LEN = nombre d'octets après LEN (CMD + DATA + BCC pour TX, STATUS + CMD + DATA + BCC pour RX)
///
/// Anti-rebond : même UID scanné dans les 1500ms → ignoré.
/// </summary>
public class SL500SerialRfidReaderService : IRfidReaderService
{
    /// <inheritdoc />
    public event EventHandler<string>? UidReceived;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    private readonly string _portName;
    private readonly int _baudRate;

    // --- Anti-rebond ---
    private string? _lastUid;
    private DateTime _lastScanTime = DateTime.MinValue;
    private const int DebounceMs = 1500;

    // --- Polling ---
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private readonly object _lock = new();

    // --- Protocole SL500 ---
    private const byte Stx = 0xAA;          // Start of frame
    private const byte CmdRequest = 0x41;    // RF_REQUEST — détection carte ISO14443A
    private const byte CmdAnticoll = 0x42;   // RF_ANTICOLL — lecture UID (anti-collision)
    private const byte RequestModeAll = 0x52; // REQALL — détecte toutes les cartes (pas seulement idle)
    private const byte StatusSuccess = 0x00;  // Réponse OK

    // Timeout lecture réponse série (ms)
    private const int ReadTimeoutMs = 150;
    // Intervalle de polling (ms)
    private const int PollIntervalMs = 200;

#if WINDOWS
    private SerialPort? _serialPort;
#endif

    /// <summary>
    /// Crée une instance du service de lecture RFID SL500 via port série.
    /// </summary>
    /// <param name="portName">Nom du port COM (ex: "COM3"). Par défaut "COM3".</param>
    /// <param name="baudRate">Vitesse de communication. Par défaut 9600 (standard SL500).</param>
    public SL500SerialRfidReaderService(string portName = "COM3", int baudRate = 9600)
    {
        _portName = portName;
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
                if (_serialPort is { IsOpen: true })
                {
                    System.Diagnostics.Debug.WriteLine("[SL500] Port déjà ouvert.");
                    return Task.CompletedTask;
                }

                _serialPort = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One)
                {
                    DtrEnable = true,
                    RtsEnable = true,
                    ReadTimeout = ReadTimeoutMs,
                    WriteTimeout = 500
                };

                _serialPort.Open();
                IsConnected = true;

                System.Diagnostics.Debug.WriteLine(
                    $"[SL500] Port {_portName} ouvert à {_baudRate} bauds (8N1). Démarrage polling...");

                // Activer l'antenne RF
                SendActivateAntenna();

                // Démarrer la boucle de polling
                _pollingCts = new CancellationTokenSource();
                _pollingTask = Task.Run(() => PollingLoopAsync(_pollingCts.Token));
            }
            catch (Exception ex)
            {
                IsConnected = false;
                System.Diagnostics.Debug.WriteLine($"[SL500] Erreur ouverture port {_portName}: {ex.Message}");
            }
        }
#else
        System.Diagnostics.Debug.WriteLine("[SL500] Port série non supporté sur cette plateforme.");
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
                if (_serialPort is { IsOpen: true })
                {
                    _serialPort.Close();
                    System.Diagnostics.Debug.WriteLine($"[SL500] Port {_portName} fermé.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SL500] Erreur fermeture port: {ex.Message}");
            }
            finally
            {
                _serialPort?.Dispose();
                _serialPort = null;
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
    /// Envoie REQUEST → si carte détectée → ANTICOLL → émet UID.
    /// </summary>
    private async Task PollingLoopAsync(CancellationToken ct)
    {
        System.Diagnostics.Debug.WriteLine("[SL500] Boucle de polling démarrée.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_serialPort is not { IsOpen: true })
                {
                    IsConnected = false;
                    System.Diagnostics.Debug.WriteLine("[SL500] Port fermé, arrêt du polling.");
                    break;
                }

                // Étape 1 : REQUEST — détecter une carte
                var requestResponse = SendCommandAndRead(BuildRequestCommand());

                if (requestResponse is not null && IsSuccessResponse(requestResponse))
                {
                    // Carte détectée → Étape 2 : ANTICOLL — obtenir l'UID
                    var anticollResponse = SendCommandAndRead(BuildAnticollCommand());

                    if (anticollResponse is not null && IsSuccessResponse(anticollResponse))
                    {
                        var uid = ParseUidFromAnticollResponse(anticollResponse);
                        if (!string.IsNullOrEmpty(uid))
                        {
                            TryEmitUid(uid);
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                // Pas de carte — normal, on continue
            }
            catch (InvalidOperationException)
            {
                // Port fermé pendant l'opération
                IsConnected = false;
                System.Diagnostics.Debug.WriteLine("[SL500] Port fermé pendant lecture, arrêt du polling.");
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SL500] Erreur polling: {ex.Message}");
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

        System.Diagnostics.Debug.WriteLine("[SL500] Boucle de polling terminée.");
    }

    // ═══════════════════════════════════════════════════════════════
    //  CONSTRUCTION DES COMMANDES SL500
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Construit la trame RF_REQUEST (0x41) pour détecter une carte ISO14443A.
    /// Format : [AA] [03] [41] [52] [BCC]
    ///   - 0x41 = commande REQUEST
    ///   - 0x52 = REQALL (détecter toutes les cartes, y compris celles déjà en état HALT)
    ///   - BCC = XOR(03, 41, 52)
    /// </summary>
    private static byte[] BuildRequestCommand()
    {
        byte len = 0x03; // CMD(1) + DATA(1) + BCC(1)
        byte cmd = CmdRequest;
        byte data = RequestModeAll;
        byte bcc = (byte)(len ^ cmd ^ data);

        return [Stx, len, cmd, data, bcc];
    }

    /// <summary>
    /// Construit la trame RF_ANTICOLL (0x42) pour lire l'UID après détection.
    /// Format : [AA] [02] [42] [BCC]
    ///   - 0x42 = commande ANTICOLL
    ///   - Pas de data
    ///   - BCC = XOR(02, 42)
    /// </summary>
    private static byte[] BuildAnticollCommand()
    {
        byte len = 0x02; // CMD(1) + BCC(1)
        byte cmd = CmdAnticoll;
        byte bcc = (byte)(len ^ cmd);

        return [Stx, len, cmd, bcc];
    }

    /// <summary>
    /// Active l'antenne RF du SL500.
    /// Commande RF_INIT_TYPE (0x08) + type ISO14443A (0x41).
    /// </summary>
    private void SendActivateAntenna()
    {
        try
        {
            // Commande RF_ANTENNA_STA (0x0C) — activer antenne
            // Format : [AA] [03] [0C] [01] [BCC]  (0x01 = antenne ON)
            byte len = 0x03;
            byte cmd = 0x0C;
            byte data = 0x01; // ON
            byte bcc = (byte)(len ^ cmd ^ data);

            byte[] frame = [Stx, len, cmd, data, bcc];

            _serialPort?.Write(frame, 0, frame.Length);
            Thread.Sleep(50); // Laisser le temps au SL500 de répondre
            DiscardInputBuffer();

            System.Diagnostics.Debug.WriteLine("[SL500] Antenne RF activée.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SL500] Erreur activation antenne: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  COMMUNICATION SÉRIE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Envoie une trame et lit la réponse complète du SL500.
    /// Retourne null si pas de réponse ou erreur.
    /// </summary>
    private byte[]? SendCommandAndRead(byte[] command)
    {
        if (_serialPort is not { IsOpen: true })
            return null;

        // Vider le buffer d'entrée avant d'envoyer
        DiscardInputBuffer();

        // Envoyer la commande
        _serialPort.Write(command, 0, command.Length);

        // Lire la réponse — format : [AA] [LEN] [STATUS] [CMD] [DATA...] [BCC]
        try
        {
            // Lire STX
            var stx = (byte)_serialPort.ReadByte();
            if (stx != Stx)
                return null;

            // Lire LEN
            var len = (byte)_serialPort.ReadByte();
            if (len < 2 || len > 64)
                return null;

            // Lire le reste (LEN octets = STATUS + CMD + DATA + BCC)
            var payload = new byte[len];
            var totalRead = 0;
            while (totalRead < len)
            {
                var bytesRead = _serialPort.Read(payload, totalRead, len - totalRead);
                if (bytesRead <= 0)
                    break;
                totalRead += bytesRead;
            }

            if (totalRead != len)
                return null;

            // Vérifier BCC
            byte computedBcc = len;
            for (var i = 0; i < len - 1; i++) // Tout sauf le dernier octet (BCC)
                computedBcc ^= payload[i];

            if (computedBcc != payload[len - 1])
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SL500] BCC invalide: attendu 0x{computedBcc:X2}, reçu 0x{payload[len - 1]:X2}");
                return null;
            }

            // Construire la réponse complète : [STX, LEN, payload...]
            var response = new byte[2 + len];
            response[0] = stx;
            response[1] = len;
            Array.Copy(payload, 0, response, 2, len);

            return response;
        }
        catch (TimeoutException)
        {
            // Pas de réponse — pas de carte à proximité
            return null;
        }
    }

    /// <summary>
    /// Vide le buffer de réception du port série.
    /// </summary>
    private void DiscardInputBuffer()
    {
        try
        {
            _serialPort?.DiscardInBuffer();
        }
        catch
        {
            // Ignorer les erreurs de purge
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  PARSING RÉPONSE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Vérifie si la réponse SL500 indique un succès (STATUS = 0x00).
    /// Format réponse : [AA] [LEN] [STATUS] [CMD] [DATA...] [BCC]
    ///                    [0]  [1]    [2]     [3]   [4..]    [last]
    /// </summary>
    private static bool IsSuccessResponse(byte[] response)
    {
        // Minimum : STX(1) + LEN(1) + STATUS(1) + CMD(1) + BCC(1) = 5
        return response.Length >= 5 && response[2] == StatusSuccess;
    }

    /// <summary>
    /// Extrait l'UID depuis la réponse ANTICOLL du SL500.
    /// Format réponse ANTICOLL : [AA] [LEN] [STATUS=00] [CMD=42] [UID0] [UID1] [UID2] [UID3] [BCC]
    /// L'UID est dans les octets [4..N-1] (entre CMD et BCC).
    /// Supporte UID 4 octets (Mifare Classic) et 7 octets (Mifare Ultralight/NTAG).
    /// </summary>
    private static string? ParseUidFromAnticollResponse(byte[] response)
    {
        // Réponse minimale : STX(1) + LEN(1) + STATUS(1) + CMD(1) + UID(4) + BCC(1) = 8
        if (response.Length < 8)
            return null;

        var len = response[1];
        // UID bytes = de l'index 4 à (2 + len - 1) exclus le BCC
        // Index 0=STX, 1=LEN, 2=STATUS, 3=CMD, 4...(2+len-2)=DATA, (2+len-1)=BCC
        var uidStart = 4;
        var uidEnd = 2 + len - 1; // Exclure BCC
        var uidLength = uidEnd - uidStart;

        if (uidLength is not (4 or 7))
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SL500] UID longueur inattendue: {uidLength} octets");
            // Accepter quand même si > 0
            if (uidLength <= 0)
                return null;
        }

        // Convertir en hex uppercase séparé par ":"
        var uidBytes = new byte[uidLength];
        Array.Copy(response, uidStart, uidBytes, 0, uidLength);

        return BitConverter.ToString(uidBytes).Replace("-", "");
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

        System.Diagnostics.Debug.WriteLine($"[SL500] UID détecté: {uid}");
        UidReceived?.Invoke(this, uid);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
