using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Operations;
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
                GripKind.MoveVertex,
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
            0 => RebuildFromThreePoints(
                arc,
                destination,
                GetMidPoint(arc),
                arc.Geometry.EndPoint),

            1 => RebuildFromThreePoints(
                arc,
                arc.Geometry.StartPoint,
                destination,
                arc.Geometry.EndPoint),

            2 => RebuildFromThreePoints(
                arc,
                arc.Geometry.StartPoint,
                GetMidPoint(arc),
                destination),

            3 => MoveWholeArc(arc, destination),
            _ => throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown arc grip index.")
        };
    }

    private static ArcEntity RebuildFromThreePoints(
        ArcEntity arc,
        Point2D startPoint,
        Point2D pointOnArc,
        Point2D endPoint)
    {
        if (!ArcCreationService.TryCreateFromThreePoints(
                startPoint,
                pointOnArc,
                endPoint,
                out Arc2D rebuiltArc))
        {
            return arc;
        }

        return CreateLike(arc, rebuiltArc);
    }

    private static ArcEntity MoveWholeArc(
        ArcEntity arc,
        Point2D destination)
    {
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


    private static ArcEntity CreateLike(
        ArcEntity source,
        Arc2D geometry)
    {
        return new ArcEntity(
            geometry.Center,
            geometry.Radius,
            geometry.StartAngle,
            geometry.EndAngle,
            geometry.IsCounterClockwise,
            source.Id,
            source.LayerId,
            source.Style,
            source.IsVisible,
            source.IsLocked,
            source.DrawOrder);
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
