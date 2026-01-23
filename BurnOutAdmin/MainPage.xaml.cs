using BurnOutAdmin.ViewModels;

namespace BurnOutAdmin;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}