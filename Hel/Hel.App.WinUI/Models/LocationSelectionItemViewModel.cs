using Hel.App.WinUI.ViewModels;

namespace Hel.App.WinUI.Models;

/// <summary>
/// Selectable location item for the Select Locations page.
/// </summary>
public sealed class LocationSelectionItemViewModel : ObservableObject
{
    private bool _isSelected;

    public string Code { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(Name)
            ? Code
            : $"{Code} ({Name})";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
