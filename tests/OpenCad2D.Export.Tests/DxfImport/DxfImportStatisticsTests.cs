using OpenCad2D.Core.Entities;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfImportStatisticsTests
{
    [Fact]
    public void Import_WhenFileContainsSupportedEntities_ShouldExposeImportStatistics()
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
            LINE
            10
            0
            20
            1
            11
            10
            21
            1
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Equal(3, result.Statistics.TotalImportedEntities);
        Assert.Equal(2, result.Statistics.GetImportedEntityCount(EntityKind.Line));
        Assert.Equal(1, result.Statistics.GetImportedEntityCount(EntityKind.Circle));
        Assert.Equal(0, result.Statistics.GetImportedEntityCount(EntityKind.Arc));
        Assert.Equal(1, result.Statistics.ImportedLayerCount);
        Assert.Equal(0, result.Statistics.WarningCount);
        Assert.Equal(0, result.Statistics.ErrorCount);
    }

    [Fact]
    public void Import_WhenFileContainsSkippedEntity_ShouldExposeWarningStatistics()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            HATCH
            8
            HatchLayer
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Equal(0, result.Statistics.TotalImportedEntities);
        Assert.Equal(1, result.Statistics.WarningCount);
        Assert.Equal(0, result.Statistics.ErrorCount);
        Assert.Equal(1, result.Statistics.SkippedEntityWarningCount);
        Assert.True(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenDxfIsMalformed_ShouldReturnErrorResultInsteadOfThrowing()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LINE
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Empty(result.Document.Entities.All);
        Assert.False(result.HasWarnings);
        Assert.True(result.HasErrors);
        Assert.Equal(1, result.Statistics.ErrorCount);
        Assert.Contains("missing ENDSEC", Assert.Single(result.Log).Message);
    }

    [Fact]
    public void Import_WhenDxfHasInvalidGroupCode_ShouldReturnErrorResultInsteadOfThrowing()
    {
        const string content = """
            X
            SECTION
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Empty(result.Document.Entities.All);
        Assert.True(result.HasErrors);
        Assert.Equal(1, result.Statistics.ErrorCount);
        Assert.Contains("Invalid DXF group code", Assert.Single(result.Log).Message);
    }
}
