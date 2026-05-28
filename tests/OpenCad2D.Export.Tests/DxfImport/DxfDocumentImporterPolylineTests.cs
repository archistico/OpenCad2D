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
    public void Import_WhenLightweightPolylineHasBulge_ShouldPreserveBulgeOnPolylineEntity()
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

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(new LayerId("Curves"), polyline.LayerId);
        Assert.Equal(2, polyline.Vertices.Count);
        Assert.False(polyline.IsClosed);
        Assert.True(polyline.HasArcSegments);
        double bulge = Assert.Single(polyline.SegmentBulges);
        Assert.Equal(1, bulge, precision: 6);
        Assert.Equal(new Point2D(0, 0), polyline.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), polyline.Vertices[1]);
        Assert.False(result.HasWarnings);
        Assert.Empty(result.Log);
    }

    [Fact]
    public void Import_WhenLightweightPolylineHasMixedStraightAndBulgeSegments_ShouldPreserveMixedSegmentsOnPolylineEntity()
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

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(result.Document.Entities.All));

        Assert.Equal(3, polyline.Vertices.Count);
        Assert.Equal(2, polyline.SegmentBulges.Count);
        Assert.Equal(new Point2D(0, 0), polyline.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), polyline.Vertices[1]);
        Assert.Equal(new Point2D(20, 0), polyline.Vertices[2]);
        Assert.Equal(0, polyline.SegmentBulges[0], precision: 6);
        Assert.Equal(1, polyline.SegmentBulges[1], precision: 6);
        Assert.True(polyline.HasArcSegments);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Import_WhenClosedLightweightPolylineHasLastVertexBulge_ShouldPreserveClosingSegmentBulge()
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

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(result.Document.Entities.All));

        Assert.True(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
        Assert.Equal(3, polyline.SegmentBulges.Count);
        Assert.Equal(0, polyline.SegmentBulges[0], precision: 6);
        Assert.Equal(0, polyline.SegmentBulges[1], precision: 6);
        Assert.Equal(-1, polyline.SegmentBulges[2], precision: 6);
        Assert.True(polyline.HasArcSegments);
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
