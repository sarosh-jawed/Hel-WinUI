namespace Hel.App.WinUI.Models;

/// <summary>
/// One preview row displayed in the bucket DataGrid.
/// </summary>
public sealed class PreviewRowViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Barcode { get; init; } = string.Empty;

    public string CallNumber { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public string RoutingReason { get; init; } = string.Empty;
}
