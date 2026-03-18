namespace Hel.Application.Configuration;

/// <summary>
/// Root configuration object for Hel.
/// This is the single typed shape bound from config.json + config.local.json.
/// </summary>
public sealed class HelConfig
{
    public CsvColumns CsvColumns { get; set; } = new();
    public List<LibraryRule> LibraryRules { get; set; } = new();
    public List<LocationRule> LocationRules { get; set; } = new();
    public List<CallNumberRule> CallNumberRules { get; set; } = new();
    public CallNumberNormalization CallNumberNormalization { get; set; } = new();
    public List<RecipientDefinition> Recipients { get; set; } = new();
    public Output Output { get; set; } = new();
    public TextTemplate TextTemplate { get; set; } = new();
}
