using CsvHelper;
using System.Globalization;
using Hel.Application.Abstractions;

namespace Hel.Infrastructure.Csv;

/// <summary>
/// Infrastructure implementation using CsvHelper.
/// Phase 1: only counts rows to prove plumbing works.
/// </summary>
public sealed class CsvLoader : ICsvLoader
{
    public async Task<int> CountRowsAsync(string csvPath, CancellationToken ct = default)
    {
        // Minimal, reliable smoke-check: count data rows.
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();

        int count = 0;
        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            count++;
        }

        return count;
    }
}
