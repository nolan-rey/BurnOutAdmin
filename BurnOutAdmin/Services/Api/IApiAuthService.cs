using BurnOutAdmin.Services.Api.Dto;

namespace BurnOutAdmin.Services.Api;

public interface IApiAuthService
{
    bool IsAuthenticated { get; }
    string? CurrentUserEmail { get; }

    /// <summary>Login Firebase. Retourne null si succès, message d'erreur sinon.</summary>
    Task<string?> LoginAsync(string email, string password);

    /// <summary>Inscription d'un nouveau compte coach ou admin.</summary>
    Task<string?> RegisterAsync(RegisterRequestDto request);

    string? GetToken();
    void Logout();
}
