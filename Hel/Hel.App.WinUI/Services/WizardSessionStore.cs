using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hel.App.WinUI.ViewModels;
using Hel.Application.Contracts;
using Hel.Domain.Models;

namespace Hel.App.WinUI.Services;

/// <summary>
/// Shared working data store for the Phase 9 shell.
/// This preserves state across page navigation and long-running operations.
/// </summary>
public sealed partial class WizardSessionStore : ObservableObject
{
    private string? _loadedCsvPath;
    private string _selectedOutputFolder = string.Empty;
    private IReadOnlyList<ItemRecord> _loadedRecords = [];
    private IReadOnlyList<LocationOption> _availableLocations = [];
    private HashSet<string> _selectedLocationCodes = new(StringComparer.OrdinalIgnoreCase);
    private ClassificationResult? _lastClassificationResult;
    private RunSummary? _lastRunSummary;
    private IReadOnlyList<string> _generatedFiles = [];
    private bool _isBusy;
    private string _busyMessage = string.Empty;

    public event EventHandler? StateChanged;

    public string? LoadedCsvPath
    {
        get => _loadedCsvPath;
        private set => SetProperty(ref _loadedCsvPath, value);
    }

    public string SelectedOutputFolder
    {
        get => _selectedOutputFolder;
        private set => SetProperty(ref _selectedOutputFolder, value);
    }

    public IReadOnlyList<ItemRecord> LoadedRecords
    {
        get => _loadedRecords;
        private set => SetProperty(ref _loadedRecords, value);
    }

    public IReadOnlyList<LocationOption> AvailableLocations
    {
        get => _availableLocations;
        private set => SetProperty(ref _availableLocations, value);
    }

    public HashSet<string> SelectedLocationCodes
    {
        get => _selectedLocationCodes;
        private set => SetProperty(ref _selectedLocationCodes, value);
    }

    public ClassificationResult? LastClassificationResult
    {
        get => _lastClassificationResult;
        private set => SetProperty(ref _lastClassificationResult, value);
    }

    public RunSummary? LastRunSummary
    {
        get => _lastRunSummary;
        private set => SetProperty(ref _lastRunSummary, value);
    }

    public IReadOnlyList<string> GeneratedFiles
    {
        get => _generatedFiles;
        private set => SetProperty(ref _generatedFiles, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string BusyMessage
    {
        get => _busyMessage;
        private set => SetProperty(ref _busyMessage, value);
    }

    public CancellationTokenSource? CurrentCts { get; private set; }

    public void SetCsvLoadResult(
        string csvPath,
        IReadOnlyList<ItemRecord> records,
        IReadOnlyList<LocationOption> availableLocations)
    {
        LoadedCsvPath = csvPath;
        LoadedRecords = records;
        AvailableLocations = availableLocations;

        SelectedLocationCodes = new HashSet<string>(
            availableLocations.Select(x => x.Code),
            StringComparer.OrdinalIgnoreCase);

        LastClassificationResult = null;
        LastRunSummary = null;
        GeneratedFiles = [];

        RaiseStateChanged();
    }

    public void SetOutputFolder(string outputFolder)
    {
        SelectedOutputFolder = outputFolder?.Trim() ?? string.Empty;
        RaiseStateChanged();
    }

    public void SetSelectedLocations(IReadOnlyCollection<string> selectedLocationCodes)
    {
        SelectedLocationCodes = new HashSet<string>(
            selectedLocationCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()),
            StringComparer.OrdinalIgnoreCase);

        RaiseStateChanged();
    }

    public void SetPreviewResult(
        ClassificationResult classificationResult,
        RunSummary summary)
    {
        LastClassificationResult = classificationResult;
        LastRunSummary = summary;
        GeneratedFiles = [];

        RaiseStateChanged();
    }

    public void SetGeneratedFiles(IReadOnlyList<string> generatedFiles)
    {
        GeneratedFiles = generatedFiles;
        RaiseStateChanged();
    }

    public void ClearPreview()
    {
        LastClassificationResult = null;
        LastRunSummary = null;
        RaiseStateChanged();
    }

    public void ClearExport()
    {
        GeneratedFiles = [];
        RaiseStateChanged();
    }

    public void ClearAfterCsvReload()
    {
        ClearPreview();
        ClearExport();
    }

    public void ClearAfterLocationChange()
    {
        ClearPreview();
        ClearExport();
    }

    public void BeginOperation(string message)
    {
        EndOperation();

        CurrentCts = new CancellationTokenSource();
        BusyMessage = message;
        IsBusy = true;

        RaiseStateChanged();
    }

    public void CancelCurrentOperation()
    {
        if (CurrentCts is null)
            return;

        if (!CurrentCts.IsCancellationRequested)
            CurrentCts.Cancel();

        RaiseStateChanged();
    }

    public void EndOperation()
    {
        CurrentCts?.Dispose();
        CurrentCts = null;

        BusyMessage = string.Empty;
        IsBusy = false;

        RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
