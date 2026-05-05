using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="IChallengeService"/> utilisant l'API REST BurnOut.
///
/// Endpoints utilisés :
///   GET    /challenges                              → liste challenges
///   GET    /challenges/{id}                         → challenge par id
///   POST   /challenges                              → créer
///   PUT    /challenges/{id}                         → modifier
///   DELETE /challenges/{id}                         → supprimer
///   GET    /challenges/{id}/participants            → leaderboard
///   POST   /challenges/{id}/participants            → ajouter participant
///   DELETE /challenges/{id}/participants/{pid}      → retirer participant
///   PUT    /challenges/{id}/participants/{pid}/progress → maj valeur
///
/// Les participants sont mis en cache par challenge (invalidé sur create/delete/update).
/// </summary>
public class ApiChallengeService : IChallengeService
{
    private readonly ApiHttpClient _api;

    // Cache participants : challengeId → liste (évite N+1 au chargement de la liste)
    private readonly Dictionary<int, List<ChallengeParticipant>> _participantsCache = new();

    public ApiChallengeService(ApiHttpClient api) => _api = api;

    // ── InitializeAsync ────────────────────────────────────────────
    // No-op pour l'implémentation API (était pour SQLite)

    public Task InitializeAsync() => Task.CompletedTask;

    // ── CRUD Challenges ────────────────────────────────────────────

    public async Task<List<Challenge>> GetChallengesAsync()
    {
        _participantsCache.Clear();

        // 1️⃣ Format enveloppé { success, data: [...] }  — format confirmé par les logs
        try
        {
            var response = await _api.GetAsync<ChallengeListResponseDto>("/challenges");
            if (response is not null)
            {
                Console.WriteLine($"[ApiChallengeService] OK — {response.Data?.Count ?? 0} challenge(s)");
                return response.Data?.Select(MapToChallenge).ToList() ?? [];
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex1)
        {
            Console.WriteLine($"[ApiChallengeService] Format enveloppé échoué : {ex1.Message} — essai tableau brut");
        }

        // 2️⃣ Fallback format tableau brut [...]
        try
        {
            var list = await _api.GetAsync<List<ChallengeDto>>("/challenges");
            if (list is not null)
            {
                Console.WriteLine($"[ApiChallengeService] Tableau brut OK — {list.Count} challenge(s)");
                return list.Select(MapToChallenge).ToList();
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] GetChallengesAsync error: {ex.Message}");
        }

        return [];
    }

    public async Task<Challenge?> GetChallengeByIdAsync(int id)
    {
        try
        {
            var response = await _api.GetAsync<ChallengeResponseDto>($"/challenges/{id}");
            return response?.Data is not null ? MapToChallenge(response.Data) : null;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] GetChallengeByIdAsync({id}) error: {ex.Message}");
            return null;
        }
    }

    public async Task<Challenge> CreateChallengeAsync(Challenge challenge)
    {
        try
        {
            var dto = new CreateChallengeDto
            {
                Nom                   = challenge.Name,
                Description           = challenge.Description,
                Type                  = MapTypeToApi(challenge.Type),
                Objectif              = challenge.TargetGoal,
                UniteObjectif         = challenge.GoalUnit,
                Statut                = MapStatusToApi(challenge.Status),
                RecompenseDescription = challenge.RewardDescription,
                DateDebut             = challenge.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                DateFin               = challenge.EndDate.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var result = await _api.PostAsync<CreateChallengeResponseDto>("/challenges", dto);

            if (result?.Success == true && result.IdChallenge.HasValue)
                challenge.Id = result.IdChallenge.Value;

            return challenge;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] CreateChallengeAsync error: {ex.Message}");
            return challenge;
        }
    }

