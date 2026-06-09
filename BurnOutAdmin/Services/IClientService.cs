using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface IClientService
{
    Task<List<Client>> GetClientsAsync();
    Task<Client?> GetClientByIdAsync(int id);
    Task<bool> UpdateClientAsync(Client client);
    Task<bool> UpdateStatusAsync(int clientId, string newStatus);
    Task<bool> DeleteClientAsync(int id);
    Task<bool> AddClientAsync(Client client);

    /// <summary>
    /// Met à jour le rang totem du client (clients.totem_rang).
    /// <c>null</c> autorisé pour réinitialiser.
    /// </summary>
    Task<bool> UpdateClientTotemAsync(int clientId, int? totemRang);
}
