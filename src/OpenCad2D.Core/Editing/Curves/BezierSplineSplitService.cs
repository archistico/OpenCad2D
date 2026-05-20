using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing.Curves;

/// <summary>
/// Splits open Bezier splines using De Casteljau subdivision, preserving native
/// Bezier control points instead of converting the edited result to a polyline.
/// </summary>
public sealed class BezierSplineSplitService
{
    public IReadOnlyList<BezierSplineEntity> SplitAt(
        BezierSplineEntity spline,
        double parameter,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(spline);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (spline.IsClosed)
        {
            return Array.Empty<BezierSplineEntity>();
        }

        double t = Math.Clamp(parameter, 0.0, 1.0);
        if (t <= effectiveTolerance.Parameter ||
            t >= 1.0 - effectiveTolerance.Parameter)
        {
            return Array.Empty<BezierSplineEntity>();
        }

        SplitControlPolygon(
            spline.ControlPoints,
            t,
            out IReadOnlyList<Point2D> left,
            out IReadOnlyList<Point2D> right);

        return new[]
        {
            CreateLike(spline, left),
            CreateLike(spline, right)
        };
    }

    public BezierSplineEntity? ExtractInterval(
        BezierSplineEntity spline,
        double startParameter,
        double endParameter,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(spline);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (spline.IsClosed)
        {
            return null;
        }

        double start = Math.Clamp(startParameter, 0.0, 1.0);
        double end = Math.Clamp(endParameter, 0.0, 1.0);

        if (end < start)
        {
            (start, end) = (end, start);
        }

        if (end - start <= effectiveTolerance.Parameter)
        {
            return null;
        }

        if (start <= effectiveTolerance.Parameter &&
            end >= 1.0 - effectiveTolerance.Parameter)
        {
            return CreateLike(spline, spline.ControlPoints);
        }

        if (start <= effectiveTolerance.Parameter)
        {
            SplitControlPolygon(
                spline.ControlPoints,
                end,
                out IReadOnlyList<Point2D> left,
                out _);
            return CreateLike(spline, left);
        }

        if (end >= 1.0 - effectiveTolerance.Parameter)
        {
            SplitControlPolygon(
                spline.ControlPoints,
                start,
                out _,
                out IReadOnlyList<Point2D> right);
            return CreateLike(spline, right);
        }

        SplitControlPolygon(
            spline.ControlPoints,
            start,
            out _,
            out IReadOnlyList<Point2D> afterStart);

        double localEnd = (end - start) / (1.0 - start);
        SplitControlPolygon(
            afterStart,
            localEnd,
            out IReadOnlyList<Point2D> interval,
            out _);

        return CreateLike(spline, interval);
    }

    public IReadOnlyList<BezierSplineEntity> RemoveInterval(
        BezierSplineEntity spline,
        double startParameter,
        double endParameter,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(spline);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;

        if (spline.IsClosed)
        {
            return Array.Empty<BezierSplineEntity>();
        }

        double start = Math.Clamp(startParameter, 0.0, 1.0);
        double end = Math.Clamp(endParameter, 0.0, 1.0);

        if (end < start)
        {
            (start, end) = (end, start);
        }

        if (end - start <= effectiveTolerance.Parameter)
        {
            return Array.Empty<BezierSplineEntity>();
        }

        var result = new List<BezierSplineEntity>();

        if (start > effectiveTolerance.Parameter)
        {
            BezierSplineEntity? left = ExtractInterval(
                spline,
                0.0,
                start,
                effectiveTolerance);
            if (left is not null)
            {
                result.Add(left);
            }
        }

        if (end < 1.0 - effectiveTolerance.Parameter)
        {
            BezierSplineEntity? right = ExtractInterval(
                spline,
                end,
                1.0,
                effectiveTolerance);
            if (right is not null)
            {
                result.Add(right);
            }
        }

        return result;
    }

    public static Point2D Evaluate(
        BezierSplineEntity spline,
        double parameter)
    {
        ArgumentNullException.ThrowIfNull(spline);
        return Evaluate(spline.ControlPoints, parameter);
    }

    public static Point2D Evaluate(
        IReadOnlyList<Point2D> controlPoints,
        double parameter)
    {
        ArgumentNullException.ThrowIfNull(controlPoints);

        if (controlPoints.Count == 0)
        {
            throw new ArgumentException(
                "A Bezier control polygon requires at least one point.",
                nameof(controlPoints));
        }

        double t = Math.Clamp(parameter, 0.0, 1.0);
        var working = controlPoints.ToList();

        for (int level = working.Count - 1; level > 0; level--)
        {
            for (int index = 0; index < level; index++)
            {
                working[index] = Lerp(working[index], working[index + 1], t);
            }
        }

        return working[0];
    }

    private static void SplitControlPolygon(
        IReadOnlyList<Point2D> controlPoints,
        double parameter,
        out IReadOnlyList<Point2D> left,
        out IReadOnlyList<Point2D> right)
    {
        ArgumentNullException.ThrowIfNull(controlPoints);

        if (controlPoints.Count < 2)
        {
            throw new ArgumentException(
                "A Bezier control polygon requires at least two points.",
                nameof(controlPoints));
        }

        double t = Math.Clamp(parameter, 0.0, 1.0);
        var current = controlPoints.ToList();
        var leftPoints = new List<Point2D>(controlPoints.Count)
        {
            current[0]
        };
        var rightPoints = new List<Point2D>(controlPoints.Count)
        {
            current[^1]
        };

        while (current.Count > 1)
        {
            var next = new List<Point2D>(current.Count - 1);
            for (int index = 0; index < current.Count - 1; index++)
            {
                next.Add(Lerp(current[index], current[index + 1], t));
            }

            leftPoints.Add(next[0]);
            rightPoints.Add(next[^1]);
            current = next;
        }

        rightPoints.Reverse();
        left = leftPoints;
        right = rightPoints;
    }

    private static BezierSplineEntity CreateLike(
        BezierSplineEntity source,
        IEnumerable<Point2D> controlPoints)
    {
        return new BezierSplineEntity(
            controlPoints,
            isClosed: false,
            layerId: source.LayerId,
            style: source.Style,
            isVisible: source.IsVisible,
            isLocked: source.IsLocked,
            drawOrder: source.DrawOrder);
    }

    private static Point2D Lerp(
        Point2D first,
        Point2D second,
        double t)
    {
        return new Point2D(
            first.X + ((second.X - first.X) * t),
            first.Y + ((second.Y - first.Y) * t));
    }
}
