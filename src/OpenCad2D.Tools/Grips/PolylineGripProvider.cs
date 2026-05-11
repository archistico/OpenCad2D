using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides grips for open and closed polyline entities.
/// Rectangular closed four-vertex polylines are edited as rectangles, preserving
/// their right angles and orientation.
/// </summary>
public sealed class PolylineGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is PolylineEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        PolylineEntity polyline = GetPolyline(entity);

        if (TryGetRectangleFrame(polyline, out RectangleFrame rectangle))
        {
            return GetRectangleGrips(polyline, rectangle);
        }

        return GetGenericPolylineGrips(polyline);
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        PolylineEntity polyline = GetPolyline(entity);

        if (TryGetRectangleFrame(polyline, out RectangleFrame rectangle))
        {
            return ApplyRectangleGripMove(
                polyline,
                rectangle,
                gripIndex,
                destination);
        }

        return ApplyGenericPolylineGripMove(
            polyline,
            gripIndex,
            destination);
    }

    private static IReadOnlyList<GripPoint> GetGenericPolylineGrips(PolylineEntity polyline)
    {
        var grips = new List<GripPoint>();

        for (int i = 0; i < polyline.Vertices.Count; i++)
        {
            grips.Add(new GripPoint(
                polyline.Vertices[i],
                GripKind.MoveVertex,
                polyline.Id,
                i));
        }

        grips.Add(new GripPoint(
            GetCentroid(polyline.Vertices),
            GripKind.MoveEntity,
            polyline.Id,
            polyline.Vertices.Count));

        return grips;
    }

    private static IReadOnlyList<GripPoint> GetRectangleGrips(
        PolylineEntity polyline,
        RectangleFrame rectangle)
    {
        Point2D p0 = rectangle.GetPoint(0.0, 0.0);
        Point2D p1 = rectangle.GetPoint(rectangle.Width, 0.0);
        Point2D p2 = rectangle.GetPoint(rectangle.Width, rectangle.Height);
        Point2D p3 = rectangle.GetPoint(0.0, rectangle.Height);

        Point2D m01 = rectangle.GetPoint(rectangle.Width / 2.0, 0.0);
        Point2D m12 = rectangle.GetPoint(rectangle.Width, rectangle.Height / 2.0);
        Point2D m23 = rectangle.GetPoint(rectangle.Width / 2.0, rectangle.Height);
        Point2D m30 = rectangle.GetPoint(0.0, rectangle.Height / 2.0);
        Point2D center = rectangle.GetPoint(rectangle.Width / 2.0, rectangle.Height / 2.0);

        return new[]
        {
            new GripPoint(p0, GripKind.MoveVertex, polyline.Id, 0),
            new GripPoint(m01, GripKind.ResizeRadius, polyline.Id, 4),
            new GripPoint(p1, GripKind.MoveVertex, polyline.Id, 1),
            new GripPoint(m12, GripKind.ResizeRadius, polyline.Id, 5),
            new GripPoint(p2, GripKind.MoveVertex, polyline.Id, 2),
            new GripPoint(m23, GripKind.ResizeRadius, polyline.Id, 6),
            new GripPoint(p3, GripKind.MoveVertex, polyline.Id, 3),
            new GripPoint(m30, GripKind.ResizeRadius, polyline.Id, 7),
            new GripPoint(center, GripKind.MoveEntity, polyline.Id, 8)
        };
    }

    private static CadEntity ApplyGenericPolylineGripMove(
        PolylineEntity polyline,
        int gripIndex,
        Point2D destination)
    {
        if (gripIndex == polyline.Vertices.Count)
        {
            Vector2D vector = GetCentroid(polyline.Vertices).VectorTo(destination);

            return new PolylineEntity(
                polyline.Vertices.Select(vertex => vertex + vector),
                polyline.IsClosed,
                polyline.Id,
                polyline.LayerId,
                polyline.Style,
                polyline.IsVisible,
                polyline.IsLocked,
                polyline.DrawOrder);
        }

        if (gripIndex < 0 || gripIndex >= polyline.Vertices.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown polyline grip index.");
        }

        Point2D[] vertices = polyline.Vertices.ToArray();
        vertices[gripIndex] = destination;

        return new PolylineEntity(
            vertices,
            polyline.IsClosed,
            polyline.Id,
            polyline.LayerId,
            polyline.Style,
            polyline.IsVisible,
            polyline.IsLocked,
            polyline.DrawOrder);
    }

    private static CadEntity ApplyRectangleGripMove(
        PolylineEntity polyline,
        RectangleFrame rectangle,
        int gripIndex,
        Point2D destination)
    {
        if (gripIndex == 8)
        {
            Vector2D vector = rectangle.GetPoint(
                    rectangle.Width / 2.0,
                    rectangle.Height / 2.0)
                .VectorTo(destination);

            return CreatePolyline(
                polyline,
                rectangle.Origin + vector,
                rectangle.AxisX,
                rectangle.AxisY,
                rectangle.Width,
                rectangle.Height);
        }

        if (gripIndex < 0 || gripIndex > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown rectangle grip index.");
        }

        double projectedX = rectangle.ProjectX(destination);
        double projectedY = rectangle.ProjectY(destination);

        double minX = 0.0;
        double maxX = rectangle.Width;
        double minY = 0.0;
        double maxY = rectangle.Height;

        switch (gripIndex)
        {
            case 0:
                minX = projectedX;
                minY = projectedY;
                break;

            case 1:
                maxX = projectedX;
                minY = projectedY;
                break;

            case 2:
                maxX = projectedX;
                maxY = projectedY;
                break;

            case 3:
                minX = projectedX;
                maxY = projectedY;
                break;

            case 4:
                minY = projectedY;
                break;

            case 5:
                maxX = projectedX;
                break;

            case 6:
                maxY = projectedY;
                break;

            case 7:
                minX = projectedX;
                break;
        }

        double width = maxX - minX;
        double height = maxY - minY;

        if (Tolerance.IsZero(width) || Tolerance.IsZero(height))
        {
            return polyline;
        }

        Point2D origin = rectangle.GetPoint(minX, minY);
        Vector2D axisX = width >= 0.0
            ? rectangle.AxisX
            : rectangle.AxisX * -1.0;
        Vector2D axisY = height >= 0.0
            ? rectangle.AxisY
            : rectangle.AxisY * -1.0;

        return CreatePolyline(
            polyline,
            origin,
            axisX,
            axisY,
            Math.Abs(width),
            Math.Abs(height));
    }

    private static PolylineEntity CreatePolyline(
        PolylineEntity source,
        Point2D origin,
        Vector2D axisX,
        Vector2D axisY,
        double width,
        double height)
    {
        var vertices = new[]
        {
            origin,
            origin + axisX * width,
            origin + axisX * width + axisY * height,
            origin + axisY * height
        };

        return new PolylineEntity(
            vertices,
            isClosed: true,
            source.Id,
            source.LayerId,
            source.Style,
            source.IsVisible,
            source.IsLocked,
            source.DrawOrder);
    }

    private static bool TryGetRectangleFrame(
        PolylineEntity polyline,
        out RectangleFrame rectangle)
    {
        rectangle = default;

        if (!polyline.IsClosed || polyline.Vertices.Count != 4)
        {
            return false;
        }

        Point2D p0 = polyline.Vertices[0];
        Point2D p1 = polyline.Vertices[1];
        Point2D p2 = polyline.Vertices[2];
        Point2D p3 = polyline.Vertices[3];

        Vector2D xVector = p0.VectorTo(p1);
        Vector2D yVector = p0.VectorTo(p3);
        double width = xVector.Length;
        double height = yVector.Length;

        if (Tolerance.IsZero(width) || Tolerance.IsZero(height))
        {
            return false;
        }

        Vector2D axisX = xVector / width;
        Vector2D axisY = yVector / height;

        if (!Tolerance.IsZero(axisX.Dot(axisY)))
        {
            return false;
        }

        Point2D expectedP2 = p1 + yVector;

        if (p2.DistanceTo(expectedP2) > Tolerance.Default)
        {
            return false;
        }

        rectangle = new RectangleFrame(
            p0,
            axisX,
            axisY,
            width,
            height);

        return true;
    }

    private static Point2D GetCentroid(IReadOnlyList<Point2D> vertices)
    {
        if (vertices.Count == 0)
        {
            return Point2D.Origin;
        }

        return new Point2D(
            vertices.Average(vertex => vertex.X),
            vertices.Average(vertex => vertex.Y));
    }

    private static PolylineEntity GetPolyline(CadEntity entity)
    {
        return entity as PolylineEntity
            ?? throw new ArgumentException(
                "Entity must be a polyline entity.",
                nameof(entity));
    }

    private readonly record struct RectangleFrame(
        Point2D Origin,
        Vector2D AxisX,
        Vector2D AxisY,
        double Width,
        double Height)
    {
        public Point2D GetPoint(double x, double y)
        {
            return Origin + AxisX * x + AxisY * y;
        }

        public double ProjectX(Point2D point)
        {
            return Origin.VectorTo(point).Dot(AxisX);
        }

        public double ProjectY(Point2D point)
        {
            return Origin.VectorTo(point).Dot(AxisY);
        }
    }
}
