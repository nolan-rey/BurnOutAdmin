namespace BurnOutAdmin.Models;

public class Subscription
{
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool AutoRenewal { get; set; }
    
    public string StartDateFormatted => StartDate.ToString("dd/MM/yyyy");
    public string EndDateFormatted => EndDate.ToString("dd/MM/yyyy");
}
