namespace Hel.Domain.ValueObjects;

public readonly record struct LibraryName(string Value)
{
    public override string ToString() => Value ?? string.Empty;
}
