using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Hel.App.WinUI.Models;
using Hel.App.WinUI.Services;
using Hel.Application.Wizard;

namespace Hel.App.WinUI.ViewModels;

/// <summary>
/// View model for the Select Locations step.
/// </summary>
public sealed class SelectLocationsPageViewModel : ObservableObject
{
    private readonly WizardSessionStore _session;
    private readonly WizardState _wizardState;

    private string _statusMessage = "Load a CSV to see locations.";

    public ObservableCollection<LocationSelectionItemViewModel> Locations { get; } = new();

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int SelectedCount => Locations.Count(x => x.IsSelected);

    public SelectLocationsPageViewModel(
        WizardSessionStore session,
        WizardState wizardState)
    {
        _session = session;
        _wizardState = wizardState;
    }

    public Task InitializeAsync()
    {
        RebuildLocationsFromSession();
        return Task.CompletedTask;
    }

    public void SelectAll()
    {
        foreach (LocationSelectionItemViewModel item in Locations)
        {
            item.IsSelected = true;
        }

        UpdateSelectionState(clearDownstreamIfNeeded: true);
    }

    public void ClearAll()
    {
        foreach (LocationSelectionItemViewModel item in Locations)
        {
            item.IsSelected = false;
        }

        UpdateSelectionState(clearDownstreamIfNeeded: true);
    }

    private void RebuildLocationsFromSession()
    {
        foreach (LocationSelectionItemViewModel item in Locations)
        {
            item.PropertyChanged -= LocationItem_PropertyChanged;
        }

        Locations.Clear();

        foreach (var location in _session.AvailableLocations)
        {
            var item = new LocationSelectionItemViewModel
            {
                Code = location.Code,
                Name = location.Name,
                IsSelected = _session.SelectedLocationCodes.Contains(location.Code)
            };

            item.PropertyChanged += LocationItem_PropertyChanged;
            Locations.Add(item);
        }

        if (Locations.Count == 0)
        {
            StatusMessage = "Load a CSV to see locations.";
        }
        else
        {
            StatusMessage = $"Select one or more WAWL locations. Selected={SelectedCount}.";
        }

        OnPropertyChanged(nameof(SelectedCount));
    }

    private void LocationItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocationSelectionItemViewModel.IsSelected))
        {
            UpdateSelectionState(clearDownstreamIfNeeded: true);
        }
    }

    private void UpdateSelectionState(bool clearDownstreamIfNeeded)
    {
        var selectedCodes = Locations
            .Where(x => x.IsSelected)
            .Select(x => x.Code)
            .ToList();

        _session.SetSelectedLocations(selectedCodes);

        if (clearDownstreamIfNeeded)
        {
            _session.ClearAfterLocationChange();
            _wizardState.ResetAfterLocationChange();
        }

        _wizardState.UpdateSelectedLocationsCount(selectedCodes.Count);

        StatusMessage = selectedCodes.Count > 0
            ? $"Selected {selectedCodes.Count} location(s). Preview is ready."
            : "Select at least one location to continue.";

        OnPropertyChanged(nameof(SelectedCount));
    }
}
