namespace Hel.Application.Configuration;

/// <summary>
/// Call number routing rule.
/// MatchMode is intentionally simple for now:
/// - "StartsWith"
/// - "DeweyRange"
/// </summary>
public sealed class CallNumberRule
{
    public string Key { get; set; } = string.Empty;
    public string? LibraryName { get; set; }

    public string MatchMode { get; set; } = "StartsWith";

    // Used for StartsWith matching
    public string? Prefix { get; set; }

    // Used for DeweyRange matching
    public decimal? RangeStart { get; set; }
    public decimal? RangeEnd { get; set; }

    public string RecipientKey { get; set; } = string.Empty;
}
