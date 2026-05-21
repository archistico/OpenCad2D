using System.Text;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Export.Pdf;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests.Pdf;

public sealed class PdfExporterTests
{
    [Fact]
    public void Export_WhenDocumentIsEmpty_ShouldReturnValidPdf()
    {
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(new CadDocument());
        string content = ToAscii(result.Content);

        Assert.StartsWith("%PDF-1.4", content);
        Assert.Contains("/Type /Catalog", content);
        Assert.Contains("/Type /Page", content);
        Assert.Contains("/MediaBox [0 0 595.276 841.89]", content);
        Assert.Contains("%%EOF", content);
        Assert.Equal(0, result.ExportedEntityCount);
        Assert.Null(result.ContentBounds);
    }

    [Fact]
    public void Export_WithA3Landscape_ShouldUseExpectedMediaBox()
    {
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(
            new CadDocument(),
            new PdfExportOptions
            {
                PageSize = PdfPageSize.A3,
                Orientation = PdfPageOrientation.Landscape
            });
        string content = ToAscii(result.Content);

        Assert.Contains("/MediaBox [0 0 1190.551 841.89]", content);
        Assert.Equal(1190.551, result.PageWidthPoints, 3);
        Assert.Equal(841.89, result.PageHeightPoints, 3);
    }

    [Fact]
    public void Export_WhenDocumentContainsLine_ShouldWriteStrokePath()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(100, 0)));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains(" m", content);
        Assert.Contains(" l", content);
        Assert.Contains("S", content);
        Assert.NotNull(result.ContentBounds);
    }

    [Fact]
    public void Export_WhenDocumentContainsCircle_ShouldWriteBezierCurve()
    {
        var document = new CadDocument();
        document.AddEntity(new CircleEntity(
            new Point2D(50, 50),
            10));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains(" c", content);
        Assert.Contains("S", content);
    }


    [Fact]
    public void Export_WhenDocumentContainsFilledCircle_ShouldWriteFillColorAndFillStrokePath()
    {
        var document = new CadDocument();
        var layerId = new LayerId("FilledCircleLayer");

        document.Layers.Add(new Layer(
            layerId,
            "FilledCircleLayer",
            LineFormatId.Continuous,
            fillColor: CadColor.FromRgb(12, 34, 56)));

        document.AddEntity(new CircleEntity(
            new Point2D(50, 50),
            25,
            layerId: layerId,
            isFilled: true));

        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Contains("0.047 0.133 0.22 rg", content);
        Assert.Contains("B", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsNotFilledCircle_ShouldStrokeOnly()
    {
        var document = new CadDocument();
        var layerId = new LayerId("NotFilledCircleLayer");

        document.Layers.Add(new Layer(
            layerId,
            "NotFilledCircleLayer",
            LineFormatId.Continuous,
            fillColor: CadColor.FromRgb(12, 34, 56)));

        document.AddEntity(new CircleEntity(
            new Point2D(50, 50),
            25,
            layerId: layerId,
            isFilled: false));

        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.DoesNotContain("0.047 0.133 0.22 rg", content);
        Assert.DoesNotContain("\nB\n", content);
        Assert.Contains("S", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsFilledClosedPolyline_ShouldWriteFillColorAndFillStrokePath()
    {
        var document = new CadDocument();
        var layerId = new LayerId("FilledPolylineLayer");

        document.Layers.Add(new Layer(
            layerId,
            "FilledPolylineLayer",
            LineFormatId.Continuous,
            fillColor: CadColor.FromRgb(90, 80, 70)));

        document.AddEntity(new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: true,
            layerId: layerId,
            isFilled: true));

        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Contains("0.353 0.314 0.275 rg", content);
        Assert.Contains("h", content);
        Assert.Contains("B", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsFilledOpenPolyline_ShouldStrokeOnly()
    {
        var document = new CadDocument();
        var layerId = new LayerId("OpenPolylineLayer");

        document.Layers.Add(new Layer(
            layerId,
            "OpenPolylineLayer",
            LineFormatId.Continuous,
            fillColor: CadColor.FromRgb(90, 80, 70)));

        document.AddEntity(new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: false,
            layerId: layerId,
            isFilled: true));

        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.DoesNotContain("0.353 0.314 0.275 rg", content);
        Assert.DoesNotContain("\nB\n", content);
        Assert.Contains("S", content);
    }

    [Fact]
    public void Export_WhenDocumentContainsText_ShouldWritePdfTextObject()
    {
        var document = new CadDocument();
        document.AddEntity(new TextEntity(
            new Point2D(10, 20),
            "Hello (PDF)"));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("BT", content);
        Assert.Contains("/F1", content);
        Assert.Contains("(Hello \\(PDF\\)) Tj", content);
        Assert.Contains("ET", content);
    }

    [Fact]
    public void Export_ShouldIgnoreEntitiesOnHiddenLayersByDefault()
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

        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
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

        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(
            document,
            new PdfExportOptions
            {
                IncludeHiddenLayers = true
            });

        Assert.Equal(1, result.ExportedEntityCount);
    }

    [Fact]
    public void Export_WithPrintFriendlyColors_ShouldConvertWhiteStrokeToBlack()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0)));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Contains("0 0 0 RG", content);
    }

    [Fact]
    public void Export_WithScreenColors_ShouldKeepWhiteStroke()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0)));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(
            document,
            new PdfExportOptions
            {
                UsePrintFriendlyColors = false
            });
        string content = ToAscii(result.Content);

        Assert.Contains("1 1 1 RG", content);
    }

    [Fact]
    public void ExportToFile_ShouldCreatePdfFile()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"opencad2d-{Guid.NewGuid():N}.pdf");

        try
        {
            var exporter = new PdfExporter();

            exporter.ExportToFile(
                new CadDocument(),
                path);

            byte[] bytes = File.ReadAllBytes(path);
            string content = ToAscii(bytes);

            Assert.True(bytes.Length > 0);
            Assert.StartsWith("%PDF-1.4", content);
            Assert.Contains("%%EOF", content);
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
    public void Export_ShouldMapLowerModelYToLowerPdfPagePosition()
    {
        var document = new CadDocument();
        document.AddEntity(new LineEntity(
            new Point2D(0, 0),
            new Point2D(0, 100)));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Contains("293.712 813.543 m", content);
        Assert.Contains("293.712 28.346 l", content);
    }

    [Fact]
    public void Export_WhenTextIsRotated_ShouldInvertRotationForPdfPageCoordinates()
    {
        var document = new CadDocument();
        document.AddEntity(new TextEntity(
            new Point2D(10, 20),
            "Rotated",
            rotationDegrees: 90));
        var exporter = new PdfExporter();

        PdfExportResult result = exporter.Export(document);
        string content = ToAscii(result.Content);

        Assert.Contains("0 -1 1 0", content);
    }

    private static string ToAscii(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes);
    }
}
