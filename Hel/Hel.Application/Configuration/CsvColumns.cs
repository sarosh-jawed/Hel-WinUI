namespace Hel.Application.Configuration;

/// <summary>
/// Header names expected in the CSV export.
/// These are config-driven so Hel can adapt without code changes.
/// </summary>
public sealed class CsvColumns
{
    public string LibraryName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string EffectiveCallNumber { get; set; } = string.Empty;
    public string HoldingsCallNumber { get; set; } = string.Empty;
}
