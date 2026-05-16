using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Svg;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Export.Tests;

public sealed class BezierSplineExportTests
{
    [Fact]
    public void DxfExport_WhenDocumentContainsSpline_ShouldWriteSplineEntity()
    {
        var document = new CadDocument();
        document.AddEntity(new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            }));

        DxfExportResult result = new DxfExporter().Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("0\nSPLINE", content);
        Assert.Contains("73\n3", content);
    }

    [Fact]
    public void SvgExport_WhenDocumentContainsSpline_ShouldWritePolylineApproximation()
    {
        var document = new CadDocument();
        document.AddEntity(new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            }));

        SvgExportResult result = new SvgExporter().Export(document);

        Assert.Contains("<polyline", result.Content);
        Assert.Contains("points=\"", result.Content);
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
