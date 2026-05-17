using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Export.Svg;

namespace OpenCad2D.Export.Tests;

public sealed class MultilineTextExportTests
{
    [Fact]
    public void SvgExport_WhenDocumentContainsMultilineText_ShouldWriteTspans()
    {
        var document = new CadDocument();
        document.AddEntity(new MultilineTextEntity(new Point2D(10, 20), "First\nSecond"));

        string svg = new SvgExporter().Export(document).Content;

        Assert.Contains("<text", svg);
        Assert.Contains("<tspan", svg);
        Assert.Contains("First", svg);
        Assert.Contains("Second", svg);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsMultilineText_ShouldWriteMTextEntity()
    {
        var document = new CadDocument();
        document.AddEntity(new MultilineTextEntity(new Point2D(10, 20), "First\nSecond"));

        string dxf = new DxfExporter().Export(document).Content;

        Assert.Contains("MTEXT", dxf);
        Assert.Contains("First\\PSecond", dxf);
        Assert.Contains("40\n10", Normalize(dxf));
        Assert.Contains("41\n0", Normalize(dxf));
    }

    [Fact]
    public void DxfExport_WhenMultilineTextHasReferenceWidth_ShouldWriteGroup41()
    {
        var document = new CadDocument();
        document.AddEntity(new MultilineTextEntity(
            new Point2D(10, 20),
            "First\nSecond",
            referenceWidth: 125));

        string dxf = Normalize(new DxfExporter().Export(document).Content);

        Assert.Contains("0\nMTEXT", dxf);
        Assert.Contains("41\n125", dxf);
    }

    [Fact]
    public void DxfImport_WhenDocumentContainsMText_ShouldCreateMultilineTextEntity()
    {
        const string dxf = "0\nSECTION\n2\nENTITIES\n0\nMTEXT\n8\nNotes\n10\n10\n20\n20\n40\n2.5\n1\nFirst\\PSecond\n50\n15\n0\nENDSEC\n0\nEOF";

        DxfImportResult result = new DxfDocumentImporter().Import(dxf);

        MultilineTextEntity text = Assert.IsType<MultilineTextEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(new Point2D(10, 20), text.InsertionPoint);
        Assert.Equal("First\nSecond", text.Text);
        Assert.Equal(15, text.RotationDegrees, 6);
        Assert.Equal(0, text.ReferenceWidth);
    }

    [Fact]
    public void DxfImport_WhenMTextContainsReferenceWidth_ShouldPreserveIt()
    {
        const string dxf = "0\nSECTION\n2\nENTITIES\n0\nMTEXT\n8\nNotes\n10\n10\n20\n20\n40\n2.5\n41\n90\n1\nFirst\\PSecond\n0\nENDSEC\n0\nEOF";

        DxfImportResult result = new DxfDocumentImporter().Import(dxf);

        MultilineTextEntity text = Assert.IsType<MultilineTextEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(90, text.ReferenceWidth);
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
