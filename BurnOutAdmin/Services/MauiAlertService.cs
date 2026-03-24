namespace BurnOutAdmin.Services;

/// <summary>
/// Implémentation MAUI de IAlertService utilisant DisplayAlert sur la page courante.
/// </summary>
public class MauiAlertService : IAlertService
{
    public async Task<bool> ConfirmAsync(string title, string message, string accept = "Oui", string cancel = "Non")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return false;
        return await page.DisplayAlertAsync(title, message, accept, cancel);
    }

    public async Task AlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;
        await page.DisplayAlertAsync(title, message, cancel);
    }
}
