using System;
using System.Threading;
using System.Threading.Tasks;
using Hel.App.WinUI.Services;
using Hel.Application.Contracts;
using Hel.Application.Wizard;
using Microsoft.Extensions.Logging;

namespace Hel.App.WinUI.ViewModels;

/// <summary>
/// View model for the Load CSV step.
/// </summary>
public sealed class LoadCsvPageViewModel : ObservableObject
{
    private readonly IConfigProvider _configProvider;
    private readonly ICsvIngestService _csvIngestService;
    private readonly ILocationFilterService _locationFilterService;
    private readonly WizardSessionStore _session;
    private readonly WizardState _wizardState;
    private readonly ILogger<LoadCsvPageViewModel> _logger;

    private string _libraryScopeName = string.Empty;
    private string _loadedCsvPath = string.Empty;
    private string _statusMessage = "Choose a CSV file to begin.";
    private int _recordCount;
    private int _locationCount;
    private int _parseFailuresCount;

    public string LibraryScopeName
    {
        get => _libraryScopeName;
        private set => SetProperty(ref _libraryScopeName, value);
    }

    public string LoadedCsvPath
    {
        get => _loadedCsvPath;
        private set => SetProperty(ref _loadedCsvPath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int RecordCount
    {
        get => _recordCount;
        private set => SetProperty(ref _recordCount, value);
    }

    public int LocationCount
    {
        get => _locationCount;
        private set => SetProperty(ref _locationCount, value);
    }

    public int ParseFailuresCount
    {
        get => _parseFailuresCount;
        private set => SetProperty(ref _parseFailuresCount, value);
    }

    public LoadCsvPageViewModel(
        IConfigProvider configProvider,
        ICsvIngestService csvIngestService,
        ILocationFilterService locationFilterService,
        WizardSessionStore session,
        WizardState wizardState,
        ILogger<LoadCsvPageViewModel> logger)
    {
        _configProvider = configProvider;
        _csvIngestService = csvIngestService;
        _locationFilterService = locationFilterService;
        _session = session;
        _wizardState = wizardState;
        _logger = logger;

        LibraryScopeName = _configProvider.GetPrimaryLibraryScopeName();
    }

    public Task InitializeAsync()
    {
        LibraryScopeName = _configProvider.GetPrimaryLibraryScopeName();

        if (!string.IsNullOrWhiteSpace(_session.LoadedCsvPath))
        {
            LoadedCsvPath = _session.LoadedCsvPath;
            RecordCount = _session.LoadedRecords.Count;
            LocationCount = _session.AvailableLocations.Count;
            ParseFailuresCount = _session.LastRunSummary?.ParseFailuresCount ?? 0;
            StatusMessage = "A CSV is already loaded. You can keep it or load a different file.";
        }

        return Task.CompletedTask;
    }

    public async Task LoadCsvAsync(string csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath))
            return;

        _session.BeginOperation("Loading CSV...");

        try
        {
            var ingestResult = await _csvIngestService.IngestAsync(
                csvPath,
                _session.CurrentCts?.Token ?? CancellationToken.None);

            var availableLocations = _locationFilterService.ExtractAvailableLocations(
                ingestResult.Records,
                LibraryScopeName);

            _session.ClearAfterCsvReload();
            _session.SetCsvLoadResult(csvPath, ingestResult.Records, availableLocations);

            _wizardState.ResetAfterCsvReload();
            _wizardState.MarkCsvLoaded();
            _wizardState.UpdateSelectedLocationsCount(_session.SelectedLocationCodes.Count);

            LoadedCsvPath = csvPath;
            RecordCount = ingestResult.Records.Count;
            LocationCount = availableLocations.Count;
            ParseFailuresCount = ingestResult.ParseFailuresCount;

            StatusMessage =
                $"CSV loaded successfully. Records={RecordCount}, WAWL locations={LocationCount}, Parse failures={ParseFailuresCount}.";

            _logger.LogInformation(
                "CSV loaded. CsvPath={CsvPath}, Records={RecordCount}, Locations={LocationCount}, ParseFailures={ParseFailuresCount}",
                csvPath,
                RecordCount,
                LocationCount,
                ParseFailuresCount);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "CSV load was cancelled.";
            _logger.LogInformation("CSV load cancelled.");
        }
        catch (Exception ex)
        {
            // Keep the app stable: show a friendly message and allow the user to try another file.
            StatusMessage = $"Failed to load CSV: {ex.Message}";
            _logger.LogError(ex, "CSV load failed. CsvPath={CsvPath}", csvPath);

            // Do NOT mark CSV loaded or unlock steps. Leave wizard state as-is.
            // If the user had a previous valid CSV loaded, this also avoids wiping that state.
        }
        finally
        {
            _session.EndOperation();
        }
    }
}
