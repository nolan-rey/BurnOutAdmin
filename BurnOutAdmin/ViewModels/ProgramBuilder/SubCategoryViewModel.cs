using System.Collections.ObjectModel;
using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class SubCategoryViewModel : BaseViewModel
{
    public SubCategoryModel Model { get; }

    private readonly Action<SubCategoryViewModel>? _removeAction;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private int _sets;

    [ObservableProperty]
    private int _restTime;

    [ObservableProperty]
    private SubCategoryType _type;

    [ObservableProperty]
    private bool _isExpanded = true;

    public string ExpandIcon => IsExpanded ? "▼" : "▶";

    public ObservableCollection<ExerciseViewModel> Exercises { get; } = new();

    public SubCategoryViewModel(SubCategoryModel model, Action<SubCategoryViewModel>? removeAction = null)
    {
        Model = model;
        _removeAction = removeAction;

        _name = model.Name;
        _order = model.Order;
        _sets = model.Sets;
        _restTime = model.RestTime;
        _type = model.Type;

        foreach (var exercise in model.Exercises)
        {
            Exercises.Add(new ExerciseViewModel(exercise, RemoveExercise));
        }
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
    }

    partial void OnOrderChanged(int value)
    {
        Model.Order = value;
    }

    partial void OnSetsChanged(int value)
    {
        Model.Sets = value;
    }

    partial void OnRestTimeChanged(int value)
    {
        Model.RestTime = value;
    }

    partial void OnTypeChanged(SubCategoryType value)
    {
        Model.Type = value;
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpandIcon));
    }

    [RelayCommand]
    private void AddExercise()
    {
        var exercise = CreateExercise();
        Exercises.Add(exercise);
    }

    [RelayCommand]
    private void DeleteSubCategory()
    {
        if (_removeAction == null)
            return;

        _removeAction(this);
    }

    public void AddExerciseFromLibrary(ExerciseLibraryItem libraryItem)
    {
        var model = new ExerciseModel
        {
            Id = Guid.NewGuid(),
            Name = libraryItem.Name,
            Sets = 3,
            Reps = 10,
            Weight = 0,
            Rpe = 0,
            Order = Exercises.Count + 1
        };

        Model.Exercises.Add(model);
        Exercises.Add(new ExerciseViewModel(model, RemoveExercise));
    }

    private ExerciseViewModel CreateExercise()
    {
        var model = new ExerciseModel
        {
            Id = Guid.NewGuid(),
            Name = "Nouvel Exercice",
            Sets = 3,
            Reps = 10,
            Weight = 0,
            Rpe = 0,
            Order = Exercises.Count + 1
        };

        Model.Exercises.Add(model);
        return new ExerciseViewModel(model, RemoveExercise);
    }

    private void RemoveExercise(ExerciseViewModel exercise)
    {
        Exercises.Remove(exercise);
        Model.Exercises.Remove(exercise.Model);
        RecalculateOrders();
    }

    private void RecalculateOrders()
    {
        for (var i = 0; i < Exercises.Count; i++)
        {
            Exercises[i].Order = i + 1;
        }
    }
}
