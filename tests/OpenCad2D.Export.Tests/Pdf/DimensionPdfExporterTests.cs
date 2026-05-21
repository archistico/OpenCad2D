using System.Text;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Export.Pdf;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests.Pdf;

public sealed class DimensionPdfExporterTests
{
    [Fact]
    public void Export_WhenDocumentContainsHorizontalDimension_ShouldWriteDimensionLinesAndText()
    {
        var document = new CadDocument();
        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("BT", content);
        Assert.Contains("/F1", content);
        Assert.Contains("(100.00) Tj", content);
        Assert.True(CountOccurrences(content, " m") >= 7);
        Assert.True(CountOccurrences(content, " l") >= 7);
        Assert.NotNull(result.ContentBounds);
    }

    [Fact]
    public void Export_WhenDocumentContainsVerticalDimension_ShouldWriteRotatedDimensionText()
    {
        var document = new CadDocument();
        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 50),
            new Point2D(20, 0),
            DimensionOrientation.Vertical));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("(50.00) Tj", content);
        // PDF uses screen-space rotation, so model 270° gives the +90° text matrix.
        // Cosine values very close to zero may be serialized as either 0 or -0.
        Assert.Contains(" 1 -1 ", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsAlignedDimension_ShouldWriteAlignedMeasurementText()
    {
        var document = new CadDocument();
        document.AddEntity(new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(3, 4),
            new Point2D(-4, 3)));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("(5.00) Tj", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsAngularDimension_ShouldWriteArcApproximationAndAngleText()
    {
        var document = new CadDocument();
        document.AddEntity(new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, 8),
            isCounterClockwise: true));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("(90.00\\260) Tj", content);
        Assert.True(CountOccurrences(content, " l") > 6);
    }

    [Fact]
    public void Export_WhenDocumentContainsRadiusDimension_ShouldWriteRadiusText()
    {
        var document = new CadDocument();
        document.AddEntity(new RadiusDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2)));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("(R 10.00) Tj", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsDiameterDimension_ShouldWriteDiameterText()
    {
        var document = new CadDocument();
        document.AddEntity(new DiameterDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2)));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("(\\330 20.00) Tj", content);
    }

    private static string ToAscii(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes);
    }

    private static int CountOccurrences(
        string value,
        string search)
    {
        int count = 0;
        int index = 0;

        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }
}
