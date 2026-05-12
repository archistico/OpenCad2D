using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Svg;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

public sealed class AngularDimensionExportTests
{
    [Fact]
    public void SvgExport_WhenDocumentContainsAngularDimension_ShouldWriteArcLinesAndAngleText()
    {
        var document = new CadDocument();
        document.AddEntity(new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, 8),
            isCounterClockwise: true));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(6, CountOccurrences(result.Content, "<line "));
        Assert.Contains("<path ", result.Content);
        Assert.Contains(">90.00°</text>", result.Content);
    }

    [Fact]
    public void SvgExport_WhenDocumentContainsReflexAngularDimension_ShouldUseLargeArcFlag()
    {
        var document = new CadDocument();
        document.AddEntity(new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, -8),
            isCounterClockwise: false));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Contains("A ", result.Content);
        Assert.Contains(" 1 ", result.Content);
        Assert.Contains(">270.00°</text>", result.Content);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsAngularDimension_ShouldWriteArcLineAndTextPrimitives()
    {
        var document = new CadDocument();
        document.AddEntity(new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, 8),
            isCounterClockwise: true));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(6, CountEntityRecords(result.Content, "LINE"));
        Assert.Equal(1, CountEntityRecords(result.Content, "ARC"));
        Assert.Equal(1, CountEntityRecords(result.Content, "TEXT"));
        Assert.Contains("90.00°", result.Content);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsReflexAngularDimension_ShouldWriteReflexAngleText()
    {
        var document = new CadDocument();
        document.AddEntity(new AngularDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            new Point2D(8, -8),
            isCounterClockwise: false));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);

        Assert.Equal(1, CountEntityRecords(result.Content, "ARC"));
        Assert.Contains("270.00°", result.Content);
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

    private static int CountEntityRecords(
        string content,
        string entityName)
    {
        string normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        return CountOccurrences(normalized, $"\n0\n{entityName}\n");
    }
}
