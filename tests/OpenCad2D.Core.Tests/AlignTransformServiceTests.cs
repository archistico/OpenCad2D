using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Transformations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class AlignTransformServiceTests
{
    [Fact]
    public void Calculate_WhenDirectionsAreEqual_ShouldTranslateSource1ToDestination1()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.Calculate(
            new Point2D(0, 0),
            new Point2D(10, 5),
            new Point2D(10, 0),
            new Point2D(20, 5),
            applyScale: false);

        Assert.Equal(new Point2D(10, 5), result.Matrix.Transform(new Point2D(0, 0)));
        Assert.Equal(new Point2D(20, 5), result.Matrix.Transform(new Point2D(10, 0)));
        Assert.False(result.ScaleApplied);
        Assert.False(result.IsDegenerate);
    }

    [Fact]
    public void Calculate_WhenDirectionsDiffer_ShouldTranslateAndRotate()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.Calculate(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            applyScale: false);

        Point2D transformed = result.Matrix.Transform(new Point2D(10, 0));

        Assert.Equal(0, transformed.X, precision: 6);
        Assert.Equal(10, transformed.Y, precision: 6);
        Assert.Equal(90, result.RotationDegrees, precision: 6);
    }

    [Fact]
    public void Calculate_WithScale_ShouldTranslateRotateAndScale()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.Calculate(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 20),
            applyScale: true);

        Point2D transformed = result.Matrix.Transform(new Point2D(10, 0));

        Assert.Equal(0, transformed.X, precision: 6);
        Assert.Equal(20, transformed.Y, precision: 6);
        Assert.Equal(2, result.ScaleFactor, precision: 6);
        Assert.True(result.ScaleApplied);
    }

    [Fact]
    public void Calculate_WithoutScale_ShouldKeepSourceLength()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.Calculate(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 20),
            applyScale: false);

        Point2D transformed = result.Matrix.Transform(new Point2D(10, 0));

        Assert.Equal(0, transformed.X, precision: 6);
        Assert.Equal(10, transformed.Y, precision: 6);
        Assert.Equal(1, result.ScaleFactor, precision: 6);
        Assert.False(result.ScaleApplied);
    }

    [Fact]
    public void Calculate_WhenSourceDirectionIsDegenerate_ShouldOnlyTranslate()
    {
        var service = new AlignTransformService();

        AlignTransformResult result = service.Calculate(
            new Point2D(1, 1),
            new Point2D(5, 5),
            new Point2D(1, 1),
            new Point2D(10, 5),
            applyScale: true);

        Assert.True(result.IsDegenerate);
        Assert.Equal(new Point2D(5, 5), result.Matrix.Transform(new Point2D(1, 1)));
        Assert.Equal(new Point2D(9, 5), result.Matrix.Transform(new Point2D(5, 1)));
    }

    [Fact]
    public void Matrix_ShouldTransformLineEntityAndPreserveId()
    {
        var service = new AlignTransformService();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        AlignTransformResult result = service.Calculate(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            applyScale: false);

        var transformed = (LineEntity)line.Transform(result.Matrix);

        Assert.Equal(line.Id, transformed.Id);
        Assert.Equal(new Point2D(0, 0), transformed.Start);
        Assert.Equal(0, transformed.End.X, precision: 6);
        Assert.Equal(10, transformed.End.Y, precision: 6);
    }

    [Fact]
    public void Matrix_ShouldTransformCircleCenterAndScaleRadiusWhenScaleIsApplied()
    {
        var service = new AlignTransformService();
        var circle = new CircleEntity(
            new Point2D(10, 0),
            5);

        AlignTransformResult result = service.Calculate(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 20),
            applyScale: true);

        var transformed = (CircleEntity)circle.Transform(result.Matrix);

        Assert.Equal(circle.Id, transformed.Id);
        Assert.Equal(0, transformed.Center.X, precision: 6);
        Assert.Equal(20, transformed.Center.Y, precision: 6);
        Assert.Equal(10, transformed.Radius, precision: 6);
    }

    [Fact]
    public void Matrix_ShouldTransformPolylineVertices()
    {
        var service = new AlignTransformService();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: false);

        AlignTransformResult result = service.Calculate(
            new Point2D(0, 0),
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 10),
            applyScale: false);

        var transformed = (PolylineEntity)polyline.Transform(result.Matrix);

        Assert.Equal(polyline.Id, transformed.Id);
        Assert.Equal(new Point2D(0, 0), transformed.Vertices[0]);
        Assert.Equal(0, transformed.Vertices[1].X, precision: 6);
        Assert.Equal(10, transformed.Vertices[1].Y, precision: 6);
        Assert.Equal(-10, transformed.Vertices[2].X, precision: 6);
        Assert.Equal(10, transformed.Vertices[2].Y, precision: 6);
    }
}
