using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Hel.App.WinUI.ViewModels;

namespace Hel.App.WinUI.Models;

/// <summary>
/// One preview bucket card shown on the Preview Results page.
/// </summary>
public sealed partial class PreviewBucketViewModel : ObservableObject
{
    private bool _isExpanded;
    private bool _canShowAll;

    public string BucketName { get; init; } = string.Empty;

    public int Count { get; init; }

    public ObservableCollection<PreviewRowViewModel> VisibleRows { get; } = [];

    public List<PreviewRowViewModel> AllRows { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool CanShowAll
    {
        get => _canShowAll;
        private set => SetProperty(ref _canShowAll, value);
    }

    public string ExpandHeaderText => $"{BucketName} ({Count})";

    public void ShowFirst(int n)
    {
        VisibleRows.Clear();

        foreach (PreviewRowViewModel row in AllRows.Take(n))
        {
            VisibleRows.Add(row);
        }

        CanShowAll = AllRows.Count > n;
        OnPropertyChanged(nameof(ExpandHeaderText));
    }

    public void ShowAll()
    {
        VisibleRows.Clear();

        foreach (PreviewRowViewModel row in AllRows)
        {
            VisibleRows.Add(row);
        }

        CanShowAll = false;
        OnPropertyChanged(nameof(ExpandHeaderText));
    }
}
