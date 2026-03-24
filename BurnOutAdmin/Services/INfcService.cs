using BurnOutAdmin.Models;

namespace BurnOutAdmin.Services;

public interface INfcService
{
    Task<List<NfcLog>> GetNfcLogsAsync();
    Task<List<NfcLog>> GetTodayLogsAsync();
    Task<int> GetTodayAccessCountAsync();
}
