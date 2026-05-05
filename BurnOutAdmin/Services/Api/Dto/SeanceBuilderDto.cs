using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponse GET /seances-builder ─────────────────────────────────────────────

public class SeanceBuilderListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<SeanceBuilderDto> Data { get; set; } = new();
}

public class SeanceBuilderResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public SeanceBuilderDto? Data { get; set; }
}

// ── DTO principal ─────────────────────────────────────────────────────────────

public class SeanceBuilderDto
{
    [JsonPropertyName("id_seance_builder")]
    public int IdSeanceBuilder { get; set; }

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("exercise_count")]
    public int ExerciseCount { get; set; }

    [JsonPropertyName("category_count")]
    public int CategoryCount { get; set; }

    [JsonPropertyName("data_json")]
    public string DataJson { get; set; } = "{}";

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

// ── Création (POST /seances-builder) ─────────────────────────────────────────

public class CreateSeanceBuilderDto
{
    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("exercise_count")]
    public int ExerciseCount { get; set; }

    [JsonPropertyName("category_count")]
    public int CategoryCount { get; set; }

    [JsonPropertyName("data_json")]
    public string DataJson { get; set; } = "{}";
}

// ── Réponse création ──────────────────────────────────────────────────────────

public class CreateSeanceBuilderResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("id_seance_builder")]
    public int? IdSeanceBuilder { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

// ── Mise à jour (PUT /seances-builder/{id}) ───────────────────────────────────

public class UpdateSeanceBuilderDto
{
    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("exercise_count")]
    public int ExerciseCount { get; set; }

    [JsonPropertyName("category_count")]
    public int CategoryCount { get; set; }

    [JsonPropertyName("data_json")]
    public string DataJson { get; set; } = "{}";
}
