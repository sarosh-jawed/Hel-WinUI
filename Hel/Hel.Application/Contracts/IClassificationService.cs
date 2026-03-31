using Hel.Domain.Models;

namespace Hel.Application.Contracts;

/// <summary>
/// Applies location rules first, then call-number rules, and returns deterministic routing results.
/// </summary>
public interface IClassificationService
{
    Task<ClassificationResult> ClassifyAsync(
        IReadOnlyList<ItemRecord> records,
        CancellationToken ct = default);
}

public sealed record ClassificationResult(
    IReadOnlyList<ClassifiedItem> Classified,
    IReadOnlyList<UnassignedItem> Unassigned,
    int FallbackUsageCount,
    int ParseFailuresCount);
