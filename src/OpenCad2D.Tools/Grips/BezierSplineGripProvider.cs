using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Grips;

/// <summary>
/// Provides grips for Bezier spline control points.
/// </summary>
public sealed class BezierSplineGripProvider : IGripProvider
{
    public bool CanHandle(CadEntity entity)
    {
        return entity is BezierSplineEntity;
    }

    public IReadOnlyList<GripPoint> GetGrips(CadEntity entity)
    {
        BezierSplineEntity spline = GetSpline(entity);
        var grips = new List<GripPoint>();

        for (int i = 0; i < spline.ControlPoints.Count; i++)
        {
            grips.Add(new GripPoint(
                spline.ControlPoints[i],
                GripKind.MoveVertex,
                spline.Id,
                i));
        }

        grips.Add(new GripPoint(
            GetCentroid(spline.ControlPoints),
            GripKind.MoveEntity,
            spline.Id,
            spline.ControlPoints.Count));

        return grips;
    }

    public CadEntity ApplyGripMove(
        CadEntity entity,
        int gripIndex,
        Point2D destination)
    {
        BezierSplineEntity spline = GetSpline(entity);

        if (gripIndex == spline.ControlPoints.Count)
        {
            Vector2D vector = GetCentroid(spline.ControlPoints).VectorTo(destination);
            return new BezierSplineEntity(
                spline.ControlPoints.Select(point => point + vector),
                spline.IsClosed,
                spline.Id,
                spline.LayerId,
                spline.Style,
                spline.IsVisible,
                spline.IsLocked,
                spline.DrawOrder);
        }

        if (gripIndex < 0 || gripIndex >= spline.ControlPoints.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gripIndex),
                gripIndex,
                "Unknown spline grip index.");
        }

        Point2D[] points = spline.ControlPoints.ToArray();
        points[gripIndex] = destination;

        return new BezierSplineEntity(
            points,
            spline.IsClosed,
            spline.Id,
            spline.LayerId,
            spline.Style,
            spline.IsVisible,
            spline.IsLocked,
            spline.DrawOrder);
    }

    private static Point2D GetCentroid(IReadOnlyList<Point2D> points)
    {
        return new Point2D(
            points.Average(point => point.X),
            points.Average(point => point.Y));
    }

    private static BezierSplineEntity GetSpline(CadEntity entity)
    {
        return entity as BezierSplineEntity
            ?? throw new ArgumentException(
                "Entity must be a Bezier spline entity.",
                nameof(entity));
    }
}
