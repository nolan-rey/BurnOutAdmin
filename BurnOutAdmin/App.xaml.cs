using BurnOutAdmin.Services.Api;
using BurnOutAdmin.Services.Nfc;
using BurnOutAdmin.Views.Auth;
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
        Console.WriteLine("[App] ========== DÉMARRAGE APPLICATION ==========");
        Console.WriteLine($"[App] Plateforme: {DeviceInfo.Platform}");
        Console.WriteLine($"[App] OS: {DeviceInfo.VersionString}");

        // ── Vérification du token d'authentification ──────────────
        var auth = _serviceProvider.GetRequiredService<IApiAuthService>();
        Page startPage;

        if (auth.IsAuthenticated)
        {
            Console.WriteLine($"[App] Token valide trouvé pour {auth.CurrentUserEmail} — accès direct au shell.");
            startPage = _serviceProvider.GetRequiredService<MainShell>();
            StartNfcOrchestrator();
        }
        else
        {
            Console.WriteLine("[App] Aucun token valide — affichage de la page de connexion.");
            startPage = _serviceProvider.GetRequiredService<LoginView>();
        }

        return new Window(startPage);
    }

    /// <summary>
    /// Appelé depuis LoginViewModel après connexion réussie pour démarrer l'orchestrateur NFC.
    /// </summary>
    public void StartNfcOrchestrator()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("[App] Résolution INfcOrchestrator depuis le DI...");
                var orchestrator = _serviceProvider.GetRequiredService<INfcOrchestrator>();
                Console.WriteLine($"[App] Orchestrateur résolu: {orchestrator.GetType().Name}");
                await orchestrator.StartAsync();
                Console.WriteLine($"[App] Orchestrateur démarré. Reader={orchestrator.IsReaderConnected}, MQTT={orchestrator.IsMqttConnected}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] ERREUR démarrage orchestrateur: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[App] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        });
    }
}
