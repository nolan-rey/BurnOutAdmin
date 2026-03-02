using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace BurnOutAdmin.Services.Mqtt;

/// <summary>
/// Implémentation du client MQTT basée sur MQTTnet.
/// Gère la connexion, la reconnexion automatique, la publication et la souscription.
/// Thread-safe via SemaphoreSlim.
/// </summary>
public class MqttService : IMqttService
{
    /// <inheritdoc />
    public event EventHandler<MqttMessageEventArgs>? MessageReceived;

    /// <inheritdoc />
    public bool IsConnected => _mqttClient?.IsConnected ?? false;

    private IMqttClient? _mqttClient;
    private MqttClientOptions? _clientOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    // --- Configuration (peut être externalisée vers un Settings service) ---
    private readonly string _broker;
    private readonly int _port;
    private readonly string _clientId;
    private readonly string? _username;
    private readonly string? _password;

    /// <summary>
    /// Crée une instance du service MQTT.
    /// </summary>
    /// <param name="broker">Adresse du broker MQTT (défaut: 172.31.254.200 — Raspberry Pi).</param>
    /// <param name="port">Port du broker (défaut: 1883).</param>
    /// <param name="username">Nom d'utilisateur (optionnel).</param>
    /// <param name="password">Mot de passe (optionnel).</param>
    public MqttService(
        string broker = "172.31.254.200",
        int port = 1883,
        string? username = null,
        string? password = null)
    {
        _broker = broker;
        _port = port;
        _clientId = $"burnout-admin-{Guid.NewGuid():N}";
        _username = username;
        _password = password;
    }

    /// <inheritdoc />
    public async Task ConnectAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_mqttClient is { IsConnected: true })
                return;

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // Construire les options de connexion
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(_broker, _port)
                .WithClientId(_clientId)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .WithTimeout(TimeSpan.FromSeconds(10));

            // Authentification optionnelle
            if (!string.IsNullOrEmpty(_username))
                optionsBuilder.WithCredentials(_username, _password ?? string.Empty);

            _clientOptions = optionsBuilder.Build();

            // Handler de réception de messages
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            // Handler de déconnexion avec reconnexion automatique
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

            await _mqttClient.ConnectAsync(_clientOptions);

            // Souscrire au topic RFID (Raspberry Pi)
            await SubscribeInternalAsync("rfid/access");

            System.Diagnostics.Debug.WriteLine($"[MQTT] Connecté au broker {_broker}:{_port} (client: {_clientId})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MQTT] Erreur de connexion: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_mqttClient is { IsConnected: true })
            {
                // Retirer les handlers pour éviter la reconnexion automatique
                _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
                await _mqttClient.DisconnectAsync();
                System.Diagnostics.Debug.WriteLine("[MQTT] Déconnecté proprement.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MQTT] Erreur déconnexion: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync(string topic, string payload)
    {
        if (_mqttClient is not { IsConnected: true })
        {
            System.Diagnostics.Debug.WriteLine($"[MQTT] Impossible de publier sur '{topic}': non connecté.");
            return;
        }

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.PublishAsync(message);
            System.Diagnostics.Debug.WriteLine($"[MQTT] Publié sur '{topic}': {payload[..Math.Min(payload.Length, 100)]}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MQTT] Erreur publication '{topic}': {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(string topic)
    {
        await _lock.WaitAsync();
        try
        {
            await SubscribeInternalAsync(topic);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Souscription interne sans verrouillage (appelée depuis un contexte déjà verrouillé).</summary>
    private async Task SubscribeInternalAsync(string topic)
    {
        if (_mqttClient is not { IsConnected: true })
            return;

        try
        {
            var options = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
                .Build();

            await _mqttClient.SubscribeAsync(options);
            System.Diagnostics.Debug.WriteLine($"[MQTT] Souscrit à '{topic}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MQTT] Erreur souscription '{topic}': {ex.Message}");
        }
    }

    /// <summary>Handler de réception de messages MQTT.</summary>
    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            System.Diagnostics.Debug.WriteLine($"[MQTT] Message reçu sur '{topic}': {payload[..Math.Min(payload.Length, 100)]}");

            MessageReceived?.Invoke(this, new MqttMessageEventArgs(topic, payload));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MQTT] Erreur traitement message: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    /// <summary>Handler de déconnexion : tente une reconnexion automatique après 5 secondes.</summary>
    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[MQTT] Déconnecté. Tentative de reconnexion dans 5s...");

        await Task.Delay(TimeSpan.FromSeconds(5));

        if (_disposed || _clientOptions is null)
            return;

        try
        {
            await _lock.WaitAsync();
            try
            {
                if (_mqttClient is { IsConnected: false })
                {
                    await _mqttClient.ConnectAsync(_clientOptions);
                    System.Diagnostics.Debug.WriteLine("[MQTT] Reconnexion réussie.");

                    // Re-souscrire au topic RFID
                    await SubscribeInternalAsync("rfid/access");
                }
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MQTT] Échec reconnexion: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_mqttClient is { IsConnected: true })
            {
                _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
                _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
            }
            _mqttClient?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MQTT] Erreur dispose: {ex.Message}");
        }

        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
