using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Svg;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

public sealed class DimensionExportTests
{
    [Fact]
    public void SvgExport_WhenDocumentContainsHorizontalDimension_ShouldWriteLinesAndMeasurementText()
    {
        var document = new CadDocument();
        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(7, CountOccurrences(result.Content, "<line "));
        Assert.Contains("<text ", result.Content);
        Assert.Contains(">100.00</text>", result.Content);
        Assert.Contains("font-size=\"8\"", result.Content);
        Assert.Contains("fill=\"#FFFF00\"", result.Content);
    }

    [Fact]
    public void SvgExport_WhenDocumentContainsVerticalDimension_ShouldWriteVerticalMeasurementText()
    {
        var document = new CadDocument();
        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 50),
            new Point2D(20, 0),
            DimensionOrientation.Vertical));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(7, CountOccurrences(result.Content, "<line "));
        Assert.Contains(">50.00</text>", result.Content);
        Assert.Contains("rotate(-90", result.Content);
    }

    [Fact]
    public void SvgExport_WhenDocumentContainsAlignedDimension_ShouldWriteAlignedMeasurementText()
    {
        var document = new CadDocument();
        document.AddEntity(new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(3, 4),
            new Point2D(-4, 3)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(7, CountOccurrences(result.Content, "<line "));
        Assert.Contains(">5.00</text>", result.Content);
        Assert.Contains("<text ", result.Content);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsHorizontalDimension_ShouldWriteGraphicPrimitives()
    {
        var document = new CadDocument();
        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<IReadOnlyList<DxfGroup>> lineRecords = GetRecords(
            ParseGroups(result.Content),
            "LINE");
        IReadOnlyList<DxfGroup> textRecord = Assert.Single(GetRecords(
            ParseGroups(result.Content),
            "TEXT"));

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(7, lineRecords.Count);
        Assert.Contains(textRecord, group => group.Code == 1 && group.Value == "100.00");
        Assert.Contains(textRecord, group => group.Code == 40 && group.Value == "8");
        Assert.Contains(textRecord, group => group.Code == 7 && group.Value == "Annotation");
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsVerticalDimension_ShouldWriteRotatedMeasurementText()
    {
        var document = new CadDocument();
        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(0, 50),
            new Point2D(20, 0),
            DimensionOrientation.Vertical));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<DxfGroup> textRecord = Assert.Single(GetRecords(
            ParseGroups(result.Content),
            "TEXT"));

        Assert.Contains(textRecord, group => group.Code == 1 && group.Value == "50.00");
        Assert.Contains(textRecord, group => group.Code == 50 && group.Value == "270");
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsAlignedDimension_ShouldWriteGraphicPrimitives()
    {
        var document = new CadDocument();
        document.AddEntity(new AlignedDimensionEntity(
            new Point2D(0, 0),
            new Point2D(3, 4),
            new Point2D(-4, 3)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<IReadOnlyList<DxfGroup>> lineRecords = GetRecords(
            ParseGroups(result.Content),
            "LINE");
        IReadOnlyList<DxfGroup> textRecord = Assert.Single(GetRecords(
            ParseGroups(result.Content),
            "TEXT"));

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Equal(7, lineRecords.Count);
        Assert.Contains(textRecord, group => group.Code == 1 && group.Value == "5.00");
    }

    [Fact]
    public void DxfExport_DimensionPrimitives_ShouldUseByLayerProperties()
    {
        var document = new CadDocument();
        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(100, 0),
            new Point2D(0, 20),
            DimensionOrientation.Horizontal));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<IReadOnlyList<DxfGroup>> records = GetRecords(
                ParseGroups(result.Content),
                "LINE")
            .Concat(GetRecords(
                ParseGroups(result.Content),
                "TEXT"))
            .ToList();

        Assert.NotEmpty(records);
        Assert.All(records, record =>
        {
            Assert.Contains(record, group => group.Code == 8 && group.Value == "0");
            Assert.Contains(record, group => group.Code == 62 && group.Value == "256");
            Assert.Contains(record, group => group.Code == 6 && group.Value == "BYLAYER");
            Assert.Contains(record, group => group.Code == 370 && group.Value == "-1");
        });
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

    private static IReadOnlyList<DxfGroup> ParseGroups(string content)
    {
        string[] lines = Normalize(content)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var groups = new List<DxfGroup>();

        for (int i = 0; i < lines.Length; i += 2)
        {
            groups.Add(new DxfGroup(
                int.Parse(lines[i], System.Globalization.CultureInfo.InvariantCulture),
                lines[i + 1]));
        }

        return groups;
    }

    private static IReadOnlyList<IReadOnlyList<DxfGroup>> GetRecords(
        IReadOnlyList<DxfGroup> groups,
        string recordType)
    {
        var records = new List<IReadOnlyList<DxfGroup>>();

        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].Code != 0 || groups[i].Value != recordType)
            {
                continue;
            }

            int start = i;
            int end = groups.Count;

            for (int j = i + 1; j < groups.Count; j++)
            {
                if (groups[j].Code == 0)
                {
                    end = j;
                    break;
                }
            }

            records.Add(groups.Skip(start).Take(end - start).ToList());
        }

        return records;
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n");
    }

    private readonly record struct DxfGroup(
        int Code,
        string Value);
}
