namespace Hel.Domain.ValueObjects;

public readonly record struct Title(string Value)
{
    public override string ToString() => Value ?? string.Empty;
}
