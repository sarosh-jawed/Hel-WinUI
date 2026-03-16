namespace Hel.Domain.Models;

/// <summary>
/// High-level run metrics for preview + RunSummary.txt generation.
/// CountsPerBucket should include only assigned buckets.
/// </summary>
public sealed record RunSummary(
    int TotalRecords,
    IReadOnlyDictionary<string, int> CountsPerBucket,
    int UnassignedCount,
    int FallbackUsageCount,
    int ParseFailuresCount)
{
    public static RunSummary Empty { get; } =
        new RunSummary(
            TotalRecords: 0,
            CountsPerBucket: new Dictionary<string, int>(),
            UnassignedCount: 0,
            FallbackUsageCount: 0,
            ParseFailuresCount: 0);
}
