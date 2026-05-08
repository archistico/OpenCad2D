using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing a finite line segment.
/// </summary>
public sealed class LineEntity : CadEntity
{
    public LineEntity(
        Point2D start,
        Point2D end,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0)
        : base(
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        Start = start;
        End = end;
    }

    public Point2D Start { get; }

    public Point2D End { get; }

    public LineSegment2D Geometry => new(Start, End);

    public override EntityKind Kind => EntityKind.Line;

    public override BoundingBox2D GetBoundingBox()
    {
        return Geometry.GetBoundingBox();
    }

    public override double DistanceTo(Point2D point)
    {
        return DistanceService.DistancePointToSegment(point, Geometry);
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return DistanceService.ClosestPointOnSegment(point, Geometry);
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new LineEntity(
            matrix.Transform(Start),
            matrix.Transform(End),
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new LineEntity(
            Start,
            End,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }
}