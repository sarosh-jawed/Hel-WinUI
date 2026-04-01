using Hel.Application.Configuration;
using Hel.Infrastructure.Configuration;
using Xunit;

namespace Hel.Tests.Configuration;

public class HelConfigValidatorTests
{
    [Fact]
    public void Validate_Should_Fail_When_RecipientKeys_Are_Duplicated()
    {
        var config = CreateValidConfig();
        config.Recipients.Add(new RecipientDefinition
        {
            Key = "wawl",
            DisplayName = "Duplicate WAWL"
        });

        var errors = HelConfigValidator.Validate(config);

        Assert.Contains(errors, e => e.Contains("duplicated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_Fail_When_Rule_References_Unknown_Recipient()
    {
        var config = CreateValidConfig();
        config.LocationRules[0].RecipientKey = "missing-key";

        var errors = HelConfigValidator.Validate(config);

        Assert.Contains(errors, e => e.Contains("unknown recipient key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_Fail_When_Required_Csv_Column_Is_Missing()
    {
        var config = CreateValidConfig();
        config.CsvColumns.Title = string.Empty;

        var errors = HelConfigValidator.Validate(config);

        Assert.Contains(errors, e => e.Contains("CsvColumns.Title", StringComparison.OrdinalIgnoreCase));
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
            LibraryRules = new List<LibraryRule>
            {
                new()
                {
                    LibraryName = "William Allen White Library",
                    RecipientKey = "wawl"
                }
            },
            LocationRules = new List<LocationRule>
            {
                new()
                {
                    Key = "wawl-stacks",
                    LibraryName = "William Allen White Library",
                    LocationCodes = new List<string> { "stacks" },
                    LocationNames = new List<string>(),
                    RecipientKey = "wawl"
                }
            },
            CallNumberRules = new List<CallNumberRule>
            {
                new()
                {
                    Key = "music-prefix",
                    LibraryName = "William Allen White Library",
                    MatchMode = "StartsWith",
                    Prefix = "M",
                    RecipientKey = "wawl"
                }
            },
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
                RecipientFileLineTemplate = "{Title}\t{Barcode}\t{ResolvedCallNumber}",
                UnassignedFileLineTemplate = "{Title}\t{Barcode}\t{ResolvedCallNumber}",
                SummaryHeader = "Hel Run Summary"
            }
        };
    }
}
