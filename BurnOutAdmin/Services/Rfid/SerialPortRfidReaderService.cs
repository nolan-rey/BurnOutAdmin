#if WINDOWS
using System.IO.Ports;
#endif

namespace BurnOutAdmin.Services.Rfid;

/// <summary>
/// Implémentation du lecteur RFID via port série COM (Windows uniquement).
/// Lit les UIDs envoyés par le lecteur RFID connecté en série.
/// Sur les plateformes non-Windows, cette classe est compilée mais inactive.
/// </summary>
public class SerialPortRfidReaderService : IRfidReaderService
{
    /// <inheritdoc />
    public event EventHandler<string>? UidReceived;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    private readonly string _portName;
    private readonly int _baudRate;
    private CancellationTokenSource? _cts;

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
                ReadTimeout = 5000,
                NewLine = "\n",
                DtrEnable = true,
                RtsEnable = true
            };

            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Open();
            IsConnected = true;

            System.Diagnostics.Debug.WriteLine($"[SerialRfid] Port {_portName} ouvert à {_baudRate} bauds.");
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
        }
#else
        IsConnected = false;
#endif
        return Task.CompletedTask;
    }

#if WINDOWS
    /// <summary>Callback déclenché à chaque réception de données sur le port série.</summary>
    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serialPort is not { IsOpen: true })
                return;

            var line = _serialPort.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(line))
                return;

            System.Diagnostics.Debug.WriteLine($"[SerialRfid] UID reçu: {line}");
            UidReceived?.Invoke(this, line);
        }
        catch (TimeoutException)
        {
            // Timeout de lecture — ignoré
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SerialRfid] Erreur lecture: {ex.Message}");
        }
    }
#endif

    /// <inheritdoc />
    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
