using System;
using System.IO;
using Hel.Application.Abstractions;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _config;
    private readonly IWorkflowRunner _workflow;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(IConfiguration config, IWorkflowRunner workflow, ILogger<MainWindow> logger)
    {
        InitializeComponent();

        // Set initial size (WinUI 3 way)
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new SizeInt32(920, 520));

        _config = config;
        _workflow = workflow;
        _logger = logger;

        OutputFolderTextBox.Text = ResolveDefaultOutputFolder();
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

            var result = await _workflow.RunAsync(csvPath, outputFolder);

            StatusTextBlock.Text = result.Success
                ? $"Success: {result.Message} Rows={result.RowsProcessed}"
                : $"Failed: {result.Message}";

            AppendLog(StatusTextBlock.Text);

            _logger.LogInformation("UI run finished. Success={Success}, Rows={Rows}",
                result.Success, result.RowsProcessed);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            AppendLog(StatusTextBlock.Text);
            _logger.LogError(ex, "Unexpected UI error.");
        }
    }

    private string ResolveDefaultOutputFolder()
    {
        // Config supports %LOCALAPPDATA% token.
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var outputRoot = _config["App:OutputRoot"]
            ?.Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase)
            ?? Path.Combine(localAppData, "Hel", "Output");

        // Month folder strategy (YYYY-MM)
        var monthFolder = DateTime.Now.ToString("yyyy-MM");
        var full = Path.Combine(outputRoot, monthFolder);

        Directory.CreateDirectory(full);
        return full;
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogTextBox.Text += line + Environment.NewLine;
    }
}
