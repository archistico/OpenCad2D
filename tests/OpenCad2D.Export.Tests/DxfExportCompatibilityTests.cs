using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

/// <summary>
/// Compatibility-oriented DXF tests. These tests do not replace validation in external
/// viewers such as LibreCAD, QCAD or TrueView; they protect the generated ASCII DXF
/// structure before manual viewer validation.
/// </summary>
public sealed class DxfExportCompatibilityTests
{
    [Fact]
    public void Export_ShouldWriteBalancedDxfCodeValuePairs()
    {
        CadDocument document = CreateRepresentativeDocument();
        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<string> lines = Normalize(result.Content)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.True(lines.Count % 2 == 0);
        Assert.Equal("0", lines[^2]);
        Assert.Equal("EOF", lines[^1]);
    }

    [Fact]
    public void Export_ShouldWriteRepresentativeEntityRecords()
    {
        CadDocument document = CreateRepresentativeDocument();
        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<DxfGroup> groups = ParseGroups(result.Content);

        Assert.Equal(6, result.ExportedEntityCount);
        Assert.Single(GetRecords(groups, "POINT"));
        Assert.Single(GetRecords(groups, "TEXT"));
        Assert.Single(GetRecords(groups, "LINE"));
        Assert.Single(GetRecords(groups, "CIRCLE"));
        Assert.Single(GetRecords(groups, "ARC"));
        Assert.Single(GetRecords(groups, "LWPOLYLINE"));
    }

    [Theory]
    [InlineData("POINT")]
    [InlineData("TEXT")]
    [InlineData("LINE")]
    [InlineData("CIRCLE")]
    [InlineData("ARC")]
    [InlineData("LWPOLYLINE")]
    public void Export_ShouldWriteEntityRecordsWithByLayerProperties(string entityType)
    {
        CadDocument document = CreateRepresentativeDocument();
        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<DxfGroup> record = Assert.Single(GetRecords(
            ParseGroups(result.Content),
            entityType));

        Assert.Contains(record, group => group.Code == 8 && group.Value == "0");
        Assert.Contains(record, group => group.Code == 62 && group.Value == "256");
        Assert.Contains(record, group => group.Code == 6 && group.Value == "BYLAYER");
        Assert.Contains(record, group => group.Code == 370 && group.Value == "-1");
    }

    [Fact]
    public void Export_LayerRecords_ShouldWriteSingleLineTypeGroup()
    {
        var document = new CadDocument();
        document.Layers.Add(new Layer(
            new LayerId("AxisLayer"),
            "AxisLayer",
            LineFormatId.Axis));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<IReadOnlyList<DxfGroup>> layerRecords = GetRecords(
            ParseGroups(result.Content),
            "LAYER");

        Assert.NotEmpty(layerRecords);
        Assert.All(layerRecords, record =>
            Assert.Equal(1, record.Count(group => group.Code == 6)));
    }

    [Theory]
    [InlineData("LayerContinuous", "Continuous", "CONTINUOUS", "16777215", "100")]
    [InlineData("LayerAxis", "Axis", "DASHDOT", "16711680", "75")]
    [InlineData("LayerDashed", "Dashed", "DASHED", "16776960", "75")]
    [InlineData("LayerDashDot", "DashDot", "DASHDOT", "65280", "75")]
    [InlineData("LayerDashDotDot", "DashDotDot", "DASHDOTDOT", "51455", "50")]
    public void Export_LayerRecords_ShouldMapBuiltInLineFormatsToDxfProperties(
        string layerName,
        string lineFormatId,
        string expectedLineType,
        string expectedTrueColor,
        string expectedLineWeight)
    {
        var document = new CadDocument();
        document.Layers.Add(new Layer(
            new LayerId(layerName),
            layerName,
            new LineFormatId(lineFormatId)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<DxfGroup> layerRecord = GetLayerRecord(
            ParseGroups(result.Content),
            layerName);

        Assert.Contains(layerRecord, group => group.Code == 6 && group.Value == expectedLineType);
        Assert.Contains(layerRecord, group => group.Code == 420 && group.Value == expectedTrueColor);
        Assert.Contains(layerRecord, group => group.Code == 370 && group.Value == expectedLineWeight);
    }

    [Fact]
    public void Export_TextEntity_ShouldWriteSingleLineDxfTextWithFormatHeightAndMirroredAngle()
    {
        var document = new CadDocument();
        document.AddEntity(new TextEntity(
            new Point2D(10, 20),
            "Room A",
            90,
            TextFormatId.Title));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<DxfGroup> textRecord = Assert.Single(GetRecords(
            ParseGroups(result.Content),
            "TEXT"));

        Assert.Contains(textRecord, group => group.Code == 1 && group.Value == "Room A");
        Assert.Contains(textRecord, group => group.Code == 40 && group.Value == "18");
        Assert.Contains(textRecord, group => group.Code == 50 && group.Value == "270");
        Assert.Contains(textRecord, group => group.Code == 7 && group.Value == "Title");
    }

    [Fact]
    public void Export_OpenPolyline_ShouldWriteVertexCountAndOpenFlag()
    {
        var document = new CadDocument();
        document.AddEntity(new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        }));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<DxfGroup> polylineRecord = Assert.Single(GetRecords(
            ParseGroups(result.Content),
            "LWPOLYLINE"));

        Assert.Contains(polylineRecord, group => group.Code == 90 && group.Value == "4");
        Assert.Contains(polylineRecord, group => group.Code == 70 && group.Value == "0");
        Assert.Equal(4, polylineRecord.Count(group => group.Code == 10));
        Assert.Equal(4, polylineRecord.Count(group => group.Code == 20));
    }

