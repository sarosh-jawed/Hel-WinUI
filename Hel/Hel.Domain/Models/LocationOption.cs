namespace Hel.Domain.Models;

/// <summary>
/// Represents one selectable location discovered in the loaded dataset.
/// </summary>
public sealed record LocationOption(string Code, string? Name)
{
    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(Name)
            ? Code
            : $"{Code} ({Name})";
}
