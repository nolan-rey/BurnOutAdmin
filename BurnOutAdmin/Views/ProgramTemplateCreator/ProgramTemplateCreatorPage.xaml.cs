using BurnOutAdmin.ViewModels.ProgramTemplateCreator;

namespace BurnOutAdmin.Views.ProgramTemplateCreator;

public partial class ProgramTemplateCreatorPage : ContentPage
{
    public ProgramTemplateCreatorPage()
    {
        InitializeComponent();
    }

    public ProgramTemplateCreatorPage(ProgramTemplateCreatorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
