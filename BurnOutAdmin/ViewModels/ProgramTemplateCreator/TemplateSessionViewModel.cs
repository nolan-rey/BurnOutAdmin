using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramTemplateCreator;

public partial class TemplateSessionViewModel : ObservableObject
{
    private readonly Action<TemplateSessionViewModel>? _removeAction;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _order;

    public string DisplayName => $"Séance {Order} : {Name}";

    public ObservableCollection<TemplateExerciseRowViewModel> ExerciseRows { get; } = new();

    public TemplateSessionViewModel(Action<TemplateSessionViewModel>? removeAction = null)
    {
        _removeAction = removeAction;
    }

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnOrderChanged(int value) => OnPropertyChanged(nameof(DisplayName));

    [RelayCommand]
    private void AddExercise()
    {
        var row = new TemplateExerciseRowViewModel(RemoveExercise);
        ExerciseRows.Add(row);
    }

    [RelayCommand]
    private void DeleteSession()
    {
        _removeAction?.Invoke(this);
    }

    private void RemoveExercise(TemplateExerciseRowViewModel row)
    {
        ExerciseRows.Remove(row);
    }
}
