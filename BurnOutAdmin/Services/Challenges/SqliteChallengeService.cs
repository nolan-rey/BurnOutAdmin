using BurnOutAdmin.Models;
using SQLite;

namespace BurnOutAdmin.Services.Challenges;

public class SqliteChallengeService : IChallengeService
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
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "burnout_challenges.db");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<Challenge>();
            await _db.CreateTableAsync<ChallengeParticipant>();
            await SeedDefaultChallengesAsync();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task SeedDefaultChallengesAsync()
    {
        var count = await _db!.Table<Challenge>().CountAsync();
        if (count > 0) return;

        var today = DateTime.Today;
        var seeds = new List<Challenge>
        {
            new()
            {
                Name = "Défi 30 jours Cardio",
                Description = "Réaliser au moins 30 séances de cardio en 30 jours. Toute activité cardio compte : course, vélo, natation, corde à sauter.",
                Type = ChallengeType.Cardio,
                StartDate = today.AddDays(-10),
                EndDate = today.AddDays(20),
                TargetGoal = 30,
                GoalUnit = "séances",
                Status = ChallengeStatus.Active,
                RewardDescription = "1 mois d'abonnement offert",
                CreatedAt = today.AddDays(-15)
            },
            new()
            {
                Name = "Challenge Force Maximale",
                Description = "Augmenter son 1RM sur au moins un exercice de force (squat, développé couché ou soulevé de terre) de 10%.",
                Type = ChallengeType.Force,
                StartDate = today.AddDays(-5),
                EndDate = today.AddDays(25),
                TargetGoal = 10,
                GoalUnit = "%",
                Status = ChallengeStatus.Active,
                RewardDescription = "T-shirt BurnOut offert",
                CreatedAt = today.AddDays(-8)
            },
            new()
            {
                Name = "Marathon Hivernal",
                Description = "Courir ou marcher 100 km cumulés sur le mois. Chaque sortie compte, même les petites !",
                Type = ChallengeType.Endurance,
                StartDate = today.AddDays(5),
                EndDate = today.AddDays(35),
                TargetGoal = 100,
                GoalUnit = "km",
                Status = ChallengeStatus.Upcoming,
                RewardDescription = "Gourde isotherme personnalisée",
                CreatedAt = today.AddDays(-2)
            },
            new()
            {
                Name = "Défi Perte de Poids",
                Description = "Atteindre son objectif de perte de poids (-5 kg) en combinant entraînement et nutrition.",
                Type = ChallengeType.Poids,
                StartDate = today.AddDays(-60),
                EndDate = today.AddDays(-2),
                TargetGoal = 5,
                GoalUnit = "kg",
                Status = ChallengeStatus.Completed,
                RewardDescription = "Séance coaching privé offerte",
                CreatedAt = today.AddDays(-65)
            }
        };

        foreach (var c in seeds)
            await _db.InsertAsync(c);
    }

    // ── CRUD Challenges ───────────────────────────────────────────

    public async Task<List<Challenge>> GetChallengesAsync()
    {
        await InitializeAsync();
        return await _db!.Table<Challenge>().OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<Challenge?> GetChallengeByIdAsync(int id)
    {
        await InitializeAsync();
        return await _db!.Table<Challenge>().Where(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Challenge> CreateChallengeAsync(Challenge challenge)
    {
        await InitializeAsync();
        challenge.CreatedAt = DateTime.UtcNow;
        await _db!.InsertAsync(challenge);
        return challenge;
    }

    public async Task UpdateChallengeAsync(Challenge challenge)
    {
        await InitializeAsync();
        await _db!.UpdateAsync(challenge);
    }

    public async Task DeleteChallengeAsync(int id)
    {
        await InitializeAsync();
        await _db!.DeleteAsync<Challenge>(id);
        // Supprimer les participants associés
        var participants = await _db.Table<ChallengeParticipant>()
            .Where(p => p.ChallengeId == id).ToListAsync();
        foreach (var p in participants)
            await _db.DeleteAsync<ChallengeParticipant>(p.Id);
    }

    public async Task<int> GetActiveChallengesCountAsync()
    {
        await InitializeAsync();
        return await _db!.Table<Challenge>()
            .Where(c => c.Status == ChallengeStatus.Active)
            .CountAsync();
    }

    // ── Participants ──────────────────────────────────────────────

    public async Task<List<ChallengeParticipant>> GetParticipantsAsync(int challengeId)
    {
        await InitializeAsync();
        return await _db!.Table<ChallengeParticipant>()
            .Where(p => p.ChallengeId == challengeId)
            .OrderBy(p => p.ClientName)
            .ToListAsync();
    }

    public async Task AddParticipantAsync(int challengeId, int clientId, string clientName)
    {
        await InitializeAsync();
        // Éviter les doublons
        var exists = await _db!.Table<ChallengeParticipant>()
            .Where(p => p.ChallengeId == challengeId && p.ClientId == clientId)
            .CountAsync() > 0;
        if (exists) return;

        await _db.InsertAsync(new ChallengeParticipant
        {
            ChallengeId = challengeId,
            ClientId = clientId,
            ClientName = clientName,
            CurrentValue = 0,
            JoinedAt = DateTime.UtcNow
        });
    }

    public async Task RemoveParticipantAsync(int participantId)
    {
        await InitializeAsync();
        await _db!.DeleteAsync<ChallengeParticipant>(participantId);
    }

    public async Task UpdateProgressAsync(int participantId, double newValue)
    {
        await InitializeAsync();
        var p = await _db!.Table<ChallengeParticipant>()
            .Where(x => x.Id == participantId).FirstOrDefaultAsync();
        if (p is null) return;
        p.CurrentValue = newValue;
        await _db.UpdateAsync(p);
    }
}
