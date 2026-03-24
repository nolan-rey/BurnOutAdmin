using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BurnOutAdmin.ViewModels.ProgramTemplateCreator;

public partial class LibraryExerciseViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();
}
