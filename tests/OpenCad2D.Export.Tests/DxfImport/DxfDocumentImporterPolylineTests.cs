using System.Linq;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfDocumentImporterPolylineTests
{
    [Fact]
    public void Import_WhenDxfContainsLightweightPolyline_ShouldCreatePolylineEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            8
            Walls
            90
            3
            70
            0
            10
            0
            20
            0
            10
            100
            20
            0
            10
            100
            20
            50
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(3, polyline.Vertices.Count);
        Assert.Equal(0, polyline.Vertices[0].X);
        Assert.Equal(0, polyline.Vertices[0].Y);
        Assert.Equal(100, polyline.Vertices[1].X);
        Assert.Equal(0, polyline.Vertices[1].Y);
        Assert.Equal(100, polyline.Vertices[2].X);
        Assert.Equal(50, polyline.Vertices[2].Y);
        Assert.False(polyline.IsClosed);
        Assert.Equal(new LayerId("Walls"), polyline.LayerId);
        Assert.True(result.Document.Layers.Contains(new LayerId("Walls")));
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenLightweightPolylineHasClosedFlag_ShouldCreateClosedPolylineEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            70
            1
            10
            0
            20
            0
            10
            10
            20
            0
            10
            10
            20
            10
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.True(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenLightweightPolylineHasBulge_ShouldConvertCurvedSegmentToArcEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            8
            Curves
            90
            2
            10
            0
            20
            0
            42
            1
            10
            10
            20
            0
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        ArcEntity arc = Assert.IsType<ArcEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(new LayerId("Curves"), arc.LayerId);
        Assert.Equal(5, arc.Center.X, precision: 6);
        Assert.Equal(0, arc.Center.Y, precision: 6);
        Assert.Equal(5, arc.Radius, precision: 6);
        Assert.Equal(180, arc.StartAngle.NormalizePositive().Degrees, precision: 6);
        Assert.Equal(0, arc.EndAngle.NormalizePositive().Degrees, precision: 6);
        Assert.True(arc.IsCounterClockwise);
        Assert.False(result.HasWarnings);
        DxfImportLogEntry info = Assert.Single(result.Log);
        Assert.Equal(DxfImportLogSeverity.Info, info.Severity);
        Assert.Contains("bulge geometry", info.Message);
    }

    [Fact]
    public void Import_WhenLightweightPolylineHasMixedStraightAndBulgeSegments_ShouldConvertSegmentsToLinesAndArcs()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            90
            3
            70
            0
            10
            0
            20
            0
            10
            10
            20
            0
            42
            1
            10
            20
            20
            0
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        IReadOnlyList<CadEntity> entities = result.Document.Entities.All.ToList();

        Assert.Equal(2, entities.Count);
        LineEntity line = Assert.IsType<LineEntity>(entities[0]);
        ArcEntity arc = Assert.IsType<ArcEntity>(entities[1]);
        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
        Assert.Equal(15, arc.Center.X, precision: 6);
        Assert.Equal(0, arc.Center.Y, precision: 6);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenClosedLightweightPolylineHasLastVertexBulge_ShouldConvertClosingSegmentToArc()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            90
            3
            70
            1
            10
            0
            20
            0
            10
            10
            20
            0
            10
            10
            20
            10
            42
            -1
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        IReadOnlyList<CadEntity> entities = result.Document.Entities.All.ToList();

        Assert.Equal(3, entities.Count);
        Assert.IsType<LineEntity>(entities[0]);
        Assert.IsType<LineEntity>(entities[1]);
        ArcEntity closingArc = Assert.IsType<ArcEntity>(entities[2]);
        Assert.False(closingArc.IsCounterClockwise);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenLightweightPolylineHasTooFewVertices_ShouldSkipPolylineAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            90
            1
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
        Assert.Contains("fewer than two valid vertices", warning.Message);
    }

    [Fact]
    public void Import_WhenLightweightPolylineHasInvalidVertexCoordinate_ShouldSkipPolylineAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            10
            0
            20
            0
            10
            invalid
            20
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
        Assert.Contains("not a valid number", warning.Message);
    }

    [Fact]
    public void Import_WhenLightweightPolylineVertexIsMissingYCoordinate_ShouldSkipPolylineAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            LWPOLYLINE
            10
            0
            20
            0
            10
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
        Assert.Contains("missing its matching Y coordinate", warning.Message);
    }
}
