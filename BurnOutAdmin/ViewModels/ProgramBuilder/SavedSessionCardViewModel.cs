using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class SavedSessionCardViewModel : ObservableObject
{
    public SavedSessionEntry Entry { get; }

    private readonly Action<SavedSessionCardViewModel>? _deleteAction;
    private readonly Action<SavedSessionCardViewModel>? _toggleAction;

    [ObservableProperty] private bool _isSelected;

    public string Name => Entry.Name;
    public string Description => Entry.Description;
    public int ExerciseCount => Entry.ExerciseCount;
    public int CategoryCount => Entry.CategoryCount;
    public string CreatedAt => Entry.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy");

    public string SubTitle =>
        $"{CategoryCount} catégorie{(CategoryCount > 1 ? "s" : "")} · {ExerciseCount} exercice{(ExerciseCount > 1 ? "s" : "")}";

    // ── Apparence dynamique selon sélection ───────────────────────
    public Color CardBackground => IsSelected ? Color.FromArgb("#EFF6FF") : Colors.White;
    public Color CardBorderColor => IsSelected ? Color.FromArgb("#60A5FA") : Color.FromArgb("#E2E8F0");
    public Color CardNameColor => IsSelected ? Color.FromArgb("#1D4ED8") : Color.FromArgb("#1E293B");
    public string SelectionMark => IsSelected ? "✓" : "";
    public bool ShowSelectionMark => IsSelected;

    public SavedSessionCardViewModel(
        SavedSessionEntry entry,
        Action<SavedSessionCardViewModel>? deleteAction = null,
        Action<SavedSessionCardViewModel>? toggleAction = null)
    {
        Entry = entry;
        _deleteAction = deleteAction;
        _toggleAction = toggleAction;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(CardBackground));
        OnPropertyChanged(nameof(CardBorderColor));
        OnPropertyChanged(nameof(CardNameColor));
        OnPropertyChanged(nameof(SelectionMark));
        OnPropertyChanged(nameof(ShowSelectionMark));
    }

    [RelayCommand]
    private void Delete() => _deleteAction?.Invoke(this);

    [RelayCommand]
    private void ToggleSelection() => _toggleAction?.Invoke(this);
}
