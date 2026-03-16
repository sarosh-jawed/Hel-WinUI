using Hel.Domain.ValueObjects;

namespace Hel.Domain.Models;

/// <summary>
/// Represents one row of input from the Monthly Missing Items CSV after normalization.
/// We keep raw values strongly typed to prevent "string soup" as rules grow.
/// </summary>
public sealed record ItemRecord(
    LibraryName LibraryName,
    LocationCode LocationCode,
    LocationName? LocationName,
    Title Title,
    Barcode Barcode,
    EffectiveCallNumber EffectiveCallNumber,
    HoldingsCallNumber HoldingsCallNumber)
{
    /// <summary>
    /// Prefer item effective call number; fall back to holdings call number.
    /// This matches the workflow requirement for resolving call number.
    /// </summary>
    public ResolvedCallNumber ResolvedCallNumber =>
        !EffectiveCallNumber.IsEmpty
            ? new ResolvedCallNumber(EffectiveCallNumber.Value)
            : new ResolvedCallNumber(HoldingsCallNumber.Value);

    /// <summary>
    /// True when we had to fall back to holdings call number.
    /// Useful for RunSummary fallback usage count.
    /// </summary>
    public bool UsedHoldingsFallback => EffectiveCallNumber.IsEmpty && !HoldingsCallNumber.IsEmpty;
}
