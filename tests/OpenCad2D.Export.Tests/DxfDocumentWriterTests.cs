using OpenCad2D.Export.Dxf;

namespace OpenCad2D.Export.Tests;

public sealed class DxfDocumentWriterTests
{
    [Fact]
    public void WriteGroup_WithString_ShouldWriteCodeValuePair()
    {
        var writer = new DxfDocumentWriter();

        writer.WriteGroup(0, "SECTION");

        string content = writer.ToString();

        Assert.Contains("0\nSECTION", Normalize(content));
    }

    [Fact]
    public void WriteGroup_WithDouble_ShouldUseInvariantCulture()
    {
        var writer = new DxfDocumentWriter();

        writer.WriteGroup(10, 12.5);

        string content = writer.ToString();

        Assert.Contains("10\n12.5", Normalize(content));
        Assert.DoesNotContain("12,5", content);
    }

    [Fact]
    public void BeginSectionAndEndSection_ShouldWriteSectionMarkers()
    {
        var writer = new DxfDocumentWriter();

        writer.BeginSection("HEADER");
        writer.EndSection();

        string content = Normalize(writer.ToString());

        Assert.Contains("0\nSECTION\n2\nHEADER", content);
        Assert.Contains("0\nENDSEC", content);
    }

    [Fact]
    public void WriteEndOfFile_ShouldWriteEofMarker()
    {
        var writer = new DxfDocumentWriter();

        writer.WriteEndOfFile();

        Assert.EndsWith("0\nEOF\n", Normalize(writer.ToString()));
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n");
    }
}
