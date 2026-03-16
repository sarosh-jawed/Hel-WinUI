using System;
using Hel.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI.Windowing;

namespace Hel.App.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly IConfigProvider _configProvider;
    private readonly IWorkflowOrchestrator _workflow;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(IConfigProvider configProvider, IWorkflowOrchestrator workflow, ILogger<MainWindow> logger)
    {
        InitializeComponent();

        // Set initial size (WinUI 3 way)
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new SizeInt32(920, 520));

        _configProvider = configProvider;
        _workflow = workflow;
        _logger = logger;

        OutputFolderTextBox.Text = _configProvider.GetDefaultOutputFolder();
        AppendLog("App started. Ready.");
    }

    private async void SelectCsvButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".csv");

            // WinUI 3 requirement: connect picker to the window handle.
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                AppendLog("CSV selection cancelled.");
                return;
            }

            var csvPath = file.Path;
            var outputFolder = OutputFolderTextBox.Text;

            StatusTextBlock.Text = "Running...";
            AppendLog($"Selected CSV: {csvPath}");

            var summary = await _workflow.RunAsync(csvPath, outputFolder);

            StatusTextBlock.Text = $"Success: Loaded CSV successfully. Rows={summary.TotalRecords}";
            AppendLog(StatusTextBlock.Text);

            _logger.LogInformation("UI run finished. TotalRecords={TotalRecords}",
                summary.TotalRecords);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            AppendLog(StatusTextBlock.Text);
            _logger.LogError(ex, "Unexpected UI error.");
        }
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogTextBox.Text += line + Environment.NewLine;
    }
}
