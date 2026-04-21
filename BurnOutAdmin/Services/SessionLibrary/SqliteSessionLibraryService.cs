using System.Text.Json;
using BurnOutAdmin.Models.Program;
using SQLite;

namespace BurnOutAdmin.Services.SessionLibrary;

public class SqliteSessionLibraryService : ISessionLibraryService
{
    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    // ── Initialisation ────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "burnout_sessions.db");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<SavedSessionEntry>();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ── Séances ───────────────────────────────────────────────────

    public async Task<List<SavedSessionEntry>> GetAllSessionsAsync()
    {
        await InitializeAsync();
        return await _db!.Table<SavedSessionEntry>()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<SavedSessionEntry> SaveSessionAsync(string name, string description, SessionModel session)
    {
        await InitializeAsync();

        var exerciseCount = session.Categories
            .SelectMany(c => c.SubCategories)
            .SelectMany(sc => sc.Exercises)
            .Count();

        var entry = new SavedSessionEntry
        {
            Name = name,
            Description = description,
            DataJson = JsonSerializer.Serialize(session),
            ExerciseCount = exerciseCount,
            CategoryCount = session.Categories.Count,
            CreatedAt = DateTime.UtcNow
        };

        await _db!.InsertAsync(entry);
        return entry;
    }

    public async Task DeleteSessionAsync(int id)
    {
        await InitializeAsync();
        await _db!.DeleteAsync<SavedSessionEntry>(id);
    }
}
