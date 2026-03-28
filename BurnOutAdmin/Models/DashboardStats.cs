namespace BurnOutAdmin.Models;

public class DashboardStats
{
    public int ActiveClientsCount { get; set; }
    public int TodayNfcAccessCount { get; set; }
    public int AlertsCount { get; set; }
    public SystemStatus SystemStatus { get; set; }
    public int ActiveChallengesCount { get; set; }
    public int ActiveProgrammesCount { get; set; }
    public int ExpiringSubscriptionsCount { get; set; }
    public List<DashboardNfcEntry> RecentNfcAccesses { get; set; } = new();

    public string SystemStatusText => SystemStatus switch
    {
        SystemStatus.Online => "En ligne",
        SystemStatus.Maintenance => "Maintenance",
        SystemStatus.Offline => "Hors ligne",
        _ => "Inconnu"
    };

    public bool IsOnline => SystemStatus == SystemStatus.Online;
}

public class DashboardNfcEntry
{
    public string Time { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Door { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
    public string StatusText => IsAuthorized ? "Validé" : "Refusé";
    public string StatusColor => IsAuthorized ? "#166534" : "#991B1B";
    public string StatusBackground => IsAuthorized ? "#F0FDF4" : "#FEF2F2";
}

public enum SystemStatus
{
    Online,
    Maintenance,
    Offline
}
