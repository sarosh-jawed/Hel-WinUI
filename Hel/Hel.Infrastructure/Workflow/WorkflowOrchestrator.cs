using CsvHelper;
using Hel.Application.Contracts;
using Hel.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Hel.Infrastructure.Workflow;

/// <summary>
/// Phase 2 orchestrator: still a smoke-test.
/// It computes summary using row count (without full parsing) to keep UI behavior intact.
/// Phase 3 will ingest ItemRecords and call classification + export.
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

        // Phase 2: call ingest contract (currently minimal).
        var ingestResult = await _ingest.IngestAsync(csvPath, ct);

        // We currently do not map rows into ItemRecord yet (Phase 3),
        // so TotalRecords is unknown from ingestResult. We will keep UI reporting by counting rows here.
        // This is temporary and will be removed when real mapping is implemented.
        int rows = await CountRowsAsync(csvPath, ct);

        var summary = new RunSummary(
            TotalRecords: rows,
            CountsPerBucket: new Dictionary<string, int>(),
            UnassignedCount: 0,
            FallbackUsageCount: 0,
            ParseFailuresCount: ingestResult.ParseFailuresCount
        );

        _logger.LogInformation("Run completed. TotalRecords={TotalRecords}", summary.TotalRecords);
        return summary;
    }

    private static async Task<int> CountRowsAsync(string csvPath, CancellationToken ct)
    {
        using var reader = new StreamReader(csvPath);
        int count = 0;
        // naive count of data lines excluding header:
        // Phase 2 acceptable; Phase 3 will parse properly.
        string? header = await reader.ReadLineAsync();
        while (await reader.ReadLineAsync() is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(line))
                count++;
        }
        return count;
    }
}
