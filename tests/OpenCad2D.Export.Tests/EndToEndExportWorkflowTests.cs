using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Export.Pdf;
using OpenCad2D.Export.Svg;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

public sealed class EndToEndExportWorkflowTests
{
    [Fact]
    public void DrawAnnotateExport_ShouldProduceSvgPdfAndDxfWithPrimaryEntities()
    {
        CadDocument document = CreateAnnotatedDocument();

        SvgExportResult svg = new SvgExporter().Export(document);
        PdfExportResult pdf = new PdfExporter().Export(document);
        DxfExportResult dxf = new DxfExporter().Export(document);
        string normalizedDxf = Normalize(dxf.Content);

        Assert.Equal(9, svg.ExportedEntityCount);
        Assert.Contains("<svg", svg.Content);
        Assert.Contains("<line", svg.Content);
        Assert.Contains("<circle", svg.Content);
        Assert.Contains("<path", svg.Content);
        Assert.Contains("<polygon", svg.Content);
        Assert.Contains("<polyline", svg.Content);
        Assert.Contains("Single line note", svg.Content);
        Assert.Contains("First line", svg.Content);
        Assert.Contains("Second line", svg.Content);

        Assert.Equal(9, pdf.ExportedEntityCount);
        Assert.True(pdf.Content.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Content, 0, 4));

