using BurnOutAdmin.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;

namespace BurnOutAdmin.ViewModels;

public partial class ClientSelectionViewModel : ObservableObject
{
    public Client Client { get; }

    [ObservableProperty] private bool _isSelected;

    public string FullName => $"{Client.FirstName} {Client.LastName}";
    public string Initials =>
        (Client.FirstName.Length > 0 ? Client.FirstName[0].ToString().ToUpper() : "") +
        (Client.LastName.Length > 0 ? Client.LastName[0].ToString().ToUpper() : "");

    // ── Apparence selon sélection ─────────────────────────────────
    public Color RowBackground => IsSelected ? Color.FromArgb("#EFF6FF") : Colors.Transparent;
    public Color CheckBorderColor => IsSelected ? Color.FromArgb("#4F46E5") : Color.FromArgb("#CBD5E1");
    public Color CheckBackground => IsSelected ? Color.FromArgb("#4F46E5") : Colors.Transparent;

    public ClientSelectionViewModel(Client client)
    {
        Client = client;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(RowBackground));
        OnPropertyChanged(nameof(CheckBorderColor));
        OnPropertyChanged(nameof(CheckBackground));
    }

    [RelayCommand]
    private void ToggleSelection() => IsSelected = !IsSelected;
}
