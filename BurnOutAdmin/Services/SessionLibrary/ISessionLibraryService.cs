using BurnOutAdmin.Models.Program;

namespace BurnOutAdmin.Services.SessionLibrary;

public interface ISessionLibraryService
{
    Task InitializeAsync();

    // ── Séances ───────────────────────────────────────────────────
    Task<List<SavedSessionEntry>> GetAllSessionsAsync();
    Task<SavedSessionEntry> SaveSessionAsync(string name, string description, SessionModel session);
    Task DeleteSessionAsync(int id);
}
