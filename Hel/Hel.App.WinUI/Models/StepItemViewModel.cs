using Hel.App.WinUI.ViewModels;
using Hel.Application.Wizard;

namespace Hel.App.WinUI.Models;

/// <summary>
/// Shell navigation step item.
/// </summary>
public sealed class StepItemViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _glyph = string.Empty;
    private bool _isEnabled;
    private bool _isCompleted;
    private bool _isActive;

    public StepId StepId { get; set; }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Glyph
    {
        get => _glyph;
        set => SetProperty(ref _glyph, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}
