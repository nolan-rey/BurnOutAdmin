using BurnOutAdmin.Models;
using BurnOutAdmin.Models.Program;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation API de <see cref="IProgrammeSeanceService"/>.
///
/// Endpoints :
///   GET    /programmes/{id}/seances
///   POST   /programmes/{id}/seances
///   DELETE /programmes/{id}/seances/{seanceId}
///
/// Pour attacher une séance, on transmet le data_json complet du template
/// (seances_builder) — le serveur peut donc créer un enregistrement
/// autonome dans `seances` sans devoir relire le template à chaque appel.
/// </summary>
public class ApiProgrammeSeanceService : IProgrammeSeanceService
{
    private readonly ApiHttpClient _api;

    public ApiProgrammeSeanceService(ApiHttpClient api) => _api = api;

    // ── Lecture ───────────────────────────────────────────────────

    public async Task<List<ProgrammeSeance>> GetSeancesAsync(int programmeId)
    {
        if (programmeId <= 0) return [];

        try
        {
            var response = await _api.GetAsync<ProgrammeSeanceListResponseDto>(
                $"/programmes/{programmeId}/seances");
            var data = response?.Data;
            if (data is { Count: > 0 })
            {
                Console.WriteLine($"[ApiProgrammeSeanceService] GET /programmes/{programmeId}/seances OK — {data.Count} séance(s)");
                return data.Select(MapToModel).OrderBy(s => s.Order).ToList();
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeSeanceService] GetSeancesAsync({programmeId}): {ex.Message}");
        }

        return [];
    }

    // ── Écriture ──────────────────────────────────────────────────

    public async Task<ProgrammeSeance?> AddSeanceFromTemplateAsync(
        int programmeId, SavedSessionEntry template, int order)
    {
        if (programmeId <= 0)
        {
            Console.WriteLine("[ApiProgrammeSeanceService] AddSeance: programmeId invalide");
            return null;
        }

        try
        {
            // ── data_json mobile-ready ────────────────────────────────
            // Le template.DataJson local est en PascalCase (cache C#).
            // On le re-désérialise en SessionModel puis on remappe au
            // format snake_case attendu par l'API mobile (clés blocks,
            // exercices, nom_exercice, etc.). Sinon la séance s'affiche
            // vide sur l'app pratiquant.
            object mobileData = new { };
            if (!string.IsNullOrWhiteSpace(template.DataJson) && template.DataJson != "{}")
            {
                try
                {
                    var session = System.Text.Json.JsonSerializer
                        .Deserialize<Models.Program.SessionModel>(template.DataJson);
                    if (session is not null)
                        mobileData = MobileDataJsonMapper.Build(session);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    Console.WriteLine($"[ApiProgrammeSeanceService] DataJson local invalide (id={template.Id}): {ex.Message}");
                }
            }

            // Body anonymous → data_json est un OBJET, pas une string.
            var body = new
            {
                id_seance_builder = template.Id,
                nom               = template.Name,
                description       = template.Description,
                ordre             = order,
                exercise_count    = template.ExerciseCount,
                category_count    = template.CategoryCount,
                data_json         = mobileData
            };

            Console.WriteLine($"[ApiProgrammeSeanceService] POST /programmes/{programmeId}/seances nom='{template.Name}' ordre={order} template={template.Id}");

            var result = await _api.PostAsync<CreateProgrammeSeanceResponseDto>(
                $"/programmes/{programmeId}/seances", body);

            if (result?.Success == true && result.ResolvedId is { } newId && newId > 0)
            {
                Console.WriteLine($"[ApiProgrammeSeanceService] Séance attachée id={newId}");
                return new ProgrammeSeance
                {
                    Id              = newId,
                    ProgrammeId     = programmeId,
                    SeanceBuilderId = template.Id,
                    Name            = template.Name,
                    Description     = template.Description,
                    Order           = order,
                    ExerciseCount   = template.ExerciseCount,
                    CategoryCount   = template.CategoryCount,
                    CreatedAt       = DateTime.UtcNow
                };
            }

            Console.WriteLine($"[ApiProgrammeSeanceService] AddSeance failed: {result?.Error}");
            return null;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeSeanceService] AddSeanceFromTemplate error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateSeanceAsync(int programmeId, int seanceId, int? order, string? name, string? description)
    {
        if (programmeId <= 0 || seanceId <= 0) return false;
        if (order is null && name is null && description is null) return true; // rien à faire

        try
        {
            var dto = new UpdateProgrammeSeanceDto
            {
                Ordre       = order,
                Nom         = name,
                Description = description
            };

            var result = await _api.PutAsync<ApiSuccessDto>(
                $"/programmes/{programmeId}/seances/{seanceId}", dto);
            var ok = result?.Success ?? false;
            Console.WriteLine($"[ApiProgrammeSeanceService] PUT /programmes/{programmeId}/seances/{seanceId} ordre={order} → success={ok}");
            return ok;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeSeanceService] UpdateSeance({programmeId}, {seanceId}): {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveSeanceAsync(int programmeId, int seanceId)
    {
        if (programmeId <= 0 || seanceId <= 0) return false;

        try
        {
            await _api.DeleteAsync($"/programmes/{programmeId}/seances/{seanceId}");
            Console.WriteLine($"[ApiProgrammeSeanceService] DELETE /programmes/{programmeId}/seances/{seanceId} OK");
            return true;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgrammeSeanceService] RemoveSeance({programmeId}, {seanceId}): {ex.Message}");
            return false;
        }
    }

    // ── Mapping ───────────────────────────────────────────────────

    private static ProgrammeSeance MapToModel(ProgrammeSeanceDto dto) => new()
    {
        Id              = dto.ResolvedId,
        ProgrammeId     = dto.IdProgramme,
        SeanceBuilderId = dto.IdSeanceBuilder,
        Name            = dto.Nom,
        Description     = dto.Description ?? string.Empty,
        Order           = dto.Ordre ?? 0,
        ExerciseCount   = dto.ExerciseCount ?? 0,
        CategoryCount   = dto.CategoryCount ?? 0,
        CreatedAt       = TryParseDate(dto.CreatedAt) ?? DateTime.UtcNow
    };

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
