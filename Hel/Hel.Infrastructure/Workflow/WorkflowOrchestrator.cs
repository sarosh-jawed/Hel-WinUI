using Hel.Application.Contracts;
using Hel.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Hel.Infrastructure.Workflow;

/// <summary>
/// Phase 4 orchestrator: now uses real ingested ItemRecord objects instead of line counting.
/// Classification and export still come later, so bucket counts remain empty for now.
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
            TotalRecords: ingestResult.Records.Count,
            CountsPerBucket: new Dictionary<string, int>(),
            UnassignedCount: 0,
            FallbackUsageCount: fallbackUsageCount,
            ParseFailuresCount: ingestResult.ParseFailuresCount
        );

        _logger.LogInformation(
            "Run completed. TotalRecords={TotalRecords}, FallbackUsageCount={FallbackUsageCount}, ParseFailures={ParseFailures}",
            summary.TotalRecords,
            summary.FallbackUsageCount,
            summary.ParseFailuresCount);

        return summary;
    }
}
