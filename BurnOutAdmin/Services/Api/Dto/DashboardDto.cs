using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponse GET /dashboard/stats ─────────────────────────────────────────────

public class DashboardResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public DashboardDataDto? Data { get; set; }
}

// ── Données dashboard ─────────────────────────────────────────────────────────

public class DashboardDataDto
{
    [JsonPropertyName("clients_actifs")]
    public int ClientsActifs { get; set; }

    [JsonPropertyName("acces_aujourd_hui")]
    public int AccesAujourdHui { get; set; }

    [JsonPropertyName("alertes_actives")]
    public int AlertesActives { get; set; }

    [JsonPropertyName("challenges_actifs")]
    public int ChallengesActifs { get; set; }

    [JsonPropertyName("programmes_actifs")]
    public int ProgrammesActifs { get; set; }

    [JsonPropertyName("abonnements_expirant_bientot")]
    public int AbonnementsExpirantBientot { get; set; }

    [JsonPropertyName("derniers_acces")]
    public List<DashboardAccesDto> DerniersAcces { get; set; } = new();
}

// ── Entrée accès dans le dashboard ───────────────────────────────────────────

public class DashboardAccesDto
{
    [JsonPropertyName("timestamp_utc")]
    public string TimestampUtc { get; set; } = string.Empty;

    [JsonPropertyName("nom_client")]
    public string NomClient { get; set; } = string.Empty;

    [JsonPropertyName("porte")]
    public string? Porte { get; set; }

    [JsonPropertyName("resultat")]
    public string Resultat { get; set; } = string.Empty;
}
