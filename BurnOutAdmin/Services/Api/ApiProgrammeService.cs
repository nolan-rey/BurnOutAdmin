using System.Text.Json;
using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="IProgrammeService"/> utilisant l'API REST BurnOut.
///
/// Mapping API → modèle local :
///   id_programme       → Id
///   nom_programme      → Name
///   description        → Description
///   type               → Type (cardio/force/souplesse/mixte → enum)
///   niveau             → Level (debutant/intermediaire/avance → enum)
///   duree_semaines     → DurationWeeks
///   seances_par_semaine → SessionsPerWeek
///   is_actif           → IsActive
///   created_at         → CreatedAt
///
/// Endpoint : GET /programmes  (liste), POST/PUT/DELETE /programmes/{id}
/// </summary>
public class ApiProgrammeService : IProgrammeService
{
    private readonly ApiHttpClient   _api;
    private readonly IApiAuthService _auth;

    public ApiProgrammeService(ApiHttpClient api, IApiAuthService auth)
    {
        _api  = api;
        _auth = auth;
    }

    // ── Résolution id_createur ────────────────────────────────────
    // Si CurrentUserId n'est pas encore en cache, on le résout via GET /users
    // en cherchant le client dont l'email correspond à CurrentUserEmail.
    private async Task<int> ResolveCurrentUserIdAsync()
    {
        if (_auth.CurrentUserId is { } cached && cached > 0)
            return cached;

        if (_auth.CurrentUserEmail is not { Length: > 0 } myEmail)
            return 0;

        try
        {
            var response = await _api.GetAsync<ClientListResponseDto>("/users");
            var me = response?.Data?.FirstOrDefault(c =>
                string.Equals(c.Email, myEmail, StringComparison.OrdinalIgnoreCase));
            var id = me?.ResolvedId ?? 0;
            if (id > 0)
            {
                _auth.CurrentUserId = id;
                Console.WriteLine($"[ApiProgrammeService] CurrentUserId résolu : {id} ({myEmail})");
            }
            return id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeService] ResolveCurrentUserId failed: {ex.Message}");
            return 0;
        }
    }

    // ── Lecture ───────────────────────────────────────────────────

    public async Task<List<Programme>> GetProgrammesAsync()
    {
        // 1️⃣ Format enveloppé { success, data: [...] }  — format confirmé par les logs
        try
        {
            var response = await _api.GetAsync<ProgrammeListResponseDto>("/programmes");
            if (response is not null)
            {
                Console.WriteLine($"[ApiProgrammeService] OK — {response.Data?.Count ?? 0} programme(s)");
                return response.Data?.Select(MapToProgramme).ToList() ?? [];
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex1)
        {
            Console.WriteLine($"[ApiProgrammeService] Format enveloppé échoué : {ex1.Message} — essai tableau brut");
        }

        // 2️⃣ Fallback format tableau brut [...]
        try
        {
            var list = await _api.GetAsync<List<ProgrammeDto>>("/programmes");
            if (list is not null)
            {
                Console.WriteLine($"[ApiProgrammeService] Tableau brut OK — {list.Count} programme(s)");
                return list.Select(MapToProgramme).ToList();
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeService] GetProgrammesAsync error: {ex.Message}");
        }

        return [];
    }

    public async Task<Programme?> GetProgrammeByIdAsync(int id)
    {
        var all = await GetProgrammesAsync();
        return all.FirstOrDefault(p => p.Id == id);
    }

    public async Task<int> GetActiveProgrammesCountAsync()
    {
        var all = await GetProgrammesAsync();
        return all.Count(p => p.IsActive);
    }

    // ── Écriture ──────────────────────────────────────────────────

    public async Task<Programme?> CreateProgrammeAsync(Programme programme)
    {
        try
        {
            // Résoudre l'ID SQL du créateur (champ obligatoire côté API)
            var creatorId = await ResolveCurrentUserIdAsync();
            if (creatorId <= 0)
            {
                Console.WriteLine("[ApiProgrammeService] CreateProgrammeAsync: id_createur introuvable — utilisateur connecté non identifié dans /users");
                return null;
            }

            var dto = new CreateProgrammeDto
            {
                NomProgramme      = programme.Name,
                Description       = programme.Description,
                Type              = MapTypeToApi(programme.Type),
                Niveau            = MapLevelToApi(programme.Level),
                DureeSemaines     = programme.DurationWeeks,
                SeancesParSemaine = programme.SessionsPerWeek,
                IdCreateur        = creatorId,
                DateDebut         = DateTime.Today.ToString("yyyy-MM-dd"),
                DateFin           = DateTime.Today.AddDays(programme.DurationWeeks * 7).ToString("yyyy-MM-dd")
            };

            Console.WriteLine($"[ApiProgrammeService] POST /programmes nom='{dto.NomProgramme}' createur={dto.IdCreateur} type={dto.Type} niveau={dto.Niveau}");

            var result = await _api.PostAsync<CreateProgrammeResponseDto>("/programmes", dto);
            if (result?.Success == true && result.ResolvedId is { } newId && newId > 0)
            {
                programme.Id        = newId;
                programme.IsActive  = true;
                programme.CreatedAt = DateTime.UtcNow;
                Console.WriteLine($"[ApiProgrammeService] Programme créé id={newId}");
                return programme;
            }

            Console.WriteLine($"[ApiProgrammeService] CreateProgrammeAsync: success={result?.Success} id={result?.ResolvedId} err={result?.Error}");
            return null;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeService] CreateProgrammeAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateProgrammeAsync(Programme programme)
    {
        try
        {
            var dto = new UpdateProgrammeDto
            {
                NomProgramme = programme.Name,
                Description  = programme.Description,
                Type         = MapTypeToApi(programme.Type),
                Niveau       = MapLevelToApi(programme.Level),
                IsActif      = programme.IsActive ? 1 : 0
            };

            var result = await _api.PutAsync<ApiSuccessDto>($"/programmes/{programme.Id}", dto);
            return result?.Success ?? false;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeService] UpdateProgrammeAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteProgrammeAsync(int id)
    {
        try
        {
            await _api.DeleteAsync($"/programmes/{id}");
            return true;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeService] DeleteProgrammeAsync({id}) error: {ex.Message}");
            return false;
        }
    }

    // ── Mapping ───────────────────────────────────────────────────

    private static Programme MapToProgramme(ProgrammeDto dto)
    {
        var today = DateTime.Today;
        var start = TryParseDate(dto.DateDebut) ?? today;
        var end   = TryParseDate(dto.DateFin)   ?? today.AddDays(90);

        // is_actif : peut être bool JSON (true/false) ou int JSON (0/1)
        var isActive = ResolveIsActif(dto.IsActif) ?? end >= today;

        var durationWeeks = dto.DureeSemaines ?? (int)Math.Round((end - start).TotalDays / 7.0);

        return new Programme
        {
            Id              = dto.IdProgramme,
            Name            = dto.NomProgramme,
            Description     = dto.Description ?? string.Empty,
            Type            = MapType(dto.Type),
            Level           = MapLevel(dto.Niveau),
            DurationWeeks   = Math.Max(1, durationWeeks),
            SessionsPerWeek = dto.SeancesParSemaine ?? 3,
            IsActive        = isActive,
            CreatedAt       = TryParseDate(dto.CreatedAt) ?? start
        };
    }

    // ── Helpers Type ──────────────────────────────────────────────

    private static ProgrammeType MapType(string? s) => s switch
    {
        "cardio"    => ProgrammeType.Cardio,
        "force"     => ProgrammeType.Strength,
        "souplesse" => ProgrammeType.Flexibility,
        "mixte"     => ProgrammeType.Mixed,
        _           => ProgrammeType.Mixed
    };

    private static string MapTypeToApi(ProgrammeType t) => t switch
    {
        ProgrammeType.Cardio      => "cardio",
        ProgrammeType.Strength    => "force",
        ProgrammeType.Flexibility => "souplesse",
        _                         => "mixte"
    };

    private static ProgrammeLevel MapLevel(string? s) => s switch
    {
        "debutant"      => ProgrammeLevel.Beginner,
        "intermediaire" => ProgrammeLevel.Intermediate,
        "avance"        => ProgrammeLevel.Advanced,
        _               => ProgrammeLevel.Intermediate
    };

    private static string MapLevelToApi(ProgrammeLevel l) => l switch
    {
        ProgrammeLevel.Beginner     => "debutant",
        ProgrammeLevel.Intermediate => "intermediaire",
        ProgrammeLevel.Advanced     => "avance",
        _                           => "intermediaire"
    };

    /// <summary>
    /// is_actif peut être bool JSON (true/false) ou int JSON (0/1) selon la version de l'API.
    /// Retourne null si le champ est absent (le mapping utilisera un fallback par date).
    /// </summary>
    private static bool? ResolveIsActif(JsonElement? el)
    {
        if (!el.HasValue || el.Value.ValueKind == JsonValueKind.Null) return null;
        if (el.Value.ValueKind == JsonValueKind.True)  return true;
        if (el.Value.ValueKind == JsonValueKind.False) return false;
        if (el.Value.ValueKind == JsonValueKind.Number) return el.Value.GetInt32() != 0;
        return null;
    }

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
