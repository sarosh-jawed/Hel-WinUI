namespace Hel.Application.Configuration;

/// <summary>
/// Library-level routing rule.
/// Useful when certain libraries should always map to a specific recipient.
/// </summary>
public sealed class LibraryRule
{
    public string LibraryName { get; set; } = string.Empty;
    public string RecipientKey { get; set; } = string.Empty;
}
