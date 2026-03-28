using BurnOutAdmin.ViewModels;
using BurnOutAdmin.ViewModels.ProgramBuilder;

namespace BurnOutAdmin.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _pageKeyToViewModelType;
    private BaseViewModel? _currentViewModel;

    public BaseViewModel? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (_currentViewModel != value)
            {
                _currentViewModel = value;
                CurrentViewModelChanged?.Invoke(value!);
            }
        }
    }

    public event Action<BaseViewModel>? CurrentViewModelChanged;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pageKeyToViewModelType = new Dictionary<string, Type>
        {
            { "Dashboard", typeof(DashboardViewModel) },
            { "Clients", typeof(ClientsViewModel) },
            { "Programmes", typeof(ProgrammesViewModel) },
            { "ProgramBuilder", typeof(ProgramBuilderViewModel) },
            { "Challenges", typeof(ChallengesViewModel) },
            { "NfcLogs", typeof(NfcLogsViewModel) },
            { "Settings", typeof(SettingsViewModel) },
        };
    }

    public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentViewModel = viewModel;
        ActivateAsync(viewModel);
    }

    public void NavigateTo(string pageKey)
    {
        if (_pageKeyToViewModelType.TryGetValue(pageKey, out var viewModelType))
        {
            var viewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
            CurrentViewModel = viewModel;
            ActivateAsync(viewModel);
        }
    }

    private async void ActivateAsync(BaseViewModel viewModel)
    {
        try
        {
            await viewModel.OnActivatedAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NavigationService] ViewModel activation error: {ex.Message}");
        }
    }
}
