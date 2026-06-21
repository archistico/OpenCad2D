using System.Globalization;
using System.Text;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Dimensions.Rendering;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Pdf;

/// <summary>
/// Exports CAD documents to a single-page vector PDF.
/// </summary>
public sealed class PdfExporter : IPdfExporter
{
    private const double PointsPerMillimeter = 72.0 / 25.4;
    private const double MinimumStrokeWidth = 0.2;
    private const double PointMarkerRadius = 2.5;

    public PdfExportResult Export(
        CadDocument document,
        PdfExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        PdfExportOptions actualOptions = options ?? PdfExportOptions.Default;

        if (actualOptions.MarginMillimeters < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                actualOptions.MarginMillimeters,
                "PDF margin cannot be negative.");
        }

        IReadOnlyList<CadEntity> entities = GetExportableEntities(
            document,
            actualOptions);

        BoundingBox2D? contentBounds = GetContentBounds(
            document,
            entities);
        (double pageWidth, double pageHeight) = GetPageSizeInPoints(
            actualOptions.PageSize,
            actualOptions.Orientation);

        double margin = actualOptions.MarginMillimeters * PointsPerMillimeter;
        double drawableWidth = Math.Max(1.0, pageWidth - margin * 2.0);
        double drawableHeight = Math.Max(1.0, pageHeight - margin * 2.0);
        double scale = GetScale(
            contentBounds,
            drawableWidth,
            drawableHeight,
            actualOptions.EmptyDocumentSizeMillimeters * PointsPerMillimeter);

        var context = new PdfExportContext(
            contentBounds,
            pageWidth,
            pageHeight,
            margin,
            drawableWidth,
            drawableHeight,
            scale,
            actualOptions.UsePrintFriendlyColors);

        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("q");
        contentBuilder.AppendLine("1 J 1 j");

        foreach (CadEntity entity in entities)
        {
            Layer layer = document.Layers.GetRequired(entity.LayerId);
            LineFormat lineFormat = ResolveLineFormat(
                document,
                layer);

            WriteEntity(
                contentBuilder,
                document,
                entity,
                layer,
                lineFormat,
                context);
        }

        contentBuilder.AppendLine("Q");

        byte[] pdf = BuildPdf(
            contentBuilder.ToString(),
            pageWidth,
            pageHeight);

