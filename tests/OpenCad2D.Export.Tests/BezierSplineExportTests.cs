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
    public void DxfExport_WhenDocumentContainsSpline_ShouldWriteOpenUniformKnotVector()
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

        Assert.Contains("71\n2", content);
        Assert.Contains("72\n6", content);
        string splineEntity = ExtractFirstEntityContent(
            content,
            "SPLINE");

        Assert.Equal(6, CountGroupOccurrences(splineEntity, 40));
        Assert.Contains("40\n0\n40\n0\n40\n0\n40\n1\n40\n1\n40\n1", splineEntity);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsCubicSpline_ShouldWriteExpectedKnotCount()
    {
        var document = new CadDocument();
        document.AddEntity(new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(3, 9),
                new Point2D(6, 9),
                new Point2D(9, 0)
            }));

        DxfExportResult result = new DxfExporter().Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("71\n3", content);
        Assert.Contains("72\n8", content);
        string splineEntity = ExtractFirstEntityContent(
            content,
            "SPLINE");

        Assert.Equal(8, CountGroupOccurrences(splineEntity, 40));
        Assert.Contains("40\n0\n40\n0\n40\n0\n40\n0\n40\n1\n40\n1\n40\n1\n40\n1", splineEntity);
    }

    [Fact]
    public void DxfExport_WhenDocumentContainsClosedSpline_ShouldWriteClosedPlanarFlagWithoutPeriodicFlag()
    {
        var document = new CadDocument();
        document.AddEntity(new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            },
            isClosed: true));

        DxfExportResult result = new DxfExporter().Export(document);
        string content = Normalize(result.Content);

        Assert.Contains("70\n9", content);
        Assert.DoesNotContain("70\n11", content);
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

    private static string ExtractFirstEntityContent(
        string content,
        string entityType)
    {
        string[] lines = content.Split('\n');

        for (int index = 0; index < lines.Length - 1; index += 2)
        {
            if (!string.Equals(
                    lines[index],
                    "0",
                    StringComparison.Ordinal) ||
                !string.Equals(
                    lines[index + 1],
                    entityType,
                    StringComparison.Ordinal))
            {
                continue;
            }

            int endIndex = lines.Length;

            for (int candidateEndIndex = index + 2; candidateEndIndex < lines.Length - 1; candidateEndIndex += 2)
            {
                if (string.Equals(
                    lines[candidateEndIndex],
                    "0",
                    StringComparison.Ordinal))
                {
                    endIndex = candidateEndIndex;
                    break;
                }
            }

            return string.Join(
                '\n',
                lines[index..endIndex]);
        }

        Assert.Fail($"Expected DXF entity {entityType} was not found.");
        return string.Empty;
    }

    private static int CountGroupOccurrences(
        string content,
        int groupCode)
    {
        return content.Split(
                $"\n{groupCode}\n",
                StringSplitOptions.None)
            .Length - 1;
    }

    private static string Normalize(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
