using System.Collections.ObjectModel;
using BurnOutAdmin.Models.Program;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BurnOutAdmin.ViewModels.ProgramBuilder;

public partial class SessionViewModel : BaseViewModel
{
    public SessionModel Model { get; }

    private readonly Action<SessionViewModel>? _removeAction;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private bool _isExpanded = true;

    public string ExpandIcon => IsExpanded ? "▼" : "▶";

    public ObservableCollection<CategoryViewModel> Categories { get; } = new();

    public SessionViewModel(SessionModel model, Action<SessionViewModel>? removeAction = null)
    {
        Model = model;
        _removeAction = removeAction;

        _name = model.Name;
        _order = model.Order;

        foreach (var category in model.Categories)
        {
            Categories.Add(new CategoryViewModel(category, RemoveCategory));
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
    private void AddCategory()
    {
        var category = CreateCategory();
        Categories.Add(category);
    }

    [RelayCommand]
    private void DeleteSession()
    {
        if (_removeAction == null)
            return;

        _removeAction(this);
    }

    private CategoryViewModel CreateCategory()
    {
        var model = new CategoryModel
        {
            Id = Guid.NewGuid(),
            Name = "Nouvelle Catégorie",
            Order = Categories.Count + 1
        };

        Model.Categories.Add(model);
        return new CategoryViewModel(model, RemoveCategory);
    }

    private void RemoveCategory(CategoryViewModel category)
    {
        Categories.Remove(category);
        Model.Categories.Remove(category.Model);
        RecalculateOrders();
    }

    private void RecalculateOrders()
    {
        for (var i = 0; i < Categories.Count; i++)
        {
            Categories[i].Order = i + 1;
        }
    }
}
