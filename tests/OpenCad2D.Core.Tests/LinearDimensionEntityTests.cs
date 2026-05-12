using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Tests;

public sealed class LinearDimensionEntityTests
{
    [Fact]
    public void Constructor_WithHorizontalOrientation_ShouldMeasureDeltaX()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(10, 5),
            new Point2D(30, 8),
            new Point2D(20, 20),
            DimensionOrientation.Horizontal);

        Assert.Equal(EntityKind.HorizontalDimension, dimension.Kind);
        Assert.Equal(20, dimension.MeasurementValue);
        Assert.Equal(DimensionStyleId.Standard, dimension.DimensionStyleId);
    }

    [Fact]
    public void Constructor_WithVerticalOrientation_ShouldMeasureDeltaY()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(10, 5),
            new Point2D(30, 25),
            new Point2D(40, 20),
            DimensionOrientation.Vertical);

        Assert.Equal(EntityKind.VerticalDimension, dimension.Kind);
        Assert.Equal(20, dimension.MeasurementValue);
    }

    [Fact]
    public void Constructor_WithTextOverride_ShouldTrimOverride()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 5),
            DimensionOrientation.Horizontal,
            textOverride: "  approx. 10  ");

        Assert.Equal("approx. 10", dimension.TextOverride);
        Assert.Equal("approx. 10", dimension.GetDisplayText());
    }

    [Fact]
    public void GetDisplayText_ShouldApplyDecimalSeparatorAndSuffix()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(12.345, 0),
            new Point2D(0, 5),
            DimensionOrientation.Horizontal);

        Assert.Equal("12,35 mm", dimension.GetDisplayText(
            decimalPlaces: 2,
            decimalSeparator: ",",
            suffix: " mm"));
    }

    [Fact]
    public void Transform_WithTranslation_ShouldMoveAllPointsAndKeepId()
    {
        EntityId id = EntityId.New();
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 5),
            DimensionOrientation.Horizontal,
            id: id);

        var transformed = Assert.IsType<LinearDimensionEntity>(dimension.Transform(
            Matrix2D.Translation(2, 3)));

        Assert.Equal(id, transformed.Id);
        Assert.Equal(new Point2D(2, 3), transformed.FirstPoint);
        Assert.Equal(new Point2D(12, 3), transformed.SecondPoint);
        Assert.Equal(new Point2D(2, 8), transformed.DimensionLinePoint);
    }

    [Fact]
    public void WithLayer_ShouldKeepDimensionData()
    {
        var dimension = new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 5),
            DimensionOrientation.Horizontal);

        var changed = Assert.IsType<LinearDimensionEntity>(dimension.WithLayer(new LayerId("Dims")));

        Assert.Equal(new LayerId("Dims"), changed.LayerId);
        Assert.Equal(dimension.FirstPoint, changed.FirstPoint);
        Assert.Equal(dimension.SecondPoint, changed.SecondPoint);
        Assert.Equal(dimension.DimensionLinePoint, changed.DimensionLinePoint);
        Assert.Equal(dimension.Orientation, changed.Orientation);
    }
}
