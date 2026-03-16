using Hel.Domain.Models;

namespace Hel.Application.Contracts;

/// <summary>
/// Orchestrates a full run: ingest → classify → export → summary.
/// </summary>
public interface IWorkflowOrchestrator
{
    Task<RunSummary> RunAsync(string csvPath, string outputFolder, CancellationToken ct = default);
}
