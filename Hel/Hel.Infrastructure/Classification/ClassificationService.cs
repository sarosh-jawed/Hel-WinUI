using Hel.Application.Configuration;
using Hel.Application.Contracts;
using Hel.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Hel.Infrastructure.Classification;

/// <summary>
/// Deterministic, config-driven routing engine.
/// Order:
/// 1) location rules
/// 2) call-number rules
/// 3) unassigned fallback
/// </summary>
public sealed class ClassificationService : IClassificationService
{
    private readonly HelConfig _config;
    private readonly ILogger<ClassificationService> _logger;

    public ClassificationService(HelConfig config, ILogger<ClassificationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<ClassificationResult> ClassifyAsync(
        IReadOnlyList<ItemRecord> records,
        CancellationToken ct = default)
    {
        if (records is null)
            throw new ArgumentNullException(nameof(records));

        var classified = new List<ClassifiedItem>();
        var unassigned = new List<UnassignedItem>();

        int fallbackUsageCount = 0;
        int parseFailuresCount = 0;

        foreach (var record in records)
        {
            ct.ThrowIfCancellationRequested();

            if (record.UsedHoldingsFallback)
                fallbackUsageCount++;

            if (TryMatchLocationRule(record, out string locationBucketKey, out string locationReason))
            {
                classified.Add(new ClassifiedItem(record, locationBucketKey, locationReason));
                continue;
            }

            bool parseFailed;
            string unmatchedReason;
            if (TryMatchCallNumberRule(record, out string callNumberBucketKey, out string callNumberReason, out parseFailed, out unmatchedReason))
            {
                classified.Add(new ClassifiedItem(record, callNumberBucketKey, callNumberReason));
                continue;
            }

            if (parseFailed)
                parseFailuresCount++;

            unassigned.Add(new UnassignedItem(record, unmatchedReason));
        }

        _logger.LogInformation(
            "Classification completed. Classified={ClassifiedCount}, Unassigned={UnassignedCount}, ParseFailures={ParseFailuresCount}, FallbackUsage={FallbackUsageCount}",
            classified.Count,
            unassigned.Count,
            parseFailuresCount,
            fallbackUsageCount);

        var result = new ClassificationResult(
            Classified: classified,
            Unassigned: unassigned,
            FallbackUsageCount: fallbackUsageCount,
            ParseFailuresCount: parseFailuresCount);

        return Task.FromResult(result);
    }

    private bool TryMatchLocationRule(ItemRecord record, out string bucketKey, out string routingReason)
    {
        bucketKey = string.Empty;
        routingReason = string.Empty;

        foreach (var rule in _config.LocationRules)
        {
            if (!LibraryMatches(rule.LibraryName, record.LibraryName.Value))
                continue;

            bool codeMatch = rule.LocationCodes.Any(code =>
                string.Equals(code?.Trim(), record.LocationCode.Value.Trim(), StringComparison.OrdinalIgnoreCase));

            bool nameMatch = rule.LocationNames.Any(name =>
                string.Equals(name?.Trim(), record.LocationName?.Value?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (codeMatch || nameMatch)
            {
                bucketKey = rule.RecipientKey;
                routingReason = $"Location rule: {rule.Key}";
                return true;
            }
        }

        return false;
    }

    private bool TryMatchCallNumberRule(
        ItemRecord record,
        out string bucketKey,
        out string routingReason,
        out bool parseFailed,
        out string unmatchedReason)
    {
        bucketKey = string.Empty;
        routingReason = string.Empty;
        unmatchedReason = "Readable call number but no rule matched";
        parseFailed = false;

        bool encounteredDeweyRule = false;
        bool deweyParsed = false;
        decimal deweyValue = 0;

        foreach (var rule in _config.CallNumberRules)
        {
            if (!LibraryMatches(rule.LibraryName, record.LibraryName.Value))
                continue;

            if (string.Equals(rule.MatchMode, "StartsWith", StringComparison.OrdinalIgnoreCase))
            {
                string normalized = CallNumberParser.NormalizeForPrefixMatch(record.ResolvedCallNumber.Value);

                if (!string.IsNullOrWhiteSpace(rule.Prefix) &&
                    normalized.StartsWith(rule.Prefix.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    bucketKey = rule.RecipientKey;
                    routingReason = $"Call number prefix rule: {rule.Key}";
                    return true;
                }

                continue;
            }

            if (string.Equals(rule.MatchMode, "DeweyRange", StringComparison.OrdinalIgnoreCase))
            {
                encounteredDeweyRule = true;

                if (!deweyParsed)
                {
                    deweyParsed = CallNumberParser.TryExtractDeweyNumber(
                        record.ResolvedCallNumber.Value,
                        _config.CallNumberNormalization.StripPrefixes,
                        out deweyValue);
                }

                if (!deweyParsed)
                {
                    parseFailed = true;
                    unmatchedReason = "Unreadable call number after normalization";
                    return false;
                }

                if (rule.RangeStart.HasValue &&
                    rule.RangeEnd.HasValue &&
                    deweyValue >= rule.RangeStart.Value &&
                    deweyValue <= rule.RangeEnd.Value)
                {
                    bucketKey = rule.RecipientKey;
                    routingReason = $"Dewey range rule: {rule.Key} (parsed {deweyValue:0.###})";
                    return true;
                }
            }
        }

        if (encounteredDeweyRule && !deweyParsed)
        {
            parseFailed = true;
            unmatchedReason = "Unreadable call number after normalization";
        }
        else
        {
            unmatchedReason = "Readable call number but no rule matched";
        }

        return false;
    }

    private static bool LibraryMatches(string? ruleLibraryName, string actualLibraryName)
    {
        if (string.IsNullOrWhiteSpace(ruleLibraryName))
            return true;

        return string.Equals(
            ruleLibraryName.Trim(),
            actualLibraryName.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }
}
