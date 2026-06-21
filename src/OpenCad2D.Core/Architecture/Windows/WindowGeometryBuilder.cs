using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Architecture.Windows;

/// <summary>
/// Builds the visible 2D linework for <see cref="WindowEntity" />.
/// </summary>
public static class WindowGeometryBuilder
{
    public static WindowGeometry Build(WindowEntity window)
    {
        ArgumentNullException.ThrowIfNull(window);

        IReadOnlyList<LineSegment2D> localSegments = BuildLocalSegments(
            window.Width,
            window.WallThickness,
            window.FrameOffset);

        BoundingBox2D localPlacementBounds = new(
            0.0,
            -window.WallThickness / 2.0,
            window.Width,
            window.WallThickness / 2.0);
        Point2D localAnchorPoint = AnchorPointService.GetPoint(
            localPlacementBounds,
            window.Anchor);

        var worldSegments = localSegments
            .Select(segment => new LineSegment2D(
                ToWorld(window, segment.Start, localAnchorPoint),
                ToWorld(window, segment.End, localAnchorPoint)))
            .ToArray();

        IReadOnlyList<Point2D> wallMaskPolygon = window.MaskWallOpening
            ? BuildLocalWallMaskPolygon(window.Width, window.WallThickness)
                .Select(point => ToWorld(window, point, localAnchorPoint))
                .ToArray()
            : Array.Empty<Point2D>();

        return new WindowGeometry(worldSegments, wallMaskPolygon);
    }

    private static IReadOnlyList<LineSegment2D> BuildLocalSegments(
        double width,
        double wallThickness,
        double frameOffset)
    {
        double halfThickness = wallThickness / 2.0;
        double clampedFrameOffset = Math.Min(frameOffset, halfThickness);

        var segments = new List<LineSegment2D>
        {
            // Wall opening edges.
            new(new Point2D(0, -halfThickness), new Point2D(width, -halfThickness)),
            new(new Point2D(0, halfThickness), new Point2D(width, halfThickness)),

            // Jambs.
            new(new Point2D(0, -halfThickness), new Point2D(0, halfThickness)),
            new(new Point2D(width, -halfThickness), new Point2D(width, halfThickness)),

            // Schematic frame/glass lines inside the wall thickness.
            new(new Point2D(0, -clampedFrameOffset), new Point2D(width, -clampedFrameOffset)),
            new(new Point2D(0, clampedFrameOffset), new Point2D(width, clampedFrameOffset)),

            // Center line for a recognizable architectural plan symbol.
            new(new Point2D(width / 2.0, -clampedFrameOffset), new Point2D(width / 2.0, clampedFrameOffset))
        };

        return segments;
    }

    private static IReadOnlyList<Point2D> BuildLocalWallMaskPolygon(
        double width,
        double wallThickness)
    {
        double halfThickness = wallThickness / 2.0;

        return new[]
        {
            new Point2D(0, -halfThickness),
            new Point2D(width, -halfThickness),
            new Point2D(width, halfThickness),
            new Point2D(0, halfThickness)
        };
    }

    private static Point2D ToWorld(
        WindowEntity window,
        Point2D localPoint,
        Point2D localAnchorPoint)
    {
        Vector2D offset = localAnchorPoint.VectorTo(localPoint);

        return window.InsertionPoint +
            window.XAxis * offset.X +
            window.YAxis * offset.Y;
    }
}
