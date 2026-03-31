using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Hel.App.WinUI.Models;
using Hel.App.WinUI.Services;
using Hel.Application.Wizard;

namespace Hel.App.WinUI.ViewModels;

/// <summary>
/// Shell-level view model for the Phase 9 stepper shell.
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    private readonly WizardState _wizardState;
    private readonly WizardSessionStore _session;
    private readonly IStepNavigationService _stepNavigationService;

    private string _currentStepTitle = "Start";
    private string _nextButtonText = "Next: Load CSV";
    private bool _canGoBack;
    private bool _canGoNext = true;
    private bool _isBusy;
    private bool _canCancel;

    public event EventHandler? StateChanged;

    public ObservableCollection<StepItemViewModel> StepItems { get; } = new();

    public string CurrentStepTitle
    {
        get => _currentStepTitle;
        private set => SetProperty(ref _currentStepTitle, value);
    }

    public string NextButtonText
    {
        get => _nextButtonText;
        private set => SetProperty(ref _nextButtonText, value);
    }

    public bool CanGoBack
    {
        get => _canGoBack;
        private set => SetProperty(ref _canGoBack, value);
    }

    public bool CanGoNext
    {
        get => _canGoNext;
        private set => SetProperty(ref _canGoNext, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool CanCancel
    {
        get => _canCancel;
        private set => SetProperty(ref _canCancel, value);
    }

    public StepId CurrentStep => _wizardState.CurrentStep;

    public ShellViewModel(
        WizardState wizardState,
        WizardSessionStore session,
        IStepNavigationService stepNavigationService)
    {
        _wizardState = wizardState;
        _session = session;
        _stepNavigationService = stepNavigationService;

        _wizardState.StateChanged += OnUnderlyingStateChanged;
        _session.StateChanged += OnUnderlyingStateChanged;
    }

    public async Task InitializeAsync()
    {
        _wizardState.Initialize();
        RefreshStepItems();
        RefreshFooterState();

        await _stepNavigationService.NavigateToAsync(StepId.Start);
        _wizardState.SetCurrentStep(StepId.Start);

        RaiseStateChanged();
    }

    public async Task NavigateToAsync(StepId step)
    {
        if (!_wizardState.IsStepEnabled(step))
            return;

        if (_wizardState.CurrentStep == StepId.Start && step != StepId.Start)
            _wizardState.MarkStartVisited();

        bool navigated = await _stepNavigationService.NavigateToAsync(step);
        if (!navigated)
            return;

        _wizardState.SetCurrentStep(step);
        RefreshStepItems();
        RefreshFooterState();
        RaiseStateChanged();
    }

    public async Task GoBackAsync()
    {
        StepId previousStep = _wizardState.CurrentStep switch
        {
            StepId.Start => StepId.Start,
            StepId.LoadCsv => StepId.Start,
            StepId.SelectLocations => StepId.LoadCsv,
            StepId.PreviewResults => StepId.SelectLocations,
            StepId.ExportFinish => StepId.PreviewResults,
            _ => StepId.Start
        };

        await NavigateToAsync(previousStep);
    }

    public async Task GoNextAsync()
    {
        if (!_wizardState.CanGoNext)
            return;

        StepId nextStep = _wizardState.CurrentStep switch
        {
            StepId.Start => StepId.LoadCsv,
            StepId.LoadCsv => StepId.SelectLocations,
            StepId.SelectLocations => StepId.PreviewResults,
            StepId.PreviewResults => StepId.ExportFinish,
            StepId.ExportFinish => StepId.ExportFinish,
            _ => StepId.Start
        };

        await NavigateToAsync(nextStep);
    }

    public void RefreshStepItems()
    {
        StepItems.Clear();

        foreach (StepId stepId in Enum.GetValues<StepId>())
        {
            StepItems.Add(new StepItemViewModel
            {
                StepId = stepId,
                Title = GetStepTitle(stepId),
                Glyph = GetStepGlyph(stepId),
                IsEnabled = _wizardState.IsStepEnabled(stepId),
                IsCompleted = _wizardState.GetStepState(stepId) == StepState.Completed,
                IsActive = _wizardState.CurrentStep == stepId
            });
        }

        CurrentStepTitle = GetStepTitle(_wizardState.CurrentStep);
    }

    public void RefreshFooterState()
    {
        IsBusy = _session.IsBusy;
        CanCancel = _session.IsBusy;
        CanGoBack = _wizardState.CanGoBack && !_session.IsBusy;

        bool canGoNext = _wizardState.CanGoNext && !_session.IsBusy;

        if (_wizardState.CurrentStep == StepId.ExportFinish)
        {
            canGoNext = !_session.IsBusy;
        }

        CanGoNext = canGoNext;

        if (_wizardState.CurrentStep == StepId.ExportFinish)
        {
            NextButtonText = _wizardState.HasExported
                ? "Generate Reports Again"
                : "Generate Reports";
        }
        else
        {
            NextButtonText = GetNextButtonText(_wizardState.CurrentStep);
        }
    }

    public void CancelCurrentOperation()
    {
        _session.CancelCurrentOperation();
        RefreshFooterState();
        RaiseStateChanged();
    }

    private void OnUnderlyingStateChanged(object? sender, EventArgs e)
    {
        RefreshStepItems();
        RefreshFooterState();
        RaiseStateChanged();
    }

    private static string GetStepTitle(StepId stepId)
    {
        return stepId switch
        {
            StepId.Start => "Start",
            StepId.LoadCsv => "Load CSV",
            StepId.SelectLocations => "Select Locations",
            StepId.PreviewResults => "Preview Results",
            StepId.ExportFinish => "Export & Finish",
            _ => "Unknown"
        };
    }

    private static string GetStepGlyph(StepId stepId)
    {
        return stepId switch
        {
            StepId.Start => "\uE80F",
            StepId.LoadCsv => "\uE8B7",
            StepId.SelectLocations => "\uE707",
            StepId.PreviewResults => "\uE8A5",
            StepId.ExportFinish => "\uE898",
            _ => "\uE10C"
        };
    }

    private static string GetNextButtonText(StepId stepId)
    {
        return stepId switch
        {
            StepId.Start => "Next: Load CSV",
            StepId.LoadCsv => "Next: Select Locations",
            StepId.SelectLocations => "Next: Preview Results",
            StepId.PreviewResults => "Next: Export & Finish",
            StepId.ExportFinish => "Generate Reports",
            _ => "Next"
        };
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
