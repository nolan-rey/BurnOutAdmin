namespace BurnOutAdmin.Models.Program;

public class SessionModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<CategoryModel> Categories { get; set; } = new();
}
