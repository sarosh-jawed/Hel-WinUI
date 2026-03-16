using FluentAssertions;
using Hel.Application.Configuration;
using Hel.Infrastructure.Csv;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hel.Tests.Csv;

public class CsvIngestServiceTests
{
    [Fact]
    public async Task IngestAsync_WhenEffectiveCallNumberIsPresent_UsesEffectiveCallNumber()
    {
        var config = CreateValidConfig();
        var service = new CsvIngestService(config, NullLogger<CsvIngestService>.Instance);

        string csvPath = CreateTempCsv(
            "loclibrary.name,effective_location.code,effective_location.name,instances.title,items.barcode,items.effective_call_number,holdings.call_number",
            "William Allen White Library,stacks,Stacks,Test Title,12345,QA 76.73,HB 1");

        try
        {
            var result = await service.IngestAsync(csvPath);

            result.ParseFailuresCount.Should().Be(0);
            result.Records.Should().HaveCount(1);

            var record = result.Records[0];
            record.EffectiveCallNumber.Value.Should().Be("QA 76.73");
            record.HoldingsCallNumber.Value.Should().Be("HB 1");
            record.ResolvedCallNumber.Value.Should().Be("QA 76.73");
            record.UsedHoldingsFallback.Should().BeFalse();
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [Fact]
    public async Task IngestAsync_WhenEffectiveCallNumberIsEmpty_UsesHoldingsCallNumber()
    {
        var config = CreateValidConfig();
        var service = new CsvIngestService(config, NullLogger<CsvIngestService>.Instance);

        string csvPath = CreateTempCsv(
            "loclibrary.name,effective_location.code,effective_location.name,instances.title,items.barcode,items.effective_call_number,holdings.call_number",
            "William Allen White Library,stacks,Stacks,Test Title,12345,,HB 1");

        try
        {
            var result = await service.IngestAsync(csvPath);

            result.ParseFailuresCount.Should().Be(0);
            result.Records.Should().HaveCount(1);

            var record = result.Records[0];
            record.EffectiveCallNumber.Value.Should().BeEmpty();
            record.HoldingsCallNumber.Value.Should().Be("HB 1");
            record.ResolvedCallNumber.Value.Should().Be("HB 1");
            record.UsedHoldingsFallback.Should().BeTrue();
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [Fact]
    public async Task IngestAsync_WhenRequiredHeaderIsMissing_ThrowsFriendlyError()
    {
        var config = CreateValidConfig();
        var service = new CsvIngestService(config, NullLogger<CsvIngestService>.Instance);

        string csvPath = CreateTempCsv(
            "loclibrary.name,effective_location.code,effective_location.name,instances.title,items.barcode,holdings.call_number",
            "William Allen White Library,stacks,Stacks,Test Title,12345,HB 1");

        try
        {
            Func<Task> act = async () => await service.IngestAsync(csvPath);

            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("*missing required header*items.effective_call_number*");
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    private static HelConfig CreateValidConfig()
    {
        return new HelConfig
        {
            CsvColumns = new CsvColumns
            {
                LibraryName = "loclibrary.name",
                LocationCode = "effective_location.code",
                LocationName = "effective_location.name",
                Title = "instances.title",
                Barcode = "items.barcode",
                EffectiveCallNumber = "items.effective_call_number",
                HoldingsCallNumber = "holdings.call_number"
            },
            Output = new Output()
        };
    }

    private static string CreateTempCsv(string headerLine, string dataLine)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, headerLine + Environment.NewLine + dataLine + Environment.NewLine);
        return path;
    }
}
