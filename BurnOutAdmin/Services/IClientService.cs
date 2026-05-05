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
}
