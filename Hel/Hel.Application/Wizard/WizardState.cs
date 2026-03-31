namespace Hel.Application.Wizard;

/// <summary>
/// Pure wizard state machine for the Phase 9 guided workflow.
/// This type contains no WinUI references and acts as the single source of truth for step locking.
/// </summary>
public sealed class WizardState
{
    private readonly Dictionary<StepId, StepState> _steps = new();

    public event EventHandler? StateChanged;

    public StepId CurrentStep { get; private set; }

    public IReadOnlyDictionary<StepId, StepState> Steps => _steps;

    public bool HasCsvLoaded { get; private set; }

    public int SelectedLocationsCount { get; private set; }

    public bool HasPreview { get; private set; }

    public bool HasExported { get; private set; }

    public bool CanGoBack { get; private set; }

    public bool CanGoNext { get; private set; }

    public WizardState()
    {
        Initialize();
    }

    public void Initialize()
    {
        _steps.Clear();

        _steps[StepId.Start] = StepState.Available;
        _steps[StepId.LoadCsv] = StepState.Available;
        _steps[StepId.SelectLocations] = StepState.Locked;
        _steps[StepId.PreviewResults] = StepState.Locked;
        _steps[StepId.ExportFinish] = StepState.Locked;

        CurrentStep = StepId.Start;
        HasCsvLoaded = false;
        SelectedLocationsCount = 0;
        HasPreview = false;
        HasExported = false;

        RefreshComputedState();
        RaiseStateChanged();
    }

    public StepState GetStepState(StepId step)
    {
        return _steps.TryGetValue(step, out StepState state)
            ? state
            : StepState.Locked;
    }

    public bool IsStepEnabled(StepId step)
    {
        return GetStepState(step) != StepState.Locked;
    }

    public void SetCurrentStep(StepId step)
    {
        if (!IsStepEnabled(step))
            return;

        CurrentStep = step;
        RefreshComputedState();
        RaiseStateChanged();
    }

    public void MarkStartVisited()
    {
        _steps[StepId.Start] = StepState.Completed;
        _steps[StepId.LoadCsv] = StepState.Available;

        RefreshComputedState();
        RaiseStateChanged();
    }

    public void MarkCsvLoaded()
    {
        HasCsvLoaded = true;
        HasPreview = false;
        HasExported = false;

        _steps[StepId.LoadCsv] = StepState.Completed;
        _steps[StepId.SelectLocations] = StepState.Available;
        _steps[StepId.PreviewResults] = StepState.Locked;
        _steps[StepId.ExportFinish] = StepState.Locked;

        RefreshComputedState();
        RaiseStateChanged();
    }

    public void ClearCsvState()
    {
        HasCsvLoaded = false;
        SelectedLocationsCount = 0;
        HasPreview = false;
        HasExported = false;

        _steps[StepId.LoadCsv] = StepState.Available;
        _steps[StepId.SelectLocations] = StepState.Locked;
        _steps[StepId.PreviewResults] = StepState.Locked;
        _steps[StepId.ExportFinish] = StepState.Locked;

        if (CurrentStep is StepId.SelectLocations or StepId.PreviewResults or StepId.ExportFinish)
            CurrentStep = StepId.LoadCsv;

        RefreshComputedState();
        RaiseStateChanged();
    }

    public void UpdateSelectedLocationsCount(int count)
    {
        SelectedLocationsCount = Math.Max(0, count);

        if (!HasCsvLoaded)
        {
            RefreshComputedState();
            RaiseStateChanged();
            return;
        }

        if (SelectedLocationsCount > 0)
        {
            _steps[StepId.SelectLocations] = StepState.Completed;
            _steps[StepId.PreviewResults] = StepState.Available;

            if (!HasPreview)
                _steps[StepId.ExportFinish] = StepState.Locked;
        }
        else
        {
            HasPreview = false;
            HasExported = false;

            _steps[StepId.SelectLocations] = StepState.Available;
            _steps[StepId.PreviewResults] = StepState.Locked;
            _steps[StepId.ExportFinish] = StepState.Locked;

            if (CurrentStep is StepId.PreviewResults or StepId.ExportFinish)
                CurrentStep = StepId.SelectLocations;
        }

        RefreshComputedState();
        RaiseStateChanged();
    }

    public void MarkPreviewReady()
    {
        HasPreview = true;
        HasExported = false;

        _steps[StepId.PreviewResults] = StepState.Completed;
        _steps[StepId.ExportFinish] = StepState.Available;

        RefreshComputedState();
        RaiseStateChanged();
    }

    public void ClearPreviewState()
    {
        HasPreview = false;
        HasExported = false;

        if (HasCsvLoaded && SelectedLocationsCount > 0)
        {
            _steps[StepId.PreviewResults] = StepState.Available;
        }
        else
        {
            _steps[StepId.PreviewResults] = StepState.Locked;
        }

        _steps[StepId.ExportFinish] = StepState.Locked;

        if (CurrentStep == StepId.ExportFinish)
            CurrentStep = StepId.PreviewResults;

        RefreshComputedState();
        RaiseStateChanged();
    }

    public void MarkExportCompleted()
    {
        HasExported = true;
        _steps[StepId.ExportFinish] = StepState.Completed;

        RefreshComputedState();
        RaiseStateChanged();
    }

    public void ResetAfterCsvReload()
    {
        HasPreview = false;
        HasExported = false;

        _steps[StepId.LoadCsv] = StepState.Completed;
        _steps[StepId.SelectLocations] = StepState.Available;
        _steps[StepId.PreviewResults] = StepState.Locked;
        _steps[StepId.ExportFinish] = StepState.Locked;

        if (CurrentStep is StepId.PreviewResults or StepId.ExportFinish)
            CurrentStep = StepId.SelectLocations;

        RefreshComputedState();
        RaiseStateChanged();
    }

    public void ResetAfterLocationChange()
    {
        HasPreview = false;
        HasExported = false;

        if (SelectedLocationsCount > 0)
        {
            _steps[StepId.SelectLocations] = StepState.Completed;
            _steps[StepId.PreviewResults] = StepState.Available;
        }
        else
        {
            _steps[StepId.SelectLocations] = StepState.Available;
            _steps[StepId.PreviewResults] = StepState.Locked;
        }

        _steps[StepId.ExportFinish] = StepState.Locked;

        if (CurrentStep == StepId.ExportFinish)
            CurrentStep = StepId.PreviewResults;

        RefreshComputedState();
        RaiseStateChanged();
    }

    private void RefreshComputedState()
    {
        CanGoBack = CurrentStep != StepId.Start;

        CanGoNext = CurrentStep switch
        {
            StepId.Start => true,
            StepId.LoadCsv => HasCsvLoaded,
            StepId.SelectLocations => SelectedLocationsCount > 0,
            StepId.PreviewResults => HasPreview,
            StepId.ExportFinish => true,
            _ => false
        };
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
