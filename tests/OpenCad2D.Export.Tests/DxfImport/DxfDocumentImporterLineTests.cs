using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfDocumentImporterLineTests
{
    [Fact]
    public void Import_WhenDxfContainsLine_ShouldCreateLineEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            8
            0
            10
            1.5
            20
            2.5
            11
            30
            21
            40
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        LineEntity line = Assert.IsType<LineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(1.5, line.Start.X);
        Assert.Equal(2.5, line.Start.Y);
        Assert.Equal(30, line.End.X);
        Assert.Equal(40, line.End.Y);
        Assert.Equal(LayerId.Default, line.LayerId);
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenLineUsesCustomLayer_ShouldCreateMissingLayer()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            8
            Walls
            10
            0
            20
            0
            11
            100
            21
            0
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        LineEntity line = Assert.IsType<LineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(new LayerId("Walls"), line.LayerId);
        Assert.True(result.Document.Layers.Contains(new LayerId("Walls")));
        Assert.Equal("Walls", result.Document.Layers.GetRequired(new LayerId("Walls")).Name);
    }

    [Fact]
    public void Import_WhenLineHasNoLayer_ShouldUseDefaultLayer()
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
            100
            21
            0
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        LineEntity line = Assert.IsType<LineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(LayerId.Default, line.LayerId);
    }

    [Fact]
    public void Import_WhenLineIsMissingRequiredCoordinate_ShouldSkipLineAndLogWarning()
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
            100
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
        Assert.Contains("group code 21", warning.Message);
    }

    [Fact]
    public void Import_WhenLineHasInvalidNumber_ShouldSkipLineAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            10
            abc
            20
            0
            11
            100
            21
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
        Assert.Contains("not a valid number", warning.Message);
    }

    [Fact]
    public void Import_WhenLineHasEqualStartAndEnd_ShouldSkipLineAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            10
            10
            20
            20
            11
            10
            21
            20
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
        Assert.Contains("start point and end point are equal", warning.Message);
    }

    [Fact]
    public void Import_WhenEntityIsUnsupported_ShouldSkipEntityAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            CIRCLE
            8
            0
            10
            0
            20
            0
            40
            5
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
        Assert.Contains("Skipped unsupported DXF entity: CIRCLE", warning.Message);
    }

    [Fact]
    public void Import_WhenEntitiesSectionIsMissing_ShouldReturnEmptyDocumentWithWarning()
    {
        const string content = """
            0
            SECTION
            2
            HEADER
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Empty(result.Document.Entities.All);
        Assert.True(result.HasWarnings);
        Assert.Contains("does not contain an ENTITIES section", Assert.Single(result.Log).Message);
    }
}
