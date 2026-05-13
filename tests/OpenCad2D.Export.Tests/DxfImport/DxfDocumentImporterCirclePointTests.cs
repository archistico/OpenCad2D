using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfDocumentImporterCirclePointTests
{
    [Fact]
    public void Import_WhenDxfContainsCircle_ShouldCreateCircleEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            CIRCLE
            8
            Circles
            10
            12.5
            20
            25.5
            40
            7.25
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        CircleEntity circle = Assert.IsType<CircleEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(12.5, circle.Center.X);
        Assert.Equal(25.5, circle.Center.Y);
        Assert.Equal(7.25, circle.Radius);
        Assert.Equal(new LayerId("Circles"), circle.LayerId);
        Assert.True(result.Document.Layers.Contains(new LayerId("Circles")));
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenCircleHasMissingRadius_ShouldSkipCircleAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            CIRCLE
            10
            0
            20
            0
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
    public void Import_WhenCircleRadiusIsNotPositive_ShouldSkipCircleAndLogWarning(string radius)
    {
        string content = $$"""
            0
            SECTION
            2
            ENTITIES
            0
            CIRCLE
            10
            0
            20
            0
            40
            {{radius}}
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
    public void Import_WhenCircleHasInvalidCenterCoordinate_ShouldSkipCircleAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            CIRCLE
            10
            abc
            20
            0
            40
            10
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
        Assert.Contains("CIRCLE center point X coordinate", warning.Message);
        Assert.Contains("not a valid number", warning.Message);
    }

    [Fact]
    public void Import_WhenDxfContainsPoint_ShouldCreatePointEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            POINT
            8
            Survey
            10
            3.5
            20
            4.5
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        PointEntity point = Assert.IsType<PointEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(3.5, point.Position.X);
        Assert.Equal(4.5, point.Position.Y);
        Assert.Equal(new LayerId("Survey"), point.LayerId);
        Assert.True(result.Document.Layers.Contains(new LayerId("Survey")));
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenPointHasMissingCoordinate_ShouldSkipPointAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            POINT
            10
            3.5
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
        Assert.Contains("POINT position Y coordinate", warning.Message);
        Assert.Contains("group code 20", warning.Message);
    }

    [Fact]
    public void Import_WhenFileContainsLineCircleAndPoint_ShouldImportAllSupportedEntities()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            10
            0
            20
            0
            11
            10
            21
            0
            0
            CIRCLE
            10
            5
            20
            5
            40
            2
            0
            POINT
            10
            7
            20
            8
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Collection(
            result.Document.Entities.All,
            entity => Assert.IsType<LineEntity>(entity),
            entity => Assert.IsType<CircleEntity>(entity),
            entity => Assert.IsType<PointEntity>(entity));
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }
}
