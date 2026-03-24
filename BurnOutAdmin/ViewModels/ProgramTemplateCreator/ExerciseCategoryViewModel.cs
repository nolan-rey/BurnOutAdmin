using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramTemplateCreator;

public partial class ExerciseCategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _iconGlyph = string.Empty;

    [ObservableProperty]
    private Color _iconColor = Colors.Grey;

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<LibraryExerciseViewModel> Exercises { get; } = new();

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }
}
