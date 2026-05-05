using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponse GET /programme-assignations/client/{id} ──────────────────────────

public class AssignationListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<AssignationDto> Data { get; set; } = new();
}

// ── DTO Assignation ───────────────────────────────────────────────────────────

public class AssignationDto
{
    [JsonPropertyName("id_assignation")]
    public string IdAssignation { get; set; } = string.Empty;

    [JsonPropertyName("id_client")]
    public int IdClient { get; set; }

    [JsonPropertyName("id_programme")]
    public int IdProgramme { get; set; }

    [JsonPropertyName("date_debut")]
    public string? DateDebut { get; set; }

    [JsonPropertyName("date_fin")]
    public string? DateFin { get; set; }

    [JsonPropertyName("date_assignation")]
    public string? DateAssignation { get; set; }

    [JsonPropertyName("programme")]
    public AssignationProgrammeDto? Programme { get; set; }
}

// ── Programme imbriqué dans l'assignation ─────────────────────────────────────

public class AssignationProgrammeDto
{
    [JsonPropertyName("id_programme")]
    public int IdProgramme { get; set; }

    [JsonPropertyName("nom_programme")]
    public string NomProgramme { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("niveau")]
    public string? Niveau { get; set; }
}

// ── Création (POST /programme-assignations) ───────────────────────────────────

public class CreateAssignationDto
{
    [JsonPropertyName("id_client")]
    public int IdClient { get; set; }

    [JsonPropertyName("id_programme")]
    public int IdProgramme { get; set; }

    [JsonPropertyName("date_debut")]
    public string? DateDebut { get; set; }

    [JsonPropertyName("date_fin")]
    public string? DateFin { get; set; }
}

// ── Réponse création ──────────────────────────────────────────────────────────

public class CreateAssignationResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("id_assignation")]
    public string? IdAssignation { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
