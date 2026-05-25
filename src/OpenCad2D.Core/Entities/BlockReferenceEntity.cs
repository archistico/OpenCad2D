using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing an inserted instance of a reusable block definition.
/// </summary>
public sealed class BlockReferenceEntity : CadEntity
{
    public BlockReferenceEntity(
        BlockDefinitionId blockDefinitionId,
        Point2D insertionPoint,
        Vector2D xAxis,
        Vector2D yAxis,
        BoundingBox2D definitionBounds,
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
        if (xAxis.Length <= 0)
        {
            throw new ArgumentException("Block reference X axis cannot be zero-length.", nameof(xAxis));
        }

        if (yAxis.Length <= 0)
        {
            throw new ArgumentException("Block reference Y axis cannot be zero-length.", nameof(yAxis));
        }

        BlockDefinitionId = blockDefinitionId;
        InsertionPoint = insertionPoint;
        XAxis = xAxis;
        YAxis = yAxis;
        DefinitionBounds = definitionBounds;
    }

    public BlockDefinitionId BlockDefinitionId { get; }

    public Point2D InsertionPoint { get; }

    public Vector2D XAxis { get; }

    public Vector2D YAxis { get; }

    public BoundingBox2D DefinitionBounds { get; }

    public Matrix2D LocalToWorldMatrix => new(
        XAxis.X,
        YAxis.X,
        XAxis.Y,
        YAxis.Y,
        InsertionPoint.X,
        InsertionPoint.Y);

    public override EntityKind Kind => EntityKind.BlockReference;

    public override BoundingBox2D GetBoundingBox()
    {
        Point2D bottomLeft = TransformLocalPoint(new Point2D(DefinitionBounds.MinX, DefinitionBounds.MinY));
        Point2D bottomRight = TransformLocalPoint(new Point2D(DefinitionBounds.MaxX, DefinitionBounds.MinY));
        Point2D topRight = TransformLocalPoint(new Point2D(DefinitionBounds.MaxX, DefinitionBounds.MaxY));
        Point2D topLeft = TransformLocalPoint(new Point2D(DefinitionBounds.MinX, DefinitionBounds.MaxY));

        double minX = Math.Min(Math.Min(bottomLeft.X, bottomRight.X), Math.Min(topRight.X, topLeft.X));
        double minY = Math.Min(Math.Min(bottomLeft.Y, bottomRight.Y), Math.Min(topRight.Y, topLeft.Y));
        double maxX = Math.Max(Math.Max(bottomLeft.X, bottomRight.X), Math.Max(topRight.X, topLeft.X));
        double maxY = Math.Max(Math.Max(bottomLeft.Y, bottomRight.Y), Math.Max(topRight.Y, topLeft.Y));

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public override double DistanceTo(Point2D point)
    {
        return point.DistanceTo(GetClosestPoint(point));
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        IReadOnlyList<LineSegment2D> edges = GetWorldBoundaryEdges();
        Point2D closest = DistanceService.ClosestPointOnSegment(point, edges[0]);
        double bestDistance = point.DistanceTo(closest);

        foreach (LineSegment2D edge in edges.Skip(1))
        {
            Point2D candidate = DistanceService.ClosestPointOnSegment(point, edge);
            double distance = point.DistanceTo(candidate);

            if (distance < bestDistance)
            {
                closest = candidate;
                bestDistance = distance;
            }
        }

        return closest;
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        return new BlockReferenceEntity(
            BlockDefinitionId,
            matrix.Transform(InsertionPoint),
            matrix.Transform(XAxis),
            matrix.Transform(YAxis),
            DefinitionBounds,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new BlockReferenceEntity(
            BlockDefinitionId,
            InsertionPoint,
            XAxis,
            YAxis,
            DefinitionBounds,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new BlockReferenceEntity(
            BlockDefinitionId,
            InsertionPoint,
            XAxis,
            YAxis,
            DefinitionBounds,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder);
    }

    public CadEntity TransformContainedEntity(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity.Transform(LocalToWorldMatrix);
    }

    private Point2D TransformLocalPoint(Point2D point)
    {
        return LocalToWorldMatrix.Transform(point);
    }

    private IReadOnlyList<LineSegment2D> GetWorldBoundaryEdges()
    {
        Point2D bottomLeft = TransformLocalPoint(new Point2D(DefinitionBounds.MinX, DefinitionBounds.MinY));
        Point2D bottomRight = TransformLocalPoint(new Point2D(DefinitionBounds.MaxX, DefinitionBounds.MinY));
        Point2D topRight = TransformLocalPoint(new Point2D(DefinitionBounds.MaxX, DefinitionBounds.MaxY));
        Point2D topLeft = TransformLocalPoint(new Point2D(DefinitionBounds.MinX, DefinitionBounds.MaxY));

        return new[]
        {
            new LineSegment2D(bottomLeft, bottomRight),
            new LineSegment2D(bottomRight, topRight),
            new LineSegment2D(topRight, topLeft),
            new LineSegment2D(topLeft, bottomLeft)
        };
    }
}