        return new PdfExportResult(
            pdf,
            entities.Count,
            contentBounds,
            pageWidth,
            pageHeight,
            scale);
    }

    public void ExportToFile(
        CadDocument document,
        string filePath,
        PdfExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "PDF export file path cannot be empty.",
                nameof(filePath));
        }

        PdfExportResult result = Export(
            document,
            options);

        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(
            filePath,
            result.Content);
    }

    private static IReadOnlyList<CadEntity> GetExportableEntities(
        CadDocument document,
        PdfExportOptions options)
    {
        IEnumerable<CadEntity> entities = options.IncludeHiddenLayers
            ? document.Entities.All.Where(entity => entity.IsVisible)
            : document.GetVisibleEntities();

        return entities
            .OrderBy(entity => document.Layers.GetRequired(entity.LayerId).Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entity => entity.DrawOrder)
            .ToList();
    }

    private static BoundingBox2D? GetContentBounds(
        CadDocument document,
        IReadOnlyList<CadEntity> entities)
    {
        if (entities.Count == 0)
        {
            return null;
        }

        BoundingBox2D bounds = GetEntityExportBounds(
            document,
            entities[0]);

        foreach (CadEntity entity in entities.Skip(1))
        {
            BoundingBox2D current = GetEntityExportBounds(
                document,
                entity);
            bounds = new BoundingBox2D(
                Math.Min(bounds.MinX, current.MinX),
                Math.Min(bounds.MinY, current.MinY),
                Math.Max(bounds.MaxX, current.MaxX),
                Math.Max(bounds.MaxY, current.MaxY));
        }

        return bounds;
    }

    private static BoundingBox2D GetEntityExportBounds(
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

    private static double GetScale(
        BoundingBox2D? bounds,
        double drawableWidth,
        double drawableHeight,
        double fallbackSizePoints)
    {
        if (bounds is null)
        {
            return Math.Min(
                drawableWidth / fallbackSizePoints,
                drawableHeight / fallbackSizePoints);
        }

        double contentWidth = Math.Max(1.0, bounds.Value.Width);
        double contentHeight = Math.Max(1.0, bounds.Value.Height);

        return Math.Min(
            drawableWidth / contentWidth,
            drawableHeight / contentHeight);
    }

    private static void WriteEntity(
        StringBuilder builder,
        CadDocument document,
        CadEntity entity,
        Layer layer,
        LineFormat lineFormat,
        PdfExportContext context)
    {
        ApplyStroke(
            builder,
            lineFormat,
            context);

        switch (entity)
        {
            case LineEntity line:
                WriteLine(
                    builder,
                    line.Start,
                    line.End,
                    context);
                break;

            case CircleEntity circle:
                WriteCircle(
                    builder,
                    circle.Center,
                    circle.Radius,
                    circle.IsFilled ? layer.FillColor : null,
                    context);
                break;

            case EllipseEntity ellipse:
                WriteEllipse(
                    builder,
                    ellipse,
                    context);
                break;

            case EllipticalArcEntity ellipticalArc:
                WriteEllipticalArc(
                    builder,
                    ellipticalArc,
                    context);
                break;

            case PointEntity point:
                WriteCircle(
                    builder,
                    point.Position,
                    PointMarkerRadius / Math.Max(context.Scale, 0.0001),
                    null,
                    context);
                break;

            case PolylineEntity polyline:
                WritePolyline(
                    builder,
                    polyline,
                    polyline.IsClosed && polyline.IsFilled ? layer.FillColor : null,
                    context);
                break;

            case BezierSplineEntity spline:
                WriteBezierSpline(
                    builder,
                    spline,
                    context);
                break;

            case ArcEntity arc:
                WriteArc(
                    builder,
                    arc,
                    context);
                break;

            case StairEntity stair:
                WriteStair(
                    builder,
                    stair,
                    context);
                break;

            case DoorEntity door:
                WriteDoor(
                    builder,
                    door,
                    context);
                break;

            case WindowEntity window:
                WriteWindow(
                    builder,
                    window,
                    context);
                break;

            case TextEntity text:
                WriteText(
                    builder,
                    document,
                    text,
                    context);
                break;

            case MultilineTextEntity multilineText:
                WriteMultilineText(
                    builder,
                    document,
                    multilineText,
                    context);
                break;

            case LinearDimensionEntity linearDimension:
                WriteDimension(
                    builder,
                    document,
                    linearDimension,
                    lineFormat,
                    context);
                break;

            case AlignedDimensionEntity alignedDimension:
                WriteDimension(
                    builder,
                    document,
                    alignedDimension,
                    lineFormat,
                    context);
                break;

            case RadiusDimensionEntity radiusDimension:
                WriteDimension(
                    builder,
                    document,
                    radiusDimension,
                    lineFormat,
                    context);
                break;

            case DiameterDimensionEntity diameterDimension:
                WriteDimension(
                    builder,
                    document,
                    diameterDimension,
                    lineFormat,
                    context);
                break;

            case AngularDimensionEntity angularDimension:
                WriteDimension(
                    builder,
                    document,
                    angularDimension,
                    lineFormat,
                    context);
                break;
        }
    }

    private static void ApplyStroke(
        StringBuilder builder,
        LineFormat lineFormat,
        PdfExportContext context)
    {
        CadColor color = GetExportColor(
            lineFormat.Color,
            context.UsePrintFriendlyColors);

        builder.AppendLine($"{Format(color.R / 255.0)} {Format(color.G / 255.0)} {Format(color.B / 255.0)} RG");
        builder.AppendLine($"{Format(Math.Max(MinimumStrokeWidth, lineFormat.LineWeight.Millimeters * PointsPerMillimeter))} w");

        IReadOnlyList<double> pattern = lineFormat.DashPattern;

        if (pattern.Count == 0)
        {
            builder.AppendLine("[] 0 d");
            return;
        }

        string dashArray = string.Join(
            " ",
            pattern.Select(value => Format(Math.Max(0.1, Math.Abs(value) * context.Scale))));
        builder.AppendLine($"[{dashArray}] 0 d");
    }

    private static void ApplyFill(
        StringBuilder builder,
        CadColor fillColor,
        PdfExportContext context)
    {
        CadColor color = GetExportColor(
            fillColor,
            context.UsePrintFriendlyColors);

        builder.AppendLine($"{Format(color.R / 255.0)} {Format(color.G / 255.0)} {Format(color.B / 255.0)} rg");
    }

    private static CadColor GetExportColor(
        CadColor color,
        bool usePrintFriendlyColors)
    {
        if (!usePrintFriendlyColors)
        {
            return color;
        }

        int brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;

        return brightness >= 220
            ? CadColor.FromRgb(0, 0, 0)
            : color;
    }

    private static void WriteLine(
        StringBuilder builder,
        Point2D start,
        Point2D end,
        PdfExportContext context)
    {
        Point2D pdfStart = ToPdfPoint(
            start,
            context);
        Point2D pdfEnd = ToPdfPoint(
            end,
            context);

        builder.AppendLine($"{Format(pdfStart.X)} {Format(pdfStart.Y)} m");
        builder.AppendLine($"{Format(pdfEnd.X)} {Format(pdfEnd.Y)} l");
        builder.AppendLine("S");
    }

    private static void WriteCircle(
        StringBuilder builder,
        Point2D center,
        double radius,
        CadColor? fillColor,
        PdfExportContext context)
    {
        Point2D c = ToPdfPoint(
            center,
            context);
        double r = Math.Abs(radius * context.Scale);
        double k = 0.5522847498307936 * r;

        builder.AppendLine($"{Format(c.X + r)} {Format(c.Y)} m");
        builder.AppendLine($"{Format(c.X + r)} {Format(c.Y + k)} {Format(c.X + k)} {Format(c.Y + r)} {Format(c.X)} {Format(c.Y + r)} c");
        builder.AppendLine($"{Format(c.X - k)} {Format(c.Y + r)} {Format(c.X - r)} {Format(c.Y + k)} {Format(c.X - r)} {Format(c.Y)} c");
        builder.AppendLine($"{Format(c.X - r)} {Format(c.Y - k)} {Format(c.X - k)} {Format(c.Y - r)} {Format(c.X)} {Format(c.Y - r)} c");
        builder.AppendLine($"{Format(c.X + k)} {Format(c.Y - r)} {Format(c.X + r)} {Format(c.Y - k)} {Format(c.X + r)} {Format(c.Y)} c");
        WriteFillOrStrokePath(
            builder,
            fillColor,
            context);
    }

    private static void WriteEllipse(
        StringBuilder builder,
        EllipseEntity ellipse,
        PdfExportContext context)
    {
        IReadOnlyList<Point2D> points = ellipse.GetSamplePoints();
        if (points.Count == 0)
        {
            return;
        }

        Point2D first = ToPdfPoint(
            points[0],
            context);
        builder.AppendLine($"{Format(first.X)} {Format(first.Y)} m");

        foreach (Point2D modelPoint in points.Skip(1))
        {
            Point2D point = ToPdfPoint(
                modelPoint,
                context);
            builder.AppendLine($"{Format(point.X)} {Format(point.Y)} l");
        }

        builder.AppendLine("h");
        builder.AppendLine("S");
    }

    private static void WriteEllipticalArc(
        StringBuilder builder,
        EllipticalArcEntity ellipticalArc,
        PdfExportContext context)
    {
        IReadOnlyList<Point2D> points = ellipticalArc.GetSamplePoints();
        if (points.Count == 0)
        {
            return;
        }

        Point2D first = ToPdfPoint(
            points[0],
            context);
        builder.AppendLine($"{Format(first.X)} {Format(first.Y)} m");

        foreach (Point2D modelPoint in points.Skip(1))
        {
            Point2D point = ToPdfPoint(
                modelPoint,
                context);
            builder.AppendLine($"{Format(point.X)} {Format(point.Y)} l");
        }

        builder.AppendLine("S");
    }

    private static void WritePolyline(
        StringBuilder builder,
        PolylineEntity polyline,
        CadColor? fillColor,
        PdfExportContext context)
    {
        PolylineEntity exportPolyline = polyline.HasArcSegments
            ? polyline.ToPolylineApproximation()
            : polyline;

        if (exportPolyline.Vertices.Count == 0)
        {
            return;
        }

        Point2D first = ToPdfPoint(
            exportPolyline.Vertices[0],
            context);

        builder.AppendLine($"{Format(first.X)} {Format(first.Y)} m");

        foreach (Point2D vertex in exportPolyline.Vertices.Skip(1))
        {
            Point2D point = ToPdfPoint(
                vertex,
                context);
            builder.AppendLine($"{Format(point.X)} {Format(point.Y)} l");
        }

        if (exportPolyline.IsClosed)
        {
            builder.AppendLine("h");
        }

        WriteFillOrStrokePath(
            builder,
            fillColor,
            context);
    }

    private static void WriteFillOrStrokePath(
        StringBuilder builder,
        CadColor? fillColor,
        PdfExportContext context)
    {
        if (fillColor.HasValue)
        {
            ApplyFill(
                builder,
                fillColor.Value,
                context);
            builder.AppendLine("B");
            return;
        }

        builder.AppendLine("S");
    }

    private static void WriteBezierSpline(
        StringBuilder builder,
        BezierSplineEntity spline,
        PdfExportContext context)
    {
        IReadOnlyList<Point2D> points = spline.GetSamplePoints();
        if (points.Count == 0)
        {
            return;
        }

        Point2D first = ToPdfPoint(
            points[0],
            context);
        builder.AppendLine($"{Format(first.X)} {Format(first.Y)} m");

        foreach (Point2D modelPoint in points.Skip(1))
        {
            Point2D point = ToPdfPoint(
                modelPoint,
                context);
            builder.AppendLine($"{Format(point.X)} {Format(point.Y)} l");
        }

        if (spline.IsClosed)
        {
            builder.AppendLine("h");
        }

        builder.AppendLine("S");
    }

    private static void WriteArc(
        StringBuilder builder,
        ArcEntity arc,
        PdfExportContext context)
    {
        double sweep = GetPositiveSweepRadians(
            arc.StartAngle.Radians,
            arc.EndAngle.Radians,
            arc.IsCounterClockwise);
        int segmentCount = Math.Max(
            8,
            (int)Math.Ceiling(sweep / (Math.PI / 24.0)));

        for (int index = 0; index <= segmentCount; index++)
        {
            double t = index / (double)segmentCount;
            double angle = arc.IsCounterClockwise
                ? arc.StartAngle.Radians + sweep * t
                : arc.StartAngle.Radians - sweep * t;

            Point2D modelPoint = new(
                arc.Center.X + Math.Cos(angle) * arc.Radius,
                arc.Center.Y + Math.Sin(angle) * arc.Radius);
            Point2D pdfPoint = ToPdfPoint(
                modelPoint,
                context);

            builder.AppendLine(index == 0
                ? $"{Format(pdfPoint.X)} {Format(pdfPoint.Y)} m"
                : $"{Format(pdfPoint.X)} {Format(pdfPoint.Y)} l");
        }

        builder.AppendLine("S");
    }

    private static void WriteStair(
        StringBuilder builder,
        StairEntity stair,
        PdfExportContext context)
    {
        foreach (LineSegment2D segment in stair.GetGeneratedGeometry().Segments)
        {
            WriteLine(
                builder,
                segment.Start,
                segment.End,
                context);
        }
    }

    private static void WriteDoor(
        StringBuilder builder,
        DoorEntity door,
        PdfExportContext context)
    {
        var geometry = door.GetGeneratedGeometry();

        if (geometry.HasWallMask)
        {
            WriteWhiteFilledPolygon(
                builder,
                geometry.WallMaskPolygon,
                context);
        }

        foreach (LineSegment2D segment in geometry.Segments)
        {
            WriteLine(
                builder,
                segment.Start,
                segment.End,
                context);
        }
    }


    private static void WriteWindow(
        StringBuilder builder,
        WindowEntity window,
        PdfExportContext context)
    {
        var geometry = window.GetGeneratedGeometry();

        if (geometry.HasWallMask)
        {
            WriteWhiteFilledPolygon(
                builder,
                geometry.WallMaskPolygon,
                context);
        }

        foreach (LineSegment2D segment in geometry.Segments)
        {
            WriteLine(
                builder,
                segment.Start,
                segment.End,
                context);
        }
    }


    private static void WriteWhiteFilledPolygon(
        StringBuilder builder,
        IReadOnlyList<Point2D> vertices,
        PdfExportContext context)
    {
        if (vertices.Count < 3)
        {
            return;
        }

        Point2D first = ToPdfPoint(
            vertices[0],
            context);

        builder.AppendLine("1 1 1 rg");
        builder.AppendLine($"{Format(first.X)} {Format(first.Y)} m");

        foreach (Point2D vertex in vertices.Skip(1))
        {
            Point2D point = ToPdfPoint(
                vertex,
                context);
            builder.AppendLine($"{Format(point.X)} {Format(point.Y)} l");
        }

        builder.AppendLine("h");
        builder.AppendLine("f");
    }

    private static void WriteDimension(
        StringBuilder builder,
        CadDocument document,
        DimensionEntity dimension,
        LineFormat lineFormat,
        PdfExportContext context)
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
            WriteLine(
                builder,
                line.Start,
                line.End,
                context);
        }

        foreach (DimensionArcPrimitive arc in model.Arcs)
        {
            WriteDimensionArc(
                builder,
                arc,
                context);
        }

        WriteDimensionText(
            builder,
            model.Text,
            textFormat,
            context);
    }

    private static void WriteDimensionArc(
        StringBuilder builder,
        DimensionArcPrimitive arc,
        PdfExportContext context)
    {
        double startRadians = arc.StartAngleDegrees * Math.PI / 180.0;
        double endRadians = arc.EndAngleDegrees * Math.PI / 180.0;
        double sweep = GetPositiveSweepRadians(
            startRadians,
            endRadians,
            arc.IsCounterClockwise);
        int segmentCount = Math.Max(
            8,
            (int)Math.Ceiling(sweep / (Math.PI / 24.0)));

        for (int index = 0; index <= segmentCount; index++)
        {
            double t = index / (double)segmentCount;
            double angle = arc.IsCounterClockwise
                ? startRadians + sweep * t
                : startRadians - sweep * t;

            Point2D modelPoint = new(
                arc.Center.X + Math.Cos(angle) * arc.Radius,
                arc.Center.Y + Math.Sin(angle) * arc.Radius);
            Point2D pdfPoint = ToPdfPoint(
                modelPoint,
                context);

            builder.AppendLine(index == 0
                ? $"{Format(pdfPoint.X)} {Format(pdfPoint.Y)} m"
                : $"{Format(pdfPoint.X)} {Format(pdfPoint.Y)} l");
        }

        builder.AppendLine("S");
    }

    private static void WriteDimensionText(
        StringBuilder builder,
        DimensionTextPrimitive text,
        TextFormat textFormat,
        PdfExportContext context)
    {
        CadColor color = GetExportColor(
            textFormat.Color,
            context.UsePrintFriendlyColors);
        Point2D point = ToPdfPoint(
            text.Position,
            context);
        double fontSize = Math.Max(1.0, textFormat.Height * context.Scale);
        double estimatedTextWidth = text.Text.Length * fontSize * 0.6;
        double estimatedTextHeight = fontSize;
        double radians = -text.RotationDegrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        builder.AppendLine("BT");
        builder.AppendLine($"{Format(color.R / 255.0)} {Format(color.G / 255.0)} {Format(color.B / 255.0)} rg");
        builder.AppendLine($"/F1 {Format(fontSize)} Tf");
        builder.AppendLine($"{Format(cos)} {Format(sin)} {Format(-sin)} {Format(cos)} {Format(point.X)} {Format(point.Y)} Tm");
        builder.AppendLine($"{Format(-estimatedTextWidth / 2.0)} {Format(-estimatedTextHeight / 2.0)} Td");
        builder.AppendLine($"({EscapePdfString(text.Text)}) Tj");
        builder.AppendLine("ET");
    }

    private static void WriteMultilineText(
        StringBuilder builder,
        CadDocument document,
        MultilineTextEntity text,
        PdfExportContext context)
    {
        TextFormat format = ResolveTextFormat(
            document,
            text);
        CadColor color = GetExportColor(
            format.Color,
            context.UsePrintFriendlyColors);
        Point2D point = ToPdfPoint(
            text.InsertionPoint,
            context);
        double fontSize = Math.Max(1.0, format.Height * context.Scale);
        double lineHeight = fontSize * 1.2;
        double radians = -text.RotationDegrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        builder.AppendLine("BT");
        builder.AppendLine($"{Format(color.R / 255.0)} {Format(color.G / 255.0)} {Format(color.B / 255.0)} rg");
        builder.AppendLine($"/F1 {Format(fontSize)} Tf");
        builder.AppendLine($"{Format(cos)} {Format(sin)} {Format(-sin)} {Format(cos)} {Format(point.X)} {Format(point.Y)} Tm");

        IReadOnlyList<string> lines = text.Lines;
        for (int index = 0; index < lines.Count; index++)
        {
            if (index > 0)
            {
                builder.AppendLine($"0 -{Format(lineHeight)} Td");
            }

            builder.AppendLine($"({EscapePdfString(lines[index])}) Tj");
        }

        builder.AppendLine("ET");
    }

    private static void WriteText(
        StringBuilder builder,
        CadDocument document,
        TextEntity text,
        PdfExportContext context)
    {
        TextFormat format = ResolveTextFormat(
            document,
            text);
        CadColor color = GetExportColor(
            format.Color,
            context.UsePrintFriendlyColors);
        Point2D point = ToPdfPoint(
            text.InsertionPoint,
            context);
        double fontSize = Math.Max(1.0, format.Height * context.Scale);
        double radians = -text.RotationDegrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        builder.AppendLine("BT");
        builder.AppendLine($"{Format(color.R / 255.0)} {Format(color.G / 255.0)} {Format(color.B / 255.0)} rg");
        builder.AppendLine($"/F1 {Format(fontSize)} Tf");
        builder.AppendLine($"{Format(cos)} {Format(sin)} {Format(-sin)} {Format(cos)} {Format(point.X)} {Format(point.Y)} Tm");
        builder.AppendLine($"({EscapePdfString(text.Text)}) Tj");
        builder.AppendLine("ET");
    }

    private static Point2D ToPdfPoint(
        Point2D point,
        PdfExportContext context)
    {
        if (context.ContentBounds is null)
        {
            return new Point2D(
                context.Margin + context.DrawableWidth / 2.0,
                context.Margin + context.DrawableHeight / 2.0);
        }

        double contentWidth = Math.Max(1.0, context.ContentBounds.Value.Width);
        double contentHeight = Math.Max(1.0, context.ContentBounds.Value.Height);
        double renderedWidth = contentWidth * context.Scale;
        double renderedHeight = contentHeight * context.Scale;
        double offsetX = context.Margin + (context.DrawableWidth - renderedWidth) / 2.0;
        double offsetY = context.Margin + (context.DrawableHeight - renderedHeight) / 2.0;

        return new Point2D(
            offsetX + (point.X - context.ContentBounds.Value.MinX) * context.Scale,
            offsetY + (context.ContentBounds.Value.MaxY - point.Y) * context.Scale);
    }

    private static double GetPositiveSweepRadians(
        double startRadians,
        double endRadians,
        bool isCounterClockwise)
    {
        double delta = isCounterClockwise
            ? endRadians - startRadians
            : startRadians - endRadians;

        delta %= 2.0 * Math.PI;

        if (delta < 0)
        {
            delta += 2.0 * Math.PI;
        }

        return delta;
    }

    private static LineFormat ResolveLineFormat(
        CadDocument document,
        Layer layer)
    {
        if (document.LineFormats.TryGetById(
            layer.LineFormatId,
            out LineFormat? lineFormat) &&
            lineFormat is not null)
        {
            return lineFormat;
        }

        return LineFormatCollection.Default.GetById(LineFormatId.Continuous);
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

        return document.TextFormats.GetById(TextFormatId.Standard);
    }

    private static (double Width, double Height) GetPageSizeInPoints(
        PdfPageSize pageSize,
        PdfPageOrientation orientation)
    {
        (double widthMillimeters, double heightMillimeters) = pageSize switch
        {
            PdfPageSize.A0 => (841.0, 1189.0),
            PdfPageSize.A1 => (594.0, 841.0),
            PdfPageSize.A2 => (420.0, 594.0),
            PdfPageSize.A3 => (297.0, 420.0),
            _ => (210.0, 297.0)
        };

        double width = widthMillimeters * PointsPerMillimeter;
        double height = heightMillimeters * PointsPerMillimeter;

        return orientation == PdfPageOrientation.Landscape
            ? (height, width)
            : (width, height);
    }

    private static byte[] BuildPdf(
        string contentStream,
        double pageWidth,
        double pageHeight)
    {
        byte[] contentBytes = Encoding.ASCII.GetBytes(contentStream);
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Format(pageWidth)} {Format(pageHeight)}] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {contentBytes.Length} >>\nstream\n{contentStream}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        };

        using var stream = new MemoryStream();
        WriteAscii(
            stream,
            "%PDF-1.4\n%OpenCad2D\n");

        var offsets = new List<long> { 0 };

        for (int index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(
                stream,
                $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        long xrefOffset = stream.Position;
        WriteAscii(
            stream,
            $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(
            stream,
            "0000000000 65535 f \n");

        foreach (long offset in offsets.Skip(1))
        {
            WriteAscii(
                stream,
                $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(
            stream,
            $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");

        return stream.ToArray();
    }

    private static void WriteAscii(
        Stream stream,
        string value)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes);
    }

    private static string EscapePdfString(string value)
    {
        var builder = new StringBuilder();

        foreach (char character in value)
        {
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;

                case '(':
                    builder.Append("\\(");
                    break;

                case ')':
                    builder.Append("\\)");
                    break;

                case '\r':
                    break;

                case '\n':
                    builder.Append(' ');
                    break;

                default:
                    if (character >= 32 && character <= 126)
                    {
                        builder.Append(character);
                    }
                    else if (character <= 255)
                    {
                        builder.Append('\\');
                        builder.Append(Convert.ToString(
                                (int)character,
                                8)
                            .PadLeft(
                                3,
                                '0'));
                    }
                    else
                    {
                        builder.Append('?');
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    private static string Format(double value)
    {
        return value.ToString(
            "0.###",
            CultureInfo.InvariantCulture);
    }

    private sealed record PdfExportContext(
        BoundingBox2D? ContentBounds,
        double PageWidth,
        double PageHeight,
        double Margin,
        double DrawableWidth,
        double DrawableHeight,
        double Scale,
        bool UsePrintFriendlyColors);
}
