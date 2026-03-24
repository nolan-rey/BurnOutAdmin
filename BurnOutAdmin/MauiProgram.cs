using BurnOutAdmin.Services;
using BurnOutAdmin.Services.Mqtt;
using BurnOutAdmin.Services.Nfc;
using BurnOutAdmin.Services.ProgramBuilder;
using BurnOutAdmin.Services.Rfid;
using BurnOutAdmin.ViewModels;
using BurnOutAdmin.ViewModels.ProgramBuilder;
using BurnOutAdmin.ViewModels.ProgramTemplateCreator;
using BurnOutAdmin.Views.ProgramBuilder;
using BurnOutAdmin.Views.ProgramTemplateCreator;
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

        // ── Infrastructure NFC ──────────────────────────────────────

        // RFID Reader : Dummy on non-Windows platforms
        builder.Services.AddSingleton<IRfidReaderService, DummyRfidReaderService>();

        // MQTT Client (Raspberry Pi broker — réseau local)
        builder.Services.AddSingleton<IMqttService>(sp =>
            new MqttService("172.31.254.200", 1883));

        // SQLite Log Repository
        builder.Services.AddSingleton<INfcLogRepository, SqliteNfcLogRepository>();

        // NFC Orchestrator (coordination RFID → MQTT → SQLite)
        builder.Services.AddSingleton<INfcOrchestrator, NfcOrchestrator>();

        // ── Services métier ─────────────────────────────────────────

        builder.Services.AddSingleton<IUserService, UserService>();
        builder.Services.AddSingleton<IClientService, MockClientService>();
        builder.Services.AddSingleton<INfcService, MockNfcService>();
        builder.Services.AddSingleton<IProgrammeService, MockProgrammeService>();
        builder.Services.AddSingleton<IChallengeService, MockChallengeService>();
        builder.Services.AddSingleton<IDashboardService, MockDashboardService>();
        builder.Services.AddSingleton<IAlertService, MauiAlertService>();
        builder.Services.AddSingleton<IProgramBuilderService, MockProgramBuilderService>();
        builder.Services.AddSingleton<IProgramAssignmentService, MockProgramAssignmentService>();
        
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
        builder.Services.AddTransient<ProgramTemplateCreatorViewModel>();
        
        // Views
        builder.Services.AddSingleton<MainShell>();
        builder.Services.AddTransient<ProgramBuilderPage>();
        builder.Services.AddTransient<ProgramTemplateCreatorPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}