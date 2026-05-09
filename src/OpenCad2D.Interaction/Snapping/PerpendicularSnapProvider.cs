using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides perpendicular snap candidates.
/// This is a contextual snap and requires a base point.
/// </summary>
public sealed class PerpendicularSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Perpendicular;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        if (request.BasePoint is null)
        {
            yield break;
        }

        Point2D basePoint = request.BasePoint.Value;

        foreach (CadEntity entity in request.Document.GetVisibleEntities())
        {
            foreach (Point2D candidatePoint in GetPerpendicularCandidates(entity, basePoint))
            {
                double distance = request.CursorPoint.DistanceTo(candidatePoint);

                if (distance <= request.Tolerance)
                {
                    yield return new SnapCandidate(
                        SnapKind.Perpendicular,
                        candidatePoint,
                        entity.Id,
                        distance);
                }
            }
        }
    }

    private static IEnumerable<Point2D> GetPerpendicularCandidates(
        CadEntity entity,
        Point2D basePoint)
    {
        switch (entity)
        {
            case LineEntity line:
                Point2D? linePoint = TryProjectPointInsideSegment(
                    basePoint,
                    line.Geometry);

                if (linePoint is not null)
                {
                    yield return linePoint.Value;
                }

                break;

            case PolylineEntity polyline:
                foreach (LineSegment2D segment in polyline.Geometry.GetSegments())
                {
                    Point2D? polylinePoint = TryProjectPointInsideSegment(
                        basePoint,
                        segment);

                    if (polylinePoint is not null)
                    {
                        yield return polylinePoint.Value;
                    }
                }

                break;

            case CircleEntity circle:
                yield return DistanceService.ClosestPointOnCircle(
                    basePoint,
                    circle.Geometry);

                break;

            case ArcEntity arc:
                Point2D pointOnArcCircle =
                    DistanceService.ClosestPointOnCircle(
                        basePoint,
                        new Circle2D(arc.Center, arc.Radius));

                if (arc.Geometry.ContainsPoint(pointOnArcCircle))
                {
                    yield return pointOnArcCircle;
                }

                break;
        }
    }

    private static Point2D? TryProjectPointInsideSegment(
        Point2D point,
        LineSegment2D segment)
    {
        Vector2D segmentVector = segment.Start.VectorTo(segment.End);
        double lengthSquared = segmentVector.LengthSquared;

        if (Tolerance.IsZero(lengthSquared))
        {
            return null;
        }

        Vector2D startToPoint = segment.Start.VectorTo(point);
        double t = startToPoint.Dot(segmentVector) / lengthSquared;

        if (t < 0.0 || t > 1.0)
        {
            return null;
        }

        return new Point2D(
            segment.Start.X + segmentVector.X * t,
            segment.Start.Y + segmentVector.Y * t);
    }
}