// TODO API : endpoint à implémenter côté Slim :
//   GET /rank-badges → { "success": true, "data": [ { tier, division, totem_slug, url } ] }
//
// Charge la table en cache mémoire à la première demande pour éviter N requêtes.

using System.Text.Json.Serialization;
using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation école de <see cref="IRankBadgeService"/>.
/// Précharge la table <c>rank_badges</c> (~224 lignes) en mémoire.
/// </summary>
public class ApiRankBadgeService : IRankBadgeService
{
    private readonly ApiHttpClient _api;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Dictionary<string, string>? _cache;

    public ApiRankBadgeService(ApiHttpClient api) => _api = api;

    public async Task<string?> GetBadgeUrlAsync(ClientRank rank, string totemSlug)
    {
        if (string.IsNullOrWhiteSpace(totemSlug)) return null;

        var cache = await EnsureLoadedAsync();
        var key = BuildKey(rank.Tier, rank.Division, totemSlug);
        if (cache.TryGetValue(key, out var url) && !string.IsNullOrEmpty(url))
            return BadgeUrlBuilder.Normalize(url);
        return null;
    }

    private async Task<Dictionary<string, string>> EnsureLoadedAsync()
    {
        if (_cache is { } existing) return existing;
        await _loadLock.WaitAsync();
        try
        {
            if (_cache is { } afterWait) return afterWait;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var response = await _api.GetAsync<RankBadgeListResponseDto>("/rank-badges");
                if (response?.Data is { Count: > 0 })
                {
                    foreach (var r in response.Data)
                    {
                        if (string.IsNullOrWhiteSpace(r.Tier)
                            || string.IsNullOrWhiteSpace(r.TotemSlug)
                            || string.IsNullOrEmpty(r.Url))
                            continue;
                        dict[BuildKey(r.Tier, r.Division, r.TotemSlug)] = r.Url;
                    }
                }
            }
            catch (Exception inner)
            {
                Console.WriteLine($"[ApiRankBadgeService] Endpoint /rank-badges indisponible — STUB cache vide: {inner.Message}");
            }

            _cache = dict;
            Console.WriteLine($"[ApiRankBadgeService] {dict.Count} badge(s) chargé(s) en cache");
            return dict;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private static string BuildKey(string tier, int division, string slug)
        => $"{tier.ToLowerInvariant()}|{division}|{slug.ToLowerInvariant()}";

    private sealed class RankBadgeListResponseDto
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")]    public List<RankBadgeRow> Data { get; set; } = new();
    }

    private sealed class RankBadgeRow
    {
        [JsonPropertyName("tier")]       public string Tier      { get; set; } = string.Empty;
        [JsonPropertyName("division")]   public int    Division  { get; set; }
        [JsonPropertyName("totem_slug")] public string TotemSlug { get; set; } = string.Empty;
        [JsonPropertyName("url")]        public string Url       { get; set; } = string.Empty;
    }
}
