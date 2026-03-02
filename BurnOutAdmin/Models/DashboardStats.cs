namespace BurnOutAdmin.Models;

public class DashboardStats
{
    public int ActiveClientsCount { get; set; }
    public int TodayNfcAccessCount { get; set; }
    public int AlertsCount { get; set; }
    public SystemStatus SystemStatus { get; set; }
    public int ActiveChallengesCount { get; set; }
    public int ActiveProgrammesCount { get; set; }
    
    public string SystemStatusText => SystemStatus switch
    {
        SystemStatus.Online => "En ligne",
        SystemStatus.Maintenance => "Maintenance",
        SystemStatus.Offline => "Hors ligne",
        _ => "Inconnu"
    };
}

public enum SystemStatus
{
    Online,
    Maintenance,
    Offline
}
