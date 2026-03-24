using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class ExerciseViewModel : BaseViewModel
{
    public ExerciseModel Model { get; }

    private readonly Action<ExerciseViewModel>? _removeAction;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private int _sets;

    [ObservableProperty]
    private int _reps;

    [ObservableProperty]
    private double _weight;

    [ObservableProperty]
    private double _rpe;

    [ObservableProperty]
    private int _order;

    public ExerciseViewModel(ExerciseModel model, Action<ExerciseViewModel>? removeAction = null)
    {
        Model = model;
        _removeAction = removeAction;

        _name = model.Name;
        _sets = model.Sets;
        _reps = model.Reps;
        _weight = model.Weight;
        _rpe = model.Rpe;
        _order = model.Order;
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
    }

    partial void OnSetsChanged(int value)
    {
        Model.Sets = value;
    }

    partial void OnRepsChanged(int value)
    {
        Model.Reps = value;
    }

    partial void OnWeightChanged(double value)
    {
        Model.Weight = value;
    }

    partial void OnRpeChanged(double value)
    {
        Model.Rpe = value;
    }

    partial void OnOrderChanged(int value)
    {
        Model.Order = value;
    }

    [RelayCommand]
    private void DeleteExercise()
    {
        if (_removeAction == null)
            return;

        _removeAction(this);
    }
}
