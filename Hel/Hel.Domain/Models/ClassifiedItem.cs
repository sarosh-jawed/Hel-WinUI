namespace Hel.Domain.Models;

/// <summary>
/// Output of classification. BucketKey maps to a configured recipient.
/// </summary>
public sealed record ClassifiedItem(ItemRecord Record, string BucketKey);
