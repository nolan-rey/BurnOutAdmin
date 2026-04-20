using BurnOutAdmin.Models.Program;
using BurnOutAdmin.ViewModels.ProgramBuilder;

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

    // ── Drop sur une séance ──────────────────────────────────────

    private void OnSessionDrop(object sender, DropEventArgs e)
    {
        if (ViewModel is null) return;
        if ((sender as Element)?.BindingContext is not SessionViewModel session) return;

        var name = e.Data.Properties.TryGetValue("ExerciseName", out var n) ? n?.ToString() ?? "" : "";
        var category = e.Data.Properties.TryGetValue("ExerciseCategory", out var c) ? c?.ToString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(name)) return;

        ViewModel.DropExerciseOnSession(session, name, category);

        // Réinitialiser le style du fond de la séance
        if (sender is Border border)
            border.BackgroundColor = Color.FromArgb("#F8FAFC");
    }

    private void OnSessionDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        if (sender is Border border)
            border.BackgroundColor = Color.FromArgb("#EFF6FF");
    }

    private void OnSessionDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
            border.BackgroundColor = Color.FromArgb("#F8FAFC");
    }
}
