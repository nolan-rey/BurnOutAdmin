using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public class MockDashboardService : IDashboardService
{
    private readonly IClientService _clientService;
    private readonly INfcService _nfcService;
    private readonly IChallengeService _challengeService;
    private readonly IProgrammeService _programmeService;

    public MockDashboardService(
        IClientService clientService,
        INfcService nfcService,
        IChallengeService challengeService,
        IProgrammeService programmeService)
    {
        _clientService = clientService;
        _nfcService = nfcService;
        _challengeService = challengeService;
        _programmeService = programmeService;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var clients = await _clientService.GetClientsAsync();
        var activeClientsCount = clients.Count(c => c.Status == "Actif");
        var todayAccessCount = await _nfcService.GetTodayAccessCountAsync();
        var activeChallengesCount = await _challengeService.GetActiveChallengesCountAsync();
        var activeProgrammesCount = await _programmeService.GetActiveProgrammesCountAsync();

        var expiringCount = clients.Count(c =>
            c.Subscription != null &&
            c.Subscription.EndDate > DateTime.Today &&
            c.Subscription.EndDate <= DateTime.Today.AddDays(30));

        var recentAccesses = new List<DashboardNfcEntry>
        {
            new() { Time = "18:42", ClientName = "Jean Dupont",    Door = "Entrée principale", IsAuthorized = true  },
            new() { Time = "18:35", ClientName = "Sophie Leroy",   Door = "Salle cardio",      IsAuthorized = true  },
            new() { Time = "18:21", ClientName = "Hugo Richard",   Door = "Entrée principale", IsAuthorized = true  },
            new() { Time = "17:58", ClientName = "Alice Martin",   Door = "Entrée principale", IsAuthorized = false },
            new() { Time = "17:44", ClientName = "Camille Girard", Door = "Salle musculation",  IsAuthorized = true  },
        };


        return new DashboardStats
        {
            ActiveClientsCount = activeClientsCount,
            TodayNfcAccessCount = todayAccessCount,
            AlertsCount = 2,
            SystemStatus = SystemStatus.Online,
            ActiveChallengesCount = activeChallengesCount,
            ActiveProgrammesCount = activeProgrammesCount,
            ExpiringSubscriptionsCount = expiringCount,
            RecentNfcAccesses = recentAccesses
        };
    }
}
