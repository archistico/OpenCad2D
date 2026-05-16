using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Non-associative horizontal or vertical linear dimension.
/// </summary>
public sealed class LinearDimensionEntity : DimensionEntity
{
    public LinearDimensionEntity(
        Point2D firstPoint,
        Point2D secondPoint,
        Point2D dimensionLinePoint,
        DimensionOrientation orientation,
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
        if (orientation == DimensionOrientation.Horizontal && Math.Abs(firstPoint.X - secondPoint.X) <= double.Epsilon)
        {
            throw new ArgumentException(
                "Horizontal dimension requires two points with different X coordinates.",
                nameof(secondPoint));
        }

        if (orientation == DimensionOrientation.Vertical && Math.Abs(firstPoint.Y - secondPoint.Y) <= double.Epsilon)
        {
            throw new ArgumentException(
                "Vertical dimension requires two points with different Y coordinates.",
                nameof(secondPoint));
        }

        FirstPoint = firstPoint;
        SecondPoint = secondPoint;
        DimensionLinePoint = dimensionLinePoint;
        Orientation = orientation;
    }

    public Point2D FirstPoint { get; }

    public Point2D SecondPoint { get; }

    public Point2D DimensionLinePoint { get; }

    public DimensionOrientation Orientation { get; }

    public override double MeasurementValue => Orientation == DimensionOrientation.Horizontal
        ? Math.Abs(SecondPoint.X - FirstPoint.X)
        : Math.Abs(SecondPoint.Y - FirstPoint.Y);

    public override EntityKind Kind => Orientation == DimensionOrientation.Horizontal
        ? EntityKind.HorizontalDimension
        : EntityKind.VerticalDimension;

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
        BoundingBox2D bounds = GetBoundingBox();

        double dx = Math.Max(
            Math.Max(bounds.MinX - point.X, 0),
            point.X - bounds.MaxX);

        double dy = Math.Max(
            Math.Max(bounds.MinY - point.Y, 0),
            point.Y - bounds.MaxY);

        return Math.Sqrt(dx * dx + dy * dy);
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        Point2D firstProjection = Orientation == DimensionOrientation.Horizontal
            ? new Point2D(FirstPoint.X, DimensionLinePoint.Y)
            : new Point2D(DimensionLinePoint.X, FirstPoint.Y);

        Point2D secondProjection = Orientation == DimensionOrientation.Horizontal
            ? new Point2D(SecondPoint.X, DimensionLinePoint.Y)
            : new Point2D(DimensionLinePoint.X, SecondPoint.Y);

        return DistanceService.ClosestPointOnSegment(
            point,
            new LineSegment2D(firstProjection, secondProjection));
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        Point2D transformedFirstPoint = matrix.Transform(FirstPoint);
        Point2D transformedSecondPoint = matrix.Transform(SecondPoint);
        Point2D transformedDimensionLinePoint = matrix.Transform(DimensionLinePoint);

        if (AreNearlyEqual(
                transformedFirstPoint.Y,
                transformedSecondPoint.Y))
        {
            return new LinearDimensionEntity(
                transformedFirstPoint,
                transformedSecondPoint,
                transformedDimensionLinePoint,
                DimensionOrientation.Horizontal,
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

        if (AreNearlyEqual(
                transformedFirstPoint.X,
                transformedSecondPoint.X))
        {
            return new LinearDimensionEntity(
                transformedFirstPoint,
                transformedSecondPoint,
                transformedDimensionLinePoint,
                DimensionOrientation.Vertical,
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

        return new AlignedDimensionEntity(
            transformedFirstPoint,
            transformedSecondPoint,
            transformedDimensionLinePoint,
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


    public override DimensionEntity WithStaleState(bool isStale)
    {
        return new LinearDimensionEntity(
            FirstPoint,
            SecondPoint,
            DimensionLinePoint,
            Orientation,
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

    private static bool AreNearlyEqual(
        double first,
        double second)
    {
        return Math.Abs(first - second) <= 1e-9;
    }

    public override CadEntity WithId(EntityId id)
    {
        return new LinearDimensionEntity(
            FirstPoint,
            SecondPoint,
            DimensionLinePoint,
            Orientation,
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
        return new LinearDimensionEntity(
            FirstPoint,
            SecondPoint,
            DimensionLinePoint,
            Orientation,
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
}
