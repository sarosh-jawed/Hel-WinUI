namespace Hel.Application.Configuration;

/// <summary>
/// Location-based routing rule.
/// A rule can match by location code and/or location name.
/// </summary>
public sealed class LocationRule
{
    public string Key { get; set; } = string.Empty;
    public string? LibraryName { get; set; }

    public List<string> LocationCodes { get; set; } = new();
    public List<string> LocationNames { get; set; } = new();

    public string RecipientKey { get; set; } = string.Empty;
}
