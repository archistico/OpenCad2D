using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfDocumentImporterArcTests
{
    [Fact]
    public void Import_WhenDxfContainsArc_ShouldCreateArcEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            ARC
            8
            Arcs
            10
            12.5
            20
            25.5
            40
            7.25
            50
            15
            51
            120
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        ArcEntity arc = Assert.IsType<ArcEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(12.5, arc.Center.X);
        Assert.Equal(25.5, arc.Center.Y);
        Assert.Equal(7.25, arc.Radius);
        Assert.Equal(15, arc.StartAngle.Degrees, precision: 8);
        Assert.Equal(120, arc.EndAngle.Degrees, precision: 8);
        Assert.True(arc.IsCounterClockwise);
        Assert.Equal(new LayerId("Arcs"), arc.LayerId);
        Assert.True(result.Document.Layers.Contains(new LayerId("Arcs")));
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenArcHasMissingRadius_ShouldSkipArcAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            ARC
            10
            0
            20
            0
            50
            0
            51
            90
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Empty(result.Document.Entities.All);
        Assert.True(result.HasWarnings);
        DxfImportLogEntry warning = Assert.Single(result.Log);
        Assert.Equal(DxfImportLogSeverity.Warning, warning.Severity);
        Assert.Contains("group code 40", warning.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void Import_WhenArcRadiusIsNotPositive_ShouldSkipArcAndLogWarning(string radius)
    {
        string content = $$"""
            0
            SECTION
            2
            ENTITIES
            0
            ARC
            10
            0
            20
            0
            40
            {{radius}}
            50
            0
            51
            90
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Empty(result.Document.Entities.All);
        Assert.True(result.HasWarnings);
        DxfImportLogEntry warning = Assert.Single(result.Log);
        Assert.Contains("radius is less than or equal to zero", warning.Message);
    }

    [Fact]
    public void Import_WhenArcHasInvalidEndAngle_ShouldSkipArcAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            ARC
            10
            0
            20
            0
            40
            10
            50
            0
            51
            abc
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Empty(result.Document.Entities.All);
        Assert.True(result.HasWarnings);
        DxfImportLogEntry warning = Assert.Single(result.Log);
        Assert.Contains("ARC end angle", warning.Message);
        Assert.Contains("not a valid number", warning.Message);
    }

    [Theory]
    [InlineData("45", "45")]
    [InlineData("0", "360")]
    [InlineData("-90", "270")]
    public void Import_WhenArcStartAndEndAnglesAreEquivalent_ShouldSkipArcAndLogWarning(
        string startAngle,
        string endAngle)
    {
        string content = $$"""
            0
            SECTION
            2
            ENTITIES
            0
            ARC
            10
            0
            20
            0
            40
            10
            50
            {{startAngle}}
            51
            {{endAngle}}
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Empty(result.Document.Entities.All);
        Assert.True(result.HasWarnings);
        DxfImportLogEntry warning = Assert.Single(result.Log);
        Assert.Contains("start angle and end angle are equal", warning.Message);
    }
}
