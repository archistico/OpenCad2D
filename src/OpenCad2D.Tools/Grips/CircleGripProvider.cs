using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides grips for circle entities.
/// </summary>
public sealed class CircleGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is CircleEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        CircleEntity circle = GetCircle(entity);
        Point2D center = circle.Center;
        double radius = circle.Radius;

        return new[]
        {
            new GripPoint(
                center,
                GripKind.MoveEntity,
                circle.Id,
                0),

            new GripPoint(
                new Point2D(center.X + radius, center.Y),
                GripKind.ResizeRadius,
                circle.Id,
                1),

            new GripPoint(
                new Point2D(center.X, center.Y + radius),
                GripKind.ResizeRadius,
                circle.Id,
                2),

            new GripPoint(
                new Point2D(center.X - radius, center.Y),
                GripKind.ResizeRadius,
                circle.Id,
                3),

            new GripPoint(
                new Point2D(center.X, center.Y - radius),
                GripKind.ResizeRadius,
                circle.Id,
                4)
        };
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        CircleEntity circle = GetCircle(entity);

        if (gripIndex == 0)
        {
            return new CircleEntity(
                destination,
                circle.Radius,
                circle.Id,
                circle.LayerId,
                circle.Style,
                circle.IsVisible,
                circle.IsLocked,
                circle.DrawOrder);
        }

        if (gripIndex is >= 1 and <= 4)
        {
            double radius = circle.Center.DistanceTo(destination);

            return new CircleEntity(
                circle.Center,
                radius,
                circle.Id,
                circle.LayerId,
                circle.Style,
                circle.IsVisible,
                circle.IsLocked,
                circle.DrawOrder);
        }

        throw new ArgumentOutOfRangeException(
            nameof(gripIndex),
            gripIndex,
            "Unknown circle grip index.");
    }

    private static CircleEntity GetCircle(CadEntity entity)
    {
        return entity as CircleEntity
            ?? throw new ArgumentException(
                "Entity must be a circle entity.",
                nameof(entity));
    }
}
