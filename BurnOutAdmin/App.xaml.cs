using BurnOutAdmin.Services.Nfc;
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

        // Démarrer l'orchestrateur NFC automatiquement (lecteur RFID + MQTT)
        // Fire-and-forget : le reader écoute en continu dès le lancement de l'app
        _ = Task.Run(async () =>
        {
            try
            {
                var orchestrator = _serviceProvider.GetRequiredService<INfcOrchestrator>();
                await orchestrator.StartAsync();
                System.Diagnostics.Debug.WriteLine("[App] Orchestrateur NFC démarré automatiquement.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Erreur démarrage orchestrateur: {ex.Message}");
            }
        });

        return new Window(mainShell);
    }
}