using System.Text.Json;
using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponse GET /programmes ───────────────────────────────────────────────────

public class ProgrammeListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ProgrammeDto> Data { get; set; } = new();
}

/// <summary>
/// Représentation JSON d'un programme tel que retourné par GET /programmes (API v2).
/// Inclut les champs type, niveau, duree_semaines, seances_par_semaine, is_actif
/// ajoutés par la migration v2.
/// </summary>
public class ProgrammeDto
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

    [JsonPropertyName("duree_semaines")]
    public int? DureeSemaines { get; set; }

    [JsonPropertyName("seances_par_semaine")]
    public int? SeancesParSemaine { get; set; }

    /// <summary>
    /// L'API peut retourner un booléen (true/false) ou un entier (0/1).
    /// On utilise JsonElement? pour accepter les deux formes.
    /// </summary>
    [JsonPropertyName("is_actif")]
    public JsonElement? IsActif { get; set; }

    [JsonPropertyName("id_client")]
    public int? IdClient { get; set; }

    [JsonPropertyName("id_createur")]
    public int? IdCreateur { get; set; }

    [JsonPropertyName("date_debut")]
    public string? DateDebut { get; set; }

    [JsonPropertyName("date_fin")]
    public string? DateFin { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

/// <summary>Body pour POST /programmes — création d'un programme.</summary>
public class CreateProgrammeDto
{
    [JsonPropertyName("nom_programme")]
    public string NomProgramme { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "mixte";

    [JsonPropertyName("niveau")]
    public string Niveau { get; set; } = "intermediaire";

    [JsonPropertyName("duree_semaines")]
    public int DureeSemaines { get; set; }

    [JsonPropertyName("seances_par_semaine")]
    public int SeancesParSemaine { get; set; }

    [JsonPropertyName("id_client")]
    public int? IdClient { get; set; }

    [JsonPropertyName("id_createur")]
    public int? IdCreateur { get; set; }

    [JsonPropertyName("date_debut")]
    public string DateDebut { get; set; } = string.Empty;

    [JsonPropertyName("date_fin")]
    public string DateFin { get; set; } = string.Empty;
}

/// <summary>Body pour PUT /programmes/{id} — mise à jour partielle.</summary>
public class UpdateProgrammeDto
{
    [JsonPropertyName("nom_programme")]
    public string NomProgramme { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("niveau")]
    public string? Niveau { get; set; }

    [JsonPropertyName("is_actif")]
    public int? IsActif { get; set; }
}

/// <summary>Réponse de création POST /programmes.</summary>
public class CreateProgrammeResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("id_programme")]
    public int? IdProgramme { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Réponse générique de succès de l'API.</summary>
public class ApiSuccessDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
