using Hel.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Hel.Infrastructure.Workflow;

/// <summary>
/// Phase 1 orchestrator: proves end-to-end wiring (CSV selection → count rows → report).
/// </summary>
public sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly ICsvLoader _csvLoader;
    private readonly ILogger<WorkflowRunner> _logger;

    public WorkflowRunner(ICsvLoader csvLoader, ILogger<WorkflowRunner> logger)
    {
        _csvLoader = csvLoader;
        _logger = logger;
    }

    public async Task<RunResult> RunAsync(string csvPath, string outputFolder, CancellationToken ct = default)
    {
        _logger.LogInformation("Run started. CSV={CsvPath}, OutputFolder={OutputFolder}", csvPath, outputFolder);

        try
        {
            int rows = await _csvLoader.CountRowsAsync(csvPath, ct);
            _logger.LogInformation("CSV loaded successfully. Rows={RowCount}", rows);

            // Phase 1: no export yet; we just confirm the plumbing works.
            return new RunResult(true, "Loaded CSV successfully.", rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Run failed.");
            return new RunResult(false, $"Run failed: {ex.Message}", 0);
        }
    }
}
