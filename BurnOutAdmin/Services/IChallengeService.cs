using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface IChallengeService
{
    Task InitializeAsync();

    // ── CRUD Challenges ───────────────────────────────────────────
    Task<List<Challenge>> GetChallengesAsync();
    Task<Challenge?> GetChallengeByIdAsync(int id);
    Task<Challenge> CreateChallengeAsync(Challenge challenge);
    Task UpdateChallengeAsync(Challenge challenge);
    Task DeleteChallengeAsync(int id);
    Task<int> GetActiveChallengesCountAsync();

    // ── Participants ──────────────────────────────────────────────
    Task<List<ChallengeParticipant>> GetParticipantsAsync(int challengeId);
    Task AddParticipantAsync(int challengeId, int clientId, string clientName);
    Task RemoveParticipantAsync(int participantId);
    Task UpdateProgressAsync(int participantId, double newValue);
}
