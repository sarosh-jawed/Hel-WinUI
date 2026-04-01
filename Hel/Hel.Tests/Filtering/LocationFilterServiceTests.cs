using Hel.Domain.Models;
using Hel.Domain.ValueObjects;
using Hel.Infrastructure.Filtering;
using Xunit;

namespace Hel.Tests.Filtering;

public class LocationFilterServiceTests
{
    [Fact]
    public void ExtractAvailableLocations_Should_ReturnDistinctLocations_ForConfiguredLibrary()
    {
        var service = new LocationFilterService();

        var records = new List<ItemRecord>
        {
            CreateRecord("William Allen White Library", "stacks", "Stacks", "Title 1"),
            CreateRecord("William Allen White Library", "stacks", "Stacks", "Title 2"),
            CreateRecord("William Allen White Library", "ref", "Reference", "Title 3"),
            CreateRecord("Other Library", "stacks", "Stacks", "Title 4")
        };

        var locations = service.ExtractAvailableLocations(records, "William Allen White Library");

        Assert.Equal(2, locations.Count);

        var codes = locations.Select(x => x.Code).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "ref", "stacks" }, codes);
    }

    [Fact]
    public void ApplyScopeAndLocationFilter_Should_ReturnOnlySelectedLocations_ForConfiguredLibrary()
    {
        var service = new LocationFilterService();

        var records = new List<ItemRecord>
        {
            CreateRecord("William Allen White Library", "stacks", "Stacks", "Title 1"),
            CreateRecord("William Allen White Library", "ref", "Reference", "Title 2"),
            CreateRecord("Other Library", "stacks", "Stacks", "Title 3")
        };

        var filtered = service.ApplyScopeAndLocationFilter(
            records,
            "William Allen White Library",
            new[] { "stacks" });

        Assert.Single(filtered);
        Assert.Equal("Title 1", filtered[0].Title.Value);
        Assert.Equal("stacks", filtered[0].LocationCode.Value);
        Assert.Equal("William Allen White Library", filtered[0].LibraryName.Value);
    }

    private static ItemRecord CreateRecord(
        string libraryName,
        string locationCode,
        string locationName,
        string title)
    {
        return new ItemRecord(
            new LibraryName(libraryName),
            new LocationCode(locationCode),
            new LocationName(locationName),
            new Title(title),
            new Barcode("12345"),
            new EffectiveCallNumber("QA 76.73"),
            new HoldingsCallNumber("HB 1"));
    }
}
