using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public class MockClientService : IClientService
{
    private readonly List<Client> _mockClients;
    private readonly object _lock = new();
    private int _nextId;

    public MockClientService()
    {
        _mockClients = new List<Client>
        {
            new()
            {
                Id = 1, FirstName = "Jean", LastName = "Dupont",
                Email = "jean.dupont@email.com", Status = "Actif",
                NfcUid = "04:A3:5B:12",
                Subscription = new Subscription
                {
                    Type = "Annuel", StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2025, 12, 31), AutoRenewal = true
                }
            },
            new()
            {
                Id = 2, FirstName = "Alice", LastName = "Martin",
                Email = "a.martin@tech.co", Status = "Expiré",
                NfcUid = "11:F2:9C:00",
                Subscription = new Subscription
                {
                    Type = "Mensuel", StartDate = new DateTime(2024, 6, 1),
                    EndDate = new DateTime(2024, 12, 31), AutoRenewal = false
                }
            },
            new()
            {
                Id = 3, FirstName = "Lucas", LastName = "Bernard",
                Email = "lucas.bernard@gym.fr", Status = "En attente",
                NfcUid = null,
                Subscription = new Subscription
                {
                    Type = "Trimestriel", StartDate = new DateTime(2025, 2, 1),
                    EndDate = new DateTime(2025, 5, 1), AutoRenewal = true
                }
            },
            new()
            {
                Id = 4, FirstName = "Sophie", LastName = "Leroy",
                Email = "sophie.leroy@mail.com", Status = "Actif",
                NfcUid = "22:B4:7D:33",
                Subscription = new Subscription
                {
                    Type = "Annuel", StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2025, 12, 31), AutoRenewal = true
                }
            },
            new()
            {
                Id = 5, FirstName = "Pierre", LastName = "Moreau",
                Email = "p.moreau@entreprise.fr", Status = "Actif",
                NfcUid = "33:44:55:66",
                Subscription = new Subscription
                {
                    Type = "Mensuel", StartDate = new DateTime(2025, 1, 15),
                    EndDate = new DateTime(2025, 2, 15), AutoRenewal = false
                }
            },
            new()
            {
                Id = 6, FirstName = "Camille", LastName = "Girard",
                Email = "camille.girard@outlook.fr", Status = "Actif",
                NfcUid = "AA:BB:CC:01",
                Subscription = new Subscription
                {
                    Type = "Annuel", StartDate = new DateTime(2025, 3, 1),
                    EndDate = new DateTime(2026, 3, 1), AutoRenewal = true
                }
            },
            new()
            {
                Id = 7, FirstName = "Thomas", LastName = "Petit",
                Email = "thomas.petit@gmail.com", Status = "Expiré",
                NfcUid = null,
                Subscription = new Subscription
                {
                    Type = "Mensuel", StartDate = new DateTime(2024, 9, 1),
                    EndDate = new DateTime(2024, 10, 1), AutoRenewal = false
                }
            },
            new()
            {
                Id = 8, FirstName = "Manon", LastName = "Robert",
                Email = "manon.robert@hotmail.fr", Status = "En attente",
                NfcUid = null,
                Subscription = new Subscription
                {
                    Type = "Trimestriel", StartDate = new DateTime(2025, 4, 1),
                    EndDate = new DateTime(2025, 7, 1), AutoRenewal = false
                }
            },
            new()
            {
                Id = 9, FirstName = "Hugo", LastName = "Richard",
                Email = "hugo.richard@proton.me", Status = "Actif",
                NfcUid = "DD:EE:FF:09",
                Subscription = new Subscription
                {
                    Type = "Annuel", StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2025, 12, 31), AutoRenewal = true
                }
            },
            new()
            {
                Id = 10, FirstName = "Léa", LastName = "Durand",
                Email = "lea.durand@free.fr", Status = "Suspendu",
                NfcUid = "10:20:30:40",
                Subscription = new Subscription
                {
                    Type = "Mensuel", StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2025, 2, 1), AutoRenewal = false
                }
            },
            new()
            {
                Id = 11, FirstName = "Nathan", LastName = "Simon",
                Email = "nathan.simon@wanadoo.fr", Status = "Actif",
                NfcUid = "50:60:70:80",
                Subscription = new Subscription
                {
                    Type = "Annuel", StartDate = new DateTime(2025, 6, 1),
                    EndDate = new DateTime(2026, 6, 1), AutoRenewal = true
                }
            },
            new()
            {
                Id = 12, FirstName = "Emma", LastName = "Laurent",
                Email = "emma.laurent@yahoo.fr", Status = "Expiré",
                NfcUid = null,
                Subscription = new Subscription
                {
                    Type = "Mensuel", StartDate = new DateTime(2024, 11, 1),
                    EndDate = new DateTime(2024, 12, 1), AutoRenewal = false
                }
            }
        };

        _nextId = _mockClients.Max(c => c.Id) + 1;
    }

    public async Task<List<Client>> GetClientsAsync()
    {
        await Task.Delay(150);
        lock (_lock)
        {
            return _mockClients.Select(CloneClient).ToList();
        }
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
        await Task.Delay(150);
        lock (_lock)
        {
            var client = _mockClients.FirstOrDefault(c => c.Id == id);
            return client is not null ? CloneClient(client) : null;
        }
    }

    public Task<bool> UpdateStatusAsync(int clientId, string newStatus)
    {
        lock (_lock)
        {
            var existing = _mockClients.FirstOrDefault(c => c.Id == clientId);
            if (existing is not null) existing.Status = newStatus;
        }
        return Task.FromResult(true);
    }

    public async Task<bool> UpdateClientAsync(Client client)
    {
        await Task.Delay(150);
        lock (_lock)
        {
            var existing = _mockClients.FirstOrDefault(c => c.Id == client.Id);
            if (existing is null) return false;

            existing.FirstName = client.FirstName;
            existing.LastName = client.LastName;
            existing.Email = client.Email;
            existing.Status = client.Status;
            existing.NfcUid = client.NfcUid;
            existing.Subscription = client.Subscription is not null
                ? new Subscription
                {
                    Type = client.Subscription.Type,
                    StartDate = client.Subscription.StartDate,
                    EndDate = client.Subscription.EndDate,
                    AutoRenewal = client.Subscription.AutoRenewal
                }
                : null;
            return true;
        }
    }

    public async Task<bool> DeleteClientAsync(int id)
    {
        await Task.Delay(150);
        lock (_lock)
        {
            var client = _mockClients.FirstOrDefault(c => c.Id == id);
            if (client is null) return false;
            _mockClients.Remove(client);
            return true;
        }
    }

    public async Task<bool> AddClientAsync(Client client)
    {
        await Task.Delay(150);
        lock (_lock)
        {
            client.Id = _nextId++;
            _mockClients.Add(CloneClient(client));
            return true;
        }
    }

    private static Client CloneClient(Client c) => new()
    {
        Id = c.Id,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Email = c.Email,
        Status = c.Status,
        NfcUid = c.NfcUid,
        Subscription = c.Subscription is not null
            ? new Subscription
            {
                Type = c.Subscription.Type,
                StartDate = c.Subscription.StartDate,
                EndDate = c.Subscription.EndDate,
                AutoRenewal = c.Subscription.AutoRenewal
            }
            : null
    };
}
