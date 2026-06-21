using OpenCad2D.Core.Anchors;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class AnchorPointServiceTests
{
    [Fact]
    public void Descriptors_ShouldUseVisualGridOrder()
    {
        AnchorPoint[] anchors = AnchorPointService.Descriptors
            .Select(descriptor => descriptor.Anchor)
            .ToArray();

        Assert.Equal(
            new[]
            {
                AnchorPoint.TopLeft,
                AnchorPoint.TopCenter,
                AnchorPoint.TopRight,
                AnchorPoint.MiddleLeft,
                AnchorPoint.Center,
                AnchorPoint.MiddleRight,
                AnchorPoint.BottomLeft,
                AnchorPoint.BottomCenter,
                AnchorPoint.BottomRight
            },
            anchors);
    }

    [Theory]
    [InlineData(AnchorPoint.TopLeft, 10, 50)]
    [InlineData(AnchorPoint.TopCenter, 20, 50)]
    [InlineData(AnchorPoint.TopRight, 30, 50)]
    [InlineData(AnchorPoint.MiddleLeft, 10, 35)]
    [InlineData(AnchorPoint.Center, 20, 35)]
    [InlineData(AnchorPoint.MiddleRight, 30, 35)]
    [InlineData(AnchorPoint.BottomLeft, 10, 20)]
    [InlineData(AnchorPoint.BottomCenter, 20, 20)]
    [InlineData(AnchorPoint.BottomRight, 30, 20)]
    public void GetPoint_ShouldResolveAnchorAgainstCadBoundingBox(
        AnchorPoint anchor,
        double expectedX,
        double expectedY)
    {
        var bounds = new BoundingBox2D(10, 20, 30, 50);

        Point2D point = AnchorPointService.GetPoint(bounds, anchor);

        Assert.Equal(new Point2D(expectedX, expectedY), point);
    }

    [Theory]
    [InlineData(0, 0, AnchorPoint.TopLeft)]
    [InlineData(0, 1, AnchorPoint.TopCenter)]
    [InlineData(0, 2, AnchorPoint.TopRight)]
    [InlineData(1, 0, AnchorPoint.MiddleLeft)]
    [InlineData(1, 1, AnchorPoint.Center)]
    [InlineData(1, 2, AnchorPoint.MiddleRight)]
    [InlineData(2, 0, AnchorPoint.BottomLeft)]
    [InlineData(2, 1, AnchorPoint.BottomCenter)]
    [InlineData(2, 2, AnchorPoint.BottomRight)]
    public void FromGridPosition_ShouldMapThreeByThreeSelector(
        int row,
        int column,
        AnchorPoint expected)
    {
        Assert.Equal(expected, AnchorPointService.FromGridPosition(row, column));
    }

    [Theory]
    [InlineData(7, AnchorPoint.TopLeft)]
    [InlineData(8, AnchorPoint.TopCenter)]
    [InlineData(9, AnchorPoint.TopRight)]
    [InlineData(4, AnchorPoint.MiddleLeft)]
    [InlineData(5, AnchorPoint.Center)]
    [InlineData(6, AnchorPoint.MiddleRight)]
    [InlineData(1, AnchorPoint.BottomLeft)]
    [InlineData(2, AnchorPoint.BottomCenter)]
    [InlineData(3, AnchorPoint.BottomRight)]
    public void TryFromNumericShortcut_ShouldUseKeypadMapping(
        int shortcut,
        AnchorPoint expected)
    {
        bool parsed = AnchorPointService.TryFromNumericShortcut(shortcut, out AnchorPoint anchor);

        Assert.True(parsed);
        Assert.Equal(expected, anchor);
    }

    [Fact]
    public void GetTranslationToPlaceAnchor_ShouldMoveSelectedAnchorToTarget()
    {
        var bounds = new BoundingBox2D(10, 20, 30, 50);
        var target = new Point2D(100, 200);

        Vector2D translation = AnchorPointService.GetTranslationToPlaceAnchor(
            bounds,
            AnchorPoint.TopLeft,
            target);

        Assert.Equal(new Vector2D(90, 150), translation);
        Assert.Equal(target, AnchorPointService.GetPoint(bounds, AnchorPoint.TopLeft) + translation);
    }

    [Fact]
    public void CreatePlacement_ShouldReturnAnchorPointTargetAndTranslation()
    {
        var bounds = new BoundingBox2D(-2, -1, 6, 3);
        var target = new Point2D(10, 20);

        AnchorPlacement placement = AnchorPointService.CreatePlacement(
            bounds,
            AnchorPoint.Center,
            target);

        Assert.Equal(AnchorPoint.Center, placement.Anchor);
        Assert.Equal(new Point2D(2, 1), placement.LocalAnchorPoint);
        Assert.Equal(target, placement.TargetPoint);
        Assert.Equal(new Vector2D(8, 19), placement.Translation);
    }

    [Fact]
    public void ParseOrDefault_ShouldRecoverInvalidPersistedValue()
    {
        Assert.Equal(AnchorPoint.BottomRight, AnchorPointService.ParseOrDefault("BottomRight"));
        Assert.Equal(AnchorPoint.Center, AnchorPointService.ParseOrDefault("bottomright"));
        Assert.Equal(AnchorPoint.TopCenter, AnchorPointService.ParseOrDefault("Invalid", AnchorPoint.TopCenter));
    }
}
