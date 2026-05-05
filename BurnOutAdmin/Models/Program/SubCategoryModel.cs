namespace BurnOutAdmin.Models.Program;

public class SubCategoryModel
{
    public Guid   Id       { get; set; }
    public string Name     { get; set; } = string.Empty;
    public int    Order    { get; set; }
    public int    Sets     { get; set; } = 3;
    public int    RestTime { get; set; } = 0;   // secondes
    public SubCategoryType Type { get; set; }

    /// <summary>Note/info affichée dans la bulle ℹ️ sur la fiche</summary>
    public string Note { get; set; } = "";

    // ── Colonnes optionnelles activées pour tous les exos de ce bloc ─
    public bool ShowPoids      { get; set; } = false;
    public bool ShowTempo      { get; set; } = false;
    public bool ShowAmplitude  { get; set; } = false;
    public bool ShowRecupInter { get; set; } = false;
    public bool ShowRir        { get; set; } = false;

    public List<ExerciseModel> Exercises { get; set; } = new();
}
