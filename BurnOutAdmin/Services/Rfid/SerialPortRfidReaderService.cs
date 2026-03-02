#if WINDOWS
using System.IO.Ports;
#endif
using System.Text;
using System.Text.RegularExpressions;

namespace BurnOutAdmin.Services.Rfid;

/// <summary>
/// Implémentation du lecteur RFID via port série COM (Windows uniquement).
/// Lecture continue via ReadExisting() + buffer interne.
/// Gère les lecteurs avec ou sans retour ligne.
/// Anti-rebond : ignore le même UID si scanné dans les 1.5 secondes.
/// </summary>
public partial class SerialPortRfidReaderService : IRfidReaderService
{
    /// <inheritdoc />
    public event EventHandler<string>? UidReceived;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    private readonly string _portName;
    private readonly int _baudRate;

    // --- Buffer interne pour accumuler les données série ---
    private readonly StringBuilder _buffer = new();
    private readonly object _bufferLock = new();

    // --- Anti-rebond ---
    private string? _lastUid;
    private DateTime _lastScanTime = DateTime.MinValue;
    private const int DebounceMs = 1500;

    // --- Regex pour extraire un UID hex (8 ou 10 caractères hex contigus) ---
    [GeneratedRegex(@"[0-9A-Fa-f]{8,10}", RegexOptions.Compiled)]
    private static partial Regex HexUidRegex();

#if WINDOWS
    private SerialPort? _serialPort;
#endif

    /// <summary>
    /// Crée une instance du service de lecture RFID série.
    /// </summary>
    /// <param name="portName">Nom du port COM (ex: "COM3"). Par défaut "COM3".</param>
    /// <param name="baudRate">Vitesse de communication. Par défaut 9600.</param>
    public SerialPortRfidReaderService(string portName = "COM3", int baudRate = 9600)
    {
        _portName = portName;
        _baudRate = baudRate;
    }

    /// <inheritdoc />
    public Task StartAsync()
    {
#if WINDOWS
        try
        {
            if (_serialPort is { IsOpen: true })
            {
                System.Diagnostics.Debug.WriteLine("[SerialRfid] Port déjà ouvert.");
                return Task.CompletedTask;
            }

            _serialPort = new SerialPort(_portName, _baudRate)
            {
                DtrEnable = true,
                RtsEnable = true,
                Encoding = Encoding.ASCII
            };

            _serialPort.DataReceived += OnDataReceived;
            _serialPort.ErrorReceived += OnErrorReceived;
            _serialPort.Open();
            IsConnected = true;

            System.Diagnostics.Debug.WriteLine($"[SerialRfid] Port {_portName} ouvert à {_baudRate} bauds (lecture continue).");
        }
        catch (Exception ex)
        {
            IsConnected = false;
            System.Diagnostics.Debug.WriteLine($"[SerialRfid] Erreur ouverture port {_portName}: {ex.Message}");
        }
#else
        System.Diagnostics.Debug.WriteLine("[SerialRfid] Port série non supporté sur cette plateforme.");
#endif
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
#if WINDOWS
        try
        {
            if (_serialPort is { IsOpen: true })
            {
                _serialPort.DataReceived -= OnDataReceived;
                _serialPort.ErrorReceived -= OnErrorReceived;
                _serialPort.Close();
                System.Diagnostics.Debug.WriteLine($"[SerialRfid] Port {_portName} fermé.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SerialRfid] Erreur fermeture port: {ex.Message}");
        }
        finally
        {
            _serialPort?.Dispose();
            _serialPort = null;
            IsConnected = false;

            lock (_bufferLock)
            {
                _buffer.Clear();
            }
        }
#else
        IsConnected = false;
#endif
        return Task.CompletedTask;
    }

#if WINDOWS
    /// <summary>
    /// Callback déclenché à chaque réception de données sur le port série.
    /// Utilise ReadExisting() pour ne jamais bloquer, même sans retour ligne.
    /// Accumule dans un buffer et extrait les UIDs hex valides.
    /// </summary>
    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serialPort is not { IsOpen: true })
                return;

            var raw = _serialPort.ReadExisting();
            if (string.IsNullOrEmpty(raw))
                return;

            lock (_bufferLock)
            {
                _buffer.Append(raw);

                // Tenter d'extraire un UID du buffer
                var bufferContent = _buffer.ToString();

                // Si le buffer contient un retour ligne ou chariot, traiter chaque ligne
                if (bufferContent.Contains('\n') || bufferContent.Contains('\r'))
                {
                    var lines = bufferContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    _buffer.Clear();

                    foreach (var line in lines)
                    {
                        TryEmitUid(line.Trim());
                    }
                }
                else if (bufferContent.Length >= 8)
                {
                    // Pas de retour ligne — le lecteur envoie l'UID brut
                    // Essayer d'extraire un UID hex du buffer
                    var match = HexUidRegex().Match(bufferContent);
                    if (match.Success)
                    {
                        _buffer.Clear();
                        TryEmitUid(match.Value);
                    }
                    else if (bufferContent.Length > 20)
                    {
                        // Buffer trop long sans UID valide → nettoyer pour éviter la fuite mémoire
                        System.Diagnostics.Debug.WriteLine($"[SerialRfid] Buffer purgé (données non-hex): {bufferContent[..20]}...");
                        _buffer.Clear();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SerialRfid] Erreur lecture: {ex.Message}");
        }
    }

    /// <summary>
    /// Callback en cas d'erreur sur le port série (déconnexion, parité, etc.).
    /// </summary>
    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[SerialRfid] Erreur port série: {e.EventType}");
        IsConnected = _serialPort is { IsOpen: true };
    }
#endif

    /// <summary>
    /// Nettoie les caractères non-hex et valide/émet l'UID.
    /// Applique l'anti-rebond (même UID &lt; 1.5s → ignoré).
    /// </summary>
    private void TryEmitUid(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        // Nettoyer : garder uniquement les caractères hex
        var cleaned = HexUidRegex().Match(raw);
        if (!cleaned.Success)
        {
            System.Diagnostics.Debug.WriteLine($"[SerialRfid] Données ignorées (non-hex): {raw}");
            return;
        }

        var uid = cleaned.Value.ToUpperInvariant();

        // Anti-rebond : ignorer si même UID dans les 1.5 secondes
        var now = DateTime.UtcNow;
        if (uid == _lastUid && (now - _lastScanTime).TotalMilliseconds < DebounceMs)
        {
            System.Diagnostics.Debug.WriteLine($"[SerialRfid] Anti-rebond: {uid} ignoré ({(now - _lastScanTime).TotalMilliseconds:F0}ms)");
            return;
        }

        _lastUid = uid;
        _lastScanTime = now;

        System.Diagnostics.Debug.WriteLine($"[SerialRfid] UID valide: {uid}");
        UidReceived?.Invoke(this, uid);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
