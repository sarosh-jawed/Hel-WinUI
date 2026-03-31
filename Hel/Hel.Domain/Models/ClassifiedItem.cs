namespace Hel.Domain.Models;

/// <summary>
/// Output of classification. BucketKey maps to a configured recipient.
/// RoutingReason is used by the Phase 9 preview grid so users can verify why an item routed.
/// </summary>
public sealed record ClassifiedItem(
    ItemRecord Record,
    string BucketKey,
    string RoutingReason);
