using Hel.Application.Contracts;
using Hel.Domain.Models;

namespace Hel.Infrastructure.Filtering;

/// <summary>
/// Applies library scoping and location-based filtering over loaded ItemRecord objects.
/// </summary>
public sealed class LocationFilterService : ILocationFilterService
{
    public IReadOnlyList<LocationOption> ExtractAvailableLocations(
        IReadOnlyList<ItemRecord> records,
        string libraryName)
    {
        if (records is null)
            throw new ArgumentNullException(nameof(records));

        if (string.IsNullOrWhiteSpace(libraryName))
            throw new ArgumentException("Library name is required.", nameof(libraryName));

        var results = records
            .Where(r => string.Equals(
                r.LibraryName.Value,
                libraryName,
                StringComparison.OrdinalIgnoreCase))
            .Where(r => !string.IsNullOrWhiteSpace(r.LocationCode.Value))
            .GroupBy(
                r => r.LocationCode.Value.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                string code = group.Key;

                string? name = group
                    .Select(x => x.LocationName?.Value)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                return new LocationOption(code, name);
            })
            .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return results;
    }

    public IReadOnlyList<ItemRecord> ApplyScopeAndLocationFilter(
        IReadOnlyList<ItemRecord> records,
        string libraryName,
        IReadOnlyCollection<string> selectedLocationCodes)
    {
        if (records is null)
            throw new ArgumentNullException(nameof(records));

        if (string.IsNullOrWhiteSpace(libraryName))
            throw new ArgumentException("Library name is required.", nameof(libraryName));

        if (selectedLocationCodes is null)
            throw new ArgumentNullException(nameof(selectedLocationCodes));

        var selectedSet = new HashSet<string>(
            selectedLocationCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var filtered = records
            .Where(r => string.Equals(
                r.LibraryName.Value,
                libraryName,
                StringComparison.OrdinalIgnoreCase))
            .Where(r => selectedSet.Contains(r.LocationCode.Value.Trim()))
            .ToList();

        return filtered;
    }
}
