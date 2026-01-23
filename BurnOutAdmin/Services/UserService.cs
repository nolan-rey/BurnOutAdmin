using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public class UserService : IUserService
{
    private readonly List<User> _users =
    [
        new User { Id = 1, Name = "Jean Dupont", Email = "jean@example.com", IsActive = true },
        new User { Id = 2, Name = "Marie Martin", Email = "marie@example.com", IsActive = true },
        new User { Id = 3, Name = "Pierre Bernard", Email = "pierre@example.com", IsActive = false }
    ];

    public Task<List<User>> GetUsersAsync()
    {
        return Task.FromResult(_users);
    }

    public Task<User?> GetUserByIdAsync(int id)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }
}
