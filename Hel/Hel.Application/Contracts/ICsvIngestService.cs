using Hel.Domain.Models;

namespace Hel.Application.Contracts;

/// <summary>
/// Reads CSV input and produces normalized ItemRecord objects.
/// Phase 2: contract only. Implementation comes in Phase 3.
/// </summary>
public interface ICsvIngestService
{
    Task<CsvIngestResult> IngestAsync(string csvPath, CancellationToken ct = default);
}

/// <summary>
/// Result of ingest: records + parse failures.
/// </summary>
public sealed record CsvIngestResult(
    IReadOnlyList<ItemRecord> Records,
    int ParseFailuresCount);
