using System;
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
    private readonly ILogger<MainWindow> _logger;

    private IReadOnlyList<ItemRecord> _loadedRecords = Array.Empty<ItemRecord>();
    private IReadOnlyList<LocationOption> _availableLocations = Array.Empty<LocationOption>();
    private string _libraryScopeName = string.Empty;
    private string? _loadedCsvPath;

    public MainWindow(
        IConfigProvider configProvider,
        ICsvIngestService csvIngestService,
        ILocationFilterService locationFilterService,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();

        // WinUI 3 window sizing
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(920, 680));

        _configProvider = configProvider;
        _csvIngestService = csvIngestService;
        _locationFilterService = locationFilterService;
        _logger = logger;

        _libraryScopeName = _configProvider.GetPrimaryLibraryScopeName();

        OutputFolderTextBox.Text = _configProvider.GetDefaultOutputFolder();
        ScopeTextBlock.Text = $"Library scope: {_libraryScopeName}";
        AppendLog("App started. Ready.");

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
                $"Found {_availableLocations.Count} WAWL location(s) in the loaded file. " +
                "All are selected by default.";

            StatusTextBlock.Text =
                $"CSV loaded successfully. Records={_loadedRecords.Count}, " +
                $"WAWL locations={_availableLocations.Count}, " +
                $"Parse failures={ingestResult.ParseFailuresCount}";

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

    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedLocationCodes = GetSelectedLocationCodes();

            var filteredRecords = _locationFilterService.ApplyScopeAndLocationFilter(
                _loadedRecords,
                _libraryScopeName,
                selectedLocationCodes);

            StatusTextBlock.Text =
                $"Ready to process {filteredRecords.Count} WAWL record(s) across " +
                $"{selectedLocationCodes.Count} selected location(s).";

            AppendLog(StatusTextBlock.Text);

            _logger.LogInformation(
                "Applied WAWL + location filter. SelectedLocations={SelectedLocationCount}, FilteredRecords={FilteredRecordCount}",
                selectedLocationCodes.Count,
                filteredRecords.Count);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            AppendLog(StatusTextBlock.Text);
            _logger.LogError(ex, "Unexpected error while applying location filtering.");
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

        RunButton.IsEnabled = hasLoadedFile && hasSelectedLocations;
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogTextBox.Text += line + Environment.NewLine;
    }
}
