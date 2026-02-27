using BurnOutAdmin.Services;
using BurnOutAdmin.ViewModels;
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
            });

        // Services - Mock implementations for MVP
        builder.Services.AddSingleton<IUserService, UserService>();
        builder.Services.AddSingleton<IClientService, MockClientService>();
        builder.Services.AddSingleton<INfcService, MockNfcService>();
        builder.Services.AddSingleton<IProgrammeService, MockProgrammeService>();
        builder.Services.AddSingleton<IChallengeService, MockChallengeService>();
        builder.Services.AddSingleton<IDashboardService, MockDashboardService>();
        builder.Services.AddSingleton<IAlertService, MauiAlertService>();
        
        // Navigation Service - Singleton for app-wide navigation
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        
        // ViewModels
        builder.Services.AddSingleton<MainShellViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ClientsViewModel>();
        builder.Services.AddTransient<ProgrammesViewModel>();
        builder.Services.AddTransient<ChallengesViewModel>();
        builder.Services.AddTransient<NfcLogsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        
        // Views
        builder.Services.AddSingleton<MainShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}