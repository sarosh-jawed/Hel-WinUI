namespace Hel.Application.Configuration;

/// <summary>
/// Config-defined recipient target.
/// Email is optional for now, but included for future use.
/// </summary>
public sealed class RecipientDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
