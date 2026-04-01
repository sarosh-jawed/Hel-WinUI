using Hel.Domain.Models;
using Hel.Domain.ValueObjects;
using Xunit;

namespace Hel.Tests.Domain;

public class ItemRecordTests
{
    [Fact]
    public void ResolvedCallNumber_Prefers_EffectiveCallNumber_WhenPresent()
    {
        var record = new ItemRecord(
            new LibraryName("WAWL"),
            new LocationCode("stacks"),
            new LocationName("Stacks"),
            new Title("Test"),
            new Barcode("123"),
            new EffectiveCallNumber("QA 76.73"),
            new HoldingsCallNumber("HOLD 1"));

        Assert.Equal("QA 76.73", record.ResolvedCallNumber.Value);
        Assert.False(record.UsedHoldingsFallback);
    }

    [Fact]
    public void ResolvedCallNumber_FallsBack_ToHoldings_WhenEffectiveMissing()
    {
        var record = new ItemRecord(
            new LibraryName("WAWL"),
            new LocationCode("stacks"),
            new LocationName("Stacks"),
            new Title("Test"),
            new Barcode("123"),
            new EffectiveCallNumber(""),
            new HoldingsCallNumber("HOLD 1"));

        Assert.Equal("HOLD 1", record.ResolvedCallNumber.Value);
        Assert.True(record.UsedHoldingsFallback);
    }
}
