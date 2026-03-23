using Hel.Application.Configuration;
using Hel.Application.Contracts;
using Hel.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Hel.Infrastructure.Export;

/// <summary>
/// Writes one TXT file per recipient, optional unassigned output, and a run summary file.
/// </summary>
public sealed class TextExportService : ITextExportService
{
    private readonly HelConfig _config;
    private readonly TextBodyBuilder _bodyBuilder;
    private readonly ILogger<TextExportService> _logger;

    public TextExportService(
        HelConfig config,
        TextBodyBuilder bodyBuilder,
        ILogger<TextExportService> logger)
    {
        _config = config;
        _bodyBuilder = bodyBuilder;
        _logger = logger;
    }

    public async Task ExportAsync(
        IReadOnlyList<ClassifiedItem> classified,
        IReadOnlyList<ItemRecord> unassigned,
        RunSummary summary,
        string outputFolder,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
            throw new ArgumentException("Output folder is required.", nameof(outputFolder));

        Directory.CreateDirectory(outputFolder);

        var recipientMap = _config.Recipients
            .GroupBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var group in classified.GroupBy(x => x.BucketKey, StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();

            string recipientKey = group.Key.Trim();
            string fileName = $"{SanitizeFileName(recipientKey)}.txt";
            string filePath = Path.Combine(outputFolder, fileName);

            string displayName = recipientMap.TryGetValue(recipientKey, out var recipient)
                ? recipient.DisplayName
                : recipientKey;

            string body = _bodyBuilder.BuildRecipientBody(displayName, group.ToList());

            await File.WriteAllTextAsync(
                filePath,
                body,
                new UTF8Encoding(false),
                ct);

            _logger.LogInformation(
                "Wrote recipient output file. RecipientKey={RecipientKey}, FilePath={FilePath}, Count={Count}",
                recipientKey,
                filePath,
                group.Count());
        }

        if (unassigned.Count > 0)
        {
            string unassignedPath = Path.Combine(outputFolder, _config.Output.UnassignedFileName);
            string unassignedBody = _bodyBuilder.BuildUnassignedBody(unassigned);

            await File.WriteAllTextAsync(
                unassignedPath,
                unassignedBody,
                new UTF8Encoding(false),
                ct);

            _logger.LogInformation(
                "Wrote unassigned output file. FilePath={FilePath}, Count={Count}",
                unassignedPath,
                unassigned.Count);
        }

        string summaryPath = Path.Combine(outputFolder, _config.Output.RunSummaryFileName);
        string summaryBody = _bodyBuilder.BuildRunSummaryBody(summary);

        await File.WriteAllTextAsync(
            summaryPath,
            summaryBody,
            new UTF8Encoding(false),
            ct);

        _logger.LogInformation(
            "Wrote run summary file. FilePath={FilePath}",
            summaryPath);
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value;
    }
}
