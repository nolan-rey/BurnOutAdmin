namespace BurnOutAdmin.Services;

/// <summary>
/// Abstraction pour les dialogues utilisateur, injectable et testable (MVVM-friendly).
/// </summary>
public interface IAlertService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non");
    Task AlertAsync(string title, string message, string cancel = "OK");
}
