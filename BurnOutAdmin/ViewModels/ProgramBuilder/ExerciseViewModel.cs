using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class ExerciseViewModel : BaseViewModel
{
    public ExerciseModel Model { get; }
    private readonly Action<ExerciseViewModel>? _removeAction;

    // ── Données exercice ──────────────────────────────────────────
    [ObservableProperty] private string _name;
    [ObservableProperty] private int    _order;

    /// Mode reps (false) ou temps (true)
    [ObservableProperty] private bool   _isTimeMode;
    /// Valeur : "10" en mode reps, "1'" en mode temps
    [ObservableProperty] private string _repsText;

    // ── Champs optionnels (valeurs) ───────────────────────────────
    [ObservableProperty] private string _weightText;
    [ObservableProperty] private string _tempo;
    [ObservableProperty] private string _amplitude;
    [ObservableProperty] private string _recupInter;
    [ObservableProperty] private string _rirText;

    // ── Visibilité champs (par exercice) ──────────────────────────
    [ObservableProperty] private bool _showPoids;
    [ObservableProperty] private bool _showTempo;
    [ObservableProperty] private bool _showAmplitude;
    [ObservableProperty] private bool _showRecupInter;
    [ObservableProperty] private bool _showRir;

    // ── Labels dynamiques ─────────────────────────────────────────
    public string RepsModeLabel   => IsTimeMode ? "⏱" : "🔁";
    public string RepsPlaceholder => IsTimeMode ? "ex: 1'" : "ex: 10";
    public bool HasOptionalFields => ShowPoids || ShowTempo || ShowAmplitude || ShowRecupInter || ShowRir;

    public ExerciseViewModel(ExerciseModel model, Action<ExerciseViewModel>? removeAction = null)
    {
        Model         = model;
        _removeAction = removeAction;

        _name        = model.Name;
        _order       = model.Order;
        _isTimeMode  = model.IsTimeMode;
        _repsText    = !string.IsNullOrEmpty(model.RepsText) ? model.RepsText
                       : model.Reps > 0 ? model.Reps.ToString() : "10";
        _weightText  = model.WeightText;
        _tempo       = model.Tempo;
        _amplitude   = model.Amplitude;
        _recupInter  = model.RecupInter;
        _rirText     = model.RirText;

        _showPoids      = model.ShowPoids;
        _showTempo      = model.ShowTempo;
        _showAmplitude  = model.ShowAmplitude;
        _showRecupInter = model.ShowRecupInter;
        _showRir        = model.ShowRir;
    }

    // ── Sync model : données ──────────────────────────────────────
    partial void OnNameChanged(string v)       { Model.Name       = v; }
    partial void OnOrderChanged(int v)         { Model.Order      = v; }
    partial void OnIsTimeModeChanged(bool v)   { Model.IsTimeMode = v; OnPropertyChanged(nameof(RepsModeLabel)); OnPropertyChanged(nameof(RepsPlaceholder)); }
    partial void OnRepsTextChanged(string v)   { Model.RepsText   = v; }
    partial void OnWeightTextChanged(string v) { Model.WeightText = v; }
    partial void OnTempoChanged(string v)      { Model.Tempo      = v; }
    partial void OnAmplitudeChanged(string v)  { Model.Amplitude  = v; }
    partial void OnRecupInterChanged(string v) { Model.RecupInter = v; }
    partial void OnRirTextChanged(string v)    { Model.RirText    = v; }

    // ── Sync model : visibilité ───────────────────────────────────
    partial void OnShowPoidsChanged(bool v)      { Model.ShowPoids      = v; OnPropertyChanged(nameof(HasOptionalFields)); }
    partial void OnShowTempoChanged(bool v)      { Model.ShowTempo      = v; OnPropertyChanged(nameof(HasOptionalFields)); }
    partial void OnShowAmplitudeChanged(bool v)  { Model.ShowAmplitude  = v; OnPropertyChanged(nameof(HasOptionalFields)); }
    partial void OnShowRecupInterChanged(bool v) { Model.ShowRecupInter = v; OnPropertyChanged(nameof(HasOptionalFields)); }
    partial void OnShowRirChanged(bool v)        { Model.ShowRir        = v; OnPropertyChanged(nameof(HasOptionalFields)); }

    // ── Commandes toggle champs optionnels ────────────────────────
    [RelayCommand] private void ToggleShowPoids()      => ShowPoids      = !ShowPoids;
    [RelayCommand] private void ToggleShowTempo()      => ShowTempo      = !ShowTempo;
    [RelayCommand] private void ToggleShowAmplitude()  => ShowAmplitude  = !ShowAmplitude;
    [RelayCommand] private void ToggleShowRecupInter() => ShowRecupInter = !ShowRecupInter;
    [RelayCommand] private void ToggleShowRir()        => ShowRir        = !ShowRir;

    // ── Commandes ─────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleTimeMode()
    {
        IsTimeMode = !IsTimeMode;
        if (string.IsNullOrWhiteSpace(RepsText))
            RepsText = IsTimeMode ? "1'" : "10";
    }

    [RelayCommand]
    private void DeleteExercise() => _removeAction?.Invoke(this);
}
