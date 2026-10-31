using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadExtendRegressionTests
{
    [Theory]
    [InlineData("line")]
    [InlineData("arc")]
    [InlineData("ellipse arc")]
    [InlineData("polyline")]
    public void ExtendToBoundary_EquidistantPick_ShouldRejectAmbiguousSide(string kind)
    {
        CadEntity target = kind switch
        {
            "arc" => new ArcEntity(new Point2D(0, 0), 5, Angle.FromDegrees(0), Angle.FromDegrees(180)),
            "ellipse arc" => new EllipticalArcEntity(new Point2D(0, 0), new Vector2D(10, 0), 5, 0, Math.PI),
            "polyline" => new PolylineEntity(new[] { new Point2D(-5, 0), new Point2D(5, 0) }),
            _ => new LineEntity(new Point2D(-5, 0), new Point2D(5, 0))
        };
        Point2D pick = kind is "arc" or "ellipse arc" ? new Point2D(0, 5) : new Point2D(0, 0);
        var boundary = new LineEntity(new Point2D(-20, -1), new Point2D(20, -1));
        Assert.False(CadExtendService.TryGetPickedEndpoint(target, pick, out _));
        Assert.Null(CadExtendService.ExtendToBoundary(target, boundary, pick));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtendToBoundaries_EndpointAlreadyOnBoundary_ShouldNotJumpToFartherIntersection(bool multiple)
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(5, 0));
        var touching = new CircleEntity(new Point2D(10, 0), 5);
        var farther = new LineEntity(new Point2D(20, -5), new Point2D(20, 5));
        CadEntity? result = multiple
            ? CadExtendService.ExtendToBoundaries(target, new CadEntity[] { farther, touching }, target.End)
            : CadExtendService.ExtendToBoundary(target, touching, target.End);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtendToBoundary_DegenerateEndpoint_ShouldReturnNullWithoutThrowing(bool polyline)
    {
        CadEntity target = polyline
            ? new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(5, 0), new Point2D(5, 0) })
            : new LineEntity(new Point2D(5, 0), new Point2D(5, 0));
        var boundary = new LineEntity(new Point2D(10, -5), new Point2D(10, 5));
        Assert.Null(CadExtendService.ExtendToBoundary(target, boundary, new Point2D(5, 0)));
    }

    [Theory]
    [InlineData(1000000000, 0.0001)]
    [InlineData(0.001, 0.000001)]
    public void ExtendToBoundary_ShortPhysicalExtension_ShouldUseDistanceTolerance(double length, double extension)
    {
        var target = new LineEntity(new Point2D(0, 0), new Point2D(length, 0));
        var boundary = new LineEntity(new Point2D(length + extension, -1), new Point2D(length + extension, 1));
        var result = Assert.IsType<LineEntity>(CadExtendService.ExtendToBoundary(target, boundary, target.End));
        Assert.Equal(boundary.Start.X, result.End.X);
        Assert.Equal(target.Id, result.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtendToBoundaries_CurvedTarget_ShouldChooseNearestNativeSweepRegardlessOfOrder(bool ellipse)
    {
        CadEntity target = ellipse
            ? new EllipticalArcEntity(new Point2D(0, 0), new Vector2D(10, 0), 5, 0, Math.PI / 2)
            : new ArcEntity(new Point2D(0, 0), 5, Angle.FromDegrees(0), Angle.FromDegrees(90));
        double scale = ellipse ? 2 : 1;
        var far = new LineEntity(new Point2D(-4 * scale, -10), new Point2D(-4 * scale, 10));
        var near = new LineEntity(new Point2D(-2 * scale, -10), new Point2D(-2 * scale, 10));
        CadEntity result = Assert.IsAssignableFrom<CadEntity>(CadExtendService.ExtendToBoundaries(
            target, new CadEntity[] { far, near, target }, new Point2D(0, 5)));
        Assert.Equal(target.Kind, result.Kind);
        Point2D endpoint = result is ArcEntity arc ? arc.Geometry.EndPoint : ((EllipticalArcEntity)result).EndPoint;
        Assert.True(Math.Abs(endpoint.X - near.Start.X) <= GeometryTolerance.Default.Distance);
        Assert.Equal(target.Id, result.Id);
    }
}
