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
    public async Task ExportAsync_Should_Create_Recipient_Unassigned_And_Summary_Files()
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
                    "wawl")
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

            string recipientPath = Path.Combine(tempFolder, "wawl.txt");
            string unassignedPath = Path.Combine(tempFolder, "Unassigned.txt");
            string summaryPath = Path.Combine(tempFolder, "RunSummary.txt");

            File.Exists(recipientPath).Should().BeTrue();
            File.Exists(unassignedPath).Should().BeTrue();
            File.Exists(summaryPath).Should().BeTrue();

            string recipientText = await File.ReadAllTextAsync(recipientPath);
            string unassignedText = await File.ReadAllTextAsync(unassignedPath);
            string summaryText = await File.ReadAllTextAsync(summaryPath);

            recipientText.Should().Contain("Hello here are the titles in your area that have been missing or lost");
            recipientText.Should().Contain("* Assigned Title | 111 | 303.38 A11");
            recipientText.Should().Contain("Thanks,");
            recipientText.Should().Contain("John");

            unassignedText.Should().Contain("* Unassigned Title | 222 | HB 1");

            summaryText.Should().Contain("Hel Run Summary");
            summaryText.Should().Contain("CSV file name: Monthly Missing Items.csv");
            summaryText.Should().Contain("Rows after location filter: 5");
            summaryText.Should().Contain("wawl: 1");
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
            Recipients = new List<RecipientDefinition>
            {
                new()
                {
                    Key = "wawl",
                    DisplayName = "William Allen White Library",
                    Email = ""
                }
            },
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
}
