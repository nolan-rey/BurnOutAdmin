using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetDashboardStatsAsync();
}
