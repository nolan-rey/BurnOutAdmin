namespace BurnOutAdmin;

/// <summary>
/// Centralise les paramètres de configuration de l'application.
/// Modifier ces valeurs pour adapter l'environnement (dev, prod, etc.).
/// </summary>
public static class AppConfiguration
{
    // ── API REST ─────────────────────────────────────────────────
    /// <summary>URL de base de l'API CallOfPhoenix.</summary>
    public const string ApiBaseUrl = "http://apiburnout.duckdns.org/";

    // ── MQTT ────────────────────────────────────────────────────
    /// <summary>Adresse IP du broker MQTT (Raspberry Pi sur le réseau local).</summary>
    public const string MqttBrokerHost = "172.31.254.200";

    /// <summary>Port du broker MQTT.</summary>
    public const int MqttBrokerPort = 1883;
}
