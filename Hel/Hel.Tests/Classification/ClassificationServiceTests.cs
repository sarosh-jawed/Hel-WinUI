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

        Assert.Single(result.Classified);
        Assert.Equal("loc-bucket", result.Classified[0].BucketKey);
        Assert.Equal("Location rule: location-stacks", result.Classified[0].RoutingReason);
        Assert.Empty(result.Unassigned);
        Assert.Equal(0, result.ParseFailuresCount);
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

        Assert.Single(result.Classified);
        Assert.Equal("dewey-bucket", result.Classified[0].BucketKey);
        Assert.StartsWith("Dewey range rule: dewey-300-399", result.Classified[0].RoutingReason);
        Assert.Empty(result.Unassigned);
        Assert.Equal(0, result.ParseFailuresCount);
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

        Assert.Empty(result.Classified);
        Assert.Single(result.Unassigned);
        Assert.Equal("Unreadable call number after normalization", result.Unassigned[0].RoutingReason);
        Assert.Equal(1, result.ParseFailuresCount);
    }

    [Fact]
    public async Task ReadableButUnmatched_Should_Go_To_Unassigned_WithReason()
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
                title: "Readable no match",
                effectiveCallNumber: "700.1 ART",
                holdingsCallNumber: "")
        };

        var result = await service.ClassifyAsync(records);

        Assert.Empty(result.Classified);
        Assert.Single(result.Unassigned);
        Assert.Equal("Readable call number but no rule matched", result.Unassigned[0].RoutingReason);
        Assert.Equal(0, result.ParseFailuresCount);
    }

    private static HelConfig CreateConfig()
    {
        return new HelConfig
        {
            LocationRules =
            [
                new()
                {
                    Key = "location-stacks",
                    LibraryName = "William Allen White Library",
                    LocationCodes = ["stacks"],
                    RecipientKey = "loc-bucket"
                }
            ],
            CallNumberRules =
            [
                new()
                {
                    Key = "dewey-300-399",
                    LibraryName = "William Allen White Library",
                    MatchMode = "DeweyRange",
                    RangeStart = 300,
                    RangeEnd = 399.999m,
                    RecipientKey = "dewey-bucket"
                }
            ],
            CallNumberNormalization = new CallNumberNormalization
            {
                StripPrefixes = ["REF", "Q", "R"]
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
