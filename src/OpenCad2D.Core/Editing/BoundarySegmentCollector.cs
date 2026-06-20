using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Collects the linear graph input used by Boundary Fill.
/// Curved boundaries are sampled only when the caller explicitly enables them.
/// </summary>
public sealed class BoundarySegmentCollector
{
    public BoundarySegmentCollection Collect(
        IEnumerable<CadEntity> entities,
        BoundaryFillOptions options)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(options);

        var segments = new List<BoundarySegment>();
        int ignoredEntityCount = 0;
        int sampledCurveSegmentCount = 0;
        GeometryTolerance tolerance = options.GeometryTolerance;

        foreach (CadEntity entity in entities)
        {
            int before = segments.Count;

            switch (entity)
            {
                case LineEntity line when line.Start.DistanceTo(line.End) > tolerance.Distance:
                    AddSegment(
                        segments,
                        line.Start,
                        line.End,
                        line,
                        BoundarySegmentSourceKind.Line,
                        isSampledCurve: false,
                        tolerance);
                    break;

                case PolylineEntity polyline when !polyline.HasArcSegments:
                    AddPolylineSegments(
                        segments,
                        polyline,
                        BoundarySegmentSourceKind.Polyline,
                        isSampledCurve: false,
                        tolerance);
                    break;

                case PolylineEntity polyline when options.IncludeCurveBoundaries:
                    int sampledBefore = segments.Count;
                    PolylineEntity approximation = polyline.ToPolylineApproximation(options.CurveSampleCount);
                    AddPolylineSegments(
                        segments,
                        approximation,
                        BoundarySegmentSourceKind.Polyline,
                        isSampledCurve: true,
                        tolerance,
                        sourceEntity: polyline);
                    sampledCurveSegmentCount += segments.Count - sampledBefore;
                    break;

                case CircleEntity circle when options.IncludeCurveBoundaries:
                    int circleBefore = segments.Count;
                    AddCircleSegments(segments, circle, options, tolerance);
                    sampledCurveSegmentCount += segments.Count - circleBefore;
                    break;

                case ArcEntity arc when options.IncludeCurveBoundaries:
                    int arcBefore = segments.Count;
                    AddArcSegments(segments, arc, options, tolerance);
                    sampledCurveSegmentCount += segments.Count - arcBefore;
                    break;
            }

            if (segments.Count == before)
            {
                ignoredEntityCount++;
            }
        }

        return new BoundarySegmentCollection(
            segments,
            ignoredEntityCount,
            sampledCurveSegmentCount);
    }

    private static void AddPolylineSegments(
        List<BoundarySegment> segments,
        PolylineEntity polyline,
        BoundarySegmentSourceKind sourceKind,
        bool isSampledCurve,
        GeometryTolerance tolerance,
        CadEntity? sourceEntity = null)
    {
        IReadOnlyList<Point2D> vertices = polyline.Vertices;

        if (vertices.Count < 2)
        {
            return;
        }

        CadEntity source = sourceEntity ?? polyline;
        int segmentCount = polyline.IsClosed
            ? vertices.Count
            : vertices.Count - 1;

        for (int index = 0; index < segmentCount; index++)
        {
            AddSegment(
                segments,
                vertices[index],
                vertices[(index + 1) % vertices.Count],
                source,
                sourceKind,
                isSampledCurve,
                tolerance);
        }
    }

    private static void AddCircleSegments(
        List<BoundarySegment> segments,
        CircleEntity circle,
        BoundaryFillOptions options,
        GeometryTolerance tolerance)
    {
        int count = Math.Max(8, options.CurveSampleCount);
        var points = new List<Point2D>(count);

        for (int index = 0; index < count; index++)
        {
            double angle = Math.Tau * index / count;
            points.Add(new Point2D(
                circle.Center.X + Math.Cos(angle) * circle.Radius,
                circle.Center.Y + Math.Sin(angle) * circle.Radius));
        }

        for (int index = 0; index < count; index++)
        {
            AddSegment(
                segments,
                points[index],
                points[(index + 1) % count],
                circle,
                BoundarySegmentSourceKind.Circle,
                isSampledCurve: true,
                tolerance);
        }
    }

    private static void AddArcSegments(
        List<BoundarySegment> segments,
        ArcEntity arc,
        BoundaryFillOptions options,
        GeometryTolerance tolerance)
    {
        double sweep = GetArcSweepRadians(arc);

        if (sweep <= tolerance.Angle)
        {
            return;
        }

        int count = Math.Max(
            2,
            (int)Math.Ceiling(options.CurveSampleCount * sweep / Math.Tau));

        var points = new List<Point2D>(count + 1);
        double start = arc.StartAngle.Radians;
        double direction = arc.IsCounterClockwise ? 1.0 : -1.0;

        for (int index = 0; index <= count; index++)
        {
            double t = index / (double)count;
            double angle = start + direction * sweep * t;
            points.Add(new Point2D(
                arc.Center.X + Math.Cos(angle) * arc.Radius,
                arc.Center.Y + Math.Sin(angle) * arc.Radius));
        }

        points[0] = arc.Geometry.StartPoint;
        points[^1] = arc.Geometry.EndPoint;

        for (int index = 0; index < points.Count - 1; index++)
        {
            AddSegment(
                segments,
                points[index],
                points[index + 1],
                arc,
                BoundarySegmentSourceKind.Arc,
                isSampledCurve: true,
                tolerance);
        }
    }

    private static double GetArcSweepRadians(ArcEntity arc)
    {
        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;
        double sweep = arc.IsCounterClockwise
            ? NormalizePositiveRadians(end - start)
            : NormalizePositiveRadians(start - end);

        return sweep;
    }

    private static double NormalizePositiveRadians(double radians)
    {
        double value = radians % Math.Tau;

        if (value < 0.0)
        {
            value += Math.Tau;
        }

        return value;
    }

    private static void AddSegment(
        List<BoundarySegment> segments,
        Point2D start,
        Point2D end,
        CadEntity sourceEntity,
        BoundarySegmentSourceKind sourceKind,
        bool isSampledCurve,
        GeometryTolerance tolerance)
    {
        if (start.DistanceTo(end) <= tolerance.Distance)
        {
            return;
        }

        segments.Add(new BoundarySegment(
            start,
            end,
            sourceEntity.Id,
            sourceKind,
            isSampledCurve));
    }
}
