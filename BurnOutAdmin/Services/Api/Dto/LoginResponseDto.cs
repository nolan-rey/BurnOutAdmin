using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

public class LoginResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
