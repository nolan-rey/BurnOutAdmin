using BurnOutAdmin.Models;
using BurnOutAdmin.Services.Nfc;

namespace BurnOutAdmin.Services;

/// <summary>
/// Service NFC hybride : utilise le dépôt SQLite si disponible,
/// sinon retombe sur des données mock en mémoire.
/// Permet une transition transparente vers la persistance réelle.
/// </summary>
public class MockNfcService : INfcService
{
    private readonly INfcLogRepository? _logRepository;
    private readonly List<NfcLog> _mockLogs;
    private bool _repositoryReady;

    public MockNfcService(INfcLogRepository? logRepository = null)
    {
        _logRepository = logRepository;

        var today = DateTime.Today;
        _mockLogs = new List<NfcLog>
        {
            new NfcLog
            {
                Id = 1, EventId = Guid.NewGuid().ToString("N"),
                Uid = "04:A3:5B:12", ClientId = 1,
                ClientName = "Jean Dupont",
                Timestamp = today.AddHours(8).AddMinutes(15),
                Result = NfcAccessResult.Authorized,
                Message = "Accès autorisé - Abonnement actif",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 2, EventId = Guid.NewGuid().ToString("N"),
                Uid = "11:F2:9C:00", ClientId = 2,
                ClientName = "Alice Martin",
                Timestamp = today.AddHours(9).AddMinutes(30),
                Result = NfcAccessResult.Denied,
                Message = "Abonnement expiré",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 3, EventId = Guid.NewGuid().ToString("N"),
                Uid = "04:A3:5B:12", ClientId = 1,
                ClientName = "Jean Dupont",
                Timestamp = today.AddHours(10).AddMinutes(45),
                Result = NfcAccessResult.Authorized,
                Message = "Accès autorisé - Abonnement actif",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 4, EventId = Guid.NewGuid().ToString("N"),
                Uid = "22:B4:7D:33", ClientId = 4,
                ClientName = "Marie Leroy",
                Timestamp = today.AddHours(11).AddMinutes(20),
                Result = NfcAccessResult.Authorized,
                Message = "Accès autorisé - Abonnement actif",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 5, EventId = Guid.NewGuid().ToString("N"),
                Uid = "FF:00:11:22",
                ClientName = "Inconnu",
                Timestamp = today.AddHours(12).AddMinutes(5),
                Result = NfcAccessResult.Denied,
                Message = "Carte non reconnue",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 6, EventId = Guid.NewGuid().ToString("N"),
                Uid = "33:44:55:66", ClientId = 5,
                ClientName = "Pierre Moreau",
                Timestamp = today.AddHours(14).AddMinutes(30),
                Result = NfcAccessResult.Authorized,
                Message = "Accès autorisé - Abonnement actif",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 7, EventId = Guid.NewGuid().ToString("N"),
                Uid = "11:F2:9C:00", ClientId = 2,
                ClientName = "Alice Martin",
                Timestamp = today.AddHours(15).AddMinutes(10),
                Result = NfcAccessResult.Denied,
                Message = "Abonnement expiré",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 8, EventId = Guid.NewGuid().ToString("N"),
                Uid = "77:88:99:AA", ClientId = 8,
                ClientName = "Sophie Bernard",
                Timestamp = today.AddDays(-1).AddHours(9),
                Result = NfcAccessResult.Authorized,
                Message = "Accès autorisé - Abonnement actif",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 9, EventId = Guid.NewGuid().ToString("N"),
                Uid = "BB:CC:DD:EE", ClientId = 7,
                ClientName = "Thomas Petit",
                Timestamp = today.AddDays(-1).AddHours(16),
                Result = NfcAccessResult.Authorized,
                Message = "Accès autorisé - Abonnement actif",
                Source = "mock"
            },
            new NfcLog
            {
                Id = 10, EventId = Guid.NewGuid().ToString("N"),
                Uid = "04:A3:5B:12", ClientId = 1,
                ClientName = "Jean Dupont",
                Timestamp = today.AddDays(-2).AddHours(7).AddMinutes(45),
                Result = NfcAccessResult.Authorized,
                Message = "Accès autorisé - Abonnement actif",
                Source = "mock"
            }
        };
    }

    public async Task<List<NfcLog>> GetNfcLogsAsync()
    {
        // Tenter la lecture depuis SQLite, sinon fallback mock
        if (await TryInitRepositoryAsync())
        {
            var dbLogs = await _logRepository!.GetLatestAsync(200);
            if (dbLogs.Count > 0)
                return dbLogs;
        }

        return _mockLogs.OrderByDescending(l => l.Timestamp).ToList();
    }

    public async Task<List<NfcLog>> GetTodayLogsAsync()
    {
        if (await TryInitRepositoryAsync())
        {
            var dbLogs = await _logRepository!.GetTodayLogsAsync();
            if (dbLogs.Count > 0)
                return dbLogs;
        }

        var today = DateTime.Today;
        return _mockLogs
            .Where(l => l.Timestamp.Date == today)
            .OrderByDescending(l => l.Timestamp)
            .ToList();
    }

    public async Task<int> GetTodayAccessCountAsync()
    {
        if (await TryInitRepositoryAsync())
        {
            var count = await _logRepository!.GetTodayAccessCountAsync();
            if (count > 0)
                return count;
        }

        var today = DateTime.Today;
        return _mockLogs.Count(l => l.Timestamp.Date == today);
    }

    /// <summary>Initialise le repository SQLite si disponible.</summary>
    private async Task<bool> TryInitRepositoryAsync()
    {
        if (_logRepository is null) return false;
        if (_repositoryReady) return true;

        try
        {
            await _logRepository.InitializeAsync();
            _repositoryReady = true;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
