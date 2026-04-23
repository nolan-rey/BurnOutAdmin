using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

/// <summary>
/// Représentation JSON d'un programme tel que retourné par GET /programmes.
/// </summary>
public class ProgrammeDto
{
    [JsonPropertyName("id_programme")]
    public int IdProgramme { get; set; }

    [JsonPropertyName("nom_programme")]
    public string NomProgramme { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("id_client")]
    public int? IdClient { get; set; }

    [JsonPropertyName("id_createur")]
    public int? IdCreateur { get; set; }

    [JsonPropertyName("date_debut")]
    public string? DateDebut { get; set; }

    [JsonPropertyName("date_fin")]
    public string? DateFin { get; set; }
}

/// <summary>
/// Body pour POST /programmes — création d'un programme.
/// </summary>
public class CreateProgrammeDto
{
    [JsonPropertyName("nom_programme")]
    public string NomProgramme { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("id_client")]
    public int? IdClient { get; set; }

    [JsonPropertyName("id_createur")]
    public int? IdCreateur { get; set; }

    [JsonPropertyName("date_debut")]
    public string DateDebut { get; set; } = string.Empty;

    [JsonPropertyName("date_fin")]
    public string DateFin { get; set; } = string.Empty;
}

/// <summary>
/// Body pour PUT /programmes/{id} — mise à jour partielle.
/// </summary>
public class UpdateProgrammeDto
{
    [JsonPropertyName("nom_programme")]
    public string NomProgramme { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Réponse générique de succès de l'API.
/// </summary>
public class ApiSuccessDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