    [Fact]
    public void Export_ClosedPolyline_ShouldWriteClosedFlag()
    {
        var document = new CadDocument();
        document.AddEntity(new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: true));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        IReadOnlyList<DxfGroup> polylineRecord = Assert.Single(GetRecords(
            ParseGroups(result.Content),
            "LWPOLYLINE"));

        Assert.Contains(polylineRecord, group => group.Code == 90 && group.Value == "3");
        Assert.Contains(polylineRecord, group => group.Code == 70 && group.Value == "1");
    }


    [Fact]
    public void Export_MixedPolyline_ShouldWriteBulgeGroupsOnOwningVertices()
    {
        var document = new CadDocument();
        document.AddEntity(new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)
            },
            isClosed: true,
            segmentBulges: new[]
            {
                0.0,
                0.414213562373095,
                0.0,
                -0.25
            }));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(
            document,
            new DxfExportOptions
            {
                UseCadViewerCoordinateSystem = false
            });
        IReadOnlyList<DxfGroup> polylineRecord = Assert.Single(GetRecords(
            ParseGroups(result.Content),
            "LWPOLYLINE"));

        Assert.Contains(polylineRecord, group => group.Code == 90 && group.Value == "4");
        Assert.Contains(polylineRecord, group => group.Code == 70 && group.Value == "1");
        Assert.Equal(2, polylineRecord.Count(group => group.Code == 42));
        Assert.Contains(polylineRecord, group => group.Code == 42 && group.Value == "0.414213562373095");
        Assert.Contains(polylineRecord, group => group.Code == 42 && group.Value == "-0.25");
    }

    private static CadDocument CreateRepresentativeDocument()
    {
        var document = new CadDocument();

        document.AddEntity(new PointEntity(new Point2D(1, 2)));
        document.AddEntity(new TextEntity(
            new Point2D(2, 3),
            "Label",
            15,
            TextFormatId.Standard));
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0)));
        document.AddEntity(new CircleEntity(
            new Point2D(5, 5),
            2));
        document.AddEntity(new ArcEntity(
            new Point2D(10, 10),
            3,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90)));
        document.AddEntity(new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0),
            new Point2D(5, 5)
        }));

        return document;
    }

    private static IReadOnlyList<DxfGroup> ParseGroups(string content)
    {
        string[] lines = Normalize(content)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var groups = new List<DxfGroup>();

        for (int index = 0; index < lines.Length; index += 2)
        {
            groups.Add(new DxfGroup(
                int.Parse(lines[index]),
                lines[index + 1]));
        }

        return groups;
    }

    private static IReadOnlyList<IReadOnlyList<DxfGroup>> GetRecords(
        IReadOnlyList<DxfGroup> groups,
        string recordType)
    {
        var records = new List<IReadOnlyList<DxfGroup>>();

        for (int index = 0; index < groups.Count; index++)
        {
            DxfGroup group = groups[index];

            if (group.Code != 0 || group.Value != recordType)
            {
                continue;
            }

            int nextRecordIndex = index + 1;

            while (nextRecordIndex < groups.Count && groups[nextRecordIndex].Code != 0)
            {
                nextRecordIndex++;
            }

            records.Add(groups
                .Skip(index)
                .Take(nextRecordIndex - index)
                .ToList());
        }

        return records;
    }

    private static IReadOnlyList<DxfGroup> GetLayerRecord(
        IReadOnlyList<DxfGroup> groups,
        string layerName)
    {
        return GetRecords(groups, "LAYER")
            .Single(record => record.Any(group => group.Code == 2 && group.Value == layerName));
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n");
    }

    private sealed record DxfGroup(int Code, string Value);
}
