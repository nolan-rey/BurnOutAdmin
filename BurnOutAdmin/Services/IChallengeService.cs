using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface IChallengeService
{
    Task<List<Challenge>> GetChallengesAsync();
    Task<Challenge?> GetChallengeByIdAsync(int id);
    Task<int> GetActiveChallengesCountAsync();
}
