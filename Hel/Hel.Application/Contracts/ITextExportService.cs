using Hel.Domain.Models;

namespace Hel.Application.Contracts;

/// <summary>
/// Writes recipient TXT files, optional unassigned output, and the run summary.
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
