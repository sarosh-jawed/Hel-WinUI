using FluentAssertions;
using Hel.Application.Configuration;
using Hel.Domain.Models;
using Hel.Domain.ValueObjects;
using Hel.Infrastructure.Classification;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hel.Tests.Classification;

public class ClassificationServiceTests
{
    [Fact]
    public async Task LocationRule_Should_Override_DeweyRule()
    {
        var service = new ClassificationService(
            CreateConfig(),
            NullLogger<ClassificationService>.Instance);

        var records = new List<ItemRecord>
        {
            CreateRecord(
                libraryName: "William Allen White Library",
                locationCode: "stacks",
                locationName: "Stacks",
                title: "Location wins",
                effectiveCallNumber: "Q 303.38",
                holdingsCallNumber: "")
        };

        var result = await service.ClassifyAsync(records);

        result.Classified.Should().HaveCount(1);
        result.Classified[0].BucketKey.Should().Be("loc-bucket");
        result.Unassigned.Should().BeEmpty();
        result.ParseFailuresCount.Should().Be(0);
    }

    [Fact]
    public async Task DeweyRange_Should_Match_Expected_Bucket()
    {
        var service = new ClassificationService(
            CreateConfig(),
            NullLogger<ClassificationService>.Instance);

        var records = new List<ItemRecord>
        {
            CreateRecord(
                libraryName: "William Allen White Library",
                locationCode: "other",
                locationName: "Other",
                title: "Dewey match",
                effectiveCallNumber: "REF 303.38",
                holdingsCallNumber: "")
        };

        var result = await service.ClassifyAsync(records);

        result.Classified.Should().HaveCount(1);
        result.Classified[0].BucketKey.Should().Be("dewey-bucket");
        result.Unassigned.Should().BeEmpty();
        result.ParseFailuresCount.Should().Be(0);
    }

    [Fact]
    public async Task ParseFailures_Should_Go_To_Unassigned()
    {
        var service = new ClassificationService(
            CreateConfig(),
            NullLogger<ClassificationService>.Instance);

        var records = new List<ItemRecord>
        {
            CreateRecord(
                libraryName: "William Allen White Library",
                locationCode: "other",
                locationName: "Other",
                title: "Bad Dewey",
                effectiveCallNumber: "REF ABC",
                holdingsCallNumber: "")
        };

        var result = await service.ClassifyAsync(records);

        result.Classified.Should().BeEmpty();
        result.Unassigned.Should().HaveCount(1);
        result.ParseFailuresCount.Should().Be(1);
    }

    private static HelConfig CreateConfig()
    {
        return new HelConfig
        {
            LocationRules = new List<LocationRule>
            {
                new()
                {
                    Key = "location-stacks",
                    LibraryName = "William Allen White Library",
                    LocationCodes = new List<string> { "stacks" },
                    RecipientKey = "loc-bucket"
                }
            },
            CallNumberRules = new List<CallNumberRule>
            {
                new()
                {
                    Key = "dewey-300-399",
                    LibraryName = "William Allen White Library",
                    MatchMode = "DeweyRange",
                    RangeStart = 300,
                    RangeEnd = 399.999m,
                    RecipientKey = "dewey-bucket"
                }
            },
            CallNumberNormalization = new CallNumberNormalization
            {
                StripPrefixes = new List<string> { "REF", "Q", "R" }
            }
        };
    }

    private static ItemRecord CreateRecord(
        string libraryName,
        string locationCode,
        string locationName,
        string title,
        string effectiveCallNumber,
        string holdingsCallNumber)
    {
        return new ItemRecord(
            new LibraryName(libraryName),
            new LocationCode(locationCode),
            new LocationName(locationName),
            new Title(title),
            new Barcode("12345"),
            new EffectiveCallNumber(effectiveCallNumber),
            new HoldingsCallNumber(holdingsCallNumber));
    }
}
