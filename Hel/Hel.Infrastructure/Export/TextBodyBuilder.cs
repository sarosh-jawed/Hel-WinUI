using Hel.Application.Configuration;
using Hel.Domain.Models;
using System.Text;

namespace Hel.Infrastructure.Export;

/// <summary>
/// Builds text bodies for recipient files, unassigned output, and summary output.
/// The actual wording and line shape come from config.
/// </summary>
public sealed class TextBodyBuilder
{
    private readonly HelConfig _config;

    public TextBodyBuilder(HelConfig config)
    {
        _config = config;
    }

    public string BuildRecipientBody(
        string recipientDisplayName,
        IReadOnlyList<ClassifiedItem> items)
    {
        var sb = new StringBuilder();

        AppendStandardIntro(sb, items.Count);

        foreach (var item in items)
        {
            string line = ApplyItemTemplate(
                _config.TextTemplate.RecipientFileLineTemplate,
                item.Record.Title.Value,
                item.Record.Barcode.Value,
                item.Record.ResolvedCallNumber.Value);

            sb.AppendLine($"{_config.TextTemplate.BulletPrefix} {line}");
        }

        AppendStandardClosing(sb);
        return sb.ToString();
    }

    public string BuildUnassignedBody(IReadOnlyList<ItemRecord> items)
    {
        var sb = new StringBuilder();

        AppendStandardIntro(sb, items.Count);

        foreach (var item in items)
        {
            string line = ApplyItemTemplate(
                _config.TextTemplate.UnassignedFileLineTemplate,
                item.Title.Value,
                item.Barcode.Value,
                item.ResolvedCallNumber.Value);

            sb.AppendLine($"{_config.TextTemplate.BulletPrefix} {line}");
        }

        AppendStandardClosing(sb);
        return sb.ToString();
    }

    public string BuildRunSummaryBody(RunSummary summary)
    {
        var sb = new StringBuilder();

        sb.AppendLine(_config.TextTemplate.SummaryHeader);
        sb.AppendLine();

        sb.AppendLine($"CSV file name: {summary.CsvFileName}");
        sb.AppendLine($"Total rows loaded: {summary.TotalRowsLoaded}");
        sb.AppendLine($"Rows after WAWL filter: {summary.RowsAfterWawlFilter}");
        sb.AppendLine($"Rows after location filter: {summary.RowsAfterLocationFilter}");
        sb.AppendLine($"Assigned count: {summary.AssignedCount}");
        sb.AppendLine($"Unassigned count: {summary.UnassignedCount}");
        sb.AppendLine($"Fallback count: {summary.FallbackUsageCount}");
        sb.AppendLine($"Parse failures count: {summary.ParseFailuresCount}");
        sb.AppendLine();

        sb.AppendLine("Counts per bucket:");

        if (summary.CountsPerBucket.Count == 0)
        {
            sb.AppendLine("  none");
        }
        else
        {
            foreach (var pair in summary.CountsPerBucket.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  {pair.Key}: {pair.Value}");
            }
        }

        return sb.ToString();
    }

    private void AppendStandardIntro(StringBuilder sb, int count)
    {
        sb.AppendLine(_config.TextTemplate.GreetingLine);
        sb.AppendLine();
        sb.AppendLine(_config.TextTemplate.CountLineTemplate.Replace("{Count}", count.ToString()));
        sb.AppendLine(_config.TextTemplate.HeaderLine);
    }

    private void AppendStandardClosing(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine(_config.TextTemplate.ClosingLine);
        sb.AppendLine(_config.TextTemplate.SignatureLine);
    }

    private static string ApplyItemTemplate(
        string template,
        string title,
        string barcode,
        string callNumber)
    {
        return template
            .Replace("{Title}", title ?? string.Empty)
            .Replace("{Barcode}", barcode ?? string.Empty)
            .Replace("{CallNumber}", callNumber ?? string.Empty);
    }
}
