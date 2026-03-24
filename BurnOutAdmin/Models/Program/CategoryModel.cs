namespace BurnOutAdmin.Models.Program;

public class CategoryModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<SubCategoryModel> SubCategories { get; set; } = new();
}
