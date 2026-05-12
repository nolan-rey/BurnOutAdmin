using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponse GET /programmes/{id}/seances ────────────────────────────────────

public class ProgrammeSeanceListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<ProgrammeSeanceDto> Data { get; set; } = new();
}

/// <summary>
/// Séance attachée à un programme (table `seances` avec id_programme).
/// </summary>
public class ProgrammeSeanceDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("id_seance")]
    public int? IdSeance { get; set; }

    [JsonIgnore]
    public int ResolvedId => Id ?? IdSeance ?? 0;

    [JsonPropertyName("id_programme")]
    public int IdProgramme { get; set; }

    /// <summary>Référence optionnelle vers le template d'origine (seances_builder).</summary>
    [JsonPropertyName("id_seance_builder")]
    public int? IdSeanceBuilder { get; set; }

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("ordre")]
    public int? Ordre { get; set; }

    [JsonPropertyName("exercise_count")]
    public int? ExerciseCount { get; set; }

    [JsonPropertyName("category_count")]
    public int? CategoryCount { get; set; }

    [JsonPropertyName("data_json")]
    public string? DataJson { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

// ── Création (POST /programmes/{id}/seances) ────────────────────────────────

public class CreateProgrammeSeanceDto
{
    /// <summary>ID du template séance dans seances_builder à attacher.</summary>
    [JsonPropertyName("id_seance_builder")]
    public int IdSeanceBuilder { get; set; }

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("ordre")]
    public int Ordre { get; set; }

    [JsonPropertyName("exercise_count")]
    public int ExerciseCount { get; set; }

    [JsonPropertyName("category_count")]
    public int CategoryCount { get; set; }

    [JsonPropertyName("data_json")]
    public string DataJson { get; set; } = "{}";
}

public class CreateProgrammeSeanceResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("id_seance")]
    public int? IdSeance { get; set; }

    [JsonIgnore]
    public int? ResolvedId => Id ?? IdSeance;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

// ── Mise à jour (PUT /programmes/{id}/seances/{seanceId}) ────────────────────

public class UpdateProgrammeSeanceDto
{
    [JsonPropertyName("ordre")]
    public int? Ordre { get; set; }

    [JsonPropertyName("nom")]
    public string? Nom { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
