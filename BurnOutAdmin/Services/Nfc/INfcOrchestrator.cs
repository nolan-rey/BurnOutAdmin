using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services.Nfc;

/// <summary>
/// Orchestrateur central NFC.
/// Coordonne le lecteur RFID, le broker MQTT et la persistance des logs.
/// </summary>
public interface INfcOrchestrator : IDisposable
{
    /// <summary>Déclenché à chaque création ou mise à jour d'un log NFC.</summary>
    event EventHandler<NfcLog>? LogUpdated;

    /// <summary>Démarre l'orchestrateur (lecteur RFID + MQTT).</summary>
    Task StartAsync();

    /// <summary>Arrête l'orchestrateur proprement.</summary>
    Task StopAsync();

    /// <summary>Indique si le lecteur RFID est connecté.</summary>
    bool IsReaderConnected { get; }

    /// <summary>Indique si le client MQTT est connecté.</summary>
    bool IsMqttConnected { get; }

    /// <summary>Indique si le mode Bind est actif (association carte → client).</summary>
    bool IsBindMode { get; }

    /// <summary>Active le mode Bind pour associer le prochain badge scanné au client spécifié.</summary>
    /// <param name="clientId">Id du client cible.</param>
    /// <param name="clientName">Nom complet du client (pour le log).</param>
    void StartBindMode(int clientId, string clientName);

    /// <summary>Annule le mode Bind en cours.</summary>
    void CancelBindMode();
}
