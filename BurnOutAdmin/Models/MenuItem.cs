namespace BurnOutAdmin.Models;

public class SidebarMenuItem
{
    public string Title { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
