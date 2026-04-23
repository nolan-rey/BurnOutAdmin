using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public class MockChallengeService : IChallengeService
{
    private readonly List<Challenge> _mockChallenges;

    public MockChallengeService()
    {
        _mockChallenges = new List<Challenge>
        {
            new Challenge
            {
                Id = 1,
                Name = "Défi 30 jours Cardio",
                Description = "Complétez 30 séances de cardio en 30 jours",
                Type = ChallengeType.Cardio,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(20),
                TargetGoal = 30,
                GoalUnit = "séances",
                Status = ChallengeStatus.Active,
                RewardDescription = "Badge Cardio Master + 1 mois offert",
                CreatedAt = DateTime.Now.AddDays(-15)
            },
            new Challenge
            {
                Id = 2,
                Name = "Challenge Force Maximale",
                Description = "Augmentez votre 1RM de 10% sur les exercices principaux",
                Type = ChallengeType.Force,
                StartDate = DateTime.Now.AddDays(-5),
                EndDate = DateTime.Now.AddDays(55),
                TargetGoal = 10,
                GoalUnit = "% d'augmentation",
                Status = ChallengeStatus.Active,
                RewardDescription = "T-shirt exclusif BurnOut",
                CreatedAt = DateTime.Now.AddDays(-8)
            },
            new Challenge
            {
                Id = 3,
                Name = "Marathon Février",
                Description = "Courez 100km au total pendant le mois de février",
                Type = ChallengeType.Endurance,
                StartDate = DateTime.Now.AddMonths(1),
                EndDate = DateTime.Now.AddMonths(2),
                TargetGoal = 100,
                GoalUnit = "km",
                Status = ChallengeStatus.Upcoming,
                RewardDescription = "Médaille Marathon + Réduction 20%",
                CreatedAt = DateTime.Now.AddDays(-2)
            },
            new Challenge
            {
                Id = 4,
                Name = "Défi Perte de Poids",
                Description = "Perdez 5kg en 8 semaines de manière saine",
                Type = ChallengeType.Poids,
                StartDate = DateTime.Now.AddMonths(-3),
                EndDate = DateTime.Now.AddMonths(-1),
                TargetGoal = 5,
                GoalUnit = "kg",
                Status = ChallengeStatus.Completed,
                RewardDescription = "Consultation nutrition offerte",
                CreatedAt = DateTime.Now.AddMonths(-4)
            }
        };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task<List<Challenge>> GetChallengesAsync() => Task.FromResult(_mockChallenges);

    public Task<Challenge?> GetChallengeByIdAsync(int id)
    {
        var c = _mockChallenges.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(c);
    }

    public Task<Challenge> CreateChallengeAsync(Challenge challenge)
    {
        challenge.Id = _mockChallenges.Count > 0 ? _mockChallenges.Max(c => c.Id) + 1 : 1;
        challenge.CreatedAt = DateTime.UtcNow;
        _mockChallenges.Add(challenge);
        return Task.FromResult(challenge);
    }

    public Task UpdateChallengeAsync(Challenge challenge)
    {
        var idx = _mockChallenges.FindIndex(c => c.Id == challenge.Id);
        if (idx >= 0) _mockChallenges[idx] = challenge;
        return Task.CompletedTask;
    }

    public Task DeleteChallengeAsync(int id)
    {
        _mockChallenges.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }

    public Task<int> GetActiveChallengesCountAsync()
    {
        var count = _mockChallenges.Count(c => c.Status == ChallengeStatus.Active);
        return Task.FromResult(count);
    }

    public Task<List<ChallengeParticipant>> GetParticipantsAsync(int challengeId) =>
        Task.FromResult(new List<ChallengeParticipant>());

    public Task AddParticipantAsync(int challengeId, int clientId, string clientName) =>
        Task.CompletedTask;

    public Task RemoveParticipantAsync(int participantId) => Task.CompletedTask;

    public Task UpdateProgressAsync(int participantId, double newValue) => Task.CompletedTask;
}
