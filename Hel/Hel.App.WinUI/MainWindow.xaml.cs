using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hel.Application.Contracts;
using Hel.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Text;
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

    private IReadOnlyList<ItemRecord> _loadedRecords = Array.Empty<ItemRecord>();
    private IReadOnlyList<LocationOption> _availableLocations = Array.Empty<LocationOption>();
    private ClassificationResult? _lastClassificationResult;
    private RunSummary? _lastRunSummary;

    private string _libraryScopeName = string.Empty;
    private string? _loadedCsvPath;
    private string _selectedOutputFolder = string.Empty;

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
        appWindow.Resize(new SizeInt32(1060, 960));

        _configProvider = configProvider;
        _csvIngestService = csvIngestService;
        _locationFilterService = locationFilterService;
        _classificationService = classificationService;
        _textExportService = textExportService;
        _logger = logger;

        _libraryScopeName = _configProvider.GetPrimaryLibraryScopeName();
        _selectedOutputFolder = string.Empty;
        _loadedCsvPath = null;

        ScopeTextBlock.Text = $"Library scope: {_libraryScopeName}";
        OutputFolderTextBox.Text = string.Empty;
        StatusTextBlock.Text = "Choose an output folder, then load a CSV.";

        ResetPreviewState();
        UpdateActionButtonState();

        AppendLog("App started.");
        AppendLog("Choose an output folder to enable export.");
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
            ResetPreviewState();
            UpdateActionButtonState();

            LocationSummaryTextBlock.Text =
                $"Found {_availableLocations.Count} WAWL location(s) in the loaded file. All are selected by default.";

            StatusTextBlock.Text =
                $"CSV loaded successfully. Records={_loadedRecords.Count}, WAWL locations={_availableLocations.Count}, Parse failures={ingestResult.ParseFailuresCount}. Click Preview to review bucket counts.";

            AppendLog(StatusTextBlock.Text);

            _logger.LogInformation(
                "CSV loaded into UI. Records={RecordCount}, Locations={LocationCount}, ParseFailures={ParseFailures}",
                _loadedRecords.Count,
                _availableLocations.Count,
                ingestResult.ParseFailuresCount);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "CSV load failed.";
            AppendLog($"{StatusTextBlock.Text} {ex.Message}");
            _logger.LogError(ex, "Unexpected error while loading CSV.");
            await ShowCsvLoadErrorAsync(ex);
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

            UpdateActionButtonState();

            if (!string.IsNullOrWhiteSpace(_loadedCsvPath))
            {
                StatusTextBlock.Text = "Output folder selected. Load is ready; click Preview.";
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Output folder selection failed.";
            AppendLog($"{StatusTextBlock.Text} {ex.Message}");
            _logger.LogError(ex, "Unexpected error while choosing output folder.");
            await ShowMessageDialogAsync("Output folder error", ex.Message);
        }
    }

    private async void PreviewButton_Click(object sender, RoutedEventArgs e)
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

            var locationFilteredRecords = _locationFilterService.ApplyScopeAndLocationFilter(
                _loadedRecords,
                _libraryScopeName,
                selectedLocationCodes);

            var classificationResult = await _classificationService.ClassifyAsync(locationFilteredRecords);

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
                RowsAfterLocationFilter: locationFilteredRecords.Count,
                CountsPerBucket: countsPerBucket,
                UnassignedCount: classificationResult.Unassigned.Count,
                FallbackUsageCount: classificationResult.FallbackUsageCount,
                ParseFailuresCount: classificationResult.ParseFailuresCount);

            _lastClassificationResult = classificationResult;
            _lastRunSummary = summary;

            RenderPreview(summary, classificationResult);
            LastRunSummaryTextBox.Text = BuildLastRunSummaryText(summary);

            UpdateActionButtonState();

            string parseFailureMessage = summary.ParseFailuresCount > 0
                ? $"{summary.ParseFailuresCount} records had unreadable call numbers and were sent to Unassigned."
                : "No call-number parse failures were detected.";

            StatusTextBlock.Text =
                $"Preview ready. Assigned={summary.AssignedCount}, Unassigned={summary.UnassignedCount}. {parseFailureMessage}";

            AppendLog(StatusTextBlock.Text);
            AppendLog($"Buckets: {BuildBucketSummary(classificationResult.Classified)}");

            _logger.LogInformation(
                "Preview generated. Assigned={AssignedCount}, Unassigned={UnassignedCount}, ParseFailures={ParseFailuresCount}, Buckets={BucketSummary}",
                summary.AssignedCount,
                summary.UnassignedCount,
                summary.ParseFailuresCount,
                BuildBucketSummary(classificationResult.Classified));
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Preview failed.";
            AppendLog($"{StatusTextBlock.Text} {ex.Message}");
            _logger.LogError(ex, "Unexpected error while building preview.");
            await ShowMessageDialogAsync("Preview error", ex.Message);
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        string outputFolder = _selectedOutputFolder;

        try
        {
            if (_lastClassificationResult is null || _lastRunSummary is null)
            {
                await ShowMessageDialogAsync("Preview required", "Please click Preview before exporting.");
                return;
            }

            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                await ShowMessageDialogAsync("Output folder required", "Please choose an output folder before exporting.");
                return;
            }

            await _textExportService.ExportAsync(
                _lastClassificationResult.Classified,
                _lastClassificationResult.Unassigned,
                _lastRunSummary,
                outputFolder);

            int exportedFileCount =
                _lastRunSummary.CountsPerBucket.Count +
                (_lastRunSummary.UnassignedCount > 0 ? 1 : 0) +
                1;

            StatusTextBlock.Text =
                $"Export complete. Assigned={_lastRunSummary.AssignedCount}, Unassigned={_lastRunSummary.UnassignedCount}, Files={exportedFileCount}.";

            AppendLog(StatusTextBlock.Text);
            AppendLog($"Output folder: {outputFolder}");

            _logger.LogInformation(
                "Export complete. Assigned={AssignedCount}, Unassigned={UnassignedCount}, Files={ExportedFileCount}, OutputFolder={OutputFolder}",
                _lastRunSummary.AssignedCount,
                _lastRunSummary.UnassignedCount,
                exportedFileCount,
                outputFolder);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Export failed.";
            AppendLog($"{StatusTextBlock.Text} {ex.Message}");
            _logger.LogError(ex, "Unexpected error while exporting classified records.");

            await ShowMessageDialogAsync(
                "Export failed",
                $"Could not export files to:\n{outputFolder}\n\nReason:\n{ex.Message}");
        }
    }

    private async void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string logFolder = _configProvider.GetLogFolder();
            AppendLog($"Resolved log folder: {logFolder}");

            Directory.CreateDirectory(logFolder);

            if (!Directory.Exists(logFolder))
            {
                throw new InvalidOperationException($"Log folder could not be found or created: {logFolder}");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = logFolder,
                UseShellExecute = true,
                Verb = "open"
            });

            AppendLog($"Opened log folder: {logFolder}");
            _logger.LogInformation("Opened log folder. LogFolder={LogFolder}", logFolder);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Could not open the log folder.";
            AppendLog($"{StatusTextBlock.Text} {ex.Message}");
            _logger.LogError(ex, "Unexpected error while opening log folder.");
            await ShowMessageDialogAsync("Open log folder failed", ex.Message);
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
        ResetPreviewState();
        UpdateActionButtonState();

        if (!string.IsNullOrWhiteSpace(_loadedCsvPath))
        {
            StatusTextBlock.Text = "Location selection changed. Click Preview to refresh results.";
        }
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

    private void UpdateActionButtonState()
    {
        bool hasLoadedFile = !string.IsNullOrWhiteSpace(_loadedCsvPath);
        bool hasSelectedLocations = GetSelectedLocationCodes().Count > 0;
        bool hasOutputFolder = !string.IsNullOrWhiteSpace(_selectedOutputFolder);
        bool hasPreview = _lastClassificationResult is not null && _lastRunSummary is not null;

        PreviewButton.IsEnabled = hasLoadedFile && hasSelectedLocations;
        ExportButton.IsEnabled = hasPreview && hasOutputFolder;
    }

    private void ResetPreviewState()
    {
        _lastClassificationResult = null;
        _lastRunSummary = null;

        PreviewSummaryTextBlock.Text = "No preview yet. Load a CSV, choose locations, then click Preview.";

        PreviewBucketsPanel.Children.Clear();
        PreviewBucketsPanel.Children.Add(new TextBlock
        {
            Text = "Counts per bucket and sample lines will appear here after preview."
        });

        LastRunSummaryTextBox.Text = string.Empty;
    }

    private void RenderPreview(RunSummary summary, ClassificationResult classificationResult)
    {
        PreviewSummaryTextBlock.Text =
            $"Loaded={summary.TotalRowsLoaded} | WAWL={summary.RowsAfterWawlFilter} | Location filtered={summary.RowsAfterLocationFilter} | Assigned={summary.AssignedCount} | Unassigned={summary.UnassignedCount} | Parse failures={summary.ParseFailuresCount}";

        PreviewBucketsPanel.Children.Clear();

        foreach (var group in classificationResult.Classified
                     .GroupBy(x => x.BucketKey, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var lines = group
                .Select(x => BuildPreviewLine(x.Record))
                .ToList();

            PreviewBucketsPanel.Children.Add(CreatePreviewCard(group.Key, group.Count(), lines));
        }

        if (classificationResult.Unassigned.Count > 0)
        {
            var lines = classificationResult.Unassigned
                .Select(BuildPreviewLine)
                .ToList();

            PreviewBucketsPanel.Children.Add(CreatePreviewCard("Unassigned", classificationResult.Unassigned.Count, lines));
        }
    }

    private FrameworkElement CreatePreviewCard(string bucketName, int count, IReadOnlyList<string> lines)
    {
        var sampleLines = lines.Take(10).ToList();
        string sampleText = string.Join("\r\n", sampleLines);
        string fullText = string.Join("\r\n", lines);

        var panel = new StackPanel { Spacing = 8 };

        panel.Children.Add(new TextBlock
        {
            Text = $"{bucketName} ({count})",
            FontWeight = FontWeights.SemiBold
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Sample lines (first 10):",
            Opacity = 0.8
        });

        panel.Children.Add(new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Child = new ScrollViewer
            {
                MaxHeight = 140,
                Content = new TextBlock
                {
                    Text = sampleText,
                    TextWrapping = TextWrapping.Wrap
                }
            }
        });

        if (lines.Count > 10)
        {
            panel.Children.Add(new Expander
            {
                Header = $"View full list ({count} items)",
                Content = new Border
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Child = new ScrollViewer
                    {
                        Height = 180,
                        Content = new TextBlock
                        {
                            Text = fullText,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                }
            });
        }

        return new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Child = panel
        };
    }

    private static string BuildPreviewLine(ItemRecord record)
    {
        return $"{record.Title.Value} | {record.Barcode.Value} | {record.ResolvedCallNumber.Value}";
    }

    private static string BuildLastRunSummaryText(RunSummary summary)
    {
        return
            $"CSV file: {summary.CsvFileName}{Environment.NewLine}" +
            $"Total rows loaded: {summary.TotalRowsLoaded}{Environment.NewLine}" +
            $"Rows after WAWL filter: {summary.RowsAfterWawlFilter}{Environment.NewLine}" +
            $"Rows after location filter: {summary.RowsAfterLocationFilter}{Environment.NewLine}" +
            $"Assigned count: {summary.AssignedCount}{Environment.NewLine}" +
            $"Unassigned count: {summary.UnassignedCount}{Environment.NewLine}" +
            $"Fallback count: {summary.FallbackUsageCount}{Environment.NewLine}" +
            $"Parse failures count: {summary.ParseFailuresCount}";
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

    private async Task ShowCsvLoadErrorAsync(Exception ex)
    {
        string title;
        string message;

        if (ex.Message.Contains("missing required header", StringComparison.OrdinalIgnoreCase))
        {
            title = "Missing required columns";
            message = ex.Message;
        }
        else
        {
            title = "CSV load failed";
            message = ex.Message;
        }

        await ShowMessageDialogAsync(title, message);
    }

    private async Task ShowMessageDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = RootGrid.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogTextBox.Text += line + Environment.NewLine;
    }
}
