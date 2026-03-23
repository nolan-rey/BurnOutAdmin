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

        return new DashboardStats
        {
            ActiveClientsCount = activeClientsCount,
            TodayNfcAccessCount = todayAccessCount,
            AlertsCount = 2,
            SystemStatus = SystemStatus.Online,
            ActiveChallengesCount = activeChallengesCount,
            ActiveProgrammesCount = activeProgrammesCount
        };
    }
}
