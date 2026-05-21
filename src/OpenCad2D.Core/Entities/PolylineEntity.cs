using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;

namespace OpenCad2D.Core.Entities;

/// <summary>
/// CAD entity representing a polyline made of straight segments.
/// </summary>
public sealed class PolylineEntity : CadEntity, IFillableEntity
{
    public PolylineEntity(
        IEnumerable<Point2D> vertices,
        bool isClosed = false,
        EntityId? id = null,
        LayerId? layerId = null,
        EntityStyle? style = null,
        bool isVisible = true,
        bool isLocked = false,
        int drawOrder = 0,
        bool isFilled = false)
        : base(
            id ?? EntityId.New(),
            layerId ?? LayerId.Default,
            style ?? EntityStyle.ByLayer,
            isVisible,
            isLocked,
            drawOrder)
    {
        Geometry = new Polyline2D(vertices, isClosed);
        IsFilled = isFilled;
    }

    public Polyline2D Geometry { get; }

    public IReadOnlyList<Point2D> Vertices => Geometry.Vertices;

    public bool IsClosed => Geometry.IsClosed;

    public bool IsFilled { get; }

    public override EntityKind Kind => EntityKind.Polyline;

    public override BoundingBox2D GetBoundingBox()
    {
        return Geometry.GetBoundingBox();
    }

    public override double DistanceTo(Point2D point)
    {
        return DistanceService.DistancePointToPolyline(point, Geometry);
    }

    public override Point2D GetClosestPoint(Point2D point)
    {
        return DistanceService.ClosestPointOnPolyline(point, Geometry);
    }

    public override CadEntity Transform(Matrix2D matrix)
    {
        var transformedVertices = Vertices
            .Select(matrix.Transform)
            .ToList();

        return new PolylineEntity(
            transformedVertices,
            IsClosed,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled);
    }

    public override CadEntity WithId(EntityId id)
    {
        return new PolylineEntity(
            Vertices,
            IsClosed,
            id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled);
    }

    public override CadEntity WithLayer(LayerId layerId)
    {
        return new PolylineEntity(
            Vertices,
            IsClosed,
            Id,
            layerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            IsFilled);
    }

    public CadEntity WithFill(bool isFilled)
    {
        return new PolylineEntity(
            Vertices,
            IsClosed,
            Id,
            LayerId,
            Style,
            IsVisible,
            IsLocked,
            DrawOrder,
            isFilled);
    }
}
