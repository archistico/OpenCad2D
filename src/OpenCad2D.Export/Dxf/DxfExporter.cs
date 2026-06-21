using System.Text;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Dimensions.Rendering;
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
        BoundingBox2D? contentBounds = actualOptions.UseCadViewerCoordinateSystem
            ? GetContentBounds(
                document,
                entities)
            : null;

        WriteEntities(
            writer,
            document,
            entities,
            contentBounds,
            actualOptions);
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
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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
        BoundingBox2D? contentBounds,
        DxfExportOptions options)
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
                        contentBounds,
                        options);
                    break;

                case MultilineTextEntity multilineText:
                    WriteMultilineText(
                        writer,
                        document,
                        layer.Name,
                        multilineText,
                        contentBounds,
                        options);
                    break;

                case LinearDimensionEntity linearDimension:
                    WriteDimension(
                        writer,
                        document,
                        layer.Name,
                        linearDimension,
                        contentBounds,
                        options);
                    break;

                case AlignedDimensionEntity alignedDimension:
                    WriteDimension(
                        writer,
                        document,
                        layer.Name,
                        alignedDimension,
                        contentBounds,
                        options);
                    break;

                case RadiusDimensionEntity radiusDimension:
                    WriteDimension(
                        writer,
                        document,
                        layer.Name,
                        radiusDimension,
                        contentBounds,
                        options);
                    break;

                case DiameterDimensionEntity diameterDimension:
                    WriteDimension(
                        writer,
                        document,
                        layer.Name,
                        diameterDimension,
                        contentBounds,
                        options);
                    break;

                case AngularDimensionEntity angularDimension:
                    WriteDimension(
                        writer,
                        document,
                        layer.Name,
                        angularDimension,
                        contentBounds,
                        options);
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
                    WriteCircleHatchIfFilled(
                        writer,
                        layer.Name,
                        layer.FillColor,
                        circle,
                        contentBounds);
                    break;

                case EllipseEntity ellipse:
                    WriteEllipse(
                        writer,
                        layer.Name,
                        ellipse,
                        contentBounds);
                    break;

                case EllipticalArcEntity ellipticalArc:
                    WriteEllipticalArc(
                        writer,
                        layer.Name,
                        ellipticalArc,
                        contentBounds,
                        options);
                    break;

                case ArcEntity arc:
                    WriteArc(
                        writer,
                        layer.Name,
                        arc,
                        contentBounds,
                        options);
                    break;

                case PolylineEntity polyline:
                    WritePolyline(
                        writer,
                        layer.Name,
                        polyline,
                        contentBounds);
                    WritePolylineHatchIfFilled(
                        writer,
                        layer.Name,
                        layer.FillColor,
                        polyline,
                        contentBounds);
                    break;

                case BezierSplineEntity spline:
                    WriteBezierSpline(
                        writer,
                        layer.Name,
                        spline,
                        contentBounds);
                    break;

                case StairEntity stair:
                    WriteStair(
                        writer,
                        layer.Name,
                        stair,
                        contentBounds);
                    break;

                case DoorEntity door:
                    WriteDoor(
                        writer,
                        layer.Name,
                        door,
                        contentBounds);
                    break;

                case WindowEntity window:
                    WriteWindow(
                        writer,
                        layer.Name,
                        window,
                        contentBounds);
                    break;
            }
        }

        writer.EndSection();
    }


    private static void WriteStair(
        DxfDocumentWriter writer,
        string layerName,
        StairEntity stair,
        BoundingBox2D? contentBounds)
    {
        foreach (LineSegment2D segment in stair.GetGeneratedGeometry().Segments)
        {
            WriteLineSegment(
                writer,
                layerName,
                segment,
                contentBounds);
        }
    }

    private static void WriteDoor(
        DxfDocumentWriter writer,
        string layerName,
        DoorEntity door,
        BoundingBox2D? contentBounds)
    {
        var geometry = door.GetGeneratedGeometry();

        if (geometry.HasWallMask)
        {
            WritePolygonHatch(
                writer,
                layerName,
                CadColor.FromRgb(255, 255, 255),
                geometry.WallMaskPolygon,
                contentBounds);
        }

        foreach (LineSegment2D segment in geometry.Segments)
        {
            WriteLineSegment(
                writer,
                layerName,
                segment,
                contentBounds);
        }
    }

    private static void WriteWindow(
        DxfDocumentWriter writer,
        string layerName,
        WindowEntity window,
        BoundingBox2D? contentBounds)
    {
        var geometry = window.GetGeneratedGeometry();

        if (geometry.HasWallMask)
        {
            WritePolygonHatch(
                writer,
                layerName,
                CadColor.FromRgb(255, 255, 255),
                geometry.WallMaskPolygon,
                contentBounds);
        }

        foreach (LineSegment2D segment in geometry.Segments)
        {
            WriteLineSegment(
                writer,
                layerName,
                segment,
                contentBounds);
        }
    }

    private static void WriteLineSegment(
        DxfDocumentWriter writer,
        string layerName,
        LineSegment2D segment,
        BoundingBox2D? contentBounds)
    {
        Point2D start = ToDxfPoint(
            segment.Start,
            contentBounds);
        Point2D end = ToDxfPoint(
            segment.End,
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

    private static void WriteDimension(
        DxfDocumentWriter writer,
        CadDocument document,
        string layerName,
        DimensionEntity dimension,
        BoundingBox2D? contentBounds,
        DxfExportOptions options)
    {
        DimensionStyle style = ResolveDimensionStyle(
            document,
            dimension);
        TextFormat textFormat = ResolveDimensionTextFormat(
            document,
            style);
        DimensionRenderModel model = new DimensionGeometryBuilder().Build(
            dimension,
            style);

        foreach (DimensionLinePrimitive line in model.Lines.Concat(model.Arrows))
        {
            WriteLinePrimitive(
                writer,
                layerName,
                line,
                contentBounds);
        }

        foreach (DimensionArcPrimitive arc in model.Arcs)
        {
            WriteArcPrimitive(
                writer,
                layerName,
                arc,
                contentBounds,
                options);
        }

        Point2D textPosition = ToDxfPoint(
            model.Text.Position,
            contentBounds);

        writer.WriteGroup(0, "TEXT");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, textPosition.X);
        writer.WriteGroup(20, textPosition.Y);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(11, textPosition.X);
        writer.WriteGroup(21, textPosition.Y);
        writer.WriteGroup(31, 0.0);
        writer.WriteGroup(40, textFormat.Height);
        writer.WriteGroup(1, model.Text.Text);
        writer.WriteGroup(50, ToDxfRotationDegrees(
            model.Text.RotationDegrees,
            options));
        writer.WriteGroup(72, 1);
        writer.WriteGroup(73, 2);
        writer.WriteGroup(7, textFormat.Name);
    }


    private static void WriteArcPrimitive(
        DxfDocumentWriter writer,
        string layerName,
        DimensionArcPrimitive arc,
        BoundingBox2D? contentBounds,
        DxfExportOptions options)
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

        double startDegrees = ToDxfRotationDegrees(
            arc.StartAngleDegrees,
            options);
        double endDegrees = ToDxfRotationDegrees(
            arc.EndAngleDegrees,
            options);
        bool dxfArcIsCounterClockwise = ToDxfCounterClockwise(
            arc.IsCounterClockwise,
            options);

        if (!dxfArcIsCounterClockwise)
        {
            (startDegrees, endDegrees) = (endDegrees, startDegrees);
        }

        writer.WriteGroup(50, startDegrees);
        writer.WriteGroup(51, endDegrees);
    }

    private static void WriteLinePrimitive(
        DxfDocumentWriter writer,
        string layerName,
        DimensionLinePrimitive line,
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

    private static void WriteText(
        DxfDocumentWriter writer,
        CadDocument document,
        string layerName,
        TextEntity text,
        BoundingBox2D? contentBounds,
        DxfExportOptions options)
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
        writer.WriteGroup(50, ToDxfRotationDegrees(
            text.RotationDegrees,
            options));
        writer.WriteGroup(7, textFormat.Name);
    }

    private static void WriteMultilineText(
        DxfDocumentWriter writer,
        CadDocument document,
        string layerName,
        MultilineTextEntity text,
        BoundingBox2D? contentBounds,
        DxfExportOptions options)
    {
        Point2D position = ToDxfPoint(
            text.InsertionPoint,
            contentBounds);

        TextFormat textFormat = ResolveTextFormat(
            document,
            text);

        writer.WriteGroup(0, "MTEXT");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, position.X);
        writer.WriteGroup(20, position.Y);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(40, textFormat.Height);
        writer.WriteGroup(41, text.ReferenceWidth);
        writer.WriteGroup(1, ToDxfMTextContent(text.Text));
        writer.WriteGroup(50, ToDxfRotationDegrees(
            text.RotationDegrees,
            options));
        writer.WriteGroup(7, textFormat.Name);
    }

    private static string ToDxfMTextContent(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", "\\P", StringComparison.Ordinal);
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

    private static void WriteCircleHatchIfFilled(
        DxfDocumentWriter writer,
        string layerName,
        CadColor fillColor,
        CircleEntity circle,
        BoundingBox2D? contentBounds)
    {
        if (!circle.IsFilled)
        {
            return;
        }

        Point2D center = ToDxfPoint(
            circle.Center,
            contentBounds);

        WriteHatchHeader(
            writer,
            layerName,
            fillColor);
        writer.WriteGroup(91, 1);
        writer.WriteGroup(92, 1);
        writer.WriteGroup(93, 1);
        writer.WriteGroup(72, 2);
        writer.WriteGroup(10, center.X);
        writer.WriteGroup(20, center.Y);
        writer.WriteGroup(40, circle.Radius);
        writer.WriteGroup(50, 0.0);
        writer.WriteGroup(51, 360.0);
        writer.WriteGroup(73, 1);
        WriteHatchFooter(writer);
    }

    private static void WriteEllipse(
        DxfDocumentWriter writer,
        string layerName,
        EllipseEntity ellipse,
        BoundingBox2D? contentBounds)
    {
        Point2D center = ToDxfPoint(
            ellipse.Center,
            contentBounds);
        Point2D majorEnd = ToDxfPoint(
            ellipse.MajorAxisEndPoint,
            contentBounds);
        Vector2D dxfMajorAxis = center.VectorTo(majorEnd);

        writer.WriteGroup(0, "ELLIPSE");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, center.X);
        writer.WriteGroup(20, center.Y);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(11, dxfMajorAxis.X);
        writer.WriteGroup(21, dxfMajorAxis.Y);
        writer.WriteGroup(31, 0.0);
        writer.WriteGroup(40, ellipse.MinorRadius / ellipse.MajorRadius);
        writer.WriteGroup(41, 0.0);
        writer.WriteGroup(42, Math.Tau);
    }

    private static void WriteEllipticalArc(
        DxfDocumentWriter writer,
        string layerName,
        EllipticalArcEntity ellipticalArc,
        BoundingBox2D? contentBounds,
        DxfExportOptions options)
    {
        Point2D center = ToDxfPoint(
            ellipticalArc.Center,
            contentBounds);
        Point2D majorEnd = ToDxfPoint(
            ellipticalArc.Center + ellipticalArc.MajorAxis,
            contentBounds);
        Vector2D dxfMajorAxis = center.VectorTo(majorEnd);

        double startParameter = NormalizeRadians(ellipticalArc.StartParameterRadians);
        double endParameter = NormalizeRadians(ellipticalArc.EndParameterRadians);
        bool dxfArcIsCounterClockwise = ToDxfCounterClockwise(
            ellipticalArc.IsCounterClockwise,
            options);

        if (!dxfArcIsCounterClockwise)
        {
            (startParameter, endParameter) = (endParameter, startParameter);
        }

        writer.WriteGroup(0, "ELLIPSE");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        writer.WriteGroup(10, center.X);
        writer.WriteGroup(20, center.Y);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(11, dxfMajorAxis.X);
        writer.WriteGroup(21, dxfMajorAxis.Y);
        writer.WriteGroup(31, 0.0);
        writer.WriteGroup(40, ellipticalArc.MinorRadius / ellipticalArc.MajorRadius);
        writer.WriteGroup(41, startParameter);
        writer.WriteGroup(42, endParameter);
    }

    private static void WriteArc(
        DxfDocumentWriter writer,
        string layerName,
        ArcEntity arc,
        BoundingBox2D? contentBounds,
        DxfExportOptions options)
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

        double startDegrees = ToDxfAngleDegrees(
            arc.StartAngle,
            options);
        double endDegrees = ToDxfAngleDegrees(
            arc.EndAngle,
            options);

        bool dxfArcIsCounterClockwise = ToDxfCounterClockwise(
            arc.IsCounterClockwise,
            options);

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

        for (int index = 0; index < polyline.Vertices.Count; index++)
        {
            Point2D dxfVertex = ToDxfPoint(
                polyline.Vertices[index],
                contentBounds);

            writer.WriteGroup(10, dxfVertex.X);
            writer.WriteGroup(20, dxfVertex.Y);

            if (index < polyline.SegmentBulges.Count && !OpenCad2D.Geometry.Tolerance.IsZero(polyline.SegmentBulges[index]))
            {
                writer.WriteGroup(42, polyline.SegmentBulges[index]);
            }
        }
    }


    private static void WritePolygonHatch(
        DxfDocumentWriter writer,
        string layerName,
        CadColor fillColor,
        IReadOnlyList<Point2D> vertices,
        BoundingBox2D? contentBounds)
    {
        if (vertices.Count < 3)
        {
            return;
        }

        WriteHatchHeader(
            writer,
            layerName,
            fillColor);
        writer.WriteGroup(91, 1);
        writer.WriteGroup(92, 7);
        writer.WriteGroup(72, 0);
        writer.WriteGroup(73, 1);
        writer.WriteGroup(93, vertices.Count);

        foreach (Point2D vertex in vertices)
        {
            Point2D dxfVertex = ToDxfPoint(
                vertex,
                contentBounds);

            writer.WriteGroup(10, dxfVertex.X);
            writer.WriteGroup(20, dxfVertex.Y);
        }

        WriteHatchFooter(writer);
    }

    private static void WritePolylineHatchIfFilled(
        DxfDocumentWriter writer,
        string layerName,
        CadColor fillColor,
        PolylineEntity polyline,
        BoundingBox2D? contentBounds)
    {
        if (!polyline.IsFilled || !polyline.IsClosed)
        {
            return;
        }

        WriteHatchHeader(
            writer,
            layerName,
            fillColor);
        writer.WriteGroup(91, 1);
        writer.WriteGroup(92, 7);
        writer.WriteGroup(72, 0);
        writer.WriteGroup(73, 1);
        PolylineEntity hatchPolyline = polyline.HasArcSegments
            ? polyline.ToPolylineApproximation()
            : polyline;

        writer.WriteGroup(93, hatchPolyline.Vertices.Count);

        foreach (Point2D vertex in hatchPolyline.Vertices)
        {
            Point2D dxfVertex = ToDxfPoint(
                vertex,
                contentBounds);

            writer.WriteGroup(10, dxfVertex.X);
            writer.WriteGroup(20, dxfVertex.Y);
        }

        WriteHatchFooter(writer);
    }

    private static void WriteHatchHeader(
        DxfDocumentWriter writer,
        string layerName,
        CadColor fillColor)
    {
        writer.WriteGroup(0, "HATCH");
        writer.WriteGroup(8, layerName);
        writer.WriteGroup(62, DxfColorMapper.ToAci(fillColor));
        writer.WriteGroup(420, DxfColorMapper.ToTrueColor(fillColor));
        writer.WriteGroup(100, "AcDbHatch");
        writer.WriteGroup(10, 0.0);
        writer.WriteGroup(20, 0.0);
        writer.WriteGroup(30, 0.0);
        writer.WriteGroup(210, 0.0);
        writer.WriteGroup(220, 0.0);
        writer.WriteGroup(230, 1.0);
        writer.WriteGroup(2, "SOLID");
        writer.WriteGroup(70, 1);
        writer.WriteGroup(71, 0);
    }

    private static void WriteHatchFooter(DxfDocumentWriter writer)
    {
        writer.WriteGroup(97, 0);
        writer.WriteGroup(75, 0);
        writer.WriteGroup(76, 1);
        writer.WriteGroup(98, 0);
    }


    private static void WriteBezierSpline(
        DxfDocumentWriter writer,
        string layerName,
        BezierSplineEntity spline,
        BoundingBox2D? contentBounds)
    {
        writer.WriteGroup(0, "SPLINE");
        WriteEntityByLayerProperties(
            writer,
            layerName);
        int degree = Math.Min(3, spline.ControlPoints.Count - 1);
        IReadOnlyList<double> knots = CreateOpenUniformKnotVector(
            spline.ControlPoints.Count,
            degree);

        writer.WriteGroup(70, spline.IsClosed ? 9 : 8);
        writer.WriteGroup(71, degree);
        writer.WriteGroup(72, knots.Count);
        writer.WriteGroup(73, spline.ControlPoints.Count);
        writer.WriteGroup(74, 0);

        foreach (double knot in knots)
        {
            writer.WriteGroup(40, knot);
        }

        foreach (Point2D controlPoint in spline.ControlPoints)
        {
            Point2D dxfPoint = ToDxfPoint(
                controlPoint,
                contentBounds);

            writer.WriteGroup(10, dxfPoint.X);
            writer.WriteGroup(20, dxfPoint.Y);
            writer.WriteGroup(30, 0.0);
        }
    }

    private static IReadOnlyList<double> CreateOpenUniformKnotVector(
        int controlPointCount,
        int degree)
    {
        if (controlPointCount <= 0)
        {
            return Array.Empty<double>();
        }

        if (degree < 1)
        {
            return Enumerable.Range(
                    0,
                    controlPointCount + degree + 1)
                .Select(index => (double)index)
                .ToArray();
        }

        int knotCount = controlPointCount + degree + 1;
        int maximumKnotValue = controlPointCount - degree;
        var knots = new double[knotCount];

        for (int index = 0; index < knotCount; index++)
        {
            if (index <= degree)
            {
                knots[index] = 0.0;
            }
            else if (index >= controlPointCount)
            {
                knots[index] = maximumKnotValue;
            }
            else
            {
                knots[index] = index - degree;
            }
        }

        return knots;
    }

    private static BoundingBox2D? GetContentBounds(
        CadDocument document,
        IReadOnlyList<CadEntity> entities)
    {
        if (entities.Count == 0)
        {
            return null;
        }

        BoundingBox2D first = GetExportBounds(
            document,
            entities[0]);

        double minX = first.MinX;
        double minY = first.MinY;
        double maxX = first.MaxX;
        double maxY = first.MaxY;

        foreach (CadEntity entity in entities.Skip(1))
        {
            BoundingBox2D bounds = GetExportBounds(
                document,
                entity);

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

    private static BoundingBox2D GetExportBounds(
        CadDocument document,
        CadEntity entity)
    {
        if (entity is DimensionEntity dimension)
        {
            DimensionStyle style = ResolveDimensionStyle(
                document,
                dimension);

            return new DimensionGeometryBuilder()
                .Build(
                    dimension,
                    style)
                .Bounds;
        }

        return entity.GetBoundingBox();
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

    private static double ToDxfAngleDegrees(
        Angle angle,
        DxfExportOptions options)
    {
        return ToDxfRotationDegrees(
            angle.Degrees,
            options);
    }

    private static double ToDxfRotationDegrees(
        double degrees,
        DxfExportOptions options)
    {
        return options.UseCadViewerCoordinateSystem
            ? NormalizeDegrees(-degrees)
            : NormalizeDegrees(degrees);
    }

    private static bool ToDxfCounterClockwise(
        bool isCounterClockwise,
        DxfExportOptions options)
    {
        return options.UseCadViewerCoordinateSystem
            ? !isCounterClockwise
            : isCounterClockwise;
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

    private static DimensionStyle ResolveDimensionStyle(
        CadDocument document,
        DimensionEntity dimension)
    {
        if (document.DimensionStyles.TryGetById(
                dimension.DimensionStyleId,
                out DimensionStyle? style) &&
            style is not null)
        {
            return style;
        }

        return document.DimensionStyles.GetById(DimensionStyleId.Standard);
    }

    private static TextFormat ResolveDimensionTextFormat(
        CadDocument document,
        DimensionStyle style)
    {
        if (document.TextFormats.TryGetById(
                style.TextFormatId,
                out TextFormat? format) &&
            format is not null)
        {
            return format;
        }

        return document.TextFormats.GetById(TextFormatId.Annotation);
    }

    private static TextFormat ResolveTextFormat(
        CadDocument document,
        TextEntity text)
    {
        return ResolveTextFormat(
            document,
            text.TextFormatId);
    }

    private static TextFormat ResolveTextFormat(
        CadDocument document,
        MultilineTextEntity text)
    {
        return ResolveTextFormat(
            document,
            text.TextFormatId);
    }

    private static TextFormat ResolveTextFormat(
        CadDocument document,
        TextFormatId textFormatId)
    {
        if (document.TextFormats.TryGetById(
                textFormatId,
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

    private static double NormalizeRadians(double radians)
    {
        double value = radians % Math.Tau;
        return value < 0.0 ? value + Math.Tau : value;
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
