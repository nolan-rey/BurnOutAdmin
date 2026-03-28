using CommunityToolkit.Mvvm.ComponentModel;

namespace BurnOutAdmin.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>
    /// Called by NavigationService after the ViewModel is resolved from DI.
    /// Override to perform async initialization without using fire-and-forget in constructors.
    /// </summary>
    public virtual Task OnActivatedAsync() => Task.CompletedTask;
}
