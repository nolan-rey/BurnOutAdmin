using System.Collections.ObjectModel;
using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class LibraryCategoryViewModel : ObservableObject
{
    private readonly List<ExerciseLibraryItem> _allExercises;

    [ObservableProperty] private string _name;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isVisible = true;

    public ObservableCollection<ExerciseLibraryItem> Exercises { get; } = new();

    public LibraryCategoryViewModel(string name, List<ExerciseLibraryItem> exercises)
    {
        _name = name;
        _allExercises = exercises;
        foreach (var e in exercises)
            Exercises.Add(e);
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    public void ApplyFilter(string lowerSearch)
    {
        if (string.IsNullOrEmpty(lowerSearch))
        {
            Exercises.Clear();
            foreach (var e in _allExercises) Exercises.Add(e);
            IsVisible = true;
            return;
        }

        var filtered = _allExercises
            .Where(e => e.Name.ToLower().Contains(lowerSearch) || e.MuscleGroup.ToLower().Contains(lowerSearch))
            .ToList();

        Exercises.Clear();
        foreach (var e in filtered) Exercises.Add(e);
        IsVisible = filtered.Count > 0;
        if (filtered.Count > 0) IsExpanded = true;
    }
}
