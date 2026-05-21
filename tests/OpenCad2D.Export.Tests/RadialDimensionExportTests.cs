using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Svg;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

public sealed class RadialDimensionExportTests
{
    [Fact]
    public void SvgExport_WhenDocumentContainsRadiusDimension_ShouldWriteGraphicPrimitivesAndRadiusText()
    {
        var document = new CadDocument();
        document.AddEntity(new RadiusDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(5, CountOccurrences(result.Content, "<line "));
        Assert.Contains("<text ", result.Content);
        Assert.Contains(">R 10.00</text>", result.Content);
    }

    [Fact]
    public void SvgExport_WhenDocumentContainsDiameterDimension_ShouldWriteGraphicPrimitivesAndDiameterText()
    {
        var document = new CadDocument();
        document.AddEntity(new DiameterDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(8, CountOccurrences(result.Content, "<line "));
        Assert.Contains("<text ", result.Content);
        Assert.Contains(">Ø 20.00</text>", result.Content);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsRadiusDimension_ShouldWriteLineAndTextPrimitives()
    {
        var document = new CadDocument();
        document.AddEntity(new RadiusDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(5, CountEntityRecords(result.Content, "LINE"));
        Assert.Equal(1, CountEntityRecords(result.Content, "TEXT"));
        Assert.Contains("R 10.00", result.Content);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsDiameterDimension_ShouldWriteLineAndTextPrimitives()
    {
        var document = new CadDocument();
        document.AddEntity(new DiameterDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(14, 2)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(8, CountEntityRecords(result.Content, "LINE"));
        Assert.Equal(1, CountEntityRecords(result.Content, "TEXT"));
        Assert.Contains("Ø 20.00", result.Content);
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
