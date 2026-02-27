using BurnOutAdmin.Views.Clients;

namespace BurnOutAdmin;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(ClientsView), typeof(ClientsView));
    }
}