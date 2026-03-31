using FluentAssertions;
using Hel.Application.Configuration;
using Hel.Domain.Models;
using Hel.Domain.ValueObjects;
using Hel.Infrastructure.Export;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hel.Tests.Export;

public class TextExportServiceTests
{
    [Fact]
    public async Task ExportAsync_Should_Match_Golden_Output_Files()
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), $"hel-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);

        try
        {
            var config = CreateConfig();
            var builder = new TextBodyBuilder(config);
            var service = new TextExportService(
                config,
                builder,
                NullLogger<TextExportService>.Instance);

            var classified = new List<ClassifiedItem>
            {
                new(
                    CreateRecord(
                        title: "Assigned Title",
                        barcode: "111",
                        effectiveCallNumber: "303.38 A11",
                        holdingsCallNumber: ""),
                    "wawl",
                    "Dewey range rule: dewey-000-099 (parsed 75.25)")
            };

            var unassigned = new List<ItemRecord>
            {
                CreateRecord(
                    title: "Unassigned Title",
                    barcode: "222",
                    effectiveCallNumber: "",
                    holdingsCallNumber: "HB 1")
            };

            var summary = new RunSummary(
                CsvFileName: "Monthly Missing Items.csv",
                TotalRowsLoaded: 10,
                RowsAfterWawlFilter: 10,
                RowsAfterLocationFilter: 5,
                CountsPerBucket: new Dictionary<string, int> { ["wawl"] = 1 },
                UnassignedCount: 1,
                FallbackUsageCount: 1,
                ParseFailuresCount: 0);

            await service.ExportAsync(classified, unassigned, summary, tempFolder);

            string actualRecipientPath = Path.Combine(tempFolder, "wawl.txt");
            string actualUnassignedPath = Path.Combine(tempFolder, "Unassigned.txt");
            string actualSummaryPath = Path.Combine(tempFolder, "RunSummary.txt");

            string expectedRecipientPath = GetGoldenFixturePath("wawl.txt");
            string expectedUnassignedPath = GetGoldenFixturePath("Unassigned.txt");
            string expectedSummaryPath = GetGoldenFixturePath("RunSummary.txt");

            File.Exists(actualRecipientPath).Should().BeTrue();
            File.Exists(actualUnassignedPath).Should().BeTrue();
            File.Exists(actualSummaryPath).Should().BeTrue();

            string actualRecipientText = await File.ReadAllTextAsync(actualRecipientPath);
            string actualUnassignedText = await File.ReadAllTextAsync(actualUnassignedPath);
            string actualSummaryText = await File.ReadAllTextAsync(actualSummaryPath);

            string expectedRecipientText = await File.ReadAllTextAsync(expectedRecipientPath);
            string expectedUnassignedText = await File.ReadAllTextAsync(expectedUnassignedPath);
            string expectedSummaryText = await File.ReadAllTextAsync(expectedSummaryPath);

            NormalizeForGoldenComparison(actualRecipientText)
                .Should().Be(NormalizeForGoldenComparison(expectedRecipientText));

            NormalizeForGoldenComparison(actualUnassignedText)
                .Should().Be(NormalizeForGoldenComparison(expectedUnassignedText));

            NormalizeForGoldenComparison(actualSummaryText)
                .Should().Be(NormalizeForGoldenComparison(expectedSummaryText));
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, recursive: true);
        }
    }

    private static HelConfig CreateConfig()
    {
        return new HelConfig
        {
            Recipients =
            [
                new()
                {
                    Key = "wawl",
                    DisplayName = "William Allen White Library",
                    Email = ""
                }
            ],
            Output = new Output
            {
                Root = "%LOCALAPPDATA%\\Hel\\Output",
                LogsRoot = "%LOCALAPPDATA%\\Hel\\Logs",
                MonthFolderFormat = "yyyy-MM",
                UnassignedFileName = "Unassigned.txt",
                RunSummaryFileName = "RunSummary.txt"
            },
            TextTemplate = new TextTemplate
            {
                GreetingLine = "Hello here are the titles in your area that have been missing or lost",
                CountLineTemplate = "Count: {Count}",
                HeaderLine = "Title | Barcode | Call number",
                BulletPrefix = "*",
                RecipientFileLineTemplate = "{Title} | {Barcode} | {CallNumber}",
                UnassignedFileLineTemplate = "{Title} | {Barcode} | {CallNumber}",
                ClosingLine = "Thanks,",
                SignatureLine = "John",
                SummaryHeader = "Hel Run Summary"
            }
        };
    }

    private static ItemRecord CreateRecord(
        string title,
        string barcode,
        string effectiveCallNumber,
        string holdingsCallNumber)
    {
        return new ItemRecord(
            new LibraryName("William Allen White Library"),
            new LocationCode("ESULCB3"),
            new LocationName("Children's Books, 3rd Floor"),
            new Title(title),
            new Barcode(barcode),
            new EffectiveCallNumber(effectiveCallNumber),
            new HoldingsCallNumber(holdingsCallNumber));
    }

    private static string GetGoldenFixturePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "Fixtures",
            "Golden",
            fileName));
    }

    private static string NormalizeForGoldenComparison(string value)
    {
        return value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .TrimEnd();
    }
}
