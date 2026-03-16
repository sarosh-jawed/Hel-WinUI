namespace Hel.Domain.Models;

/// <summary>
/// A recipient bucket target. Email is optional now, but included for future workflows.
/// </summary>
public sealed record Recipient(string Key, string DisplayName, string? Email = null);
