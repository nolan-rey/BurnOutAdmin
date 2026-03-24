using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramTemplateCreator;

public partial class TemplateExerciseRowViewModel : ObservableObject
{
    private readonly Action<TemplateExerciseRowViewModel>? _removeAction;

    [ObservableProperty]
    private string _exerciseName = string.Empty;

    [ObservableProperty]
    private int _sets;

    [ObservableProperty]
    private int _reps;

    [ObservableProperty]
    private string _charge = string.Empty;

    [ObservableProperty]
    private int _rpe;

    public TemplateExerciseRowViewModel(Action<TemplateExerciseRowViewModel>? removeAction = null)
    {
        _removeAction = removeAction;
    }

    [RelayCommand]
    private void Delete()
    {
        _removeAction?.Invoke(this);
    }
}
