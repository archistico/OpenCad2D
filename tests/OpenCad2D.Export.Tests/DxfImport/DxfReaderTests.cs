using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfReaderTests
{
    [Fact]
    public void Read_ShouldReadCodeValuePairs()
    {
        const string content = "0\nSECTION\n2\nENTITIES\n0\nENDSEC\n0\nEOF\n";
        var reader = new DxfReader();

        IReadOnlyList<DxfCodePair> pairs = reader.Read(content);

        Assert.Equal(4, pairs.Count);
        Assert.Equal(0, pairs[0].Code);
        Assert.Equal("SECTION", pairs[0].Value);
        Assert.Equal(1, pairs[0].CodeLineNumber);
        Assert.Equal(2, pairs[0].ValueLineNumber);
        Assert.Equal(2, pairs[1].Code);
        Assert.Equal("ENTITIES", pairs[1].Value);
    }

    [Fact]
    public void Read_ShouldTrimGroupCodesAndValues()
    {
        const string content = "  0  \n  EOF  \n";
        var reader = new DxfReader();

        IReadOnlyList<DxfCodePair> pairs = reader.Read(content);

        Assert.Single(pairs);
        Assert.Equal(0, pairs[0].Code);
        Assert.Equal("EOF", pairs[0].Value);
    }

    [Fact]
    public void Read_WithInvalidGroupCode_ShouldThrowReadableException()
    {
        const string content = "X\nSECTION\n";
        var reader = new DxfReader();

        DxfReadException exception = Assert.Throws<DxfReadException>(() => reader.Read(content));

        Assert.Contains("line 1", exception.Message);
        Assert.Contains("Invalid DXF group code", exception.Message);
    }

    [Fact]
    public void Read_WithMissingValue_ShouldThrowReadableException()
    {
        const string content = "0\n";
        var reader = new DxfReader();

        DxfReadException exception = Assert.Throws<DxfReadException>(() => reader.Read(content));

        Assert.Contains("Missing DXF value", exception.Message);
        Assert.Contains("line 1", exception.Message);
    }
}
