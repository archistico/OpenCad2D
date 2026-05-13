using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfSectionReaderTests
{
    [Fact]
    public void ReadSections_ShouldReturnNamedSectionsWithoutMarkers()
    {
        const string content = """
            0
            SECTION
            2
            HEADER
            9
            $ACADVER
            1
            AC1015
            0
            ENDSEC
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            8
            0
            0
            ENDSEC
            0
            EOF
            """;

        IReadOnlyList<DxfCodePair> pairs = new DxfReader().Read(content);
        var sectionReader = new DxfSectionReader();

        IReadOnlyList<DxfSection> sections = sectionReader.ReadSections(pairs);

        Assert.Equal(2, sections.Count);
        Assert.Equal("HEADER", sections[0].Name);
        Assert.Equal("ENTITIES", sections[1].Name);
        Assert.DoesNotContain(sections[0].Pairs, pair => pair.IsMarkerValue("SECTION"));
        Assert.DoesNotContain(sections[0].Pairs, pair => pair.IsMarkerValue("ENDSEC"));
        Assert.Contains(sections[1].Pairs, pair => pair.IsMarkerValue("LINE"));
    }

    [Fact]
    public void ReadSections_WithMissingSectionName_ShouldThrowReadableException()
    {
        const string content = "0\nSECTION\n0\nENDSEC\n";
        IReadOnlyList<DxfCodePair> pairs = new DxfReader().Read(content);
        var sectionReader = new DxfSectionReader();

        DxfReadException exception = Assert.Throws<DxfReadException>(() => sectionReader.ReadSections(pairs));

        Assert.Contains("missing its name", exception.Message);
    }

    [Fact]
    public void ReadSections_WithMissingEndSection_ShouldThrowReadableException()
    {
        const string content = "0\nSECTION\n2\nENTITIES\n0\nLINE\n";
        IReadOnlyList<DxfCodePair> pairs = new DxfReader().Read(content);
        var sectionReader = new DxfSectionReader();

        DxfReadException exception = Assert.Throws<DxfReadException>(() => sectionReader.ReadSections(pairs));

        Assert.Contains("missing ENDSEC", exception.Message);
        Assert.Contains("ENTITIES", exception.Message);
    }

    [Fact]
    public void ReadSections_ShouldIgnoreTopLevelEofAndUnknownPairs()
    {
        const string content = "999\nComment\n0\nEOF\n";
        IReadOnlyList<DxfCodePair> pairs = new DxfReader().Read(content);
        var sectionReader = new DxfSectionReader();

        IReadOnlyList<DxfSection> sections = sectionReader.ReadSections(pairs);

        Assert.Empty(sections);
    }
}
