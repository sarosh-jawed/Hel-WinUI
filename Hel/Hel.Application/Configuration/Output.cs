namespace Hel.Application.Configuration;

/// <summary>
/// Output and logging paths plus output file naming.
/// </summary>
public sealed class Output
{
    public string Root { get; set; } = "%LOCALAPPDATA%\\Hel\\Output";
    public string LogsRoot { get; set; } = "%LOCALAPPDATA%\\Hel\\Logs";
    public string MonthFolderFormat { get; set; } = "yyyy-MM";
    public string UnassignedFileName { get; set; } = "Unassigned.txt";
    public string RunSummaryFileName { get; set; } = "RunSummary.txt";
}
