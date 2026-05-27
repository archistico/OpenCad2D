using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Interaction.BlockReferences;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides midpoint snap candidates.
/// </summary>
public sealed class MidpointSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Midpoint;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        foreach (CadEntity entity in request.Document.GetVisibleEntities(request.SearchArea))
        {
            foreach ((Point2D point, EntityId entityId) in GetCandidatePoints(request, entity))
            {
                double distance = request.CursorPoint.DistanceTo(point);

                if (distance <= request.Tolerance)
                {
                    yield return new SnapCandidate(
                        SnapKind.Midpoint,
                        point,
                        entityId,
                        distance);
                }
            }
        }
    }

    private static IEnumerable<(Point2D Point, EntityId EntityId)> GetCandidatePoints(
        SnapRequest request,
        CadEntity entity)
    {
        if (entity is BlockReferenceEntity blockReference)
        {
            foreach (CadEntity worldEntity in BlockReferenceGeometryResolver.GetWorldEntities(
                request.Document,
                blockReference))
            {
                foreach (Point2D point in GetEntityMidpoints(worldEntity))
                {
                    yield return (point, blockReference.Id);
                }
            }

            yield break;
        }

        foreach (Point2D point in GetEntityMidpoints(entity))
        {
            yield return (point, entity.Id);
        }
    }

    private static IEnumerable<Point2D> GetEntityMidpoints(CadEntity entity)
    {
        switch (entity)
        {
            case LineEntity line:
                yield return line.Geometry.Midpoint;
                break;

            case PolylineEntity polyline:
                foreach (LineSegment2D segment in polyline.Geometry.GetSegments())
                {
                    yield return segment.Midpoint;
                }

                break;

            case ArcEntity arc:
                yield return GetArcMidpoint(arc.Geometry);
                break;

            case EllipticalArcEntity ellipticalArc:
                yield return ellipticalArc.GetPointAt(GetEllipticalArcMidParameter(ellipticalArc));
                break;

            case BezierSplineEntity spline:
                foreach (LineSegment2D segment in spline.ToPolylineApproximation().Geometry.GetSegments())
                {
                    yield return segment.Midpoint;
                }

                break;

            case ImageReferenceEntity imageReference:
                yield return Midpoint(imageReference.BottomLeft, imageReference.BottomRight);
                yield return Midpoint(imageReference.BottomRight, imageReference.TopRight);
                yield return Midpoint(imageReference.TopRight, imageReference.TopLeft);
                yield return Midpoint(imageReference.TopLeft, imageReference.BottomLeft);
                break;
        }
    }

    private static Point2D Midpoint(
        Point2D first,
        Point2D second)
    {
        return new Point2D(
            (first.X + second.X) / 2.0,
            (first.Y + second.Y) / 2.0);
    }

    private static double GetEllipticalArcMidParameter(EllipticalArcEntity ellipticalArc)
    {
        double signedSweep = ellipticalArc.IsCounterClockwise
            ? ellipticalArc.SweepRadians
            : -ellipticalArc.SweepRadians;

        return ellipticalArc.StartParameterRadians + signedSweep / 2.0;
    }

    private static Point2D GetArcMidpoint(Arc2D arc)
    {
        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;

        double sweep;

        if (arc.IsCounterClockwise)
        {
            if (end < start)
            {
                end += 2.0 * Math.PI;
            }

            sweep = end - start;
            return arc.PointAt(Angle.FromRadians(start + sweep / 2.0));
        }

        if (start < end)
        {
            start += 2.0 * Math.PI;
        }

        sweep = start - end;
        return arc.PointAt(Angle.FromRadians(start - sweep / 2.0));
    }
}