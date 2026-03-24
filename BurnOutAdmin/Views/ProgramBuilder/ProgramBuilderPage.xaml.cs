using BurnOutAdmin.ViewModels.ProgramBuilder;

namespace BurnOutAdmin.Views.ProgramBuilder;

public partial class ProgramBuilderPage : ContentPage
{
    public ProgramBuilderPage()
    {
        InitializeComponent();
        BindingContext = App.Current!.Handler!.MauiContext!.Services.GetRequiredService<ProgramBuilderViewModel>();
    }

    public ProgramBuilderPage(ProgramBuilderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ProgramBuilderViewModel vm)
        {
            await vm.LoadProgramCommand.ExecuteAsync(null);
        }
    }
}
