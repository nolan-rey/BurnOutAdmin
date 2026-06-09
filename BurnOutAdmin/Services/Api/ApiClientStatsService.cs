// TODO API : endpoint à implémenter côté Slim :
//   GET /clients/{id}/stats → { "success": true, "data": { "points": int, "seances_total": int, "streak": int } }

using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation école de <see cref="IClientStatsService"/>.
/// Attend le format <c>{ success, data: { points } }</c>.
/// Retourne 0 si l'endpoint n'existe pas (échec silencieux).
/// </summary>
public class ApiClientStatsService : IClientStatsService
{
    private readonly ApiHttpClient _api;

    public ApiClientStatsService(ApiHttpClient api) => _api = api;

    public async Task<int> GetPointsAsync(int clientId)
    {
        if (clientId <= 0) return 0;
        try
        {
            var response = await _api.GetAsync<ClientStatsResponseDto>($"/clients/{clientId}/stats");
            return response?.Data?.Points ?? 0;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiClientStatsService] GetPoints({clientId}) — STUB ou erreur: {ex.Message}");
            return 0;
        }
    }

    private sealed class ClientStatsResponseDto
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")]    public ClientStatsRow? Data { get; set; }
    }

    private sealed class ClientStatsRow
    {
        [JsonPropertyName("points")] public int Points { get; set; }
    }
}
