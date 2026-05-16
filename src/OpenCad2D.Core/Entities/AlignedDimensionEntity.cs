using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Non-associative aligned dimension measured along the direction between two points.
/// </summary>
public sealed class AlignedDimensionEntity : DimensionEntity
{
    public AlignedDimensionEntity(
        Point2D firstPoint,
        Point2D secondPoint,
        Point2D dimensionLinePoint,
        DimensionStyleId? dimensionStyleId = null,
        string? textOverride = null,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0,
        bool isStale = false)
        : base(
            dimensionStyleId,
            textOverride,
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder,
            isStale)
    {
        if (firstPoint.DistanceTo(secondPoint) <= double.Epsilon)
        {
            throw new ArgumentException(
                "Aligned dimension requires two distinct measured points.",
                nameof(secondPoint));
        }

        FirstPoint = firstPoint;
        SecondPoint = secondPoint;
        DimensionLinePoint = dimensionLinePoint;
    }

    public Point2D FirstPoint { get; }

    public Point2D SecondPoint { get; }

    public Point2D DimensionLinePoint { get; }

    public override double MeasurementValue => FirstPoint.DistanceTo(SecondPoint);

    public override EntityKind Kind => EntityKind.AlignedDimension;

    public override BoundingBox2D GetBoundingBox()
    {
        double minX = Math.Min(
            Math.Min(FirstPoint.X, SecondPoint.X),
            DimensionLinePoint.X);
        double minY = Math.Min(
            Math.Min(FirstPoint.Y, SecondPoint.Y),
            DimensionLinePoint.Y);
        double maxX = Math.Max(
            Math.Max(FirstPoint.X, SecondPoint.X),
            DimensionLinePoint.X);
        double maxY = Math.Max(
            Math.Max(FirstPoint.Y, SecondPoint.Y),
            DimensionLinePoint.Y);

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public override double DistanceTo(Point2D point)
    {
        Point2D firstProjection;
        Point2D secondProjection;
        (firstProjection, secondProjection) = GetDimensionLineEndpoints();

        return DistanceService.DistancePointToSegment(
            point,
            new LineSegment2D(firstProjection, secondProjection));
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        Point2D firstProjection;
        Point2D secondProjection;
        (firstProjection, secondProjection) = GetDimensionLineEndpoints();

        return DistanceService.ClosestPointOnSegment(
            point,
            new LineSegment2D(firstProjection, secondProjection));
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new AlignedDimensionEntity(
            matrix.Transform(FirstPoint),
            matrix.Transform(SecondPoint),
            matrix.Transform(DimensionLinePoint),
            DimensionStyleId,
            TextOverride,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsStale);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new AlignedDimensionEntity(
            FirstPoint,
            SecondPoint,
            DimensionLinePoint,
            DimensionStyleId,
            TextOverride,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsStale);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new AlignedDimensionEntity(
            FirstPoint,
            SecondPoint,
            DimensionLinePoint,
            DimensionStyleId,
            TextOverride,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsStale);
    }


    public override DimensionEntity WithStaleState(bool isStale)
    {
        return new AlignedDimensionEntity(
            FirstPoint,
            SecondPoint,
            DimensionLinePoint,
            DimensionStyleId,
            TextOverride,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            isStale);
    }

    private (Point2D FirstProjection, Point2D SecondProjection) GetDimensionLineEndpoints()
    {
        Vector2D direction = (SecondPoint - FirstPoint).Normalize();
        Vector2D normal = direction.PerpendicularLeft();
        double offset = (DimensionLinePoint - FirstPoint).Dot(normal);
        Vector2D offsetVector = normal * offset;

        return (FirstPoint + offsetVector, SecondPoint + offsetVector);
    }
}
