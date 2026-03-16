using Hel.Domain.Models;

namespace Hel.Application.Contracts;

/// <summary>
/// Writes recipient TXT files and run summary artifacts.
/// </summary>
public interface ITextExportService
{
    Task ExportAsync(
        IReadOnlyList<ClassifiedItem> classified,
        IReadOnlyList<ItemRecord> unassigned,
        RunSummary summary,
        string outputFolder,
        CancellationToken ct = default);
}
