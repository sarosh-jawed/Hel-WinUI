//Optional Value Object for Location Name, as it may be null or empty in some cases. This allows us to handle such cases gracefully without throwing exceptions.
namespace Hel.Domain.ValueObjects;

public readonly record struct LocationName(string Value)
{
    public override string ToString() => Value ?? string.Empty;
}
