namespace BurnOutAdmin;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        
        Routing.RegisterRoute("MainPage", typeof(MainPage));
        
        var mainPage = serviceProvider.GetRequiredService<MainPage>();
        Items.Add(new ShellContent
        {
            Title = "Home",
            Route = "MainPage",
            Content = mainPage
        });
    }
}