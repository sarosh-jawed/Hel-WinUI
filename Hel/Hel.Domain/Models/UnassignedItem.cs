namespace Hel.Domain.Models;

/// <summary>
/// Represents an item that could not be assigned to a configured recipient.
/// RoutingReason explains whether the item was unreadable or simply unmatched.
/// </summary>
public sealed record UnassignedItem(
    ItemRecord Record,
    string RoutingReason);
