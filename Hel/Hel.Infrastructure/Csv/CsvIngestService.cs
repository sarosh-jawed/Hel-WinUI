using CsvHelper;
using Hel.Application.Configuration;
using Hel.Application.Contracts;
using Hel.Domain.Models;
using Hel.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Hel.Infrastructure.Csv;

/// <summary>
/// Reads the CSV using config-driven header names and produces normalized ItemRecord objects.
/// This keeps ingestion resilient when column order changes, as long as headers stay consistent.
/// </summary>
public sealed class CsvIngestService : ICsvIngestService
{
    private readonly HelConfig _config;
    private readonly ILogger<CsvIngestService> _logger;

    public CsvIngestService(HelConfig config, ILogger<CsvIngestService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<CsvIngestResult> IngestAsync(string csvPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(csvPath))
            throw new ArgumentException("CSV path is required.", nameof(csvPath));

        if (!File.Exists(csvPath))
            throw new FileNotFoundException("CSV file was not found.", csvPath);

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!await csv.ReadAsync())
            throw new InvalidOperationException("CSV file is empty or missing a header row.");

        csv.ReadHeader();

        string[] headers = csv.HeaderRecord ?? Array.Empty<string>();
        ValidateRequiredHeaders(headers);

        var records = new List<ItemRecord>();
        int parseFailures = 0;

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var record = new ItemRecord(
                    LibraryName: new LibraryName(ReadField(csv, _config.CsvColumns.LibraryName)),
                    LocationCode: new LocationCode(ReadField(csv, _config.CsvColumns.LocationCode)),
                    LocationName: new LocationName(ReadField(csv, _config.CsvColumns.LocationName)),
                    Title: new Title(ReadField(csv, _config.CsvColumns.Title)),
                    Barcode: new Barcode(ReadField(csv, _config.CsvColumns.Barcode)),
                    EffectiveCallNumber: new EffectiveCallNumber(ReadField(csv, _config.CsvColumns.EffectiveCallNumber)),
                    HoldingsCallNumber: new HoldingsCallNumber(ReadField(csv, _config.CsvColumns.HoldingsCallNumber))
                );

                records.Add(record);
            }
            catch (Exception ex)
            {
                parseFailures++;
                _logger.LogWarning(
                    ex,
                    "Skipping CSV row {RowNumber} because it could not be parsed cleanly.",
                    csv.Parser.Row);
            }
        }

        _logger.LogInformation(
            "CSV ingestion completed. ParsedRecords={ParsedRecords}, ParseFailures={ParseFailures}",
            records.Count,
            parseFailures);

        return new CsvIngestResult(records, parseFailures);
    }

    private void ValidateRequiredHeaders(IEnumerable<string> headers)
    {
        var availableHeaders = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);

        var requiredHeaders = new[]
        {
            _config.CsvColumns.LibraryName,
            _config.CsvColumns.LocationCode,
            _config.CsvColumns.LocationName,
            _config.CsvColumns.Title,
            _config.CsvColumns.Barcode,
            _config.CsvColumns.EffectiveCallNumber,
            _config.CsvColumns.HoldingsCallNumber
        };

        var missingHeaders = requiredHeaders
            .Where(header => !availableHeaders.Contains(header))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingHeaders.Count > 0)
        {
            throw new InvalidOperationException(
                "CSV is missing required header(s): " + string.Join(", ", missingHeaders));
        }
    }

    private static string ReadField(CsvReader csv, string headerName)
    {
        if (!csv.TryGetField(headerName, out string? value))
        {
            throw new InvalidOperationException(
                $"CSV row {csv.Parser.Row} could not read header '{headerName}'.");
        }

        return Normalize(value);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}
