using BurnOutAdmin.ViewModels;

namespace BurnOutAdmin.Services;

public interface INavigationService
{
    BaseViewModel? CurrentViewModel { get; }
    event Action<BaseViewModel>? CurrentViewModelChanged;
    
    void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
    void NavigateTo(string pageKey);
}
