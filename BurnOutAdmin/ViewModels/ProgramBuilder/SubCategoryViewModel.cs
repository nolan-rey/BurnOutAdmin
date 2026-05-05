using System.Collections.ObjectModel;
using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class SubCategoryViewModel : BaseViewModel
{
    public SubCategoryModel Model { get; }
    private readonly Action<SubCategoryViewModel>? _removeAction;

    [ObservableProperty] private string _name;
    [ObservableProperty] private int    _order;
    [ObservableProperty] private int    _sets;
    [ObservableProperty] private int    _restTime;
    [ObservableProperty] private SubCategoryType _type;
    [ObservableProperty] private bool   _isExpanded = true;

    // ── Note ──────────────────────────────────────────────────────
    [ObservableProperty] private string _note;

    // ── Repos formaté (secondes → "0'", "2'", "90''") ────────────
    public string RestTimeDisplay
    {
        get
        {
            if (RestTime == 0) return "0'";
            if (RestTime % 60 == 0) return $"{RestTime / 60}'";
            return $"{RestTime}''";
        }
    }

    // ── Label header bloc ─────────────────────────────────────────
    public string SetsLabel => $"{Sets} série{(Sets > 1 ? "s" : "")}  ({RestTimeDisplay} récup)";

    public string ExpandIcon => IsExpanded ? "▼" : "▶";

    public ObservableCollection<ExerciseViewModel> Exercises { get; } = new();

    public SubCategoryViewModel(SubCategoryModel model, Action<SubCategoryViewModel>? removeAction = null)
    {
        Model         = model;
        _removeAction = removeAction;

        _name     = model.Name;
        _order    = model.Order;
        _sets     = model.Sets;
        _restTime = model.RestTime;
        _type     = model.Type;
        _note     = model.Note;

        foreach (var exercise in model.Exercises)
            Exercises.Add(new ExerciseViewModel(exercise, RemoveExercise));
    }

    // ── Sync model ────────────────────────────────────────────────
    partial void OnNameChanged(string v)  { Model.Name     = v; }
    partial void OnOrderChanged(int v)    { Model.Order    = v; }
    partial void OnSetsChanged(int v)     { Model.Sets     = v; OnPropertyChanged(nameof(SetsLabel)); }
    partial void OnRestTimeChanged(int v) { Model.RestTime = v; OnPropertyChanged(nameof(RestTimeDisplay)); OnPropertyChanged(nameof(SetsLabel)); }
    partial void OnNoteChanged(string v)  { Model.Note     = v; }

    // ── Commandes ─────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpandIcon));
    }

    [RelayCommand]
    private void AddExercise()
    {
        var model = new ExerciseModel
        {
            Id       = Guid.NewGuid(),
            Name     = "Nouvel exercice",
            RepsText = "10",
            Order    = Exercises.Count + 1
        };
        Model.Exercises.Add(model);
        Exercises.Add(new ExerciseViewModel(model, RemoveExercise));
    }

    [RelayCommand]
    private void DeleteSubCategory() => _removeAction?.Invoke(this);

    public void AddExerciseFromLibrary(ExerciseLibraryItem item)
    {
        var model = new ExerciseModel
        {
            Id       = Guid.NewGuid(),
            Name     = item.Name,
            RepsText = "10",
            Order    = Exercises.Count + 1
        };
        Model.Exercises.Add(model);
        Exercises.Add(new ExerciseViewModel(model, RemoveExercise));
        IsExpanded = true;
    }

    private void RemoveExercise(ExerciseViewModel ex)
    {
        Exercises.Remove(ex);
        Model.Exercises.Remove(ex.Model);
        RecalculateOrders();
    }

    private void RecalculateOrders()
    {
        for (var i = 0; i < Exercises.Count; i++)
            Exercises[i].Order = i + 1;
    }
}
