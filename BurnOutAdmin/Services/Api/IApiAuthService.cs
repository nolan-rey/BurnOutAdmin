namespace BurnOutAdmin.Services.Api;

/// <summary>
/// Gère l'authentification contre l'API CallOfPhoenix (Firebase JWT).
/// </summary>
public interface IApiAuthService
{
    /// <summary>Indique si un token valide (non expiré) est présent en mémoire/stockage.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Email de l'utilisateur connecté, ou null si non authentifié.</summary>
    string? CurrentUserEmail { get; }

    /// <summary>
    /// Tente de se connecter avec les identifiants fournis.
    /// Stocke le token JWT en cas de succès.
    /// </summary>
    /// <returns>null si succès, message d'erreur sinon.</returns>
    Task<string?> LoginAsync(string email, string password);

    /// <summary>Retourne le token JWT courant (non expiré), ou null.</summary>
    string? GetToken();

    /// <summary>Déconnecte l'utilisateur (efface le token stocké).</summary>
    void Logout();
}
