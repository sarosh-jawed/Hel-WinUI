using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Hel.Application.Contracts;
using Hel.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Hel.App.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly IConfigProvider _configProvider;
    private readonly ICsvIngestService _csvIngestService;
    private readonly ILocationFilterService _locationFilterService;
    private readonly IClassificationService _classificationService;
    private readonly ITextExportService _textExportService;
    private readonly ILogger<MainWindow> _logger;
    private string _selectedOutputFolder = string.Empty;

    private IReadOnlyList<ItemRecord> _loadedRecords = Array.Empty<ItemRecord>();
    private IReadOnlyList<LocationOption> _availableLocations = Array.Empty<LocationOption>();
    private string _libraryScopeName = string.Empty;
    private string? _loadedCsvPath;

    public MainWindow(
        IConfigProvider configProvider,
        ICsvIngestService csvIngestService,
        ILocationFilterService locationFilterService,
        IClassificationService classificationService,
        ITextExportService textExportService,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(920, 680));

        _configProvider = configProvider;
        _csvIngestService = csvIngestService;
        _locationFilterService = locationFilterService;
        _classificationService = classificationService;
        _textExportService = textExportService;
        _logger = logger;

        _libraryScopeName = _configProvider.GetPrimaryLibraryScopeName();

        _selectedOutputFolder = string.Empty;
        OutputFolderTextBox.Text = string.Empty;

        ScopeTextBlock.Text = $"Library scope: {_libraryScopeName}";
        AppendLog("App started.");
        AppendLog("Choose an output folder to enable export.");
        StatusTextBlock.Text = "Choose an output folder, then load a CSV.";

        UpdateRunButtonState();
    }

    private async void SelectCsvButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".csv");

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                AppendLog("CSV selection cancelled.");
                return;
            }

            _loadedCsvPath = file.Path;

            StatusTextBlock.Text = "Loading CSV...";
            AppendLog($"Selected CSV: {_loadedCsvPath}");

            var ingestResult = await _csvIngestService.IngestAsync(_loadedCsvPath);
            _loadedRecords = ingestResult.Records;

            _availableLocations = _locationFilterService.ExtractAvailableLocations(
                _loadedRecords,
                _libraryScopeName);

            RenderLocationCheckboxes();
            UpdateRunButtonState();

            LocationSummaryTextBlock.Text =
                $"Found {_availableLocations.Count} WAWL location(s) in the loaded file. All are selected by default.";

            StatusTextBlock.Text =
                $"CSV loaded successfully. Records={_loadedRecords.Count}, WAWL locations={_availableLocations.Count}, Parse failures={ingestResult.ParseFailuresCount}";

            AppendLog(StatusTextBlock.Text);

            _logger.LogInformation(
                "CSV loaded into UI. Records={RecordCount}, Locations={LocationCount}, ParseFailures={ParseFailures}",
                _loadedRecords.Count,
                _availableLocations.Count,
                ingestResult.ParseFailuresCount);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            AppendLog(StatusTextBlock.Text);
            _logger.LogError(ex, "Unexpected error while loading CSV.");
        }
    }

    private async void ChooseOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                AppendLog("Output folder selection cancelled.");
                return;
            }

            _selectedOutputFolder = folder.Path;
            OutputFolderTextBox.Text = _selectedOutputFolder;

            AppendLog($"Output folder changed: {_selectedOutputFolder}");
            _logger.LogInformation("Output folder changed by user. OutputFolder={OutputFolder}", _selectedOutputFolder);

            UpdateRunButtonState();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            AppendLog(StatusTextBlock.Text);
            _logger.LogError(ex, "Unexpected error while choosing output folder.");
        }
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedLocationCodes = GetSelectedLocationCodes();

            var wawlFilteredRecords = _loadedRecords
                .Where(r => string.Equals(
                    r.LibraryName.Value,
                    _libraryScopeName,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var filteredRecords = _locationFilterService.ApplyScopeAndLocationFilter(
                _loadedRecords,
                _libraryScopeName,
                selectedLocationCodes);

            var classificationResult = await _classificationService.ClassifyAsync(filteredRecords);

            var countsPerBucket = classificationResult.Classified
                .GroupBy(x => x.BucketKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count(),
                    StringComparer.OrdinalIgnoreCase);

            var summary = new RunSummary(
                CsvFileName: Path.GetFileName(_loadedCsvPath ?? string.Empty),
                TotalRowsLoaded: _loadedRecords.Count,
                RowsAfterWawlFilter: wawlFilteredRecords.Count,
                RowsAfterLocationFilter: filteredRecords.Count,
                CountsPerBucket: countsPerBucket,
                UnassignedCount: classificationResult.Unassigned.Count,
                FallbackUsageCount: classificationResult.FallbackUsageCount,
                ParseFailuresCount: classificationResult.ParseFailuresCount);

            string outputFolder = _selectedOutputFolder;
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                throw new InvalidOperationException("Please choose an output folder before running export.");
            }

            await _textExportService.ExportAsync(
                classificationResult.Classified,
                classificationResult.Unassigned,
                summary,
                outputFolder);

            int exportedFileCount =
                countsPerBucket.Count +
                (classificationResult.Unassigned.Count > 0 ? 1 : 0) +
                1; // RunSummary.txt

            string bucketSummary = BuildBucketSummary(classificationResult.Classified);

            StatusTextBlock.Text =
                $"Export complete. Assigned={summary.AssignedCount}, Unassigned={summary.UnassignedCount}, Files={exportedFileCount}.";

            AppendLog(StatusTextBlock.Text);
            AppendLog($"Buckets: {bucketSummary}");
            AppendLog($"Summary: Loaded={summary.TotalRowsLoaded}, WAWL={summary.RowsAfterWawlFilter}, LocationFiltered={summary.RowsAfterLocationFilter}, Fallback={summary.FallbackUsageCount}, ParseFailures={summary.ParseFailuresCount}");
            AppendLog($"Output folder: {outputFolder}");

            _logger.LogInformation(
                "Export complete. Assigned={AssignedCount}, Unassigned={UnassignedCount}, Files={ExportedFileCount}, Buckets={BucketSummary}, OutputFolder={OutputFolder}",
                summary.AssignedCount,
                summary.UnassignedCount,
                exportedFileCount,
                bucketSummary,
                outputFolder);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            AppendLog(StatusTextBlock.Text);
            _logger.LogError(ex, "Unexpected error while exporting classified records.");
        }
    }

    private void RenderLocationCheckboxes()
    {
        LocationsPanel.Children.Clear();

        if (_availableLocations.Count == 0)
        {
            LocationsPanel.Children.Add(new TextBlock
            {
                Text = "No WAWL locations were found in the loaded file."
            });

            return;
        }

        foreach (var location in _availableLocations)
        {
            var checkBox = new CheckBox
            {
                Content = location.DisplayLabel,
                Tag = location.Code,
                IsChecked = true
            };

            checkBox.Checked += LocationCheckBox_Changed;
            checkBox.Unchecked += LocationCheckBox_Changed;

            LocationsPanel.Children.Add(checkBox);
        }
    }

    private void LocationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateRunButtonState();
    }

    private List<string> GetSelectedLocationCodes()
    {
        return LocationsPanel.Children
            .OfType<CheckBox>()
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Tag?.ToString())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!.Trim())
            .ToList();
    }

    private void UpdateRunButtonState()
    {
        bool hasLoadedFile = !string.IsNullOrWhiteSpace(_loadedCsvPath);
        bool hasSelectedLocations = GetSelectedLocationCodes().Count > 0;
        bool hasOutputFolder = !string.IsNullOrWhiteSpace(_selectedOutputFolder);

        RunButton.IsEnabled = hasLoadedFile && hasSelectedLocations && hasOutputFolder;
    }

    private static string BuildBucketSummary(IReadOnlyList<ClassifiedItem> classifiedItems)
    {
        if (classifiedItems.Count == 0)
            return "none";

        return string.Join(
            ", ",
            classifiedItems
                .GroupBy(x => x.BucketKey, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => $"{g.Key}={g.Count()}"));
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogTextBox.Text += line + Environment.NewLine;
    }
}
