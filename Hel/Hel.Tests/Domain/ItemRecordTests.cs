using FluentAssertions;
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

        record.ResolvedCallNumber.Value.Should().Be("QA 76.73");
        record.UsedHoldingsFallback.Should().BeFalse();
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

        record.ResolvedCallNumber.Value.Should().Be("HOLD 1");
        record.UsedHoldingsFallback.Should().BeTrue();
    }
}
