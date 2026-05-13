using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Pdf;

/// <summary>
/// Result of a PDF export operation.
/// </summary>
public sealed class PdfExportResult
{
    public PdfExportResult(
        byte[] content,
        int exportedEntityCount,
        BoundingBox2D? contentBounds,
        double pageWidthPoints,
        double pageHeightPoints,
        double scale)
    {
        Content = content;
        ExportedEntityCount = exportedEntityCount;
        ContentBounds = contentBounds;
        PageWidthPoints = pageWidthPoints;
        PageHeightPoints = pageHeightPoints;
        Scale = scale;
    }

    /// <summary>
    /// Gets the complete PDF bytes.
    /// </summary>
    public byte[] Content { get; }

    public int ExportedEntityCount { get; }

    public BoundingBox2D? ContentBounds { get; }

    public double PageWidthPoints { get; }

    public double PageHeightPoints { get; }

    /// <summary>
    /// Gets the model-unit to PDF-point scale used for the export.
    /// </summary>
    public double Scale { get; }
}
