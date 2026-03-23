namespace BurnOutAdmin.Models.Program;

public class SubCategoryModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int Sets { get; set; }
    public int RestTime { get; set; }
    public SubCategoryType Type { get; set; }
    public List<ExerciseModel> Exercises { get; set; } = new();
}
