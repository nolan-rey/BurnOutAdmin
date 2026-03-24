using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BurnOutAdmin.Models;

public class SidebarMenuItem : INotifyPropertyChanged
{
    public string Title { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
