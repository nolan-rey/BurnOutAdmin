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
        Console.WriteLine("[App] ========== DÉMARRAGE APPLICATION ==========");
        Console.WriteLine($"[App] Plateforme: {DeviceInfo.Platform}");
        Console.WriteLine($"[App] OS: {DeviceInfo.VersionString}");
        _ = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("[App] Résolution INfcOrchestrator depuis le DI...");
                var orchestrator = _serviceProvider.GetRequiredService<INfcOrchestrator>();
                Console.WriteLine($"[App] Orchestrateur résolu: {orchestrator.GetType().Name}");
                Console.WriteLine("[App] Appel orchestrator.StartAsync()...");
                await orchestrator.StartAsync();
                Console.WriteLine($"[App] Orchestrateur démarré. Reader={orchestrator.IsReaderConnected}, MQTT={orchestrator.IsMqttConnected}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] ERREUR démarrage orchestrateur: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[App] Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[App] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        });

        return new Window(mainShell);
    }
}