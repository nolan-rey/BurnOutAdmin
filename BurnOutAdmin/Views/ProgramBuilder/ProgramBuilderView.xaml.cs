using BurnOutAdmin.ViewModels.ProgramBuilder;

namespace BurnOutAdmin.Views.ProgramBuilder;

public partial class ProgramBuilderView : ContentView
{
    public ProgramBuilderView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is ProgramBuilderViewModel vm)
        {
            vm.LoadProgramCommand.Execute(null);
        }
    }
}
