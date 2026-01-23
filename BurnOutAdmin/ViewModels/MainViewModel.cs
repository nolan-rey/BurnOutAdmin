using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    private string _counterText = "Click me";

    public MainViewModel()
    {
        Title = "BurnOut Admin";
    }

    [RelayCommand]
    private void IncrementCounter()
    {
        Count++;
        CounterText = Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";
        SemanticScreenReader.Announce(CounterText);
    }
}
