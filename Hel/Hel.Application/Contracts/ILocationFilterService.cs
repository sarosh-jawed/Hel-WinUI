using Hel.Domain.Models;

namespace Hel.Application.Contracts;

/// <summary>
/// Provides location extraction and filtering over already-loaded ItemRecord objects.
/// This keeps UI logic thin and keeps filtering rules testable.
/// </summary>
public interface ILocationFilterService
{
    IReadOnlyList<LocationOption> ExtractAvailableLocations(
        IReadOnlyList<ItemRecord> records,
        string libraryName);

    IReadOnlyList<ItemRecord> ApplyScopeAndLocationFilter(
        IReadOnlyList<ItemRecord> records,
        string libraryName,
        IReadOnlyCollection<string> selectedLocationCodes);
}
