using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing a circular arc.
/// </summary>
public sealed class ArcEntity : CadEntity
{
    public ArcEntity(
        Point2D center,
        double radius,
        Angle startAngle,
        Angle endAngle,
        bool isCounterClockwise = true,
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
        Geometry = new Arc2D(
            center,
            radius,
            startAngle,
            endAngle,
            isCounterClockwise);
    }

    public Arc2D Geometry { get; }

    public Point2D Center => Geometry.Center;

    public double Radius => Geometry.Radius;

    public Angle StartAngle => Geometry.StartAngle;

    public Angle EndAngle => Geometry.EndAngle;

    public bool IsCounterClockwise => Geometry.IsCounterClockwise;

    public override EntityKind Kind => EntityKind.Arc;

    public override BoundingBox2D GetBoundingBox()
    {
        return Geometry.GetBoundingBox();
    }

    public override double DistanceTo(Point2D point)
    {
        return DistanceService.DistancePointToArc(point, Geometry);
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return DistanceService.ClosestPointOnArc(point, Geometry);
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        Point2D transformedCenter = matrix.Transform(Center);
        Point2D transformedStart = matrix.Transform(Geometry.StartPoint);
        Point2D transformedEnd = matrix.Transform(Geometry.EndPoint);

        double transformedRadius = transformedCenter.DistanceTo(transformedStart);

        Angle transformedStartAngle = Angle.FromRadians(
            Math.Atan2(
                transformedStart.Y - transformedCenter.Y,
                transformedStart.X - transformedCenter.X));

        Angle transformedEndAngle = Angle.FromRadians(
            Math.Atan2(
                transformedEnd.Y - transformedCenter.Y,
                transformedEnd.X - transformedCenter.X));

        return new ArcEntity(
            transformedCenter,
            transformedRadius,
            transformedStartAngle,
            transformedEndAngle,
            IsCounterClockwise,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new ArcEntity(
            Center,
            Radius,
            StartAngle,
            EndAngle,
            IsCounterClockwise,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new ArcEntity(
            Center,
            Radius,
            StartAngle,
            EndAngle,
            IsCounterClockwise,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }
}