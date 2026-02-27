using BurnOutAdmin.ViewModels;
using BurnOutAdmin.Views.Shell;

namespace BurnOutAdmin;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainShell = _serviceProvider.GetRequiredService<MainShell>();
        return new Window(mainShell);
    }
}