namespace Hel.Domain.ValueObjects;

public readonly record struct Barcode(string Value)
{
    public override string ToString() => Value ?? string.Empty;
}
