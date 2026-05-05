namespace BurnOutAdmin.Models.Program;

public class ExerciseModel
{
    public Guid   Id    { get; set; }
    public string Name  { get; set; } = string.Empty;
    public int    Order { get; set; }

    // ── Reps vs Temps ─────────────────────────────────────────────
    /// <summary>false = nombre de répétitions, true = durée (ex: "1'", "30''")</summary>
    public bool   IsTimeMode { get; set; } = false;
    /// <summary>Valeur affichée : "10", "12" en mode reps — "1'", "30''" en mode temps</summary>
    public string RepsText   { get; set; } = "10";

    // ── Champs optionnels (valeurs) ───────────────────────────────
    public string WeightText  { get; set; } = "";  // "Fatigue Tot", "Élastique fin", "50 kg"…
    public string Tempo       { get; set; } = "";  // "1010", "2010", "Statique"
    public string Amplitude   { get; set; } = "";  // "Complète", "Partielle"
    public string RecupInter  { get; set; } = "";  // "30''", "1'30"
    public string RirText     { get; set; } = "";  // "0", "1", "2"

    // ── Visibilité des champs (activé par le coach, par exercice) ──
    public bool ShowPoids      { get; set; } = false;
    public bool ShowTempo      { get; set; } = false;
    public bool ShowAmplitude  { get; set; } = false;
    public bool ShowRecupInter { get; set; } = false;
    public bool ShowRir        { get; set; } = false;

    // ── Compatibilité descendante (anciennes séances) ──────────────
    public int    Sets   { get; set; }
    public int    Reps   { get; set; }
    public double Weight { get; set; }
    public double Rpe    { get; set; }
}
