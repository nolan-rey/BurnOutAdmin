// TODO API : endpoints à implémenter côté Slim :
//   GET /clients/{id}/history          → liste des séances assignées (réalisées + à venir)
//   GET /clients/{id}/performances/{builderId} → performances d'un client pour une séance template
//   GET /clients/{id}/session-feedback?date={iso}&assignation={uuid?} → feedback RPE de fin de séance
//
// Pour l'instant : implémentation STUB (retours vides + logs).
// Les vraies données seront branchées quand l'API PHP exposera ces routes.

using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation école de <see cref="IClientHistoryService"/> — STUB.
/// Retourne des listes vides en attendant les endpoints API correspondants.
/// </summary>
public class ApiClientHistoryService : IClientHistoryService
{
    private readonly ApiHttpClient _api;

    public ApiClientHistoryService(ApiHttpClient api) => _api = api;

    public Task<List<ClientSeanceItem>> GetClientSeancesAsync(int clientId)
    {
        // TODO API : GET /clients/{clientId}/history
        Console.WriteLine($"[ApiClientHistoryService] GetClientSeancesAsync({clientId}) — STUB (endpoint à implémenter)");
        return Task.FromResult(new List<ClientSeanceItem>());
    }

    public Task<List<ClientPerformanceItem>> GetPerformancesForSessionAsync(int clientId, int builderId)
    {
        // TODO API : GET /clients/{clientId}/performances/{builderId}
        Console.WriteLine($"[ApiClientHistoryService] GetPerformancesForSessionAsync({clientId}, {builderId}) — STUB");
        return Task.FromResult(new List<ClientPerformanceItem>());
    }

    public Task<ClientSessionFeedback?> GetSessionFeedbackAsync(
        int clientId,
        DateTime dateRealisation,
        string? idAssignation = null)
    {
        // TODO API : GET /clients/{clientId}/session-feedback?date={dateRealisation:O}&assignation={idAssignation?}
        Console.WriteLine($"[ApiClientHistoryService] GetSessionFeedbackAsync({clientId}, {dateRealisation:O}) — STUB");
        return Task.FromResult<ClientSessionFeedback?>(null);
    }
}
