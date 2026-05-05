using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api.Dto;
using BurnOutAdmin.Services.Nfc;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="INfcService"/> utilisant l'API REST BurnOut.
///
/// Retourne uniquement les données cloud (API). Pas de fallback SQLite local
/// pour les données historiques — si l'endpoint n'est pas encore implémenté
/// côté API, la liste est vide (pas de données mock ou SQLite).
///
/// Note : les logs temps-réel (NFC Orchestrator) restent gérés séparément
/// via les événements INfcOrchestrator.LogUpdated et le SQLite local.
///
/// Endpoints utilisés (quand disponibles) :
///   GET /logs-acces          → historique paginé
///   GET /logs-acces/today    → logs du jour
/// </summary>
public class ApiNfcService : INfcService
{
    private readonly ApiHttpClient _api;

    public ApiNfcService(ApiHttpClient api)
    {
        _api = api;
    }

    // ── INfcService ────────────────────────────────────────────────

    public Task<List<NfcLog>> GetNfcLogsAsync()
        => FetchApiLogsAsync("/logs-acces?limit=200");

    public Task<List<NfcLog>> GetTodayLogsAsync()
        => FetchApiLogsAsync("/logs-acces/today");

    public async Task<int> GetTodayAccessCountAsync()
    {
        var logs = await GetTodayLogsAsync();
        return logs.Count;
    }

    // ── Fetch API ─────────────────────────────────────────────────

    private async Task<List<NfcLog>> FetchApiLogsAsync(string path)
    {
        // 1️⃣ Format enveloppé { success, data: [...] }
        try
        {
            var response = await _api.GetAsync<NfcLogListResponseDto>(path);
            if (response is not null)
            {
                Console.WriteLine($"[ApiNfcService] Format enveloppé OK pour {path} — {response.Data?.Count ?? 0} log(s)");
                return response.Data?.Select(MapToNfcLog).ToList() ?? [];
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex1)
        {
            Console.WriteLine($"[ApiNfcService] Format enveloppé échoué pour {path} : {ex1.Message} — essai tableau brut");
        }

        // 2️⃣ Format tableau brut [...]
        try
        {
            var list = await _api.GetAsync<List<NfcLogDto>>(path);
            Console.WriteLine($"[ApiNfcService] Format tableau brut OK pour {path} — {list?.Count ?? 0} log(s)");
            return list?.Select(MapToNfcLog).ToList() ?? [];
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiNfcService] {path} non disponible : {ex.Message}");
            return [];
        }
    }

    // ── Mapping ────────────────────────────────────────────────────

    private static NfcLog MapToNfcLog(NfcLogDto dto) => new()
    {
        Id           = dto.IdLog,
        EventId      = dto.EventId,
        Uid          = dto.UidNfc,
        ClientId     = dto.IdClient,
        ClientName   = dto.NomClient,
        Result       = MapResultat(dto.Resultat),
        Reason       = dto.Raison,
        TimestampUtc = TryParseDate(dto.TimestampUtc) ?? DateTime.UtcNow,
        Door         = dto.Porte,
        Source       = dto.Source
    };

    private static NfcAccessResult MapResultat(string s) => s switch
    {
        "autorise"   => NfcAccessResult.Authorized,
        "refuse"     => NfcAccessResult.Denied,
        "en_attente" => NfcAccessResult.Pending,
        _            => NfcAccessResult.Pending
    };

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
