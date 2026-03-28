namespace BurnOutAdmin;

/// <summary>
/// Centralise les paramètres de configuration de l'application.
/// Modifier ces valeurs pour adapter l'environnement (dev, prod, etc.).
/// </summary>
public static class AppConfiguration
{
    // ── MQTT ────────────────────────────────────────────────────
    /// <summary>Adresse IP du broker MQTT (Raspberry Pi sur le réseau local).</summary>
    public const string MqttBrokerHost = "172.31.254.200";

    /// <summary>Port du broker MQTT.</summary>
    public const int MqttBrokerPort = 1883;
}
