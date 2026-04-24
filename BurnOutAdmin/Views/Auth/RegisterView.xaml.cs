using BurnOutAdmin.ViewModels.Auth;

namespace BurnOutAdmin.Views.Auth;

public partial class RegisterView : ContentPage
{
    public RegisterView(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
