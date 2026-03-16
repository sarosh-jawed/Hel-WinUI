using Hel.Application.Configuration;

namespace Hel.Infrastructure.Configuration;

/// <summary>
/// Validates Hel configuration before the app starts running.
/// We fail fast so bad config is caught immediately instead of during processing.
/// </summary>
public static class HelConfigValidator
{
    public static List<string> Validate(HelConfig config)
    {
        var errors = new List<string>();

        ValidateCsvColumns(config, errors);
        ValidateRecipients(config, errors);
        ValidateRuleRecipientReferences(config, errors);
        ValidateCallNumberRules(config, errors);
        ValidateOutput(config, errors);
        ValidateTextTemplate(config, errors);

        return errors;
    }

    private static void ValidateCsvColumns(HelConfig config, List<string> errors)
    {
        if (config.CsvColumns is null)
        {
            errors.Add("CsvColumns section is required.");
            return;
        }

        var requiredColumns = new Dictionary<string, string?>
        {
            ["CsvColumns.LibraryName"] = config.CsvColumns.LibraryName,
            ["CsvColumns.LocationCode"] = config.CsvColumns.LocationCode,
            ["CsvColumns.LocationName"] = config.CsvColumns.LocationName,
            ["CsvColumns.Title"] = config.CsvColumns.Title,
            ["CsvColumns.Barcode"] = config.CsvColumns.Barcode,
            ["CsvColumns.EffectiveCallNumber"] = config.CsvColumns.EffectiveCallNumber,
            ["CsvColumns.HoldingsCallNumber"] = config.CsvColumns.HoldingsCallNumber
        };

        foreach (var pair in requiredColumns)
        {
            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                errors.Add($"{pair.Key} is required.");
            }
        }
    }

    private static void ValidateRecipients(HelConfig config, List<string> errors)
    {
        if (config.Recipients is null || config.Recipients.Count == 0)
        {
            errors.Add("At least one recipient must be configured.");
            return;
        }

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < config.Recipients.Count; i++)
        {
            var recipient = config.Recipients[i];
            string label = $"Recipients[{i}]";

            if (string.IsNullOrWhiteSpace(recipient.Key))
                errors.Add($"{label}.Key is required.");

            if (string.IsNullOrWhiteSpace(recipient.DisplayName))
                errors.Add($"{label}.DisplayName is required.");

            if (!string.IsNullOrWhiteSpace(recipient.Key) && !seenKeys.Add(recipient.Key))
                errors.Add($"Recipient key '{recipient.Key}' is duplicated. Recipient keys must be unique.");
        }
    }

    private static void ValidateRuleRecipientReferences(HelConfig config, List<string> errors)
    {
        var validRecipientKeys = new HashSet<string>(
            config.Recipients.Select(r => r.Key),
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < config.LibraryRules.Count; i++)
        {
            var rule = config.LibraryRules[i];

            if (string.IsNullOrWhiteSpace(rule.LibraryName))
                errors.Add($"LibraryRules[{i}].LibraryName is required.");

            ValidateRecipientReference(rule.RecipientKey, validRecipientKeys, $"LibraryRules[{i}].RecipientKey", errors);
        }

        for (int i = 0; i < config.LocationRules.Count; i++)
        {
            var rule = config.LocationRules[i];

            if (string.IsNullOrWhiteSpace(rule.Key))
                errors.Add($"LocationRules[{i}].Key is required.");

            bool hasCodes = rule.LocationCodes is { Count: > 0 };
            bool hasNames = rule.LocationNames is { Count: > 0 };

            if (!hasCodes && !hasNames)
                errors.Add($"LocationRules[{i}] must define at least one LocationCode or LocationName.");

            ValidateRecipientReference(rule.RecipientKey, validRecipientKeys, $"LocationRules[{i}].RecipientKey", errors);
        }

        for (int i = 0; i < config.CallNumberRules.Count; i++)
        {
            var rule = config.CallNumberRules[i];

            if (string.IsNullOrWhiteSpace(rule.Key))
                errors.Add($"CallNumberRules[{i}].Key is required.");

            ValidateRecipientReference(rule.RecipientKey, validRecipientKeys, $"CallNumberRules[{i}].RecipientKey", errors);
        }
    }

    private static void ValidateCallNumberRules(HelConfig config, List<string> errors)
    {
        for (int i = 0; i < config.CallNumberRules.Count; i++)
        {
            var rule = config.CallNumberRules[i];
            string matchMode = rule.MatchMode?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(matchMode))
            {
                errors.Add($"CallNumberRules[{i}].MatchMode is required.");
                continue;
            }

            if (matchMode.Equals("StartsWith", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(rule.Prefix))
                    errors.Add($"CallNumberRules[{i}].Prefix is required when MatchMode is StartsWith.");
            }
            else if (matchMode.Equals("DeweyRange", StringComparison.OrdinalIgnoreCase))
            {
                if (rule.RangeStart is null || rule.RangeEnd is null)
                {
                    errors.Add($"CallNumberRules[{i}] requires RangeStart and RangeEnd when MatchMode is DeweyRange.");
                }
                else if (rule.RangeStart > rule.RangeEnd)
                {
                    errors.Add($"CallNumberRules[{i}] has RangeStart greater than RangeEnd.");
                }
            }
            else
            {
                errors.Add($"CallNumberRules[{i}].MatchMode '{rule.MatchMode}' is not supported. Use StartsWith or DeweyRange.");
            }
        }
    }

    private static void ValidateOutput(HelConfig config, List<string> errors)
    {
        if (config.Output is null)
        {
            errors.Add("Output section is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.Output.Root))
            errors.Add("Output.Root is required.");

        if (string.IsNullOrWhiteSpace(config.Output.LogsRoot))
            errors.Add("Output.LogsRoot is required.");

        if (string.IsNullOrWhiteSpace(config.Output.MonthFolderFormat))
            errors.Add("Output.MonthFolderFormat is required.");

        if (string.IsNullOrWhiteSpace(config.Output.UnassignedFileName))
            errors.Add("Output.UnassignedFileName is required.");

        if (string.IsNullOrWhiteSpace(config.Output.RunSummaryFileName))
            errors.Add("Output.RunSummaryFileName is required.");
    }

    private static void ValidateTextTemplate(HelConfig config, List<string> errors)
    {
        if (config.TextTemplate is null)
        {
            errors.Add("TextTemplate section is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.TextTemplate.RecipientFileLineTemplate))
            errors.Add("TextTemplate.RecipientFileLineTemplate is required.");

        if (string.IsNullOrWhiteSpace(config.TextTemplate.UnassignedFileLineTemplate))
            errors.Add("TextTemplate.UnassignedFileLineTemplate is required.");

        if (string.IsNullOrWhiteSpace(config.TextTemplate.SummaryHeader))
            errors.Add("TextTemplate.SummaryHeader is required.");
    }

    private static void ValidateRecipientReference(
        string? recipientKey,
        HashSet<string> validRecipientKeys,
        string fieldName,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(recipientKey))
        {
            errors.Add($"{fieldName} is required.");
            return;
        }

        if (!validRecipientKeys.Contains(recipientKey))
        {
            errors.Add($"{fieldName} references unknown recipient key '{recipientKey}'.");
        }
    }
}
