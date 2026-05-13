using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

public sealed class DxfExporterTests
{
    [Fact]
    public void Export_WhenDocumentIsEmpty_ShouldWriteMinimalDxfStructure()
    {
        var document = new CadDocument();
        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);

        string content = Normalize(result.Content);

        Assert.Contains("0\nSECTION\n2\nHEADER", content);
        Assert.Contains("9\n$ACADVER\n1\nAC1015", content);
        Assert.Contains("0\nSECTION\n2\nTABLES", content);
        Assert.Contains("0\nSECTION\n2\nENTITIES", content);
        Assert.EndsWith("0\nEOF\n", content);
        Assert.Equal(0, result.ExportedEntityCount);
        Assert.Equal("AC1015", result.AcadVersion);
    }

    [Fact]
    public void Export_ShouldWriteSectionsInDxfOrder()
    {
        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(new CadDocument());
        string content = Normalize(result.Content);

        int headerIndex = content.IndexOf("2\nHEADER", StringComparison.Ordinal);
        int tablesIndex = content.IndexOf("2\nTABLES", StringComparison.Ordinal);
        int entitiesIndex = content.IndexOf("2\nENTITIES", StringComparison.Ordinal);
        int eofIndex = content.IndexOf("0\nEOF", StringComparison.Ordinal);

        Assert.True(headerIndex >= 0);
        Assert.True(tablesIndex > headerIndex);
        Assert.True(entitiesIndex > tablesIndex);
        Assert.True(eofIndex > entitiesIndex);
    }

    [Fact]
    public void Export_WithCustomAcadVersion_ShouldWriteVersion()
    {
        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(
            new CadDocument(),
            new DxfExportOptions
            {
                AcadVersion = "AC1018"
            });

        Assert.Contains("1\nAC1018", Normalize(result.Content));
        Assert.Equal("AC1018", result.AcadVersion);
    }

    [Fact]
    public void Export_WithEmptyAcadVersion_ShouldThrow()
    {
        var exporter = new DxfExporter();

        Assert.Throws<ArgumentException>(() => exporter.Export(
            new CadDocument(),
            new DxfExportOptions
            {
                AcadVersion = " "
            }));
    }

    [Fact]
    public void ExportToFile_ShouldCreateAsciiDxfFile()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-{Guid.NewGuid():N}.dxf");

        try
        {
            var exporter = new DxfExporter();

            exporter.ExportToFile(
                new CadDocument(),
                path);

            string content = File.ReadAllText(path);

            Assert.Contains("SECTION", content);
            Assert.Contains("EOF", content);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Export_WhenDocumentContainsLine_ShouldWriteLineEntity()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(1, 2),
            new Point2D(30, 40)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("0\nLINE", content);
        Assert.Contains("8\n0", content);
        Assert.Contains("10\n1\n20\n40", content);
        Assert.Contains("11\n30\n21\n2", content);
    }

    [Fact]
    public void Export_ShouldFlipYCoordinatesToMatchCadViewers()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 100)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("10\n0\n20\n100", content);
        Assert.Contains("11\n10\n21\n0", content);
    }


    [Fact]
    public void Export_WhenDocumentContainsCircle_ShouldWriteCircleEntity()
    {
        var document = new CadDocument();
        document.AddEntity(new CircleEntity(
            new Point2D(5, 6),
            12.5));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("0\nCIRCLE", content);
        Assert.Contains("10\n5\n20\n6", content);
        Assert.Contains("40\n12.5", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsCounterClockwiseArc_ShouldWriteArcEntity()
    {
        var document = new CadDocument();
        document.AddEntity(new ArcEntity(
            new Point2D(10, 20),
            15,
            Angle.FromDegrees(30),
            Angle.FromDegrees(120),
            isCounterClockwise: true));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("0\nARC", content);
        Assert.Contains("10\n10\n20\n42.5", content);
        Assert.Contains("40\n15", content);
        Assert.Contains("50\n240\n51\n330", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsClockwiseArc_ShouldSwapAnglesForDxfArc()
    {
        var document = new CadDocument();
        document.AddEntity(new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(30),
            Angle.FromDegrees(120),
            isCounterClockwise: false));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("0\nARC", content);
        Assert.Contains("50\n330\n51\n240", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsOpenPolyline_ShouldWriteLwPolylineEntity()
    {
        var document = new CadDocument();
        document.AddEntity(new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 20)
        }));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("0\nLWPOLYLINE", content);
        Assert.Contains("90\n3", content);
        Assert.Contains("70\n0", content);
        Assert.Contains("10\n0\n20\n20", content);
        Assert.Contains("10\n10\n20\n0", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsClosedPolyline_ShouldWriteClosedLwPolylineFlag()
    {
        var document = new CadDocument();
        document.AddEntity(new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 20)
            },
            isClosed: true));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("0\nLWPOLYLINE", content);
        Assert.Contains("70\n1", content);
    }


    [Fact]
    public void Export_WithModelCoordinateSystem_ShouldNotFlipYCoordinates()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 100)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(
            document,
            new DxfExportOptions
            {
                UseCadViewerCoordinateSystem = false
            });
        string content = Normalize(result.Content);

        Assert.Contains("10\n0\n20\n0", content);
        Assert.Contains("11\n10\n21\n100", content);
    }

    [Fact]
    public void Export_WithModelCoordinateSystem_ShouldNotInvertAngles()
    {
        var document = new CadDocument();
        document.AddEntity(new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(30),
            Angle.FromDegrees(120),
            isCounterClockwise: true));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(
            document,
            new DxfExportOptions
            {
                UseCadViewerCoordinateSystem = false
            });
        string content = Normalize(result.Content);

        Assert.Contains("50\n30\n51\n120", content);
    }

    [Fact]
    public void Export_ShouldIgnoreEntitiesOnHiddenLayersByDefault()
    {
        var document = new CadDocument();
        var hiddenLayerId = new LayerId("Hidden");

        document.Layers.Add(new Layer(
            hiddenLayerId,
            "Hidden",
            isVisible: false));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: hiddenLayerId));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Equal(0, result.ExportedEntityCount);
        Assert.DoesNotContain("0\nLINE", content);
    }

    [Fact]
    public void Export_WithIncludeHiddenLayers_ShouldExportVisibleEntitiesOnHiddenLayers()
    {
        var document = new CadDocument();
        var hiddenLayerId = new LayerId("Hidden");

        document.Layers.Add(new Layer(
            hiddenLayerId,
            "Hidden",
            isVisible: false));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: hiddenLayerId));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(
            document,
            new DxfExportOptions
            {
                IncludeHiddenLayers = true
            });

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("0\nLINE", Normalize(result.Content));
    }


    [Fact]
    public void Export_ShouldWriteLineTypeTable()
    {
        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(new CadDocument());
        string content = Normalize(result.Content);

        Assert.Contains("0\nTABLE\n2\nLTYPE", content);
        Assert.Contains("2\nCONTINUOUS", content);
        Assert.Contains("2\nDASHED", content);
        Assert.Contains("2\nDASHDOT", content);
        Assert.Contains("2\nDASHDOTDOT", content);
        Assert.Contains("49\n6\n74\n0\n49\n-3", content);
        Assert.Contains("49\n6\n74\n0\n49\n-2\n74\n0\n49\n0\n74\n0\n49\n-2", content);
    }

    [Fact]
    public void Export_ShouldWriteLayerTableWithDefaultLayerLineFormat()
    {
        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(new CadDocument());
        string content = Normalize(result.Content);

        Assert.Contains("0\nTABLE\n2\nLAYER", content);
        Assert.Contains("0\nLAYER\n2\n0", content);
        Assert.Contains("62\n7", content);
        Assert.Contains("420\n16777215", content);
        Assert.Contains("6\nCONTINUOUS", content);
        Assert.Contains("370\n100", content);
    }

    [Fact]
    public void Export_ShouldUseLayerLineFormatForColorLineWeightAndLineType()
    {
        var document = new CadDocument();
        var constructionLayerId = new LayerId("Construction");

        document.Layers.Add(new Layer(
            constructionLayerId,
            "Construction",
            LineFormatId.Axis));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: constructionLayerId));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("0\nLAYER\n2\nConstruction", content);
        Assert.Contains("62\n1", content);
        Assert.Contains("420\n16711680", content);
        Assert.Contains("6\nDASHDOT", content);
        Assert.Contains("370\n75", content);
    }

    [Fact]
    public void Export_ShouldWriteEntitiesWithByLayerProperties()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(1, 2),
            new Point2D(3, 4)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("0\nLINE\n8\n0\n62\n256\n6\nBYLAYER\n370\n-1", content);
    }

    [Fact]
    public void Export_ShouldWriteHiddenLayerWithNegativeAciColor()
    {
        var document = new CadDocument();
        var hiddenLayerId = new LayerId("HiddenAxis");

        document.Layers.Add(new Layer(
            hiddenLayerId,
            "HiddenAxis",
            LineFormatId.Axis,
            isVisible: false));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("0\nLAYER\n2\nHiddenAxis", content);
        Assert.Contains("62\n-1", content);
    }

    [Fact]
    public void Export_WhenLayerReferencesUnknownLineFormat_ShouldFallBackToContinuous()
    {
        var document = new CadDocument();
        var layerId = new LayerId("UnknownFormatLayer");

        document.Layers.Add(new Layer(
            layerId,
            "UnknownFormatLayer",
            new LineFormatId("MissingFormat")));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("0\nLAYER\n2\nUnknownFormatLayer", content);
        Assert.Contains("6\nCONTINUOUS", content);
        Assert.Contains("370\n100", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsPoint_ShouldWritePointEntity()
    {
        var document = new CadDocument();
        document.AddEntity(new PointEntity(new Point2D(5, 9)));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = Normalize(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("0\nPOINT", content);
        Assert.Contains("8\n0", content);
        Assert.Contains("10\n5\n20\n9\n30\n0", content);
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n");
    }
}
