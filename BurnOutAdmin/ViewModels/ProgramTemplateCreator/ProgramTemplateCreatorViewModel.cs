using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramTemplateCreator;

public partial class ProgramTemplateCreatorViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _templateTitle = string.Empty;

    [ObservableProperty]
    private string _templateDescription = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<ExerciseCategoryViewModel> Categories { get; } = new();

    public ObservableCollection<TemplateSessionViewModel> Sessions { get; } = new();

    public ProgramTemplateCreatorViewModel()
    {
        Title = "Créateur de Modèle";
        LoadSampleData();
    }

    [RelayCommand]
    private void AddSession()
    {
        var session = new TemplateSessionViewModel(RemoveSession)
        {
            Order = Sessions.Count + 1,
            Name = "Nouvelle Séance"
        };
        Sessions.Add(session);
    }

    [RelayCommand]
    private void AssignToClient()
    {
        // UI only — no business logic
    }

    [RelayCommand]
    private void SaveToLibrary()
    {
        // UI only — no business logic
    }

    [RelayCommand]
    private void Publish()
    {
        // UI only — no business logic
    }

    private void RemoveSession(TemplateSessionViewModel session)
    {
        Sessions.Remove(session);
        RecalculateOrders();
    }

    private void RecalculateOrders()
    {
        for (var i = 0; i < Sessions.Count; i++)
        {
            Sessions[i].Order = i + 1;
        }
    }

    private void LoadSampleData()
    {
        // Exercise Library categories
        var echauffement = new ExerciseCategoryViewModel
        {
            Name = "Échauffement",
            IconGlyph = "🔥",
            IconColor = Color.FromArgb("#F97316"),
            IsExpanded = false
        };

        var musculation = new ExerciseCategoryViewModel
        {
            Name = "Musculation",
            IconGlyph = "🏋",
            IconColor = Color.FromArgb("#2563EB"),
            IsExpanded = true
        };
        musculation.Exercises.Add(new LibraryExerciseViewModel
        {
            Name = "Squat Barre",
            Tags = new ObservableCollection<string> { "Jambes", "Force" }
        });
        musculation.Exercises.Add(new LibraryExerciseViewModel
        {
            Name = "Développé Couché",
            Tags = new ObservableCollection<string> { "Pecs", "Force" }
        });
        musculation.Exercises.Add(new LibraryExerciseViewModel
        {
            Name = "Soulevé de Terre",
            Tags = new ObservableCollection<string> { "Dos", "Complet" }
        });

        var cardio = new ExerciseCategoryViewModel
        {
            Name = "Cardio",
            IconGlyph = "💓",
            IconColor = Color.FromArgb("#EF4444"),
            IsExpanded = false
        };

        var recuperation = new ExerciseCategoryViewModel
        {
            Name = "Récupération",
            IconGlyph = "🍃",
            IconColor = Color.FromArgb("#22C55E"),
            IsExpanded = false
        };

        Categories.Add(echauffement);
        Categories.Add(musculation);
        Categories.Add(cardio);
        Categories.Add(recuperation);

        // Sample sessions
        var session1 = new TemplateSessionViewModel(RemoveSession)
        {
            Order = 1,
            Name = "Force A"
        };

        var session2 = new TemplateSessionViewModel(RemoveSession)
        {
            Order = 2,
            Name = "HIIT Intervals"
        };

        Sessions.Add(session1);
        Sessions.Add(session2);
    }
}
