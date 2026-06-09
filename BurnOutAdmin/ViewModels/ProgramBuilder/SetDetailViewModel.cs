using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

/// <summary>
/// Wrapper observable d'un <see cref="SetDetail"/> (une ligne du tableau
/// « séries différenciées » dans l'éditeur d'exercice).
/// </summary>
public partial class SetDetailViewModel : ObservableObject
{
    public SetDetail Model { get; }

    [ObservableProperty] private int    _order;
    [ObservableProperty] private string _repsText;
    [ObservableProperty] private string _weightText;
    [ObservableProperty] private string _rirText;
    [ObservableProperty] private string _recupInter;
    [ObservableProperty] private string _tempo;
    [ObservableProperty] private string _amplitude;

    public string OrderLabel => $"S{Order}";

    public SetDetailViewModel(SetDetail model)
    {
        Model       = model;
        _order      = model.Order;
        _repsText   = model.RepsText;
        _weightText = model.WeightText;
        _rirText    = model.RirText;
        _recupInter = model.RecupInter;
        _tempo      = model.Tempo;
        _amplitude  = model.Amplitude;
    }

    partial void OnOrderChanged(int value)         { Model.Order      = value; OnPropertyChanged(nameof(OrderLabel)); }
    partial void OnRepsTextChanged(string value)   { Model.RepsText   = value; }
    partial void OnWeightTextChanged(string value) { Model.WeightText = value; }
    partial void OnRirTextChanged(string value)    { Model.RirText    = value; }
    partial void OnRecupInterChanged(string value) { Model.RecupInter = value; }
    partial void OnTempoChanged(string value)      { Model.Tempo      = value; }
    partial void OnAmplitudeChanged(string value)  { Model.Amplitude  = value; }
}
