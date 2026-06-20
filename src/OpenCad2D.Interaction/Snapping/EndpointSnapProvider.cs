using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Interaction.BlockReferences;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Interaction.Snapping;

/// <summary>
/// Provides endpoint snap candidates.
/// </summary>
public sealed class EndpointSnapProvider : ISnapProvider
{
    public SnapKind Kind => SnapKind.Endpoint;

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
                        SnapKind.Endpoint,
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
                foreach (Point2D point in GetEntityEndpoints(worldEntity))
                {
                    yield return (point, blockReference.Id);
                }
            }

            yield break;
        }

        foreach (Point2D point in GetEntityEndpoints(entity))
        {
            yield return (point, entity.Id);
        }
    }

    private static IEnumerable<Point2D> GetEntityEndpoints(CadEntity entity)
    {
        switch (entity)
        {
            case PointEntity point:
                yield return point.Position;
                break;

            case TextEntity text:
                yield return text.InsertionPoint;
                break;

            case MultilineTextEntity multilineText:
                yield return multilineText.InsertionPoint;
                break;

            case LineEntity line:
                yield return line.Start;
                yield return line.End;
                break;

            case PolylineEntity polyline:
                foreach (Point2D vertex in polyline.Vertices)
                {
                    yield return vertex;
                }

                break;

            case BezierSplineEntity spline:
                foreach (Point2D point in spline.ControlPoints)
                {
                    yield return point;
                }

                break;

            case ArcEntity arc:
                yield return arc.Geometry.StartPoint;
                yield return arc.Geometry.EndPoint;
                break;

            case EllipticalArcEntity ellipticalArc:
                yield return ellipticalArc.StartPoint;
                yield return ellipticalArc.EndPoint;
                break;

            case ImageReferenceEntity imageReference:
                foreach (Point2D corner in imageReference.GetCorners())
                {
                    yield return corner;
                }

                break;

            case StairEntity stair:
                foreach (Point2D endpoint in GetStairEndpoints(stair))
                {
                    yield return endpoint;
                }

                break;
        }
    }

    private static IEnumerable<Point2D> GetStairEndpoints(StairEntity stair)
    {
        var points = new List<Point2D>();

        foreach (LineSegment2D segment in stair.GetGeneratedGeometry().PrimarySegments)
        {
            AddDistinct(points, segment.Start);
            AddDistinct(points, segment.End);
        }

        return points;
    }

    private static void AddDistinct(
        List<Point2D> points,
        Point2D point)
    {
        bool alreadyExists = points.Any(existing =>
            GeometryTolerance.Default.AreCoordinatesEqual(existing, point));

        if (!alreadyExists)
        {
            points.Add(point);
        }
    }
}
