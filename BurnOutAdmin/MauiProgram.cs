using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Api;
using BurnOutAdmin.Services.Challenges;
using BurnOutAdmin.Services.ExerciseLibrary;
using BurnOutAdmin.Services.SessionLibrary;
using BurnOutAdmin.Services.Mqtt;
using BurnOutAdmin.Services.Nfc;
using BurnOutAdmin.Services.Rfid;
using BurnOutAdmin.ViewModels;
using BurnOutAdmin.ViewModels.Auth;
using BurnOutAdmin.ViewModels.ProgramBuilder;
using BurnOutAdmin.Views.Auth;
using BurnOutAdmin.ViewModels.Auth;
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
            new SL500NativeRfidReaderService(comPort: 4, baudRate: 9600)); // COM5 = index 4
#else
        builder.Services.AddSingleton<IRfidReaderService, DummyRfidReaderService>();
#endif

        // MQTT Client (Raspberry Pi broker — réseau local)
        builder.Services.AddSingleton<IMqttService>(sp =>
            new MqttService(AppConfiguration.MqttBrokerHost, AppConfiguration.MqttBrokerPort));

        // SQLite Log Repository
        builder.Services.AddSingleton<INfcLogRepository, SqliteNfcLogRepository>();

        // NFC Orchestrator (coordination RFID → MQTT → SQLite)
        builder.Services.AddSingleton<INfcOrchestrator, NfcOrchestrator>();

        // ── Services métier ─────────────────────────────────────────

        builder.Services.AddSingleton<IUserService, UserService>();
        builder.Services.AddSingleton<IClientService, MockClientService>();        // TODO: ApiClientService
        builder.Services.AddSingleton<INfcService, MockNfcService>();              // TODO: ApiNfcService
        builder.Services.AddSingleton<IProgrammeService, ApiProgrammeService>();   // ✅ Connecté à l'API
        builder.Services.AddSingleton<IChallengeService, SqliteChallengeService>(); // TODO: ApiChallengeService
        builder.Services.AddSingleton<IDashboardService, MockDashboardService>();   // TODO: ApiDashboardService
        builder.Services.AddSingleton<IAlertService, MauiAlertService>();
        builder.Services.AddSingleton<IProgramAssignmentService, MockProgramAssignmentService>(); // TODO: ApiProgramAssignmentService
        builder.Services.AddSingleton<IExerciseLibraryService, SqliteExerciseLibraryService>();   // TODO: ApiExerciseService
        builder.Services.AddSingleton<ISessionLibraryService, SqliteSessionLibraryService>();     // TODO: ApiSessionService

        // Navigation Service - Singleton for app-wide navigation
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
