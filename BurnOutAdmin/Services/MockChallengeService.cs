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
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(20),
                ParticipantsCount = 45,
                TargetGoal = 30,
                GoalUnit = "séances",
                Status = ChallengeStatus.Active,
                RewardDescription = "Badge Cardio Master + 1 mois offert"
            },
            new Challenge
            {
                Id = 2,
                Name = "Challenge Force Maximale",
                Description = "Augmentez votre 1RM de 10% sur les exercices principaux",
                StartDate = DateTime.Now.AddDays(-5),
                EndDate = DateTime.Now.AddDays(55),
                ParticipantsCount = 28,
                TargetGoal = 10,
                GoalUnit = "% d'augmentation",
                Status = ChallengeStatus.Active,
                RewardDescription = "T-shirt exclusif BurnOut"
            },
            new Challenge
            {
                Id = 3,
                Name = "Marathon Février",
                Description = "Courez 100km au total pendant le mois de février",
                StartDate = DateTime.Now.AddMonths(1),
                EndDate = DateTime.Now.AddMonths(2),
                ParticipantsCount = 0,
                TargetGoal = 100,
                GoalUnit = "km",
                Status = ChallengeStatus.Upcoming,
                RewardDescription = "Médaille Marathon + Réduction 20%"
            },
            new Challenge
            {
                Id = 4,
                Name = "Défi Perte de Poids",
                Description = "Perdez 5kg en 8 semaines de manière saine",
                StartDate = DateTime.Now.AddMonths(-3),
                EndDate = DateTime.Now.AddMonths(-1),
                ParticipantsCount = 32,
                TargetGoal = 5,
                GoalUnit = "kg",
                Status = ChallengeStatus.Completed,
                RewardDescription = "Consultation nutrition offerte"
            }
        };
    }

    public Task<List<Challenge>> GetChallengesAsync()
    {
        return Task.FromResult(_mockChallenges);
    }

    public Task<Challenge?> GetChallengeByIdAsync(int id)
    {
        var challenge = _mockChallenges.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(challenge);
    }

    public Task<int> GetActiveChallengesCountAsync()
    {
        var count = _mockChallenges.Count(c => c.Status == ChallengeStatus.Active);
        return Task.FromResult(count);
    }
}
