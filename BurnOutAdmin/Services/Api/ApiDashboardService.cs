using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="IDashboardService"/> utilisant l'API REST BurnOut.
///
/// Endpoint utilisé :
///   GET /dashboard/stats → agrégat complet (stats + derniers accès)
///
/// Fallback : si l'API échoue, agrège depuis les autres services.
/// </summary>
public class ApiDashboardService : IDashboardService
{
    private readonly ApiHttpClient       _api;
    private readonly IClientService      _clientService;
    private readonly INfcService         _nfcService;
    private readonly IChallengeService   _challengeService;
    private readonly IProgrammeService   _programmeService;

    public ApiDashboardService(
        ApiHttpClient     api,
        IClientService    clientService,
        INfcService       nfcService,
        IChallengeService challengeService,
        IProgrammeService programmeService)
    {
        _api              = api;
        _clientService    = clientService;
        _nfcService       = nfcService;
        _challengeService = challengeService;
        _programmeService = programmeService;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        try
        {
            var response = await _api.GetAsync<DashboardResponseDto>("/dashboard/stats");

            if (response?.Success == true && response.Data is not null)
                return MapToStats(response.Data);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiDashboardService] API call failed, using fallback: {ex.Message}");
        }

        // Fallback : agrégation locale si l'endpoint /dashboard/stats n'existe pas encore
        return await BuildFallbackStatsAsync();
    }

    // ── Mapping depuis API ────────────────────────────────────────

    private static DashboardStats MapToStats(DashboardDataDto data) => new()
    {
        ActiveClientsCount          = data.ClientsActifs,
        TodayNfcAccessCount         = data.AccesAujourdHui,
        AlertsCount                 = data.AlertesActives,
        SystemStatus                = SystemStatus.Online,
        ActiveChallengesCount       = data.ChallengesActifs,
        ActiveProgrammesCount       = data.ProgrammesActifs,
        ExpiringSubscriptionsCount  = data.AbonnementsExpirantBientot,
        RecentNfcAccesses           = data.DerniersAcces.Select(MapToEntry).ToList()
    };

    private static DashboardNfcEntry MapToEntry(DashboardAccesDto dto)
    {
        var ts          = TryParseDate(dto.TimestampUtc);
        var isAuthorized = string.Equals(dto.Resultat, "autorise",
            StringComparison.OrdinalIgnoreCase);

        return new DashboardNfcEntry
        {
            Time         = ts.HasValue
                ? ts.Value.ToLocalTime().ToString("HH:mm")
                : "--:--",
            ClientName   = dto.NomClient,
            Door         = dto.Porte ?? "Entrée principale",
            IsAuthorized = isAuthorized
        };
    }

    // ── Fallback si /dashboard/stats n'est pas encore implémenté ─

    private async Task<DashboardStats> BuildFallbackStatsAsync()
    {
        var clients            = await _clientService.GetClientsAsync();
        var activeClientsCount = clients.Count(c => c.Status == "Actif");
        var todayCount         = await _nfcService.GetTodayAccessCountAsync();
        var activeChallenges   = await _challengeService.GetActiveChallengesCountAsync();
        var activeProgrammes   = await _programmeService.GetActiveProgrammesCountAsync();

        var expiringCount = clients.Count(c =>
            c.Subscription is not null &&
            c.Subscription.EndDate > DateTime.Today &&
            c.Subscription.EndDate <= DateTime.Today.AddDays(30));

        var recentLogs = await _nfcService.GetTodayLogsAsync();
        var recent = recentLogs.Take(10).Select(l => new DashboardNfcEntry
        {
            Time         = l.TimestampUtc.ToLocalTime().ToString("HH:mm"),
            ClientName   = l.ClientName,
            Door         = l.Door ?? "Entrée principale",
            IsAuthorized = l.IsAuthorized
        }).ToList();

        var alertsCount = recentLogs.Count(l => !l.IsAuthorized && !l.IsPending);

        return new DashboardStats
        {
            ActiveClientsCount         = activeClientsCount,
            TodayNfcAccessCount        = todayCount,
            AlertsCount                = alertsCount,
            SystemStatus               = SystemStatus.Online,
            ActiveChallengesCount      = activeChallenges,
            ActiveProgrammesCount      = activeProgrammes,
            ExpiringSubscriptionsCount = expiringCount,
            RecentNfcAccesses          = recent
        };
    }

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
