using Hel.Application.Contracts;
using Hel.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Hel.Infrastructure.Workflow;

/// <summary>
/// Phase 7 placeholder orchestrator.
/// The UI currently drives filtering, classification, and export directly,
/// but this class stays compile-safe and ready for later orchestration refactoring.
/// </summary>
public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly ICsvIngestService _ingest;
    private readonly ILogger<WorkflowOrchestrator> _logger;

    public WorkflowOrchestrator(ICsvIngestService ingest, ILogger<WorkflowOrchestrator> logger)
    {
        _ingest = ingest;
        _logger = logger;
    }

    public async Task<RunSummary> RunAsync(string csvPath, string outputFolder, CancellationToken ct = default)
    {
        _logger.LogInformation("Run started. CSV={CsvPath}, OutputFolder={OutputFolder}", csvPath, outputFolder);

        var ingestResult = await _ingest.IngestAsync(csvPath, ct);

        int fallbackUsageCount = ingestResult.Records.Count(r => r.UsedHoldingsFallback);

        var summary = new RunSummary(
            CsvFileName: Path.GetFileName(csvPath),
            TotalRowsLoaded: ingestResult.Records.Count,
            RowsAfterWawlFilter: ingestResult.Records.Count,
            RowsAfterLocationFilter: ingestResult.Records.Count,
            CountsPerBucket: new Dictionary<string, int>(),
            UnassignedCount: 0,
            FallbackUsageCount: fallbackUsageCount,
            ParseFailuresCount: ingestResult.ParseFailuresCount
        );

        _logger.LogInformation(
            "Run completed. TotalRowsLoaded={TotalRowsLoaded}, FallbackUsageCount={FallbackUsageCount}, ParseFailures={ParseFailuresCount}",
            summary.TotalRowsLoaded,
            summary.FallbackUsageCount,
            summary.ParseFailuresCount);

        return summary;
    }
}
