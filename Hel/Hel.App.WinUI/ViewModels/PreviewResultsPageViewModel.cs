using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hel.App.WinUI.Models;
using Hel.App.WinUI.Services;
using Hel.Application.Contracts;
using Hel.Application.Wizard;
using Hel.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Hel.App.WinUI.ViewModels;

/// <summary>
/// View model for the Preview Results step.
/// </summary>
public sealed partial class PreviewResultsPageViewModel(
    IConfigProvider configProvider,
    ILocationFilterService locationFilterService,
    IClassificationService classificationService,
    WizardSessionStore session,
    WizardState wizardState,
    ILogger<PreviewResultsPageViewModel> logger) : ObservableObject
{
    private const int DefaultPreviewRowsPerBucket = 50;

    private readonly IConfigProvider _configProvider = configProvider;
    private readonly ILocationFilterService _locationFilterService = locationFilterService;
    private readonly IClassificationService _classificationService = classificationService;
    private readonly WizardSessionStore _session = session;
    private readonly WizardState _wizardState = wizardState;
    private readonly ILogger<PreviewResultsPageViewModel> _logger = logger;

    private string _statusMessage = "No preview yet.";
    private bool _hasUnassignedWarning;
    private string _unassignedWarningMessage = string.Empty;
    private bool _hasParseFailureWarning;
    private string _parseFailureWarningMessage = string.Empty;

    private int _totalRowsLoaded;
    private int _rowsAfterWawlFilter;
    private int _rowsAfterLocationFilter;
    private int _assignedCount;
    private int _unassignedCount;
    private int _fallbackUsageCount;
    private int _parseFailuresCount;

    public ObservableCollection<PreviewBucketViewModel> Buckets { get; } = [];

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasUnassignedWarning
    {
        get => _hasUnassignedWarning;
        private set => SetProperty(ref _hasUnassignedWarning, value);
    }

    public string UnassignedWarningMessage
    {
        get => _unassignedWarningMessage;
        private set => SetProperty(ref _unassignedWarningMessage, value);
    }

    public bool HasParseFailureWarning
    {
        get => _hasParseFailureWarning;
        private set => SetProperty(ref _hasParseFailureWarning, value);
    }

    public string ParseFailureWarningMessage
    {
        get => _parseFailureWarningMessage;
        private set => SetProperty(ref _parseFailureWarningMessage, value);
    }

    public int TotalRowsLoaded
    {
        get => _totalRowsLoaded;
        private set => SetProperty(ref _totalRowsLoaded, value);
    }

    public int RowsAfterWawlFilter
    {
        get => _rowsAfterWawlFilter;
        private set => SetProperty(ref _rowsAfterWawlFilter, value);
    }

    public int RowsAfterLocationFilter
    {
        get => _rowsAfterLocationFilter;
        private set => SetProperty(ref _rowsAfterLocationFilter, value);
    }

    public int AssignedCount
    {
        get => _assignedCount;
        private set => SetProperty(ref _assignedCount, value);
    }

    public int UnassignedCount
    {
        get => _unassignedCount;
        private set => SetProperty(ref _unassignedCount, value);
    }

    public int FallbackUsageCount
    {
        get => _fallbackUsageCount;
        private set => SetProperty(ref _fallbackUsageCount, value);
    }

    public int ParseFailuresCount
    {
        get => _parseFailuresCount;
        private set => SetProperty(ref _parseFailuresCount, value);
    }

    public async Task InitializeAsync()
    {
        if (_session.LoadedRecords.Count == 0)
        {
            StatusMessage = "Load a CSV first.";
            return;
        }

        if (_session.SelectedLocationCodes.Count == 0)
        {
            StatusMessage = "Select at least one location first.";
            return;
        }

        if (_session.LastRunSummary is not null && _session.LastClassificationResult is not null)
        {
            BuildFromSession();
            StatusMessage = "Showing the most recent preview.";
            return;
        }

        await RunPreviewAsync();
    }

    public async Task RunPreviewAsync()
    {
        string libraryScopeName = _configProvider.GetPrimaryLibraryScopeName();

        _session.BeginOperation("Generating preview...");

        try
        {
            var wawlFilteredRecords = _session.LoadedRecords
                .Where(r => string.Equals(
                    r.LibraryName.Value,
                    libraryScopeName,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var locationFilteredRecords = _locationFilterService.ApplyScopeAndLocationFilter(
                _session.LoadedRecords,
                libraryScopeName,
                [.. _session.SelectedLocationCodes]);

            var classificationResult = await _classificationService.ClassifyAsync(
                locationFilteredRecords,
                _session.CurrentCts?.Token ?? CancellationToken.None);

            var countsPerBucket = classificationResult.Classified
                .GroupBy(x => x.BucketKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count(),
                    StringComparer.OrdinalIgnoreCase);

            var summary = new RunSummary(
                CsvFileName: Path.GetFileName(_session.LoadedCsvPath ?? string.Empty),
                TotalRowsLoaded: _session.LoadedRecords.Count,
                RowsAfterWawlFilter: wawlFilteredRecords.Count,
                RowsAfterLocationFilter: locationFilteredRecords.Count,
                CountsPerBucket: countsPerBucket,
                UnassignedCount: classificationResult.Unassigned.Count,
                FallbackUsageCount: classificationResult.FallbackUsageCount,
                ParseFailuresCount: classificationResult.ParseFailuresCount);

            _session.SetPreviewResult(classificationResult, summary);
            _wizardState.MarkPreviewReady();

            BuildFromSession();

            StatusMessage =
                $"Preview ready. Assigned={summary.AssignedCount}, Unassigned={summary.UnassignedCount}, Parse failures={summary.ParseFailuresCount}.";

            _logger.LogInformation(
                "Preview generated. Assigned={AssignedCount}, Unassigned={UnassignedCount}, ParseFailures={ParseFailuresCount}",
                summary.AssignedCount,
                summary.UnassignedCount,
                summary.ParseFailuresCount);
        }
        catch (OperationCanceledException)
        {
            _session.ClearPreview();
            _wizardState.ClearPreviewState();
            StatusMessage = "Preview was cancelled.";
            _logger.LogInformation("Preview cancelled.");
        }
        finally
        {
            _session.EndOperation();
        }
    }

    private void BuildFromSession()
    {
        Buckets.Clear();

        if (_session.LastRunSummary is null || _session.LastClassificationResult is null)
            return;

        RunSummary summary = _session.LastRunSummary;
        ClassificationResult classificationResult = _session.LastClassificationResult;

        TotalRowsLoaded = summary.TotalRowsLoaded;
        RowsAfterWawlFilter = summary.RowsAfterWawlFilter;
        RowsAfterLocationFilter = summary.RowsAfterLocationFilter;
        AssignedCount = summary.AssignedCount;
        UnassignedCount = summary.UnassignedCount;
        FallbackUsageCount = summary.FallbackUsageCount;
        ParseFailuresCount = summary.ParseFailuresCount;

        int bucketIndex = 0;

        foreach (var group in classificationResult.Classified
                     .GroupBy(x => x.BucketKey, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var bucket = new PreviewBucketViewModel
            {
                BucketName = group.Key,
                Count = group.Count(),
                IsExpanded = bucketIndex == 0
            };

            foreach (ClassifiedItem item in group)
            {
                bucket.AllRows.Add(new PreviewRowViewModel
                {
                    Title = item.Record.Title.Value,
                    Barcode = item.Record.Barcode.Value,
                    CallNumber = item.Record.ResolvedCallNumber.Value,
                    Location = BuildLocationLabel(item.Record),
                    RoutingReason = item.RoutingReason
                });
            }

            bucket.ShowFirst(DefaultPreviewRowsPerBucket);
            Buckets.Add(bucket);
            bucketIndex++;
        }

        if (classificationResult.Unassigned.Count > 0)
        {
            var bucket = new PreviewBucketViewModel
            {
                BucketName = "Unassigned",
                Count = classificationResult.Unassigned.Count,
                IsExpanded = Buckets.Count == 0
            };

            foreach (UnassignedItem item in classificationResult.Unassigned)
            {
                bucket.AllRows.Add(new PreviewRowViewModel
                {
                    Title = item.Record.Title.Value,
                    Barcode = item.Record.Barcode.Value,
                    CallNumber = item.Record.ResolvedCallNumber.Value,
                    Location = BuildLocationLabel(item.Record),
                    RoutingReason = item.RoutingReason
                });
            }

            bucket.ShowFirst(DefaultPreviewRowsPerBucket);
            Buckets.Add(bucket);
        }

        HasUnassignedWarning = UnassignedCount > 0;
        UnassignedWarningMessage = HasUnassignedWarning
            ? $"{UnassignedCount} item(s) were routed to Unassigned."
            : string.Empty;

        HasParseFailureWarning = ParseFailuresCount > 0;
        ParseFailureWarningMessage = HasParseFailureWarning
            ? $"{ParseFailuresCount} item(s) had unreadable call numbers after normalization."
            : string.Empty;
    }

    private static string BuildLocationLabel(ItemRecord record)
    {
        return string.IsNullOrWhiteSpace(record.LocationName?.Value)
            ? record.LocationCode.Value
            : $"{record.LocationCode.Value} ({record.LocationName.Value})";
    }
}