    public async Task UpdateChallengeAsync(Challenge challenge)
    {
        try
        {
            var dto = new UpdateChallengeDto
            {
                Nom                   = challenge.Name,
                Description           = challenge.Description,
                Type                  = MapTypeToApi(challenge.Type),
                Objectif              = challenge.TargetGoal,
                UniteObjectif         = challenge.GoalUnit,
                Statut                = MapStatusToApi(challenge.Status),
                RecompenseDescription = challenge.RewardDescription,
                DateDebut             = challenge.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                DateFin               = challenge.EndDate.ToString("yyyy-MM-dd HH:mm:ss")
            };

            await _api.PutAsync<ApiSuccessDto>($"/challenges/{challenge.Id}", dto);
            _participantsCache.Remove(challenge.Id);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] UpdateChallengeAsync error: {ex.Message}");
        }
    }

    public async Task DeleteChallengeAsync(int id)
    {
        try
        {
            await _api.DeleteAsync($"/challenges/{id}");
            _participantsCache.Remove(id);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] DeleteChallengeAsync({id}) error: {ex.Message}");
        }
    }

    public async Task<int> GetActiveChallengesCountAsync()
    {
        var all = await GetChallengesAsync();
        return all.Count(c => c.Status == ChallengeStatus.Active);
    }

    // ── Participants ───────────────────────────────────────────────

    public async Task<List<ChallengeParticipant>> GetParticipantsAsync(int challengeId)
    {
        // Retourner depuis le cache si disponible
        if (_participantsCache.TryGetValue(challengeId, out var cached))
            return cached;

        try
        {
            var response = await _api.GetAsync<ParticipantListResponseDto>(
                $"/challenges/{challengeId}/participants");

            var list = response?.Data?.Select(MapToParticipant).ToList() ?? [];
            _participantsCache[challengeId] = list;
            return list;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] GetParticipantsAsync({challengeId}) error: {ex.Message}");
            return [];
        }
    }

    public async Task AddParticipantAsync(int challengeId, int clientId, string clientName)
    {
        try
        {
            // Vérifier s'il est déjà participant (évite doublon)
            var existing = await GetParticipantsAsync(challengeId);
            if (existing.Any(p => p.ClientId == clientId)) return;

            var dto = new AddParticipantDto { IdClient = clientId, NomClient = clientName };
            await _api.PostAsync<ApiSuccessDto>($"/challenges/{challengeId}/participants", dto);
            _participantsCache.Remove(challengeId);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] AddParticipantAsync error: {ex.Message}");
        }
    }

    public async Task RemoveParticipantAsync(int participantId)
    {
        try
        {
            // Trouver le challengeId depuis le cache
            var challengeId = _participantsCache
                .FirstOrDefault(kv => kv.Value.Any(p => p.Id == participantId)).Key;

            await _api.DeleteAsync($"/challenge-participants/{participantId}");
            _participantsCache.Remove(challengeId);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] RemoveParticipantAsync({participantId}) error: {ex.Message}");
        }
    }

    public async Task UpdateProgressAsync(int participantId, double newValue)
    {
        try
        {
            var dto = new UpdateProgressDto { ValeurActuelle = newValue };
            await _api.PutAsync<ApiSuccessDto>($"/challenge-participants/{participantId}/progress", dto);

            // Invalider le cache pour le challenge concerné
            var challengeId = _participantsCache
                .FirstOrDefault(kv => kv.Value.Any(p => p.Id == participantId)).Key;
            _participantsCache.Remove(challengeId);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiChallengeService] UpdateProgressAsync({participantId}) error: {ex.Message}");
        }
    }

    // ── Mapping ────────────────────────────────────────────────────

    private static Challenge MapToChallenge(ChallengeDto dto) => new()
    {
        Id                  = dto.IdChallenge,
        Name                = dto.Nom,
        Description         = dto.Description ?? string.Empty,
        Type                = MapTypeFromApi(dto.Type),
        TargetGoal          = dto.Objectif,
        GoalUnit            = dto.UniteObjectif ?? string.Empty,
        Status              = MapStatusFromApi(dto.Statut),
        RewardDescription   = dto.RecompenseDescription ?? string.Empty,
        StartDate           = TryParseDate(dto.DateDebut) ?? DateTime.Today,
        EndDate             = TryParseDate(dto.DateFin)   ?? DateTime.Today.AddDays(30),
        CreatedAt           = TryParseDate(dto.CreatedAt) ?? DateTime.UtcNow
    };

    private static ChallengeParticipant MapToParticipant(ParticipantDto dto) => new()
    {
        Id           = dto.IdParticipant,
        ChallengeId  = dto.IdChallenge,
        ClientId     = dto.IdClient,
        ClientName   = dto.NomClient,
        CurrentValue = dto.ValeurActuelle,
        JoinedAt     = TryParseDate(dto.JoinedAt) ?? DateTime.UtcNow
    };

    // ── Helpers Type ──────────────────────────────────────────────

    private static ChallengeType MapTypeFromApi(string s) => s switch
    {
        "cardio"    => ChallengeType.Cardio,
        "force"     => ChallengeType.Force,
        "endurance" => ChallengeType.Endurance,
        "poids"     => ChallengeType.Poids,
        "souplesse" => ChallengeType.Flexibilite,
        _           => ChallengeType.General
    };

    private static string MapTypeToApi(ChallengeType t) => t switch
    {
        ChallengeType.Cardio     => "cardio",
        ChallengeType.Force      => "force",
        ChallengeType.Endurance  => "endurance",
        ChallengeType.Poids      => "poids",
        ChallengeType.Flexibilite => "souplesse",
        _                        => "general"
    };

    // ── Helpers Statut ────────────────────────────────────────────

    private static ChallengeStatus MapStatusFromApi(string s) => s switch
    {
        "actif"   => ChallengeStatus.Active,
        "a_venir" => ChallengeStatus.Upcoming,
        "termine" => ChallengeStatus.Completed,
        "annule"  => ChallengeStatus.Cancelled,
        _         => ChallengeStatus.Upcoming
    };

    private static string MapStatusToApi(ChallengeStatus s) => s switch
    {
        ChallengeStatus.Active    => "actif",
        ChallengeStatus.Upcoming  => "a_venir",
        ChallengeStatus.Completed => "termine",
        ChallengeStatus.Cancelled => "annule",
        _                         => "a_venir"
    };

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
