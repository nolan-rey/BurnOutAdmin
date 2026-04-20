namespace BurnOutAdmin.Services;

/// <summary>
/// Implémentation MAUI de IAlertService utilisant DisplayAlert sur la page courante.
/// </summary>
public class MauiAlertService : IAlertService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return Task.FromResult(false);
        return MainThread.InvokeOnMainThreadAsync(() => page.DisplayAlert(title, message, accept, cancel));
    }

    public Task AlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return Task.CompletedTask;
        return MainThread.InvokeOnMainThreadAsync(() => page.DisplayAlert(title, message, cancel));
    }
}
