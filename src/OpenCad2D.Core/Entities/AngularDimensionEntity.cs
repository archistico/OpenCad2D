using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Non-associative angular dimension defined by a center, two ray points and an arc placement point.
/// </summary>
public sealed class AngularDimensionEntity : DimensionEntity
{
    public AngularDimensionEntity(
        Point2D center,
        Point2D firstRayPoint,
        Point2D secondRayPoint,
        Point2D arcPoint,
        bool isCounterClockwise,
        DimensionStyleId? dimensionStyleId = null,
        string? textOverride = null,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0)
        : base(
            dimensionStyleId,
            textOverride,
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        if (center.DistanceTo(firstRayPoint) <= double.Epsilon)
        {
            throw new ArgumentException(
                "Angular dimension requires a first ray point different from the center.",
                nameof(firstRayPoint));
        }

        if (center.DistanceTo(secondRayPoint) <= double.Epsilon)
        {
            throw new ArgumentException(
                "Angular dimension requires a second ray point different from the center.",
                nameof(secondRayPoint));
        }

        if (center.DistanceTo(arcPoint) <= double.Epsilon)
        {
            throw new ArgumentException(
                "Angular dimension requires an arc placement point different from the center.",
                nameof(arcPoint));
        }

        double sweep = GetSweepDegrees(
            center,
            firstRayPoint,
            secondRayPoint,
            isCounterClockwise);

        if (sweep <= double.Epsilon || Math.Abs(sweep - 360.0) <= double.Epsilon)
        {
            throw new ArgumentException(
                "Angular dimension requires two different ray directions.",
                nameof(secondRayPoint));
        }

        Center = center;
        FirstRayPoint = firstRayPoint;
        SecondRayPoint = secondRayPoint;
        ArcPoint = arcPoint;
        IsCounterClockwise = isCounterClockwise;
    }

    public Point2D Center { get; }

    public Point2D FirstRayPoint { get; }

    public Point2D SecondRayPoint { get; }

    public Point2D ArcPoint { get; }

    public bool IsCounterClockwise { get; }

    public double Radius => Center.DistanceTo(ArcPoint);

    public double StartAngleDegrees => NormalizeDegrees(ToDegrees(Math.Atan2(
        FirstRayPoint.Y - Center.Y,
        FirstRayPoint.X - Center.X)));

    public double EndAngleDegrees => NormalizeDegrees(ToDegrees(Math.Atan2(
        SecondRayPoint.Y - Center.Y,
        SecondRayPoint.X - Center.X)));

    public override double MeasurementValue => GetSweepDegrees(
        Center,
        FirstRayPoint,
        SecondRayPoint,
        IsCounterClockwise);

    public override EntityKind Kind => EntityKind.AngularDimension;

    public override BoundingBox2D GetBoundingBox()
    {
        Arc2D arc = GetArcGeometry();
        BoundingBox2D arcBounds = arc.GetBoundingBox();

        double minX = Math.Min(Math.Min(arcBounds.MinX, Center.X), Math.Min(FirstRayPoint.X, SecondRayPoint.X));
        double minY = Math.Min(Math.Min(arcBounds.MinY, Center.Y), Math.Min(FirstRayPoint.Y, SecondRayPoint.Y));
        double maxX = Math.Max(Math.Max(arcBounds.MaxX, Center.X), Math.Max(FirstRayPoint.X, SecondRayPoint.X));
        double maxY = Math.Max(Math.Max(arcBounds.MaxY, Center.Y), Math.Max(FirstRayPoint.Y, SecondRayPoint.Y));

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public override double DistanceTo(Point2D point)
    {
        return DistanceService.DistancePointToArc(
            point,
            GetArcGeometry());
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return DistanceService.ClosestPointOnArc(
            point,
            GetArcGeometry());
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new AngularDimensionEntity(
            matrix.Transform(Center),
            matrix.Transform(FirstRayPoint),
            matrix.Transform(SecondRayPoint),
            matrix.Transform(ArcPoint),
            IsCounterClockwise,
            DimensionStyleId,
            TextOverride,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new AngularDimensionEntity(
            Center,
            FirstRayPoint,
            SecondRayPoint,
            ArcPoint,
            IsCounterClockwise,
            DimensionStyleId,
            TextOverride,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new AngularDimensionEntity(
            Center,
            FirstRayPoint,
            SecondRayPoint,
            ArcPoint,
            IsCounterClockwise,
            DimensionStyleId,
            TextOverride,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public Arc2D GetArcGeometry()
    {
        return new Arc2D(
            Center,
            Radius,
            Angle.FromDegrees(StartAngleDegrees),
            Angle.FromDegrees(EndAngleDegrees),
            IsCounterClockwise);
    }

    public static bool ShouldUseCounterClockwiseSweep(
        Point2D center,
        Point2D firstRayPoint,
        Point2D secondRayPoint,
        Point2D arcPoint)
    {
        double start = NormalizeDegrees(ToDegrees(Math.Atan2(
            firstRayPoint.Y - center.Y,
            firstRayPoint.X - center.X)));
        double end = NormalizeDegrees(ToDegrees(Math.Atan2(
            secondRayPoint.Y - center.Y,
            secondRayPoint.X - center.X)));
        double candidate = NormalizeDegrees(ToDegrees(Math.Atan2(
            arcPoint.Y - center.Y,
            arcPoint.X - center.X)));

        return IsAngleInsideCounterClockwiseSweep(
            start,
            end,
            candidate);
    }

    public static double GetSweepDegrees(
        Point2D center,
        Point2D firstRayPoint,
        Point2D secondRayPoint,
        bool isCounterClockwise)
    {
        double start = NormalizeDegrees(ToDegrees(Math.Atan2(
            firstRayPoint.Y - center.Y,
            firstRayPoint.X - center.X)));
        double end = NormalizeDegrees(ToDegrees(Math.Atan2(
            secondRayPoint.Y - center.Y,
            secondRayPoint.X - center.X)));

        return isCounterClockwise
            ? NormalizeDegrees(end - start)
            : NormalizeDegrees(start - end);
    }

    private static bool IsAngleInsideCounterClockwiseSweep(
        double start,
        double end,
        double value)
    {
        double sweep = NormalizeDegrees(end - start);
        double candidateSweep = NormalizeDegrees(value - start);

        return candidateSweep <= sweep;
    }

    private static double NormalizeDegrees(double degrees)
    {
        double normalized = degrees % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    private static double ToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }
}
