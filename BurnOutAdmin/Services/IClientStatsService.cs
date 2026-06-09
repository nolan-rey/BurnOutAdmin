namespace BurnOutAdmin.Services;

/// <summary>
/// Accès aux statistiques agrégées d'un client (points d'expérience, séances
/// totales, streak…). Utilisé notamment pour résoudre le palier de rang
/// affiché dans le badge totem.
/// </summary>
public interface IClientStatsService
{
    /// <summary>
    /// Récupère le total de points (PE) accumulés par le client. Renvoie
    /// <c>0</c> si aucune ligne n'existe (compte tout neuf) ou en cas
    /// d'erreur.
    /// </summary>
    Task<int> GetPointsAsync(int clientId);
}
