using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Svg;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

public sealed class TextExportTests
{
    [Fact]
    public void SvgExport_WhenDocumentContainsText_ShouldWriteTextElement()
    {
        var document = new CadDocument();
        document.AddEntity(new TextEntity(
            new Point2D(0, 0),
            "A&B",
            30,
            TextFormatId.Standard));

        var exporter = new SvgExporter();

        SvgExportResult result = exporter.Export(document);

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("<text ", result.Content);
        Assert.Contains("font-family=\"Arial\"", result.Content);
        Assert.Contains("font-size=\"2.5\"", result.Content);
        Assert.Contains("A&amp;B", result.Content);
        Assert.Contains("rotate(-30", result.Content);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsText_ShouldWriteTextEntity()
    {
        var document = new CadDocument();
        document.AddEntity(new TextEntity(
            new Point2D(10, 20),
            "Room",
            45,
            TextFormatId.Standard));

        var exporter = new DxfExporter();

        DxfExportResult result = exporter.Export(document);
        string content = result.Content.Replace("\r\n", "\n");

        Assert.Equal(1, result.ExportedEntityCount);
        Assert.Contains("0\nTEXT", content);
        Assert.Contains("1\nRoom", content);
        Assert.Contains("40\n2.5", content);
        Assert.Contains("50\n315", content);
        Assert.Contains("7\nStandard", content);
    }
}
