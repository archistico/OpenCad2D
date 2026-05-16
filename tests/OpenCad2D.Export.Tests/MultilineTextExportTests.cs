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
    }
}
