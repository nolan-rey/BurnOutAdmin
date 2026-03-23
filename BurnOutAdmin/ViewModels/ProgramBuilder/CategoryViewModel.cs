using System.Collections.ObjectModel;
using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class CategoryViewModel : BaseViewModel
{
    public CategoryModel Model { get; }

    private readonly Action<CategoryViewModel>? _removeAction;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private bool _isExpanded = true;

    public string ExpandIcon => IsExpanded ? "▼" : "▶";

    public ObservableCollection<SubCategoryViewModel> SubCategories { get; } = new();

    public CategoryViewModel(CategoryModel model, Action<CategoryViewModel>? removeAction = null)
    {
        Model = model;
        _removeAction = removeAction;

        _name = model.Name;
        _order = model.Order;

        foreach (var subCategory in model.SubCategories)
        {
            SubCategories.Add(new SubCategoryViewModel(subCategory, RemoveSubCategory));
        }
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
    }

    partial void OnOrderChanged(int value)
    {
        Model.Order = value;
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpandIcon));
    }

    [RelayCommand]
    private void AddSubCategory()
    {
        var subCategory = CreateSubCategory();
        SubCategories.Add(subCategory);
    }

    [RelayCommand]
    private void DeleteCategory()
    {
        if (_removeAction == null)
            return;

        _removeAction(this);
    }

    private SubCategoryViewModel CreateSubCategory()
    {
        var model = new SubCategoryModel
        {
            Id = Guid.NewGuid(),
            Name = "Nouvelle Sous-Catégorie",
            Order = SubCategories.Count + 1,
            Sets = 3,
            RestTime = 60,
            Type = SubCategoryType.Normal
        };

        Model.SubCategories.Add(model);
        return new SubCategoryViewModel(model, RemoveSubCategory);
    }

    private void RemoveSubCategory(SubCategoryViewModel subCategory)
    {
        SubCategories.Remove(subCategory);
        Model.SubCategories.Remove(subCategory.Model);
        RecalculateOrders();
    }

    private void RecalculateOrders()
    {
        for (var i = 0; i < SubCategories.Count; i++)
        {
            SubCategories[i].Order = i + 1;
        }
    }
}
