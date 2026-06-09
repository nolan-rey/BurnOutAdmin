using BurnOutAdmin.Models.Program;

namespace BurnOutAdmin.Services.SessionLibrary;

public interface ISessionLibraryService
{
    Task InitializeAsync();

    // ── Séances ───────────────────────────────────────────────────
    Task<List<SavedSessionEntry>> GetAllSessionsAsync();
    Task<SavedSessionEntry> SaveSessionAsync(string name, string description, SessionModel session);
    Task UpdateSessionAsync(int id, string name, string description, SessionModel session);
    Task DeleteSessionAsync(int id);

    /// <summary>
    /// Charge un <see cref="SessionModel"/> complet pour l'édition / visualisation.
    /// Retourne null si la séance n'existe pas ou est inaccessible.
    /// </summary>
    Task<SessionModel?> LoadSessionModelAsync(int builderId);
}
