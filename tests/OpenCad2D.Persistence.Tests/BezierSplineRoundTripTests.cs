using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Persistence;
using OpenCad2D.Persistence.Dto;

namespace OpenCad2D.Persistence.Tests;

public sealed class BezierSplineRoundTripTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveBezierSplineEntity()
    {
        var document = new CadDocument();
        var entity = new BezierSplineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 10),
                new Point2D(10, 0)
            },
            isClosed: true);
        document.AddEntity(entity);

        var serializer = new JsonDocumentSerializer();
        DocumentDto dto = serializer.Serialize(
            document,
            LayerId.Default.Value,
            new ViewportStateDto());

        CadDocument restored = serializer.Deserialize(
            dto,
            out string _,
            out ViewportStateDto _);

        BezierSplineEntity restoredSpline = Assert.IsType<BezierSplineEntity>(
            restored.Entities.GetRequired(entity.Id));
        Assert.True(restoredSpline.IsClosed);
        Assert.Equal(3, restoredSpline.ControlPoints.Count);
        Assert.Equal(new Point2D(5, 10), restoredSpline.ControlPoints[1]);
    }
}
