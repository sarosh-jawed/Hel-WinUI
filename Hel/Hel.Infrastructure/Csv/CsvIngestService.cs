using CsvHelper;
using Hel.Application.Contracts;
using Hel.Domain.Models;
using Hel.Domain.ValueObjects;
using System.Globalization;

namespace Hel.Infrastructure.Csv;

/// <summary>
/// Phase 2 implementation is intentionally minimal:
/// we confirm the pipeline by counting rows and returning an empty record list.
/// Phase 3 will map each CSV row into ItemRecord using header-based mapping.
/// </summary>
public sealed class CsvIngestService : ICsvIngestService
{
    public async Task<CsvIngestResult> IngestAsync(string csvPath, CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        await csv.ReadAsync();
        csv.ReadHeader();

        int rowCount = 0;
        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            rowCount++;
        }

        // Phase 2: not mapping to ItemRecord yet (that starts Phase 3).
        // We still report row count by treating all as "parsed records" count later in orchestrator.
        return new CsvIngestResult(Records: Array.Empty<ItemRecord>(), ParseFailuresCount: 0);
    }
}
