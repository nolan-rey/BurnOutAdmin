using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface IUserService
{
    Task<List<User>> GetUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
}
