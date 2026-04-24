using System.Text.Json.Serialization;

namespace BurnOutAdmin.Services.Api.Dto;

public class RegisterRequestDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("prenom")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("nom")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // "coach" | "admin"

    [JsonPropertyName("specialite")]
    public string? Specialty { get; set; }

    [JsonPropertyName("code_verification")]
    public string VerificationCode { get; set; } = string.Empty;
}

public class RegisterResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
