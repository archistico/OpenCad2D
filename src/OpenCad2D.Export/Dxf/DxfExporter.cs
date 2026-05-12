using System.Text;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Dxf;

/// <summary>
/// Exports CAD documents to a minimal AutoCAD 2000 ASCII DXF structure.
/// </summary>
public sealed class DxfExporter : IDxfExporter
{
    public DxfExportResult Export(
        CadDocument document,
        DxfExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        DxfExportOptions actualOptions = options ?? DxfExportOptions.Default;

        if (string.IsNullOrWhiteSpace(actualOptions.AcadVersion))
        {
            throw new ArgumentException(
                "DXF ACAD version cannot be empty.",
                nameof(options));
        }

        IReadOnlyList<CadEntity> entities = GetExportableEntities(
            document,
            actualOptions);

        var writer = new DxfDocumentWriter();

        WriteHeader(
            writer,
            actualOptions);

        WriteTables(
            writer,
            document);
        BoundingBox2D? contentBounds = GetContentBounds(entities);

        WriteEntities(
            writer,
            document,
            entities,
            contentBounds);
        writer.WriteEndOfFile();

        return new DxfExportResult(
            writer.ToString(),
            entities.Count,
            actualOptions.AcadVersion);
    }

    public void ExportToFile(
        CadDocument document,
        string filePath,
        DxfExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "DXF export file path cannot be empty.",
                nameof(filePath));
        }

        DxfExportResult result = Export(
            document,
            options);

        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            filePath,
            result.Content,
            Encoding.ASCII);
    }

    private static IReadOnlyList<CadEntity> GetExportableEntities(
        CadDocument document,
        DxfExportOptions options)
    {
        IEnumerable<CadEntity> entities = options.IncludeHiddenLayers
            ? document.Entities.All.Where(entity => entity.IsVisible)
            : document.GetVisibleEntities();

        return entities
            .OrderBy(entity => document.Layers.GetRequired(entity.LayerId).Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entity => entity.DrawOrder)
            .ToList();
    }

    private static void WriteHeader(
        DxfDocumentWriter writer,
        DxfExportOptions options)
    {
        writer.BeginSection("HEADER");
        writer.WriteGroup(9, "$ACADVER");
        writer.WriteGroup(1, options.AcadVersion);
        writer.EndSection();
    }

    private static void WriteTables(
        DxfDocumentWriter writer,
        CadDocument document)
    {
        writer.BeginSection("TABLES");
        WriteLineTypeTable(writer);
        WriteLayerTable(
            writer,
            document);
        writer.EndSection();
    }

    private static void WriteLineTypeTable(DxfDocumentWriter writer)
    {
        writer.WriteGroup(0, "TABLE");
        writer.WriteGroup(2, "LTYPE");
        writer.WriteGroup(70, DxfLineTypeMapper.AllExportedStyles.Count);

        foreach (LineStyle style in DxfLineTypeMapper.AllExportedStyles)
        {
            WriteLineTypeRecord(
                writer,
                style);
        }

        writer.WriteGroup(0, "ENDTAB");
    }

    private static void WriteLineTypeRecord(
        DxfDocumentWriter writer,
        LineStyle style)
    {
        string name = DxfLineTypeMapper.ToDxfName(style);
        IReadOnlyList<double> pattern = DxfLineTypeMapper.GetPattern(style);

        writer.WriteGroup(0, "LTYPE");
        writer.WriteGroup(2, name);
        writer.WriteGroup(70, 0);
        writer.WriteGroup(3, GetLineTypeDescription(style));
        writer.WriteGroup(72, 65);
        writer.WriteGroup(73, pattern.Count);
        writer.WriteGroup(40, pattern.Sum(value => Math.Abs(value)));

        foreach (double value in pattern)
        {
            writer.WriteGroup(49, value);
            writer.WriteGroup(74, 0);
        }
    }

    private static string GetLineTypeDescription(LineStyle style)
    {
        return style switch
        {
            LineStyle.Continuous => "Solid line",
            LineStyle.Dashed => "Dashed line",
            LineStyle.DashDot => "Dash dot line",
            LineStyle.DashDotDot => "Dash dot dot line",
            _ => "Solid line",
        };
    }

    private static void WriteLayerTable(
        DxfDocumentWriter writer,
        CadDocument document)
    {
        IReadOnlyList<Layer> layers = document.Layers.All
            .OrderBy(layer => layer.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        writer.WriteGroup(0, "TABLE");
        writer.WriteGroup(2, "LAYER");
        writer.WriteGroup(70, layers.Count);

        foreach (Layer layer in layers)
        {
            WriteLayerRecord(
                writer,
                document,
                layer);
        }

        writer.WriteGroup(0, "ENDTAB");
    }

    private static void WriteLayerRecord(
        DxfDocumentWriter writer,
        CadDocument document,
        Layer layer)
    {
        LineFormat format = ResolveLineFormat(
            document,
            layer);

        int aciColor = DxfColorMapper.ToAci(format.Color);

        if (!layer.IsVisible && aciColor > 0)
        {
            aciColor = -aciColor;
        }

        writer.WriteGroup(0, "LAYER");
        writer.WriteGroup(2, layer.Name);
        writer.WriteGroup(70, layer.IsLocked ? 4 : 0);
        writer.WriteGroup(62, aciColor);
        writer.WriteGroup(420, DxfColorMapper.ToTrueColor(format.Color));
        writer.WriteGroup(6, DxfLineTypeMapper.ToDxfName(format.LineStyle));
        writer.WriteGroup(370, DxfLineWeightMapper.ToDxfLineWeight(format.LineWeight));
    }

    private static void WriteEntities(
        DxfDocumentWriter writer,
        CadDocument document,
        IEnumerable<CadEntity> entities,
        BoundingBox2D? contentBounds)
    {
        writer.BeginSection("ENTITIES");

        foreach (CadEntity entity in entities)
        {
            Layer layer = document.Layers.GetRequired(entity.LayerId);

            switch (entity)
            {
                case PointEntity point:
                    WritePoint(
                        writer,
                        layer.Name,
                        point,
                        contentBounds);
                    break;

                case TextEntity text:
                    WriteText(
                        writer,
                        document,
                        layer.Name,
                        text,
                        contentBounds);
                    break;

                case LineEntity line:
                    WriteLine(
                        writer,
                        layer.Name,
                        line,
                        contentBounds);
                    break;

                case CircleEntity circle:
                    WriteCircle(
                        writer,
                        layer.Name,
                        circle,
                        contentBounds);
                    break;

                case ArcEntity arc:
                    WriteArc(
                        writer,
                        layer.Name,
                        arc,
                        contentBounds);
                    break;

                case PolylineEntity polyline:
                    WritePolyline(
                        writer,
                        layer.Name,
                        polyline,
                        contentBounds);
                    break;
            }
        }

        writer.EndSection();
    }

    private static void WriteText(
        DxfDocumentWriter writer,
        CadDocument document,
        string layerName,
        TextEntity text,
        BoundingBox2D? contentBounds)
    {
        Point2D position = ToDxfPoint(
            text.InsertionPoint,
            contentBounds);

        TextFormat textFormat = ResolveTextFormat(
            document,
            text);

        writer.WriteGroup(0, "TEXT");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, position.X);
        writer.WriteGroup(20, position.Y);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(40, textFormat.Height);
        writer.WriteGroup(1, text.Text);
        writer.WriteGroup(50, NormalizeDegrees(-text.RotationDegrees));
        writer.WriteGroup(7, textFormat.Name);
    }

    private static void WritePoint(
        DxfDocumentWriter writer,
        string layerName,
        PointEntity point,
        BoundingBox2D? contentBounds)
    {
        Point2D position = ToDxfPoint(
            point.Position,
            contentBounds);

        writer.WriteGroup(0, "POINT");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, position.X);
        writer.WriteGroup(20, position.Y);
        writer.WriteGroup(30, 0.0);
    }

    private static void WriteLine(
        DxfDocumentWriter writer,
        string layerName,
        LineEntity line,
        BoundingBox2D? contentBounds)
    {
        Point2D start = ToDxfPoint(
            line.Start,
            contentBounds);

        Point2D end = ToDxfPoint(
            line.End,
            contentBounds);

        writer.WriteGroup(0, "LINE");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, start.X);
        writer.WriteGroup(20, start.Y);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(11, end.X);
        writer.WriteGroup(21, end.Y);
        writer.WriteGroup(31, 0.0);
    }

    private static void WriteCircle(
        DxfDocumentWriter writer,
        string layerName,
        CircleEntity circle,
        BoundingBox2D? contentBounds)
    {
        Point2D center = ToDxfPoint(
            circle.Center,
            contentBounds);

        writer.WriteGroup(0, "CIRCLE");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, center.X);
        writer.WriteGroup(20, center.Y);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(40, circle.Radius);
    }

    private static void WriteArc(
        DxfDocumentWriter writer,
        string layerName,
        ArcEntity arc,
        BoundingBox2D? contentBounds)
    {
        Point2D center = ToDxfPoint(
            arc.Center,
            contentBounds);

        writer.WriteGroup(0, "ARC");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, center.X);
        writer.WriteGroup(20, center.Y);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(40, arc.Radius);

        double startDegrees = ToDxfAngleDegrees(arc.StartAngle);
        double endDegrees = ToDxfAngleDegrees(arc.EndAngle);

        bool dxfArcIsCounterClockwise = !arc.IsCounterClockwise;

        if (!dxfArcIsCounterClockwise)
        {
            (startDegrees, endDegrees) = (endDegrees, startDegrees);
        }

        writer.WriteGroup(50, startDegrees);
        writer.WriteGroup(51, endDegrees);
    }

    private static void WritePolyline(
        DxfDocumentWriter writer,
        string layerName,
        PolylineEntity polyline,
        BoundingBox2D? contentBounds)
    {
        writer.WriteGroup(0, "LWPOLYLINE");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(90, polyline.Vertices.Count);
        writer.WriteGroup(70, polyline.IsClosed ? 1 : 0);

        foreach (Point2D vertex in polyline.Vertices)
        {
            Point2D dxfVertex = ToDxfPoint(
                vertex,
                contentBounds);

            writer.WriteGroup(10, dxfVertex.X);
            writer.WriteGroup(20, dxfVertex.Y);
        }
    }


    private static BoundingBox2D? GetContentBounds(
        IReadOnlyList<CadEntity> entities)
    {
        if (entities.Count == 0)
        {
            return null;
        }

        BoundingBox2D first = entities[0].GetBoundingBox();

        double minX = first.MinX;
        double minY = first.MinY;
        double maxX = first.MaxX;
        double maxY = first.MaxY;

        foreach (CadEntity entity in entities.Skip(1))
        {
            BoundingBox2D bounds = entity.GetBoundingBox();

            minX = Math.Min(minX, bounds.MinX);
            minY = Math.Min(minY, bounds.MinY);
            maxX = Math.Max(maxX, bounds.MaxX);
            maxY = Math.Max(maxY, bounds.MaxY);
        }

        return new BoundingBox2D(
            minX,
            minY,
            maxX,
            maxY);
    }

    private static Point2D ToDxfPoint(
        Point2D point,
        BoundingBox2D? contentBounds)
    {
        if (contentBounds is null)
        {
            return point;
        }

        BoundingBox2D bounds = contentBounds.Value;

        return new Point2D(
            point.X,
            bounds.MinY + bounds.MaxY - point.Y);
    }

    private static double ToDxfAngleDegrees(Angle angle)
    {
        return NormalizeDegrees(-angle.Degrees);
    }

    private static void WriteEntityByLayerProperties(
        DxfDocumentWriter writer,
        string layerName)
    {
        writer.WriteGroup(8, layerName);
        writer.WriteGroup(62, 256);
        writer.WriteGroup(6, "BYLAYER");
        writer.WriteGroup(370, -1);
    }

    private static TextFormat ResolveTextFormat(
        CadDocument document,
        TextEntity text)
    {
        if (document.TextFormats.TryGetById(
                text.TextFormatId,
                out TextFormat? format) &&
            format is not null)
        {
            return format;
        }

        return document.TextFormats.GetById(TextFormatId.Standard);
    }

    private static LineFormat ResolveLineFormat(
        CadDocument document,
        Layer layer)
    {
        if (!document.LineFormats.TryGetById(
                layer.LineFormatId,
                out LineFormat? format) ||
            format is null)
        {
            return document.LineFormats.GetById(LineFormatId.Continuous);
        }

        return format;
    }

    private static double NormalizeDegrees(double degrees)
    {
        double normalized = degrees % 360.0;

        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }
}
