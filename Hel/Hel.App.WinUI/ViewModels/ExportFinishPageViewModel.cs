using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hel.App.WinUI.Services;
using Hel.Application.Contracts;
using Hel.Application.Wizard;
using Microsoft.Extensions.Logging;

namespace Hel.App.WinUI.ViewModels;

/// <summary>
/// View model for the Export & Finish step.
/// </summary>
public sealed class ExportFinishPageViewModel : ObservableObject
{
    private readonly ITextExportService _textExportService;
    private readonly IConfigProvider _configProvider;
    private readonly WizardSessionStore _session;
    private readonly WizardState _wizardState;
    private readonly ILogger<ExportFinishPageViewModel> _logger;

    private string _selectedOutputFolder = string.Empty;
    private string _statusMessage = "Choose an output folder and generate reports.";

    public ObservableCollection<string> GeneratedFiles { get; } = new();

    public ObservableCollection<string> GeneratedFileNames { get; } = new();

    public string SelectedOutputFolder
    {
        get => _selectedOutputFolder;
        private set => SetProperty(ref _selectedOutputFolder, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasGeneratedFiles => GeneratedFiles.Count > 0;

    public int GeneratedFileCount => GeneratedFiles.Count;

    public string SuccessMessage =>
        HasGeneratedFiles
            ? $"{GeneratedFileCount} file(s) generated successfully. Open the output folder to review them before sharing."
            : string.Empty;

    public ExportFinishPageViewModel(
        ITextExportService textExportService,
        IConfigProvider configProvider,
        WizardSessionStore session,
        WizardState wizardState,
        ILogger<ExportFinishPageViewModel> logger)
    {
        _textExportService = textExportService;
        _configProvider = configProvider;
        _session = session;
        _wizardState = wizardState;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        SelectedOutputFolder = _session.SelectedOutputFolder;
        ReloadGeneratedFiles();

        if (string.IsNullOrWhiteSpace(SelectedOutputFolder))
        {
            StatusMessage = "Choose an output folder and generate reports.";
        }
        else if (GeneratedFiles.Count > 0)
        {
            StatusMessage = "Reports have been generated. You can open the output folder or generate again.";
        }
        else
        {
            StatusMessage = "Output folder selected. Generate reports when you are ready.";
        }

        return Task.CompletedTask;
    }

    public void SetOutputFolder(string outputFolder)
    {
        _session.SetOutputFolder(outputFolder);
        SelectedOutputFolder = _session.SelectedOutputFolder;

        StatusMessage = string.IsNullOrWhiteSpace(SelectedOutputFolder)
            ? "Choose an output folder and generate reports."
            : "Output folder selected. Generate reports when you are ready.";
    }

    public async Task GenerateReportsAsync()
    {
        if (_session.LastClassificationResult is null || _session.LastRunSummary is null)
        {
            StatusMessage = "Preview results are required before export.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedOutputFolder))
        {
            StatusMessage = "Choose an output folder before generating reports.";
            return;
        }

        _session.BeginOperation("Generating reports...");

        try
        {
            var unassignedRecords = _session.LastClassificationResult.Unassigned
                .Select(x => x.Record)
                .ToList();

            await _textExportService.ExportAsync(
                _session.LastClassificationResult.Classified,
                unassignedRecords,
                _session.LastRunSummary,
                SelectedOutputFolder,
                _session.CurrentCts?.Token ?? CancellationToken.None);

            var generatedFiles = new List<string>();

            foreach (string bucketKey in _session.LastRunSummary.CountsPerBucket.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                generatedFiles.Add(Path.Combine(SelectedOutputFolder, $"{bucketKey}.txt"));
            }

            if (_session.LastRunSummary.UnassignedCount > 0)
            {
                generatedFiles.Add(Path.Combine(SelectedOutputFolder, "Unassigned.txt"));
            }

            generatedFiles.Add(Path.Combine(SelectedOutputFolder, "RunSummary.txt"));

            _session.SetGeneratedFiles(generatedFiles);
            _wizardState.MarkExportCompleted();

            ReloadGeneratedFiles();

            StatusMessage =
                $"Reports generated successfully. Files={GeneratedFiles.Count}.";

            _logger.LogInformation(
                "Reports generated. OutputFolder={OutputFolder}, Files={FileCount}",
                SelectedOutputFolder,
                GeneratedFiles.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Report generation was cancelled.";
            _logger.LogInformation("Report generation cancelled.");
        }
        finally
        {
            _session.EndOperation();
        }
    }

    public void OpenOutputFolder()
    {
        if (string.IsNullOrWhiteSpace(SelectedOutputFolder))
            return;

        Directory.CreateDirectory(SelectedOutputFolder);

        Process.Start(new ProcessStartInfo
        {
            FileName = SelectedOutputFolder,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    public void OpenLogFolder()
    {
        string logFolder = _configProvider.GetLogFolder();

        Directory.CreateDirectory(logFolder);

        Process.Start(new ProcessStartInfo
        {
            FileName = logFolder,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private void ReloadGeneratedFiles()
    {
        GeneratedFiles.Clear();
        GeneratedFileNames.Clear();

        foreach (string file in _session.GeneratedFiles)
        {
            GeneratedFiles.Add(file);
            GeneratedFileNames.Add(Path.GetFileName(file));
        }

        OnPropertyChanged(nameof(HasGeneratedFiles));
        OnPropertyChanged(nameof(GeneratedFileCount));
        OnPropertyChanged(nameof(SuccessMessage));
    }
}
