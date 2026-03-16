namespace Hel.Domain.ValueObjects;

public readonly record struct LocationCode(string Value)
{
    public override string ToString() => Value ?? string.Empty;
}
