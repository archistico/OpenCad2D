using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfDocumentImporterLayerTableTests
{
    [Fact]
    public void Import_WhenDxfContainsLayerTable_ShouldCreateLayerBeforeEntities()
    {
        const string content = """
            0
            SECTION
            2
            TABLES
            0
            TABLE
            2
            LAYER
            70
            1
            0
            LAYER
            2
            Walls
            70
            0
            62
            1
            6
            DASHED
            370
            35
            0
            ENDTAB
            0
            ENDSEC
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
            10
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

        Layer layer = result.Document.Layers.GetRequired(new LayerId("Walls"));
        Assert.Equal("Walls", layer.Name);
        Assert.Equal(LineFormatId.Dashed, layer.LineFormatId);
        Assert.Equal(CadColor.FromRgb(255, 0, 0), layer.Color);
        Assert.Equal(0.35, layer.LineWeight.Millimeters);
        Assert.True(layer.IsVisible);
        Assert.False(layer.IsLocked);
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenLayerColorIsNegative_ShouldImportLayerAsHidden()
    {
        const string content = """
            0
            SECTION
            2
            TABLES
            0
            TABLE
            2
            LAYER
            0
            LAYER
            2
            HiddenNotes
            62
            -3
            0
            ENDTAB
            0
            ENDSEC
            0
            SECTION
            2
            ENTITIES
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Layer layer = result.Document.Layers.GetRequired(new LayerId("HiddenNotes"));
        Assert.Equal(CadColor.FromRgb(0, 255, 0), layer.Color);
        Assert.False(layer.IsVisible);
        Assert.False(layer.IsLocked);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenLayerFlagIsFrozen_ShouldImportLayerAsHidden()
    {
        const string content = """
            0
            SECTION
            2
            TABLES
            0
            TABLE
            2
            LAYER
            0
            LAYER
            2
            FrozenLayer
            70
            1
            62
            5
            0
            ENDTAB
            0
            ENDSEC
            0
            SECTION
            2
            ENTITIES
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Layer layer = result.Document.Layers.GetRequired(new LayerId("FrozenLayer"));
        Assert.Equal(CadColor.FromRgb(0, 0, 255), layer.Color);
        Assert.False(layer.IsVisible);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenLayerFlagIsLocked_ShouldImportLayerAsLocked()
    {
        const string content = """
            0
            SECTION
            2
            TABLES
            0
            TABLE
            2
            LAYER
            0
            LAYER
            2
            LockedLayer
            70
            4
            62
            4
            6
            CENTER
            0
            ENDTAB
            0
            ENDSEC
            0
            SECTION
            2
            ENTITIES
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Layer layer = result.Document.Layers.GetRequired(new LayerId("LockedLayer"));
        Assert.Equal(CadColor.FromRgb(0, 255, 255), layer.Color);
        Assert.Equal(LineFormatId.Axis, layer.LineFormatId);
        Assert.True(layer.IsVisible);
        Assert.True(layer.IsLocked);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenLayerRecordHasEmptyName_ShouldSkipLayerAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            TABLES
            0
            TABLE
            2
            LAYER
            0
            LAYER
            2
               
            62
            1
            0
            ENDTAB
            0
            ENDSEC
            0
            SECTION
            2
            ENTITIES
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Assert.Equal(1, result.Document.Layers.Count);
        Assert.True(result.HasWarnings);
        DxfImportLogEntry warning = Assert.Single(result.Log);
        Assert.Contains("layer name is missing or empty", warning.Message);
    }

    [Fact]
    public void Import_WhenEntityUsesUndeclaredLayer_ShouldStillCreateLayerAutomatically()
    {
        const string content = """
            0
            SECTION
            2
            TABLES
            0
            TABLE
            2
            LAYER
            0
            LAYER
            2
            DeclaredLayer
            62
            2
            0
            ENDTAB
            0
            ENDSEC
            0
            SECTION
            2
            ENTITIES
            0
            POINT
            8
            UndeclaredLayer
            10
            5
            20
            6
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        PointEntity point = Assert.IsType<PointEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(new LayerId("UndeclaredLayer"), point.LayerId);
        Assert.True(result.Document.Layers.Contains(new LayerId("DeclaredLayer")));
        Assert.True(result.Document.Layers.Contains(new LayerId("UndeclaredLayer")));
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenLayerTableContainsDefaultLayer_ShouldReplaceDefaultLayerAppearance()
    {
        const string content = """
            0
            SECTION
            2
            TABLES
            0
            TABLE
            2
            LAYER
            0
            LAYER
            2
            0
            62
            6
            6
            DASHDOT
            370
            50
            0
            ENDTAB
            0
            ENDSEC
            0
            SECTION
            2
            ENTITIES
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        Layer layer = result.Document.Layers.Default;
        Assert.Equal(CadColor.FromRgb(255, 0, 255), layer.Color);
        Assert.Equal(LineFormatId.DashDot, layer.LineFormatId);
        Assert.Equal(0.5, layer.LineWeight.Millimeters);
        Assert.True(layer.IsVisible);
        Assert.False(result.HasWarnings);
    }
}
