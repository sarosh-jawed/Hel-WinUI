namespace Hel.Application.Abstractions;

/// <summary>
/// Loads raw records from the Monthly Missing Items CSV.
/// Phase 1: keep it simple; full mapping rules come later.
/// </summary>

public interface ICsvLoader
{
    Task<int> CountRowsAsync(string csvPath, CancellationToken ct = default);
}
