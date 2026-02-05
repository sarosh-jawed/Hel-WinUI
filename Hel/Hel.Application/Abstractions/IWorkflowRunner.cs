namespace Hel.Application.Abstractions;

/// <summary>
/// Orchestrates one run: load → classify → export → summary.
/// Phase 1: returns a simple status result.
/// </summary>
public interface IWorkflowRunner
{
    Task<RunResult> RunAsync(string csvPath, string outputFolder, CancellationToken ct = default);
}

public sealed record RunResult(bool Success, string Message, int RowsProcessed);
