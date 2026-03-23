using System.Linq;

namespace Hel.Domain.Models;

/// <summary>
/// High-level run metrics for preview and RunSummary.txt generation.
/// </summary>
public sealed record RunSummary(
    string CsvFileName,
    int TotalRowsLoaded,
    int RowsAfterWawlFilter,
    int RowsAfterLocationFilter,
    IReadOnlyDictionary<string, int> CountsPerBucket,
    int UnassignedCount,
    int FallbackUsageCount,
    int ParseFailuresCount)
{
    public int AssignedCount => CountsPerBucket.Values.Sum();

    public static RunSummary Empty { get; } =
        new RunSummary(
            CsvFileName: string.Empty,
            TotalRowsLoaded: 0,
            RowsAfterWawlFilter: 0,
            RowsAfterLocationFilter: 0,
            CountsPerBucket: new Dictionary<string, int>(),
            UnassignedCount: 0,
            FallbackUsageCount: 0,
            ParseFailuresCount: 0);
}
