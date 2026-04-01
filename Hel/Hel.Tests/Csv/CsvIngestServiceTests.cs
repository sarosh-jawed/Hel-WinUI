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

            Assert.Equal(0, result.ParseFailuresCount);
            Assert.Single(result.Records);

            var record = result.Records[0];
            Assert.Equal("QA 76.73", record.EffectiveCallNumber.Value);
            Assert.Equal("HB 1", record.HoldingsCallNumber.Value);
            Assert.Equal("QA 76.73", record.ResolvedCallNumber.Value);
            Assert.False(record.UsedHoldingsFallback);
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

            Assert.Equal(0, result.ParseFailuresCount);
            Assert.Single(result.Records);

            var record = result.Records[0];
            Assert.Equal(string.Empty, record.EffectiveCallNumber.Value);
            Assert.Equal("HB 1", record.HoldingsCallNumber.Value);
            Assert.Equal("HB 1", record.ResolvedCallNumber.Value);
            Assert.True(record.UsedHoldingsFallback);
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
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.IngestAsync(csvPath));
            Assert.Contains("missing required header", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("items.effective_call_number", ex.Message, StringComparison.OrdinalIgnoreCase);
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
