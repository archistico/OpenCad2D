using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Svg;

/// <summary>
/// Result of an SVG export operation.
/// </summary>
public sealed class SvgExportResult
{
    public SvgExportResult(
        string content,
        int exportedEntityCount,
        BoundingBox2D? contentBounds,
        double width,
        double height)
    {
        Content = content;
        ExportedEntityCount = exportedEntityCount;
        ContentBounds = contentBounds;
        Width = width;
        Height = height;
    }

    public string Content { get; }

    public int ExportedEntityCount { get; }

    public BoundingBox2D? ContentBounds { get; }

    public double Width { get; }

    public double Height { get; }
}
