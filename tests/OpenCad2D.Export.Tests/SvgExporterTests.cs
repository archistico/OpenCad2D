using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Export.Svg;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

public sealed class SvgExporterTests
{
    [Fact]
    public void Export_WhenDocumentIsEmpty_ShouldReturnValidSvg()
    {
        var document = new CadDocument();
        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Contains("<svg", result.Content);
        Assert.Contains("viewBox=\"0 0 140 140\"", result.Content);
        Assert.Equal(0, result.ExportedEntityCount);
        Assert.Null(result.ContentBounds);
    }

    [Fact]
    public void Export_WhenDocumentContainsLine_ShouldWriteLineElement()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(100, 0)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("<line ", result.Content);
        Assert.Contains("x1=\"20\"", result.Content);
        Assert.Contains("x2=\"120\"", result.Content);
        Assert.Contains("stroke=\"#FFFFFF\"", result.Content);
        Assert.Contains("stroke-width=\"1\"", result.Content);
    }

    [Fact]
    public void Export_WhenDocumentContainsCircle_ShouldWriteCircleElement()
    {
        var document = new CadDocument();
        document.AddEntity(new CircleEntity(
            new Point2D(50, 50),
            25));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("<circle ", result.Content);
        Assert.Contains("r=\"25\"", result.Content);
    }

    [Fact]
    public void Export_WhenDocumentContainsOpenPolyline_ShouldWritePolylineElement()
    {
        var document = new CadDocument();
        document.AddEntity(new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            }));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("<polyline ", result.Content);
        Assert.DoesNotContain("<polygon ", result.Content);
    }

    [Fact]
    public void Export_WhenDocumentContainsClosedPolyline_ShouldWritePolygonElement()
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

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("<polygon ", result.Content);
        Assert.DoesNotContain("<polyline ", result.Content);
    }

    [Fact]
    public void Export_ShouldIgnoreEntitiesOnHiddenLayers()
    {
        var document = new CadDocument();
        var visibleLayerId = new LayerId("Visible");
        var hiddenLayerId = new LayerId("Hidden");

        document.Layers.Add(new Layer(
            visibleLayerId,
            "Visible"));

        document.Layers.Add(new Layer(
            hiddenLayerId,
            "Hidden",
            isVisible: false));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: visibleLayerId));

        document.AddEntity(new LineEntity(
            new Point2D(0, 10),
            new Point2D(10, 10),
            layerId: hiddenLayerId));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Single(result.Content.Split("<line ").Skip(1));
    }

    [Fact]
    public void Export_ShouldIncludeEntitiesOnLockedVisibleLayers()
    {
        var document = new CadDocument();
        var lockedLayerId = new LayerId("Reference");

        document.Layers.Add(new Layer(
            lockedLayerId,
            "Reference",
            isLocked: true));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: lockedLayerId));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("<line ", result.Content);
    }

    [Fact]
    public void Export_ShouldUseLineFormatColorAndLineWeight()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Walls");
        var formatId = new LineFormatId("WallsFormat");

        document.ReplaceLineFormats(new LineFormatCollection(new[]
        {
            new LineFormat(
                formatId,
                "Walls format",
                CadColor.FromRgb(255, 170, 0),
                LineWeight.FromMillimeters(0.5),
                LineStyle.Continuous)
        }));

        document.Layers.Add(new Layer(
            layerId,
            "Walls",
            formatId));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Contains("stroke=\"#FFAA00\"", result.Content);
        Assert.Contains("stroke-width=\"0.5\"", result.Content);
    }

    [Fact]
    public void Export_ShouldIgnoreEntityStyleAndUseOnlyLayerLineFormat()
    {
        var document = new CadDocument();
        var layerId = new LayerId("LayerWeight");
        var formatId = new LineFormatId("LayerWeightFormat");

        document.ReplaceLineFormats(new LineFormatCollection(new[]
        {
            new LineFormat(
                formatId,
                "Layer weight format",
                CadColor.FromRgb(10, 20, 30),
                LineWeight.FromMillimeters(0.75),
                LineStyle.Continuous)
        }));

        document.Layers.Add(new Layer(
            layerId,
            "LayerWeight",
            formatId));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId,
            style: new EntityStyle
            {
                Color = CadColor.FromRgb(200, 200, 200),
                LineWeight = LineWeight.FromMillimeters(8)
            }));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Contains("stroke=\"#0A141E\"", result.Content);
        Assert.Contains("stroke-width=\"0.75\"", result.Content);
        Assert.DoesNotContain("stroke-width=\"8\"", result.Content);
        Assert.DoesNotContain("stroke=\"#C8C8C8\"", result.Content);
    }

    [Fact]
    public void Export_ShouldWriteStrokeDashArray_ForDashedLineFormat()
    {
        var document = new CadDocument();
        var layerId = new LayerId("DashedLayer");
        var formatId = new LineFormatId("DashedFormat");

        document.ReplaceLineFormats(new LineFormatCollection(new[]
        {
            new LineFormat(
                formatId,
                "Dashed format",
                CadColor.FromRgb(255, 255, 255),
                LineWeight.FromMillimeters(0.25),
                LineStyle.Dashed)
        }));

        document.Layers.Add(new Layer(
            layerId,
            "DashedLayer",
            formatId));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Contains("stroke-dasharray=\"6 3\"", result.Content);
    }

    [Fact]
    public void Export_ShouldNotWriteStrokeDashArray_ForContinuousLineFormat()
    {
        var document = new CadDocument();
        var layerId = new LayerId("ContinuousLayer");
        var formatId = new LineFormatId("ContinuousFormat");

        document.ReplaceLineFormats(new LineFormatCollection(new[]
        {
            new LineFormat(
                formatId,
                "Continuous format",
                CadColor.FromRgb(255, 255, 255),
                LineWeight.FromMillimeters(0.25),
                LineStyle.Continuous)
        }));

        document.Layers.Add(new Layer(
            layerId,
            "ContinuousLayer",
            formatId));

        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.DoesNotContain("stroke-dasharray", result.Content);
    }

    [Fact]
    public void Export_ShouldGenerateViewBoxFromVisibleContentBounds()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(10, 20),
            new Point2D(110, 70)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(140, result.Width);
        Assert.Equal(90, result.Height);
        Assert.Contains("viewBox=\"0 0 140 90\"", result.Content);
    }

    [Fact]
    public void ExportToFile_ShouldWriteSvgFile()
    {
        var document = new CadDocument();
        document.AddEntity(new CircleEntity(
            new Point2D(0, 0),
            10));

        string path = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-export-{Guid.NewGuid():N}.svg");

        try
        {
            var exporter = new SvgExporter();

            exporter.ExportToFile(
                document,
                path);

            string content = File.ReadAllText(path);

            Assert.Contains("<svg", content);
            Assert.Contains("<circle ", content);
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
    public void Export_ShouldPreserveCanvasYOrientation()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 10),
            new Point2D(100, 10)));
        document.AddEntity(new LineEntity(
            new Point2D(0, 90),
            new Point2D(100, 90)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Contains("y1=\"20\"", result.Content);
        Assert.Contains("y2=\"20\"", result.Content);
        Assert.Contains("y1=\"100\"", result.Content);
        Assert.Contains("y2=\"100\"", result.Content);
    }

    [Fact]
    public void Export_ShouldWriteBackgroundRectangleByDefault()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Contains("<rect x=\"0\" y=\"0\"", result.Content);
        Assert.Contains("fill=\"#1E1E1E\"", result.Content);
    }

    [Fact]
    public void Export_WhenBackgroundIsDisabled_ShouldNotWriteBackgroundRectangle()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(
            document,
            new SvgExportOptions
            {
                IncludeBackground = false
            });

        Assert.DoesNotContain("<rect ", result.Content);
    }

    [Fact]
    public void Export_WhenDocumentContainsPoint_ShouldWritePointMarker()
    {
        var document = new CadDocument();
        document.AddEntity(new PointEntity(new Point2D(5, 9)));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("<circle", result.Content);
        Assert.Contains("cx=\"20\"", result.Content);
        Assert.Contains("cy=\"20\"", result.Content);
        Assert.Contains("r=\"2\"", result.Content);
    }

}
