namespace Hel.Domain.ValueObjects;

public readonly record struct ResolvedCallNumber(string Value)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value ?? string.Empty;
}
