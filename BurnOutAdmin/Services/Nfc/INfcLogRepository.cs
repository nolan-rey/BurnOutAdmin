using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services.Nfc;

/// <summary>
/// Abstraction du dépôt de logs NFC.
/// Gère la persistance des événements NFC en SQLite.
/// </summary>
public interface INfcLogRepository
{
    /// <summary>Initialise la base de données et crée la table si nécessaire.</summary>
    Task InitializeAsync();

    /// <summary>Ajoute un nouveau log NFC.</summary>
    Task AddAsync(NfcLog log);

    /// <summary>Met à jour un log existant (ex: après réception du résultat MQTT).</summary>
    Task UpdateAsync(NfcLog log);

    /// <summary>Récupère les N derniers logs, triés par date décroissante.</summary>
    Task<List<NfcLog>> GetLatestAsync(int count = 100);

    /// <summary>Récupère les logs du jour.</summary>
    Task<List<NfcLog>> GetTodayLogsAsync();

    /// <summary>Récupère le nombre d'accès du jour.</summary>
    Task<int> GetTodayAccessCountAsync();

    /// <summary>Recherche un log par son EventId (corrélation MQTT).</summary>
    Task<NfcLog?> GetByEventIdAsync(string eventId);
}
