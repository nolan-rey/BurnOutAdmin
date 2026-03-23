using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public class MockProgrammeService : IProgrammeService
{
    private readonly List<Programme> _mockProgrammes;

    public MockProgrammeService()
    {
        _mockProgrammes = new List<Programme>
        {
            new Programme
            {
                Id = 1,
                Name = "Force & Puissance",
                Description = "Programme intensif de musculation pour développer la force maximale",
                Type = ProgrammeType.Strength,
                DurationWeeks = 12,
                SessionsPerWeek = 4,
                Level = ProgrammeLevel.Advanced,
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-3)
            },
            new Programme
            {
                Id = 2,
                Name = "Cardio Burn",
                Description = "Entraînement cardio haute intensité pour brûler les graisses",
                Type = ProgrammeType.Cardio,
                DurationWeeks = 8,
                SessionsPerWeek = 5,
                Level = ProgrammeLevel.Intermediate,
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-2)
            },
            new Programme
            {
                Id = 3,
                Name = "Flex & Zen",
                Description = "Programme de souplesse et relaxation musculaire",
                Type = ProgrammeType.Flexibility,
                DurationWeeks = 6,
                SessionsPerWeek = 3,
                Level = ProgrammeLevel.Beginner,
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-1)
            },
            new Programme
            {
                Id = 4,
                Name = "Total Body",
                Description = "Programme complet combinant force, cardio et souplesse",
                Type = ProgrammeType.Mixed,
                DurationWeeks = 10,
                SessionsPerWeek = 4,
                Level = ProgrammeLevel.Intermediate,
                IsActive = false,
                CreatedAt = DateTime.Now.AddMonths(-6)
            }
        };
    }

    public Task<List<Programme>> GetProgrammesAsync()
    {
        return Task.FromResult(_mockProgrammes);
    }

    public Task<Programme?> GetProgrammeByIdAsync(int id)
    {
        var programme = _mockProgrammes.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(programme);
    }

    public Task<int> GetActiveProgrammesCountAsync()
    {
        var count = _mockProgrammes.Count(p => p.IsActive);
        return Task.FromResult(count);
    }
}
