using System.Collections.ObjectModel;
using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class LibraryCategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<ExerciseLibraryItem> Exercises { get; }

    public LibraryCategoryViewModel(string name, List<ExerciseLibraryItem> exercises)
    {
        _name = name;
        Exercises = new ObservableCollection<ExerciseLibraryItem>(exercises);
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }
}
