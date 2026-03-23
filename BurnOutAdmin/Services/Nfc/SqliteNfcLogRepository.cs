using BurnOutAdmin.Models;
using SQLite;

namespace BurnOutAdmin.Services.Nfc;

/// <summary>
/// Implémentation SQLite du dépôt de logs NFC.
/// Base de données locale, thread-safe via SQLiteAsyncConnection.
/// Initialisation automatique au premier appel.
/// </summary>
public class SqliteNfcLogRepository : INfcLogRepository
{
    private SQLiteAsyncConnection? _db;
    private readonly string _dbPath;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public SqliteNfcLogRepository()
    {
        _dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "burnout_nfc_logs.db");
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _db = new SQLiteAsyncConnection(_dbPath);
            await _db.CreateTableAsync<NfcLog>();

            // Créer un index sur TimestampUtc pour les requêtes par date
            await _db.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_nfclog_timestamp ON NfcLog(TimestampUtc DESC)");

            // Créer un index sur EventId pour la corrélation MQTT
            await _db.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_nfclog_eventid ON NfcLog(EventId)");

            _initialized = true;
            System.Diagnostics.Debug.WriteLine($"[NfcLogRepo] Base initialisée : {_dbPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NfcLogRepo] Erreur init: {ex.Message}");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(NfcLog log)
    {
        await EnsureInitializedAsync();
        await _db!.InsertAsync(log);
        System.Diagnostics.Debug.WriteLine($"[NfcLogRepo] Log ajouté: EventId={log.EventId}, Uid={log.Uid}");
    }

    /// <inheritdoc />
    public async Task UpdateAsync(NfcLog log)
    {
        await EnsureInitializedAsync();
        await _db!.UpdateAsync(log);
        System.Diagnostics.Debug.WriteLine($"[NfcLogRepo] Log mis à jour: EventId={log.EventId}, Result={log.Result}");
    }

    /// <inheritdoc />
    public async Task<List<NfcLog>> GetLatestAsync(int count = 100)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<NfcLog>()
            .OrderByDescending(l => l.TimestampUtc)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<NfcLog>> GetTodayLogsAsync()
    {
        await EnsureInitializedAsync();
        var todayUtc = DateTime.UtcNow.Date;
        return await _db!.Table<NfcLog>()
            .Where(l => l.TimestampUtc >= todayUtc)
            .OrderByDescending(l => l.TimestampUtc)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetTodayAccessCountAsync()
    {
        await EnsureInitializedAsync();
        var todayUtc = DateTime.UtcNow.Date;
        return await _db!.Table<NfcLog>()
            .Where(l => l.TimestampUtc >= todayUtc)
            .CountAsync();
    }

    /// <inheritdoc />
    public async Task<NfcLog?> GetByEventIdAsync(string eventId)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<NfcLog>()
            .FirstOrDefaultAsync(l => l.EventId == eventId);
    }

    /// <summary>Garantit que la DB est initialisée avant toute opération.</summary>
    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
            await InitializeAsync();
    }
}
