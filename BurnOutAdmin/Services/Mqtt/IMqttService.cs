namespace BurnOutAdmin.Services.Mqtt;

/// <summary>
/// Abstraction du client MQTT.
/// Gère la connexion au broker, la publication et la souscription aux topics.
/// </summary>
public interface IMqttService : IDisposable
{
    /// <summary>Déclenché à la réception d'un message sur un topic souscrit.</summary>
    event EventHandler<MqttMessageEventArgs>? MessageReceived;

    /// <summary>Connecte le client au broker MQTT.</summary>
    Task ConnectAsync();

    /// <summary>Déconnecte proprement le client du broker.</summary>
    Task DisconnectAsync();

    /// <summary>Publie un message JSON sur un topic.</summary>
    Task PublishAsync(string topic, string payload);

    /// <summary>Souscrit à un topic MQTT.</summary>
    Task SubscribeAsync(string topic);

    /// <summary>Indique si le client est connecté au broker.</summary>
    bool IsConnected { get; }
}

/// <summary>Arguments d'événement pour un message MQTT reçu.</summary>
public class MqttMessageEventArgs : EventArgs
{
    /// <summary>Topic du message.</summary>
    public string Topic { get; }

    /// <summary>Payload JSON du message.</summary>
    public string Payload { get; }

    public MqttMessageEventArgs(string topic, string payload)
    {
        Topic = topic;
        Payload = payload;
    }
}
