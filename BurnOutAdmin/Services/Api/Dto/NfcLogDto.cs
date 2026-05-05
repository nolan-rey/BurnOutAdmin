using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

// ── Réponse GET /logs-acces ───────────────────────────────────────────────────

public class NfcLogListResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<NfcLogDto> Data { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

// ── DTO NFC Log ───────────────────────────────────────────────────────────────

public class NfcLogDto
{
    [JsonPropertyName("id_log")]
    public int IdLog { get; set; }

    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("uid_nfc")]
    public string UidNfc { get; set; } = string.Empty;

    [JsonPropertyName("id_client")]
    public int? IdClient { get; set; }

    [JsonPropertyName("nom_client")]
    public string NomClient { get; set; } = string.Empty;

    [JsonPropertyName("resultat")]
    public string Resultat { get; set; } = string.Empty;

    [JsonPropertyName("raison")]
    public string? Raison { get; set; }

    [JsonPropertyName("porte")]
    public string? Porte { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("timestamp_utc")]
    public string TimestampUtc { get; set; } = string.Empty;
}
