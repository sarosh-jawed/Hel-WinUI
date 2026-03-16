namespace Hel.Application.Configuration;

/// <summary>
/// Text output templates.
/// These are placeholders for future export formatting phases.
/// </summary>
public sealed class TextTemplate
{
    public string RecipientFileLineTemplate { get; set; } = "{Title}\t{Barcode}\t{ResolvedCallNumber}";
    public string UnassignedFileLineTemplate { get; set; } = "{Title}\t{Barcode}\t{ResolvedCallNumber}";
    public string SummaryHeader { get; set; } = "Hel Run Summary";
}
