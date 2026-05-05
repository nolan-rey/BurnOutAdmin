using BurnOutAdmin.ViewModels.Auth;

namespace BurnOutAdmin.Views.Auth;

public partial class LoginView : ContentPage
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
