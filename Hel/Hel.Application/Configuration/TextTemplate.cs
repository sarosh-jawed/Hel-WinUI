namespace Hel.Application.Configuration;

/// <summary>
/// Text output templates used for recipient files, unassigned files, and summary output.
/// These values are config-driven so formatting can change without code edits.
/// </summary>
public sealed class TextTemplate
{
    public string GreetingLine { get; set; } =
        "Hello here are the titles in your area that have been missing or lost";

    public string CountLineTemplate { get; set; } = "Count: {Count}";

    public string HeaderLine { get; set; } = "Title | Barcode | Call number";

    public string BulletPrefix { get; set; } = "*";

    public string RecipientFileLineTemplate { get; set; } =
        "{Title} | {Barcode} | {CallNumber}";

    public string UnassignedFileLineTemplate { get; set; } =
        "{Title} | {Barcode} | {CallNumber}";

    public string ClosingLine { get; set; } = "Thanks,";

    public string SignatureLine { get; set; } = "John";

    public string SummaryHeader { get; set; } = "Hel Run Summary";
}
