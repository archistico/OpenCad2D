using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides tangent snap candidates from a base point to circles and arcs.
/// </summary>
public sealed class TangentSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Tangent;

    public IEnumerable<SnapCandidate> GetCandidates(SnapRequest request)
    {
        if (request.BasePoint is null)
        {
            yield break;
        }

        Point2D basePoint = request.BasePoint.Value;

        foreach (CadEntity entity in request.Document.GetVisibleEntities())
        {
            foreach (Point2D tangentPoint in GetTangentPoints(entity, basePoint))
            {
                double distance = request.CursorPoint.DistanceTo(tangentPoint);

                if (distance <= request.Tolerance)
                {
                    yield return new SnapCandidate(
                        SnapKind.Tangent,
                        tangentPoint,
                        entity.Id,
                        distance);
                }
            }
        }
    }

    private static IEnumerable<Point2D> GetTangentPoints(
        CadEntity entity,
        Point2D basePoint)
    {
        switch (entity)
        {
            case CircleEntity circle:
                foreach (Point2D point in GetCircleTangentPoints(
                             basePoint,
                             circle.Center,
                             circle.Radius))
                {
                    yield return point;
                }

                break;

            case ArcEntity arc:
                foreach (Point2D point in GetCircleTangentPoints(
                             basePoint,
                             arc.Center,
                             arc.Radius))
                {
                    if (arc.Geometry.ContainsPoint(point))
                    {
                        yield return point;
                    }
                }

                break;
        }
    }

    private static IReadOnlyList<Point2D> GetCircleTangentPoints(
        Point2D externalPoint,
        Point2D center,
        double radius)
    {
        Vector2D centerToPoint = center.VectorTo(externalPoint);

        double distanceSquared = centerToPoint.LengthSquared;
        double radiusSquared = radius * radius;

        if (distanceSquared <= radiusSquared || Tolerance.AreEqual(distanceSquared, radiusSquared))
        {
            return Array.Empty<Point2D>();
        }

        double distance = Math.Sqrt(distanceSquared);

        Vector2D unit = centerToPoint / distance;
        Vector2D perpendicular = unit.PerpendicularLeft();

        double along = radiusSquared / distance;
        double height = radius * Math.Sqrt(distanceSquared - radiusSquared) / distance;

        Point2D basePointOnDirection = center + unit * along;

        Point2D first = basePointOnDirection + perpendicular * height;
        Point2D second = basePointOnDirection - perpendicular * height;

        return new[]
        {
            first,
            second
        };
    }
}