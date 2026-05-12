using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api;
using BurnOutAdmin.Services.ExerciseLibrary;
using BurnOutAdmin.Services.SessionLibrary;
// SqliteExerciseLibraryService et SqliteSessionLibraryService remplacés par les services API
using BurnOutAdmin.Services.Mqtt;
using BurnOutAdmin.Services.Nfc;
using BurnOutAdmin.Services.Rfid;
using BurnOutAdmin.ViewModels;
using BurnOutAdmin.ViewModels.Auth;
using BurnOutAdmin.ViewModels.ProgramBuilder;
using BurnOutAdmin.Views.Auth;
using BurnOutAdmin.Views.ProgramBuilder;
using BurnOutAdmin.Views.Shell;
using Microsoft.Extensions.Logging;

namespace BurnOutAdmin;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        // ── Infrastructure API REST ──────────────────────────────────
        builder.Services.AddSingleton<IApiAuthService, ApiAuthService>();
        builder.Services.AddSingleton<ApiHttpClient>();

        // ── Infrastructure NFC ──────────────────────────────────────

        // RFID Reader : SL500 via DLL native sur Windows, Dummy sur les autres plateformes
#if WINDOWS
        builder.Services.AddSingleton<IRfidReaderService>(sp =>
            new SL500NativeRfidReaderService(comPort: 4, baudRate: 9600));
#else
        builder.Services.AddSingleton<IRfidReaderService, DummyRfidReaderService>();
#endif

        // MQTT Client (Raspberry Pi broker — réseau local)
        builder.Services.AddSingleton<IMqttService>(sp =>
            new MqttService(AppConfiguration.MqttBrokerHost, AppConfiguration.MqttBrokerPort));

        // SQLite Log Repository (logs NFC temps réel en local)
        builder.Services.AddSingleton<INfcLogRepository, SqliteNfcLogRepository>();

        // NFC Orchestrator (coordination RFID → MQTT → SQLite)
        builder.Services.AddSingleton<INfcOrchestrator, NfcOrchestrator>();

        // ── Services métier — API REST ──────────────────────────────

        builder.Services.AddSingleton<IUserService, UserService>();

        // Clients → API réelle (remplace MockClientService)
        builder.Services.AddSingleton<IClientService, ApiClientService>();

        // NFC Service → API uniquement (données cloud, pas de fallback SQLite)
        builder.Services.AddSingleton<INfcService, ApiNfcService>();

        // Programmes → API réelle (avec nouveaux champs type/niveau)
        builder.Services.AddSingleton<IProgrammeService, ApiProgrammeService>();

        // Challenges → API réelle (remplace SqliteChallengeService)
        builder.Services.AddSingleton<IChallengeService, ApiChallengeService>();

        // Dashboard → API réelle avec fallback agrégation (remplace MockDashboardService)
        builder.Services.AddSingleton<IDashboardService, ApiDashboardService>();

        // Assignations programme → API réelle (remplace MockProgramAssignmentService)
        builder.Services.AddSingleton<IProgramAssignmentService, ApiProgramAssignmentService>();

        // Séances attachées à un programme (jointure programmes ↔ seances)
        builder.Services.AddSingleton<IProgrammeSeanceService, ApiProgrammeSeanceService>();

        // Bibliothèque d'exercices → API (27 exercices par défaut depuis la BDD)
        builder.Services.AddSingleton<IExerciseLibraryService, ApiExerciseLibraryService>();

        // Bibliothèque de séances → API (séances builder depuis seances_builder)
        builder.Services.AddSingleton<ISessionLibraryService, ApiSessionLibraryService>();

        // Services UI
        builder.Services.AddSingleton<IAlertService, MauiAlertService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // ── ViewModels ──────────────────────────────────────────────

        builder.Services.AddSingleton<MainShellViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ClientsViewModel>();
        builder.Services.AddTransient<ProgrammesViewModel>();
        builder.Services.AddTransient<ProgramBuilderViewModel>();
        builder.Services.AddTransient<ChallengesViewModel>();
        builder.Services.AddTransient<NfcLogsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();

        // ── Views ───────────────────────────────────────────────────

        builder.Services.AddSingleton<MainShell>();
        builder.Services.AddTransient<ProgramBuilderPage>();
        builder.Services.AddTransient<LoginView>();
        builder.Services.AddTransient<RegisterView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
