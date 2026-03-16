namespace Hel.Domain.ValueObjects;

public readonly record struct HoldingsCallNumber(string Value)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value ?? string.Empty;
}
