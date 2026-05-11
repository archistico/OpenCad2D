using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides grips for circular arc entities.
/// </summary>
public sealed class ArcGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is ArcEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        ArcEntity arc = GetArc(entity);

        return new[]
        {
            new GripPoint(
                arc.Geometry.StartPoint,
                GripKind.MoveVertex,
                arc.Id,
                0),

            new GripPoint(
                GetMidPoint(arc),
                GripKind.ResizeRadius,
                arc.Id,
                1),

            new GripPoint(
                arc.Geometry.EndPoint,
                GripKind.MoveVertex,
                arc.Id,
                2),

            new GripPoint(
                arc.Center,
                GripKind.MoveEntity,
                arc.Id,
                3)
        };
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        ArcEntity arc = GetArc(entity);

        return gripIndex switch
        {
            0 => MoveEndpoint(arc, destination, moveStart: true),
            1 => ResizeRadius(arc, destination),
            2 => MoveEndpoint(arc, destination, moveStart: false),
            3 => MoveWholeArc(arc, destination),
            _ => throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown arc grip index.")
        };
    }

    private static ArcEntity MoveEndpoint(
        ArcEntity arc,
        Point2D destination,
        bool moveStart)
    {
        double radius = arc.Center.DistanceTo(destination);

        if (Tolerance.IsZero(radius))
        {
            return arc;
        }

        Angle angle = Angle.FromRadians(
            Math.Atan2(
                destination.Y - arc.Center.Y,
                destination.X - arc.Center.X));

        return new ArcEntity(
            arc.Center,
            radius,
            moveStart ? angle : arc.StartAngle,
            moveStart ? arc.EndAngle : angle,
            arc.IsCounterClockwise,
            arc.Id,
            arc.LayerId,
            arc.Style,
            arc.IsVisible,
            arc.IsLocked,
            arc.DrawOrder);
    }

    private static ArcEntity ResizeRadius(
        ArcEntity arc,
        Point2D destination)
    {
        double radius = arc.Center.DistanceTo(destination);

        if (Tolerance.IsZero(radius))
        {
            return arc;
        }

        return new ArcEntity(
            arc.Center,
            radius,
            arc.StartAngle,
            arc.EndAngle,
            arc.IsCounterClockwise,
            arc.Id,
            arc.LayerId,
            arc.Style,
            arc.IsVisible,
            arc.IsLocked,
            arc.DrawOrder);
    }

    private static ArcEntity MoveWholeArc(
        ArcEntity arc,
        Point2D destination)
    {
        Vector2D vector = arc.Center.VectorTo(destination);

        return new ArcEntity(
            destination,
            arc.Radius,
            arc.StartAngle,
            arc.EndAngle,
            arc.IsCounterClockwise,
            arc.Id,
            arc.LayerId,
            arc.Style,
            arc.IsVisible,
            arc.IsLocked,
            arc.DrawOrder);
    }

    private static Point2D GetMidPoint(ArcEntity arc)
    {
        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;
        double sweep;

        if (arc.IsCounterClockwise)
        {
            sweep = end - start;

            if (sweep < 0)
            {
                sweep += 2.0 * Math.PI;
            }

            return arc.Geometry.PointAt(Angle.FromRadians(start + sweep / 2.0));
        }

        sweep = start - end;

        if (sweep < 0)
        {
            sweep += 2.0 * Math.PI;
        }

        return arc.Geometry.PointAt(Angle.FromRadians(start - sweep / 2.0));
    }

    private static ArcEntity GetArc(CadEntity entity)
    {
        return entity as ArcEntity
            ?? throw new ArgumentException(
                "Entity must be an arc entity.",
                nameof(entity));
    }
}
