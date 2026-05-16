using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfDocumentImporterSplineTests
{
    [Fact]
    public void Import_WhenDxfContainsSplineControlPoints_ShouldCreateBezierSplineEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            SPLINE
            8
            Curves
            70
            8
            71
            3
            73
            4
            10
            0
            20
            0
            30
            0
            10
            5
            20
            10
            30
            0
            10
            10
            20
            10
            30
            0
            10
            15
            20
            0
            30
            0
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        BezierSplineEntity spline = Assert.IsType<BezierSplineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(new LayerId("Curves"), spline.LayerId);
        Assert.False(spline.IsClosed);
        Assert.Equal(4, spline.ControlPoints.Count);
        Assert.Equal(0, spline.ControlPoints[0].X, precision: 6);
        Assert.Equal(0, spline.ControlPoints[0].Y, precision: 6);
        Assert.Equal(15, spline.ControlPoints[^1].X, precision: 6);
        Assert.Equal(0, spline.ControlPoints[^1].Y, precision: 6);
        DxfImportLogEntry info = Assert.Single(result.Log);
        Assert.Equal(DxfImportLogSeverity.Info, info.Severity);
        Assert.Contains("BezierSplineEntity", info.Message);
    }

    [Fact]
    public void Import_WhenDxfContainsClosedSplineControlPoints_ShouldCreateClosedBezierSplineEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            SPLINE
            70
            11
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
            10
            0
            20
            10
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        BezierSplineEntity spline = Assert.IsType<BezierSplineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.True(spline.IsClosed);
        Assert.Equal(4, spline.ControlPoints.Count);
    }

    [Fact]
    public void Import_WhenDxfContainsSplineFitPointsOnly_ShouldCreatePolylineApproximation()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            SPLINE
            8
            FitCurves
            70
            0
            11
            0
            21
            0
            11
            5
            21
            4
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

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(new LayerId("FitCurves"), polyline.LayerId);
        Assert.False(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
        DxfImportLogEntry info = Assert.Single(result.Log);
        Assert.Equal(DxfImportLogSeverity.Info, info.Severity);
        Assert.Contains("fit points", info.Message);
    }

    [Fact]
    public void Import_WhenSplineHasTooFewReadablePoints_ShouldSkipAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            SPLINE
            70
            8
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
        Assert.Contains("at least two", warning.Message);
    }

    [Fact]
    public void Import_WhenSplineControlPointMissesY_ShouldSkipAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            SPLINE
            10
            0
            10
            5
            20
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
        Assert.Contains(result.Log, entry => entry.Message.Contains("missing its matching Y coordinate", StringComparison.Ordinal));
    }
}
