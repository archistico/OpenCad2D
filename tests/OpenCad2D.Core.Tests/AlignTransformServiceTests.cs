using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Transformations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class AlignTransformServiceTests
{
    [Fact]
    public void CreateTransform_WithParallelVectorsAndNoScale_ShouldTranslateOnly()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.CreateTransform(
            new Point2D(0, 0),
            new Point2D(5, 5),
            new Point2D(10, 0),
            new Point2D(15, 5),
            applyScale: false);

        Point2D transformedFirst = result.Transform(new Point2D(0, 0));
        Point2D transformedSecond = result.Transform(new Point2D(10, 0));

        AssertPoint(new Point2D(5, 5), transformedFirst);
        AssertPoint(new Point2D(15, 5), transformedSecond);
        Assert.Equal(0, result.RotationAngle.Degrees, precision: 10);
        Assert.Equal(1, result.ScaleFactor, precision: 10);
        Assert.False(result.ScaleApplied);
        Assert.False(result.IsDegenerate);
    }

    [Fact]
    public void CreateTransform_WithPerpendicularVectorsAndNoScale_ShouldTranslateAndRotate()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.CreateTransform(
            new Point2D(0, 0),
            new Point2D(5, 5),
            new Point2D(10, 0),
            new Point2D(5, 15),
            applyScale: false);

        Point2D transformedFirst = result.Transform(new Point2D(0, 0));
        Point2D transformedSecond = result.Transform(new Point2D(10, 0));
        Point2D transformedOffsetPoint = result.Transform(new Point2D(0, 2));

        AssertPoint(new Point2D(5, 5), transformedFirst);
        AssertPoint(new Point2D(5, 15), transformedSecond);
        AssertPoint(new Point2D(3, 5), transformedOffsetPoint);
        Assert.Equal(90, result.RotationAngle.Degrees, precision: 10);
        Assert.Equal(1, result.ScaleFactor, precision: 10);
        Assert.False(result.ScaleApplied);
    }

    [Fact]
    public void CreateTransform_WithScale_ShouldMapSecondSourcePointToSecondDestinationPoint()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.CreateTransform(
            new Point2D(0, 0),
            new Point2D(5, 5),
            new Point2D(10, 0),
            new Point2D(25, 5),
            applyScale: true);

        Point2D transformedFirst = result.Transform(new Point2D(0, 0));
        Point2D transformedSecond = result.Transform(new Point2D(10, 0));
        Point2D transformedOffsetPoint = result.Transform(new Point2D(0, 5));

        AssertPoint(new Point2D(5, 5), transformedFirst);
        AssertPoint(new Point2D(25, 5), transformedSecond);
        AssertPoint(new Point2D(5, 15), transformedOffsetPoint);
        Assert.Equal(0, result.RotationAngle.Degrees, precision: 10);
        Assert.Equal(2, result.ScaleFactor, precision: 10);
        Assert.True(result.ScaleApplied);
    }

    [Fact]
    public void CreateTransform_WithDifferentLengthsAndNoScale_ShouldPreserveSourceLength()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.CreateTransform(
            new Point2D(0, 0),
            new Point2D(5, 5),
            new Point2D(10, 0),
            new Point2D(25, 5),
            applyScale: false);

        Point2D transformedSecond = result.Transform(new Point2D(10, 0));

        AssertPoint(new Point2D(15, 5), transformedSecond);
        Assert.Equal(1, result.ScaleFactor, precision: 10);
        Assert.False(result.ScaleApplied);
    }

    [Fact]
    public void CreateTransform_WithZeroLengthSourceDirection_ShouldFallbackToTranslationOnly()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.CreateTransform(
            new Point2D(1, 1),
            new Point2D(10, 20),
            new Point2D(1, 1),
            new Point2D(30, 20),
            applyScale: true);

        Point2D transformed = result.Transform(new Point2D(2, 3));

        AssertPoint(new Point2D(11, 22), transformed);
        Assert.Equal(0, result.RotationAngle.Degrees, precision: 10);
        Assert.Equal(1, result.ScaleFactor, precision: 10);
        Assert.False(result.ScaleApplied);
        Assert.True(result.IsDegenerate);
    }

    [Fact]
    public void CreateTransform_WithZeroLengthDestinationDirection_ShouldFallbackToTranslationOnly()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.CreateTransform(
            new Point2D(1, 1),
            new Point2D(10, 20),
            new Point2D(5, 1),
            new Point2D(10, 20),
            applyScale: true);

        Point2D transformed = result.Transform(new Point2D(2, 3));

        AssertPoint(new Point2D(11, 22), transformed);
        Assert.True(result.IsDegenerate);
    }

    [Fact]
    public void TransformEntity_WithLine_ShouldPreserveEntityIdAndTransformGeometry()
    {
        var service = new AlignTransformService();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        AlignTransformResult transform = service.CreateTransform(
            new Point2D(0, 0),
            new Point2D(5, 5),
            new Point2D(10, 0),
            new Point2D(5, 15),
            applyScale: false);

        var result = (LineEntity)service.TransformEntity(line, transform);

        Assert.Equal(line.Id, result.Id);
        AssertPoint(new Point2D(5, 5), result.Start);
        AssertPoint(new Point2D(5, 15), result.End);
    }

    [Fact]
    public void TransformEntity_WithCircle_ShouldTransformCenterAndScaleRadius()
    {
        var service = new AlignTransformService();
        var circle = new CircleEntity(
            new Point2D(10, 0),
            3);

        AlignTransformResult transform = service.CreateTransform(
            new Point2D(0, 0),
            new Point2D(5, 5),
            new Point2D(10, 0),
            new Point2D(25, 5),
            applyScale: true);

        var result = (CircleEntity)service.TransformEntity(circle, transform);

        Assert.Equal(circle.Id, result.Id);
        AssertPoint(new Point2D(25, 5), result.Center);
        Assert.Equal(6, result.Radius, precision: 10);
    }

    [Fact]
    public void TransformEntity_WithPolyline_ShouldTransformAllVertices()
    {
        var service = new AlignTransformService();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: true);

        AlignTransformResult transform = service.CreateTransform(
            new Point2D(0, 0),
            new Point2D(5, 5),
            new Point2D(10, 0),
            new Point2D(5, 15),
            applyScale: false);

        var result = (PolylineEntity)service.TransformEntity(polyline, transform);

        Assert.Equal(polyline.Id, result.Id);
        Assert.True(result.IsClosed);
        AssertPoint(new Point2D(5, 5), result.Vertices[0]);
        AssertPoint(new Point2D(5, 15), result.Vertices[1]);
        AssertPoint(new Point2D(-5, 15), result.Vertices[2]);
    }

    private static void AssertPoint(
        Point2D expected,
        Point2D actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 10);
        Assert.Equal(expected.Y, actual.Y, precision: 10);
    }
}
