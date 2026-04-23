using BurnOutAdmin.Models;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="IProgrammeService"/> utilisant l'API REST CallOfPhoenix.
///
/// Mapping API → modèle local :
///   id_programme  → Id
///   nom_programme → Name
///   description   → Description
///   date_debut / date_fin → DurationWeeks (calculé)
///   (type, level non fournis par l'API) → valeurs par défaut
///   date_fin >= today → IsActive = true
///
/// NOTE : L'API ne fournit pas encore les champs type, level, sessions_per_week.
/// Ces champs seront mappés correctement dès que l'API sera mise à jour.
/// Voir docs/API_Gap_Analysis.md pour le détail des modifications nécessaires.
/// </summary>
public class ApiProgrammeService : IProgrammeService
{
    private readonly ApiHttpClient _api;

    public ApiProgrammeService(ApiHttpClient api)
    {
        _api = api;
    }

    public async Task<List<Programme>> GetProgrammesAsync()
    {
        try
        {
            var dtos = await _api.GetAsync<List<ProgrammeDto>>("/programmes");
            if (dtos is null) return [];
            return dtos.Select(MapToProgramme).ToList();
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeService] GetProgrammesAsync error: {ex.Message}");
            return [];
        }
    }

    public async Task<Programme?> GetProgrammeByIdAsync(int id)
    {
        // L'API ne fournit pas GET /programmes/{id} — on charge tout et on filtre
        var all = await GetProgrammesAsync();
        return all.FirstOrDefault(p => p.Id == id);
    }

    public async Task<int> GetActiveProgrammesCountAsync()
    {
        var all = await GetProgrammesAsync();
        return all.Count(p => p.IsActive);
    }

    // ── CRUD (utilisable à terme depuis les ViewModels) ───────────

    public async Task<bool> CreateProgrammeAsync(CreateProgrammeDto dto)
    {
        try
        {
            var result = await _api.PostAsync<ApiSuccessDto>("/programmes", dto);
            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeService] CreateProgrammeAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateProgrammeAsync(int id, UpdateProgrammeDto dto)
    {
        try
        {
            var result = await _api.PutAsync<ApiSuccessDto>($"/programmes/{id}", dto);
            return result?.Success ?? false;
        }
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
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeService] DeleteProgrammeAsync error: {ex.Message}");
            return false;
        }
    }

    // ── Mapping ───────────────────────────────────────────────────

    private static Programme MapToProgramme(ProgrammeDto dto)
    {
        var today = DateTime.Today;
        var start = TryParseDate(dto.DateDebut) ?? today;
        var end   = TryParseDate(dto.DateFin)   ?? today.AddDays(90);

        var durationWeeks = (int)Math.Round((end - start).TotalDays / 7.0);

        return new Programme
        {
            Id             = dto.IdProgramme,
            Name           = dto.NomProgramme,
            Description    = dto.Description ?? string.Empty,
            Type           = ProgrammeType.Mixed,           // Non fourni par l'API (v1)
            Level          = ProgrammeLevel.Intermediate,   // Non fourni par l'API (v1)
            DurationWeeks  = Math.Max(1, durationWeeks),
            SessionsPerWeek = 3,                            // Non fourni par l'API (v1)
            IsActive       = end >= today,
            CreatedAt      = start
        };
    }

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, out var d) ? d : null;
    }
}
