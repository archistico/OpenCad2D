using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfDocumentImporterTextTests
{
    [Fact]
    public void Import_WhenDxfContainsText_ShouldCreateTextEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            TEXT
            8
            Notes
            10
            12.5
            20
            25.5
            40
            3.5
            1
            Hello OpenCad2D
            50
            30
            7
            Standard
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        TextEntity text = Assert.IsType<TextEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(12.5, text.InsertionPoint.X);
        Assert.Equal(25.5, text.InsertionPoint.Y);
        Assert.Equal("Hello OpenCad2D", text.Text);
        Assert.Equal(30, text.RotationDegrees);
        Assert.Equal(TextFormatId.Standard, text.TextFormatId);
        Assert.Equal(new LayerId("Notes"), text.LayerId);
        Assert.True(result.Document.Layers.Contains(new LayerId("Notes")));
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenTextHasNoLayer_ShouldUseDefaultLayer()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            TEXT
            10
            0
            20
            0
            1
            Default layer text
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        TextEntity text = Assert.IsType<TextEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(LayerId.Default, text.LayerId);
        Assert.Equal(0, text.RotationDegrees);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenTextValueIsMissing_ShouldSkipTextAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            TEXT
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
        Assert.Contains("TEXT value", warning.Message);
        Assert.Contains("group code 1", warning.Message);
    }

    [Fact]
    public void Import_WhenTextValueIsEmpty_ShouldSkipTextAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            TEXT
            10
            0
            20
            0
            1
               
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
        Assert.Contains("text value is empty", warning.Message);
    }

    [Fact]
    public void Import_WhenTextInsertionPointIsMissingCoordinate_ShouldSkipTextAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            TEXT
            10
            3
            1
            Missing Y
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
        Assert.Contains("TEXT insertion point Y coordinate", warning.Message);
        Assert.Contains("group code 20", warning.Message);
    }

    [Fact]
    public void Import_WhenTextRotationIsInvalid_ShouldSkipTextAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            TEXT
            10
            0
            20
            0
            1
            Invalid rotation
            50
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
        Assert.Contains("TEXT rotation", warning.Message);
        Assert.Contains("not a valid number", warning.Message);
    }

    [Fact]
    public void Import_WhenFileContainsAllBaseEntities_ShouldImportAllSupportedEntities()
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
            1
            20
            1
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
            90
            0
            LWPOLYLINE
            10
            0
            20
            0
            10
            3
            20
            3
            0
            TEXT
            10
            2
            20
            2
            1
            Label
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
            entity => Assert.IsType<PointEntity>(entity),
            entity => Assert.IsType<ArcEntity>(entity),
            entity => Assert.IsType<PolylineEntity>(entity),
            entity => Assert.IsType<TextEntity>(entity));
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }
}
