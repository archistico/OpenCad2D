using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Export.Dxf.Import;

namespace OpenCad2D.Export.Tests.DxfImport;

public sealed class DxfDocumentImporterEllipseTests
{
    [Fact]
    public void Import_WhenDxfContainsFullEllipse_ShouldCreateEllipseEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            ELLIPSE
            8
            Curves
            10
            5
            20
            6
            11
            10
            21
            0
            40
            0.4
            41
            0
            42
            6.283185307179586
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        EllipseEntity ellipse = Assert.IsType<EllipseEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(new LayerId("Curves"), ellipse.LayerId);
        Assert.Equal(5, ellipse.Center.X, precision: 6);
        Assert.Equal(6, ellipse.Center.Y, precision: 6);
        Assert.Equal(10, ellipse.MajorAxis.X, precision: 6);
        Assert.Equal(0, ellipse.MajorAxis.Y, precision: 6);
        Assert.Equal(4, ellipse.MinorRadius, precision: 6);
        Assert.False(result.HasWarnings);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Import_WhenFullEllipseOmitsParameters_ShouldCreateEllipseEntity()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            ELLIPSE
            10
            0
            20
            0
            11
            0
            21
            8
            40
            0.5
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        EllipseEntity ellipse = Assert.IsType<EllipseEntity>(Assert.Single(result.Document.Entities.All));
        Assert.Equal(0, ellipse.MajorAxis.X, precision: 6);
        Assert.Equal(8, ellipse.MajorAxis.Y, precision: 6);
        Assert.Equal(4, ellipse.MinorRadius, precision: 6);
        Assert.Empty(result.Log);
    }

    [Fact]
    public void Import_WhenDxfContainsPartialEllipse_ShouldCreateOpenPolylineApproximation()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            ELLIPSE
            8
            PartialCurves
            10
            5
            20
            6
            11
            10
            21
            0
            40
            0.4
            41
            0
            42
            1.5707963267948966
            0
            ENDSEC
            0
            EOF
            """;
        var importer = new DxfDocumentImporter();

        DxfImportResult result = importer.Import(content);

        PolylineEntity polyline = Assert.IsType<PolylineEntity>(Assert.Single(result.Document.Entities.All));
        Assert.False(polyline.IsClosed);
        Assert.Equal(new LayerId("PartialCurves"), polyline.LayerId);
        Assert.True(polyline.Vertices.Count >= 9);
        Assert.Equal(15, polyline.Vertices[0].X, precision: 6);
        Assert.Equal(6, polyline.Vertices[0].Y, precision: 6);
        Assert.Equal(5, polyline.Vertices[^1].X, precision: 6);
        Assert.Equal(10, polyline.Vertices[^1].Y, precision: 6);

        DxfImportLogEntry info = Assert.Single(result.Log);
        Assert.Equal(DxfImportLogSeverity.Info, info.Severity);
        Assert.Contains("partial ELLIPSE", info.Message);
    }

    [Fact]
    public void Import_WhenEllipseHasZeroMajorAxis_ShouldSkipAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            ELLIPSE
            10
            0
            20
            0
            11
            0
            21
            0
            40
            0.5
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
        Assert.Contains("major axis vector", warning.Message);
    }

    [Fact]
    public void Import_WhenEllipseHasInvalidRatio_ShouldSkipAndLogWarning()
    {
        const string content = """
            0
            SECTION
            2
            ENTITIES
            0
            ELLIPSE
            10
            0
            20
            0
            11
            10
            21
            0
            40
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
        Assert.Contains("axis ratio", warning.Message);
    }
}
