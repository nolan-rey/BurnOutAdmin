using BurnOutAdmin.ViewModels;
using BurnOutAdmin.ViewModels.ProgramBuilder;
using BurnOutAdmin.Views.Dashboard;
using BurnOutAdmin.Views.Clients;
using BurnOutAdmin.Views.Programmes;
using BurnOutAdmin.Views.ProgramBuilder;
using BurnOutAdmin.Views.Challenges;
using BurnOutAdmin.Views.NfcLogs;
using BurnOutAdmin.Views.Settings;

namespace BurnOutAdmin.Views.Shell;

public partial class MainShell : ContentPage
{
    private readonly MainShellViewModel _viewModel;
    
    public MainShell(MainShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        
        // Subscribe to navigation changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Show initial view
        UpdateContentView();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainShellViewModel.CurrentViewModel))
        {
            UpdateContentView();
        }
    }

    private void UpdateContentView()
    {
        var currentVm = _viewModel.CurrentViewModel;
        if (currentVm == null)
        {
            ContentArea.Content = new Label { Text = "Sélectionnez une page", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            return;
        }

        View? newView = currentVm switch
        {
            DashboardViewModel vm => new DashboardView { BindingContext = vm },
            ClientsViewModel vm => new ClientsView { BindingContext = vm },
            ProgrammesViewModel vm => new ProgrammesView { BindingContext = vm },
            ProgramBuilderViewModel vm => new ProgramBuilderView { BindingContext = vm },
            ChallengesViewModel vm => new ChallengesView { BindingContext = vm },
            NfcLogsViewModel vm => new NfcLogsView { BindingContext = vm },
            SettingsViewModel vm => new SettingsView { BindingContext = vm },
            _ => null
        };

        ContentArea.Content = newView ?? new Label { Text = "Page non trouvée", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    }
}
