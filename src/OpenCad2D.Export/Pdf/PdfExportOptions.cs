namespace OpenCad2D.Export.Pdf;

/// <summary>
/// Options used when exporting a CAD document to PDF.
/// </summary>
public sealed class PdfExportOptions
{
    /// <summary>
    /// Gets the target page size. The default is A4.
    /// </summary>
    public PdfPageSize PageSize { get; init; } = PdfPageSize.A4;

    /// <summary>
    /// Gets the target page orientation. The default is portrait.
    /// </summary>
    public PdfPageOrientation Orientation { get; init; } = PdfPageOrientation.Portrait;

    /// <summary>
    /// Gets the page margin in millimeters.
    /// </summary>
    public double MarginMillimeters { get; init; } = 10.0;

    /// <summary>
    /// Gets whether entities on hidden layers should be exported.
    /// </summary>
    public bool IncludeHiddenLayers { get; init; } = false;

    /// <summary>
    /// Gets whether light screen colors should be converted to black for print readability.
    /// </summary>
    public bool UsePrintFriendlyColors { get; init; } = true;

    /// <summary>
    /// Gets the fallback drawing size used when the document contains no exportable entities.
    /// </summary>
    public double EmptyDocumentSizeMillimeters { get; init; } = 50.0;

    public static PdfExportOptions Default => new();
}
