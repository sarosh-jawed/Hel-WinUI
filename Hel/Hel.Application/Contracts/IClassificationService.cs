using Hel.Domain.Models;

namespace Hel.Application.Contracts;

/// <summary>
/// Applies location + call number rules and assigns each record to a bucket.
/// </summary>
public interface IClassificationService
{
    Task<ClassificationResult> ClassifyAsync(
        IReadOnlyList<ItemRecord> records,
        CancellationToken ct = default);
}

public sealed record ClassificationResult(
    IReadOnlyList<ClassifiedItem> Classified,
    IReadOnlyList<ItemRecord> Unassigned,
    int FallbackUsageCount);
