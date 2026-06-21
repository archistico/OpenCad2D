using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Architecture.Doors;

/// <summary>
/// Builds the visible 2D linework for <see cref="DoorEntity" />.
/// </summary>
public static class DoorGeometryBuilder
{
    private const int SwingArcSegmentCount = 16;

    public static DoorGeometry Build(DoorEntity door)
    {
        ArgumentNullException.ThrowIfNull(door);

        IReadOnlyList<LineSegment2D> localSegments = BuildLocalSegments(
            door.Width,
            door.WallThickness,
            door.OpeningAngleDegrees,
            door.SwingDirection);

        BoundingBox2D localPlacementBounds = new(
            0.0,
            -door.WallThickness / 2.0,
            door.Width,
            door.WallThickness / 2.0);
        Point2D localAnchorPoint = AnchorPointService.GetPoint(
            localPlacementBounds,
            door.Anchor);

        var worldSegments = localSegments
            .Select(segment => new LineSegment2D(
                ToWorld(door, segment.Start, localAnchorPoint),
                ToWorld(door, segment.End, localAnchorPoint)))
            .ToArray();

        IReadOnlyList<Point2D> wallMaskPolygon = door.MaskWallOpening
            ? BuildLocalWallMaskPolygon(door.Width, door.WallThickness)
                .Select(point => ToWorld(door, point, localAnchorPoint))
                .ToArray()
            : Array.Empty<Point2D>();

        return new DoorGeometry(worldSegments, wallMaskPolygon);
    }

    private static IReadOnlyList<LineSegment2D> BuildLocalSegments(
        double width,
        double wallThickness,
        double openingAngleDegrees,
        DoorSwingDirection swingDirection)
    {
        double halfThickness = wallThickness / 2.0;
        double sign = swingDirection == DoorSwingDirection.Left ? 1.0 : -1.0;
        double angleRadians = Math.Abs(openingAngleDegrees) * Math.PI / 180.0 * sign;

        var segments = new List<LineSegment2D>
        {
            // Wall opening edges.
            new(new Point2D(0, -halfThickness), new Point2D(width, -halfThickness)),
            new(new Point2D(0, halfThickness), new Point2D(width, halfThickness)),

            // Jambs.
            new(new Point2D(0, -halfThickness), new Point2D(0, halfThickness)),
            new(new Point2D(width, -halfThickness), new Point2D(width, halfThickness))
        };

        Point2D hinge = Point2D.Origin;
        Point2D leafEnd = new(
            width * Math.Cos(angleRadians),
            width * Math.Sin(angleRadians));

        // Open door leaf.
        segments.Add(new LineSegment2D(hinge, leafEnd));

        // Schematic swing arc.
        Point2D previous = new(width, 0);

        for (int index = 1; index <= SwingArcSegmentCount; index++)
        {
            double t = index / (double)SwingArcSegmentCount;
            double currentAngle = angleRadians * t;
            Point2D current = new(
                width * Math.Cos(currentAngle),
                width * Math.Sin(currentAngle));

            segments.Add(new LineSegment2D(previous, current));
            previous = current;
        }

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
        DoorEntity door,
        Point2D localPoint,
        Point2D localAnchorPoint)
    {
        Vector2D offset = localAnchorPoint.VectorTo(localPoint);

        return door.InsertionPoint +
            door.XAxis * offset.X +
            door.YAxis * offset.Y;
    }
}