        Assert.Equal(9, dxf.ExportedEntityCount);
        Assert.Contains("0\nLINE", normalizedDxf);
        Assert.Contains("0\nCIRCLE", normalizedDxf);
        Assert.Contains("0\nARC", normalizedDxf);
        Assert.Contains("0\nLWPOLYLINE", normalizedDxf);
        Assert.Contains("0\nELLIPSE", normalizedDxf);
        Assert.Contains("0\nSPLINE", normalizedDxf);
        Assert.Contains("0\nTEXT", normalizedDxf);
        Assert.Contains("0\nMTEXT", normalizedDxf);
        Assert.Contains("First line\\PSecond line", normalizedDxf);
    }

    [Fact]
    public void ImportDxfTrimLineAndExport_ShouldKeepModifiedGeometryExportable()
    {
        const string dxf = "0\nSECTION\n2\nENTITIES\n" +
                           "0\nLINE\n8\nGeometry\n10\n0\n20\n0\n11\n100\n21\n0\n" +
                           "0\nLINE\n8\nGeometry\n10\n50\n20\n-20\n11\n50\n21\n20\n" +
                           "0\nTEXT\n8\nNotes\n10\n5\n20\n10\n40\n2.5\n1\nImported note\n" +
                           "0\nMTEXT\n8\nNotes\n10\n5\n20\n20\n40\n2.5\n1\nImported\\PMText\n" +
                           "0\nENDSEC\n0\nEOF";

        DxfImportResult importResult = new DxfDocumentImporter().Import(dxf);
        Assert.DoesNotContain(
            importResult.Log,
            entry => entry.Severity == DxfImportLogSeverity.Error);

        var importedLines = importResult.Document.Entities.All
            .OfType<LineEntity>()
            .ToList();

        LineEntity target = Assert.Single(
            importedLines,
            line => Math.Abs(line.Start.Y - line.End.Y) < 1e-9);
        LineEntity boundary = Assert.Single(
            importedLines,
            line => Math.Abs(line.Start.X - line.End.X) < 1e-9);

        IReadOnlyList<CadEntity> fragments = CadTrimService.TrimByBoundary(
            target,
            boundary,
            new Point2D(75, 0));

        Assert.Single(fragments);

        importResult.Document.RemoveEntity(target.Id);
        importResult.Document.AddEntities(fragments);

        DxfExportResult exportResult = new DxfExporter().Export(importResult.Document);
        string exported = Normalize(exportResult.Content);

        Assert.Equal(4, exportResult.ExportedEntityCount);
        Assert.Contains("0\nLINE", exported);
        Assert.Contains("0\nTEXT", exported);
        Assert.Contains("0\nMTEXT", exported);
        Assert.Contains("Imported\\PMText", exported);

        DxfImportResult roundTripResult = new DxfDocumentImporter().Import(exported);
        Assert.DoesNotContain(
            roundTripResult.Log,
            entry => entry.Severity == DxfImportLogSeverity.Error);

        var roundTripLines = roundTripResult.Document.Entities.All
            .OfType<LineEntity>()
            .ToList();

        LineEntity trimmedHorizontal = Assert.Single(
            roundTripLines,
            line => IsHorizontal(line));
        LineEntity verticalBoundary = Assert.Single(
            roundTripLines,
            line => IsVertical(line));

        Assert.True(
            NearlyEqual(trimmedHorizontal.Geometry.Length, 50),
            $"Expected the trimmed horizontal line to keep a length of 50, but it was {trimmedHorizontal.Geometry.Length}.");
        Assert.True(
            NearlyEqual(verticalBoundary.Geometry.Length, 40),
            $"Expected the vertical boundary line to keep a length of 40, but it was {verticalBoundary.Geometry.Length}.");
        Assert.True(
            LineHasEndpointOnVerticalLine(trimmedHorizontal, verticalBoundary),
            "Expected the trimmed line to terminate on the vertical boundary after round-trip export/import.");
    }

    private static CadDocument CreateAnnotatedDocument()
    {
        var document = new CadDocument();

        LayerId geometryLayerId = new("Geometry");
        LayerId annotationLayerId = new("Annotations");

        document.Layers.Add(new Layer(geometryLayerId, "Geometry"));
        document.Layers.Add(new Layer(annotationLayerId, "Annotations"));

        document.AddEntities(new CadEntity[]
        {
            new LineEntity(
                new Point2D(0, 0),
                new Point2D(100, 0),
                layerId: geometryLayerId),

            new CircleEntity(
                new Point2D(25, 25),
                10,
                layerId: geometryLayerId),

            new ArcEntity(
                new Point2D(50, 25),
                12,
                Angle.FromDegrees(0),
                Angle.FromDegrees(135),
                layerId: geometryLayerId),

            new PolylineEntity(
                new[]
                {
                    new Point2D(0, 0),
                    new Point2D(40, 0),
                    new Point2D(40, 30),
                    new Point2D(0, 30)
                },
                isClosed: true,
                layerId: geometryLayerId),

            new EllipseEntity(
                new Point2D(70, 25),
                new Vector2D(18, 0),
                7,
                layerId: geometryLayerId),

            new BezierSplineEntity(
                new[]
                {
                    new Point2D(0, 50),
                    new Point2D(20, 70),
                    new Point2D(45, 45),
                    new Point2D(70, 60)
                },
                layerId: geometryLayerId),

            new TextEntity(
                new Point2D(0, 40),
                "Single line note",
                textFormatId: TextFormatId.Standard,
                layerId: annotationLayerId),

            new MultilineTextEntity(
                new Point2D(0, 48),
                "First line\nSecond line",
                textFormatId: TextFormatId.Standard,
                layerId: annotationLayerId),

            new LinearDimensionEntity(
                new Point2D(0, 0),
                new Point2D(100, 0),
                new Point2D(50, -12),
                DimensionOrientation.Horizontal,
                layerId: annotationLayerId)
        });

        return document;
    }

    private static bool IsHorizontal(LineEntity line)
    {
        return NearlyEqual(line.Start.Y, line.End.Y);
    }

    private static bool IsVertical(LineEntity line)
    {
        return NearlyEqual(line.Start.X, line.End.X);
    }

    private static bool LineHasEndpointOnVerticalLine(LineEntity line, LineEntity verticalLine)
    {
        double minY = Math.Min(verticalLine.Start.Y, verticalLine.End.Y);
        double maxY = Math.Max(verticalLine.Start.Y, verticalLine.End.Y);

        return EndpointIsOnVerticalLine(line.Start, verticalLine.Start.X, minY, maxY)
               || EndpointIsOnVerticalLine(line.End, verticalLine.Start.X, minY, maxY);
    }

    private static bool EndpointIsOnVerticalLine(Point2D point, double x, double minY, double maxY)
    {
        return NearlyEqual(point.X, x)
               && point.Y >= minY - 1e-6
               && point.Y <= maxY + 1e-6;
    }

    private static bool NearlyEqual(double first, double second)
    {
        return Math.Abs(first - second) <= 1e-6;
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
