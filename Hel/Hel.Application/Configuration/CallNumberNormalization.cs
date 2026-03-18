namespace Hel.Application.Configuration;

/// <summary>
/// Controls how call numbers are normalized before Dewey parsing.
/// Prefix stripping is config-driven so routing behavior can be changed without code edits.
/// </summary>
public sealed class CallNumberNormalization
{
    public List<string> StripPrefixes { get; set; } = new();
}
