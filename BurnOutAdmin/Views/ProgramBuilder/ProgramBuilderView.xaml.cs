using BurnOutAdmin.Models.Program;
using BurnOutAdmin.ViewModels.ProgramBuilder;
using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.Views.ProgramBuilder;

public partial class ProgramBuilderView : ContentView
{
    private ProgramBuilderViewModel? ViewModel => BindingContext as ProgramBuilderViewModel;

    public ProgramBuilderView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (ViewModel is not null)
            _ = ViewModel.LoadCommand.ExecuteAsync(null);
    }

    // ── Drag depuis la bibliothèque ──────────────────────────────

    private void OnLibraryExerciseDragStarting(object sender, DragStartingEventArgs e)
    {
        if ((sender as Element)?.BindingContext is not ExerciseLibraryItem item) return;

        e.Data.Properties["ExerciseName"] = item.Name;
        e.Data.Properties["ExerciseCategory"] = item.Category;
        // Ne pas définir e.Data.Text — MAUI l'insère dans l'Entry focusée
    }

    // ── Drop sur une sous-catégorie ──────────────────────────────

    private void OnSubCategoryDrop(object sender, DropEventArgs e)
    {
        if ((sender as Element)?.BindingContext is not SubCategoryViewModel subCat) return;

        var name = e.Data.Properties.TryGetValue("ExerciseName", out var n) ? n?.ToString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(name)) return;

        var item = new ExerciseLibraryItem { Name = name };
        subCat.AddExerciseFromLibrary(item);
        subCat.IsExpanded = true;

        if (sender is Border border)
        {
            border.BackgroundColor = Colors.White;
            border.Stroke = new SolidColorBrush(Color.FromArgb("#E2E8F0"));
        }
    }

    private void OnSubCategoryDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        if (sender is Border border)
        {
            border.BackgroundColor = Color.FromArgb("#F0F9FF");
            border.Stroke = new SolidColorBrush(Color.FromArgb("#93C5FD"));
        }
    }

    private void OnSubCategoryDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.BackgroundColor = Colors.White;
            border.Stroke = new SolidColorBrush(Color.FromArgb("#E2E8F0"));
        }
    }
}
