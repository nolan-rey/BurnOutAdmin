using System.Text.Json;
using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

/// <summary>
/// Réponse PostgREST pour une ligne <c>seance_blocks</c>
/// (un bloc = une sous-catégorie de la séance builder).
/// </summary>
public class SeanceBlockDto
{
    [JsonPropertyName("id_block")]
    public int? IdBlock { get; set; }

    /// <summary>Variante éventuelle "id".</summary>
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonIgnore]
    public int ResolvedId => IdBlock ?? Id ?? 0;

    [JsonPropertyName("id_seance_builder")]
    public int IdSeanceBuilder { get; set; }

    [JsonPropertyName("nom")]
    public string Nom { get; set; } = string.Empty;

    [JsonPropertyName("partie_label")]
    public string? PartieLabel { get; set; }

    [JsonPropertyName("ordre")]
    public int Ordre { get; set; }

    [JsonPropertyName("is_enchainement")]
    public bool IsEnchainement { get; set; }

    [JsonPropertyName("is_amrap")]
    public bool IsAmrap { get; set; }

    [JsonPropertyName("amrap_minutes")]
    public int AmrapMinutes { get; set; }

    [JsonPropertyName("recup_secondes")]
    public int? RecupSecondes { get; set; }
}

/// <summary>
/// Réponse PostgREST pour une ligne <c>seance_block_exercices</c>.
/// </summary>
public class SeanceBlockExerciceDto
{
    [JsonPropertyName("id_sbe")]
    public int? IdSbe { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonIgnore]
    public int ResolvedId => IdSbe ?? Id ?? 0;

    [JsonPropertyName("id_block")]
    public int IdBlock { get; set; }

    [JsonPropertyName("id_exercice")]
    public int IdExercice { get; set; }

    [JsonPropertyName("ordre")]
    public int Ordre { get; set; }

    [JsonPropertyName("series")]
    public int Series { get; set; }

    [JsonPropertyName("reps")]
    public int? Reps { get; set; }

    [JsonPropertyName("reps_varied")]
    public string? RepsVaried { get; set; }

    [JsonPropertyName("poids")]
    public double? Poids { get; set; }

    [JsonPropertyName("poids_label")]
    public string? PoidsLabel { get; set; }

    [JsonPropertyName("duree_secondes")]
    public int? DureeSecondes { get; set; }

    [JsonPropertyName("recup_secondes")]
    public int? RecupSecondes { get; set; }

    [JsonPropertyName("recup_inter_secondes")]
    public int? RecupInterSecondes { get; set; }

    [JsonPropertyName("tempo")]
    public string? Tempo { get; set; }

    [JsonPropertyName("amplitude")]
    public string? Amplitude { get; set; }

    [JsonPropertyName("rir")]
    public int? Rir { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>Détail par série (mode séries différenciées). Null si désactivé.</summary>
    [JsonPropertyName("set_details")]
    public JsonElement? SetDetails { get; set; }

    /// <summary>Embed PostgREST <c>exercices(...)</c> — peuplé quand l'URL contient <c>select=...,exercices(...)</c>.</summary>
    [JsonPropertyName("exercices")]
    public ExerciceDto? Exercice { get; set; }

    /// <summary>Texte de répétitions reconstitué pour contenu_seance (reps, reps variées ou durée).</summary>
    [JsonIgnore]
    public string RepetitionText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(RepsVaried)) return RepsVaried!;
            if (Reps is > 0)                            return Reps!.Value.ToString();
            if (DureeSecondes is > 0)                   return $"{DureeSecondes}s";
            return "10";
        }
    }
}
