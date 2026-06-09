namespace BurnOutAdmin.Models.Program;

/// <summary>
/// Détail d'une série (mode <see cref="ExerciseModel.UsePerSetDetails"/>).
/// Permet d'avoir, pour un même exercice, des reps et une charge différentes
/// par série (ex. pyramide : 12-10-8-6 reps avec des poids croissants).
///
/// Si <see cref="ExerciseModel.UsePerSetDetails"/> est false, ce détail n'est
/// ni utilisé ni écrit — comportement identique à avant.
/// </summary>
public class SetDetail
{
    /// <summary>Numéro de série (1-based).</summary>
    public int Order { get; set; }

    /// <summary>Reps (ou durée si l'exo est en mode temps). Texte libre.</summary>
    public string RepsText { get; set; } = string.Empty;

    /// <summary>Charge libre (ex. "50 kg", "Élastique fin", "50").</summary>
    public string WeightText { get; set; } = string.Empty;

    /// <summary>RIR optionnel pour cette série.</summary>
    public string RirText { get; set; } = string.Empty;

    /// <summary>Récup après la série (ex: "30''", "1'30").</summary>
    public string RecupInter { get; set; } = string.Empty;

    /// <summary>Tempo (ex: "2010").</summary>
    public string Tempo { get; set; } = string.Empty;

    /// <summary>Amplitude (ex: "Complète", "Partielle").</summary>
    public string Amplitude { get; set; } = string.Empty;
}
