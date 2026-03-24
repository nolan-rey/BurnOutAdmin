using BurnOutAdmin.Views.ProgramBuilder;

namespace BurnOutAdmin;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute("program-builder", typeof(ProgramBuilderPage));
    }
}