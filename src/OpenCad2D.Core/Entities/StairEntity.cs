using OpenCad2D.Core.Architecture.Stairs;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// Parametric 2D stair entity for plan, side elevation and front elevation drafting.
/// </summary>
public sealed class StairEntity : CadEntity
{
    public StairEntity(
        Point2D insertionPoint,
        StairViewKind viewKind,
        double width,
        int treadCount,
        double treadDepth,
        double riserHeight,
        bool showStructure = false,
        double slabThickness = 0.25,
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
        ValidatePositive(treadDepth, nameof(treadDepth));
        ValidatePositive(riserHeight, nameof(riserHeight));

        if (treadCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(treadCount),
                "Stair tread count must be at least 1.");
        }

        if (slabThickness < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slabThickness),
                "Stair slab thickness cannot be negative.");
        }

        Vector2D normalizedXAxis = NormalizeAxis(xAxis ?? new Vector2D(1, 0), nameof(xAxis));
        Vector2D normalizedYAxis = NormalizeAxis(yAxis ?? new Vector2D(0, 1), nameof(yAxis));

        if (Tolerance.IsZero(Math.Abs(normalizedXAxis.Cross(normalizedYAxis))))
        {
            throw new ArgumentException("Stair axes cannot be collinear.");
        }

        InsertionPoint = insertionPoint;
        ViewKind = viewKind;
        Width = width;
        TreadCount = treadCount;
        TreadDepth = treadDepth;
        RiserHeight = riserHeight;
        ShowStructure = showStructure;
        SlabThickness = slabThickness;
        XAxis = normalizedXAxis;
        YAxis = normalizedYAxis;
    }

    public Point2D InsertionPoint { get; }

    public StairViewKind ViewKind { get; }

    public double Width { get; }

    public int TreadCount { get; }

    public double TreadDepth { get; }

    public double RiserHeight { get; }

    public bool ShowStructure { get; }

    public double SlabThickness { get; }

    public Vector2D XAxis { get; }

    public Vector2D YAxis { get; }

    public double TotalRun => TreadDepth * TreadCount;

    public double TotalRise => RiserHeight * TreadCount;

    public override EntityKind Kind => EntityKind.Stair;

    public StairGeometry GetGeneratedGeometry()
    {
        return StairGeometryBuilder.Build(this);
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
            throw new InvalidOperationException("Stair transform collapsed the entity axes.");
        }

        return new StairEntity(
            matrix.Transform(InsertionPoint),
            ViewKind,
            Width * parameterScale,
            TreadCount,
            TreadDepth * parameterScale,
            RiserHeight * parameterScale,
            ShowStructure,
            SlabThickness * parameterScale,
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
        return new StairEntity(
            InsertionPoint,
            ViewKind,
            Width,
            TreadCount,
            TreadDepth,
            RiserHeight,
            ShowStructure,
            SlabThickness,
            XAxis,
            YAxis,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new StairEntity(
            InsertionPoint,
            ViewKind,
            Width,
            TreadCount,
            TreadDepth,
            RiserHeight,
            ShowStructure,
            SlabThickness,
            XAxis,
            YAxis,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public StairEntity WithParameters(
        StairViewKind? viewKind = null,
        double? width = null,
        int? treadCount = null,
        double? treadDepth = null,
        double? riserHeight = null,
        bool? showStructure = null,
        double? slabThickness = null)
    {
        return new StairEntity(
            InsertionPoint,
            viewKind ?? ViewKind,
            width ?? Width,
            treadCount ?? TreadCount,
            treadDepth ?? TreadDepth,
            riserHeight ?? RiserHeight,
            showStructure ?? ShowStructure,
            slabThickness ?? SlabThickness,
            XAxis,
            YAxis,
            Id,
            LayerId,
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
                "Stair numeric parameters must be finite positive values.");
        }
    }

    private static Vector2D NormalizeAxis(Vector2D axis, string paramName)
    {
        if (Tolerance.IsZero(axis.Length))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Stair axis cannot be a zero-length vector.");
        }

        return axis.Normalize();
    }
}
