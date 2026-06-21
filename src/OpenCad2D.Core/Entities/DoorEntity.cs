using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Architecture.Doors;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Parametric 2D architectural door entity.
/// </summary>
public sealed class DoorEntity : CadEntity
{
    public DoorEntity(
        Point2D insertionPoint,
        double width,
        double wallThickness,
        double openingAngleDegrees = 90.0,
        DoorSwingDirection swingDirection = DoorSwingDirection.Left,
        AnchorPoint anchor = AnchorPoint.MiddleLeft,
        bool maskWallOpening = true,
        Vector2D? xAxis = null,
        Vector2D? yAxis = null,
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
        ValidatePositive(width, nameof(width));
        ValidatePositive(wallThickness, nameof(wallThickness));

        if (openingAngleDegrees <= 0.0 ||
            openingAngleDegrees > 180.0 ||
            double.IsNaN(openingAngleDegrees) ||
            double.IsInfinity(openingAngleDegrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(openingAngleDegrees),
                "Door opening angle must be finite and greater than 0 up to 180 degrees.");
        }

        Vector2D normalizedXAxis = NormalizeAxis(xAxis ?? new Vector2D(1, 0), nameof(xAxis));
        Vector2D normalizedYAxis = NormalizeAxis(yAxis ?? new Vector2D(0, 1), nameof(yAxis));

        if (Tolerance.IsZero(Math.Abs(normalizedXAxis.Cross(normalizedYAxis))))
        {
            throw new ArgumentException("Door axes cannot be collinear.");
        }

        InsertionPoint = insertionPoint;
        Width = width;
        WallThickness = wallThickness;
        OpeningAngleDegrees = openingAngleDegrees;
        SwingDirection = swingDirection;
        Anchor = anchor;
        MaskWallOpening = maskWallOpening;
        XAxis = normalizedXAxis;
        YAxis = normalizedYAxis;
    }

    public Point2D InsertionPoint { get; }

    public double Width { get; }

    public double WallThickness { get; }

    public double OpeningAngleDegrees { get; }

    public DoorSwingDirection SwingDirection { get; }

    public AnchorPoint Anchor { get; }

    /// <summary>
    /// Gets whether the door draws a non-destructive wall-opening mask before its visible linework.
    /// </summary>
    public bool MaskWallOpening { get; }

    public Vector2D XAxis { get; }

    public Vector2D YAxis { get; }

    public override EntityKind Kind => EntityKind.Door;

    public DoorGeometry GetGeneratedGeometry()
    {
        return DoorGeometryBuilder.Build(this);
    }

    public override BoundingBox2D GetBoundingBox()
    {
        IReadOnlyList<LineSegment2D> segments = GetGeneratedGeometry().Segments;
        Point2D first = segments[0].Start;
        double minX = first.X;
        double minY = first.Y;
        double maxX = first.X;
        double maxY = first.Y;

        foreach (LineSegment2D segment in segments)
        {
            Include(segment.Start);
            Include(segment.End);
        }

        return new BoundingBox2D(minX, minY, maxX, maxY);

        void Include(Point2D point)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }
    }

    public override double DistanceTo(Point2D point)
    {
        return point.DistanceTo(GetClosestPoint(point));
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        IReadOnlyList<LineSegment2D> segments = GetGeneratedGeometry().Segments;
        Point2D closestPoint = DistanceService.ClosestPointOnSegment(point, segments[0]);
        double bestDistance = point.DistanceTo(closestPoint);

        for (int index = 1; index < segments.Count; index++)
        {
            Point2D candidate = DistanceService.ClosestPointOnSegment(point, segments[index]);
            double candidateDistance = point.DistanceTo(candidate);

            if (candidateDistance < bestDistance)
            {
                closestPoint = candidate;
                bestDistance = candidateDistance;
            }
        }

        return closestPoint;
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        Vector2D transformedXAxis = matrix.Transform(XAxis);
        Vector2D transformedYAxis = matrix.Transform(YAxis);

        double xScale = transformedXAxis.Length;
        double yScale = transformedYAxis.Length;
        double parameterScale = (xScale + yScale) / 2.0;

        if (Tolerance.IsZero(parameterScale))
        {
            throw new InvalidOperationException("Door transform collapsed the entity axes.");
        }

        return new DoorEntity(
            matrix.Transform(InsertionPoint),
            Width * parameterScale,
            WallThickness * parameterScale,
            OpeningAngleDegrees,
            SwingDirection,
            Anchor,
            MaskWallOpening,
            transformedXAxis,
            transformedYAxis,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return Recreate(id: id);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return Recreate(layerId: layerId);
    }

    public DoorEntity WithParameters(
        Point2D? insertionPoint = null,
        double? width = null,
        double? wallThickness = null,
        double? openingAngleDegrees = null,
        DoorSwingDirection? swingDirection = null,
        AnchorPoint? anchor = null,
        bool? maskWallOpening = null)
    {
        return Recreate(
            insertionPoint: insertionPoint,
            width: width,
            wallThickness: wallThickness,
            openingAngleDegrees: openingAngleDegrees,
            swingDirection: swingDirection,
            anchor: anchor,
            maskWallOpening: maskWallOpening);
    }

    private DoorEntity Recreate(
        Point2D? insertionPoint = null,
        double? width = null,
        double? wallThickness = null,
        double? openingAngleDegrees = null,
        DoorSwingDirection? swingDirection = null,
        AnchorPoint? anchor = null,
        bool? maskWallOpening = null,
        EntityId? id = null,
        LayerId? layerId = null)
    {
        return new DoorEntity(
            insertionPoint ?? InsertionPoint,
            width ?? Width,
            wallThickness ?? WallThickness,
            openingAngleDegrees ?? OpeningAngleDegrees,
            swingDirection ?? SwingDirection,
            anchor ?? Anchor,
            maskWallOpening ?? MaskWallOpening,
            XAxis,
            YAxis,
            id ?? Id,
            layerId ?? LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    private static void ValidatePositive(double value, string paramName)
    {
        if (value <= 0.0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Door numeric parameters must be finite positive values.");
        }
    }

    private static Vector2D NormalizeAxis(Vector2D axis, string paramName)
    {
        if (Tolerance.IsZero(axis.Length))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Door axis cannot be a zero-length vector.");
        }

        return axis.Normalize();
    }
}
