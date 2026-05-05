using BurnOutAdmin.Models;
using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Implémentation de <see cref="IProgramAssignmentService"/> utilisant l'API REST BurnOut.
///
/// Endpoints utilisés :
///   POST /programme-assignations                    → assigner un programme à un client
///   GET  /programme-assignations/client/{id_client} → programmes d'un client
/// </summary>
public class ApiProgramAssignmentService : IProgramAssignmentService
{
    private readonly ApiHttpClient _api;

    public ApiProgramAssignmentService(ApiHttpClient api) => _api = api;

    // ── Assignation ───────────────────────────────────────────────

    public async Task AssignProgramAsync(ClientProgramAssignment assignment)
    {
        try
        {
            var dto = new CreateAssignationDto
            {
                IdClient    = assignment.ClientId,
                IdProgramme = ExtractProgrammeId(assignment),
                DateDebut   = assignment.AssignedAt.ToString("yyyy-MM-dd"),
                DateFin     = assignment.AssignedAt.AddMonths(3).ToString("yyyy-MM-dd")
            };

            var result = await _api.PostAsync<CreateAssignationResponseDto>(
                "/programme-assignations", dto);

            if (result?.Success == true && !string.IsNullOrEmpty(result.IdAssignation))
            {
                if (Guid.TryParse(result.IdAssignation, out var g))
                    assignment.Id = g;
            }
            else
            {
                Console.WriteLine($"[ApiProgramAssignmentService] AssignProgramAsync failed: {result?.Error}");
            }
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgramAssignmentService] AssignProgramAsync error: {ex.Message}");
        }
    }

    // ── Lecture ───────────────────────────────────────────────────

    public async Task<List<ClientProgramAssignment>> GetAssignmentsForClientAsync(int clientId)
    {
        try
        {
            var response = await _api.GetAsync<AssignationListResponseDto>(
                $"/programme-assignations/client/{clientId}");

            return response?.Data?.Select(MapToAssignment).ToList() ?? [];
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiProgramAssignmentService] GetAssignmentsForClientAsync({clientId}) error: {ex.Message}");
            return [];
        }
    }

    // ── Mapping ────────────────────────────────────────────────────

    private static ClientProgramAssignment MapToAssignment(AssignationDto dto) => new()
    {
        Id          = Guid.TryParse(dto.IdAssignation, out var g) ? g : Guid.NewGuid(),
        ClientId    = dto.IdClient,
        ProgramName = dto.Programme?.NomProgramme ?? $"Programme #{dto.IdProgramme}",
        AssignedAt  = TryParseDate(dto.DateAssignation) ?? DateTime.Now,
        Sessions    = []   // Enrichi ultérieurement si besoin
    };

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Tente d'extraire l'id_programme depuis le nom du programme (fallback 0).
    /// En production, le ViewModel passe l'id directement.
    /// </summary>
    private static int ExtractProgrammeId(ClientProgramAssignment assignment)
    {
        // Si le ProgramName contient "#ID" on extrait (ex: "Programme #3")
        var parts = assignment.ProgramName.Split('#');
        if (parts.Length > 1 && int.TryParse(parts[^1].Trim(), out var id))
            return id;
        return 0;
    }

    private static DateTime? TryParseDate(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.TryParse(s, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
    }
}
