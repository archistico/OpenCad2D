using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Operations;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Builds a filled closed polyline from the visible linear boundaries around a seed point.
/// </summary>
public sealed class BoundaryFillService
{
    public BoundaryFillResult CreateFilledPolyline(
        IEnumerable<CadEntity> boundaryEntities,
        Point2D seedPoint,
        LayerId layerId,
        GeometryTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(boundaryEntities);

        GeometryTolerance effectiveTolerance = tolerance ?? GeometryTolerance.Default;
        IReadOnlyList<BoundarySegment> sourceSegments = CollectLinearSegments(boundaryEntities, effectiveTolerance);

        if (sourceSegments.Count == 0)
        {
            return BoundaryFillResult.Failure("Boundary fill needs visible line or straight polyline boundaries.");
        }

        PlanarGraph graph = BuildPlanarGraph(sourceSegments, effectiveTolerance);

        if (graph.EdgeCount == 0)
        {
            return BoundaryFillResult.Failure("No usable boundary segments were found.");
        }

        IReadOnlyList<IReadOnlyList<Point2D>> faces = FindInteriorFaces(graph, effectiveTolerance);
        IReadOnlyList<Point2D>? containingFace = faces
            .Where(face => ContainsPoint(face, seedPoint, effectiveTolerance))
            .OrderBy(face => Math.Abs(SignedArea(face)))
            .FirstOrDefault();

        if (containingFace is null)
        {
            return BoundaryFillResult.Failure("No closed boundary was found around the picked point.");
        }

        IReadOnlyList<Point2D> vertices = RemoveCollinearVertices(containingFace, effectiveTolerance);

        if (vertices.Count < 3)
        {
            return BoundaryFillResult.Failure("The detected boundary is degenerate.");
        }

        var polyline = new PolylineEntity(
            vertices,
            isClosed: true,
            layerId: layerId,
            isFilled: true);

        return BoundaryFillResult.Success(polyline);
    }

    private static IReadOnlyList<BoundarySegment> CollectLinearSegments(
        IEnumerable<CadEntity> entities,
        GeometryTolerance tolerance)
    {
        var segments = new List<BoundarySegment>();

        foreach (CadEntity entity in entities)
        {
            switch (entity)
            {
                case LineEntity line when line.Start.DistanceTo(line.End) > tolerance.Distance:
                    segments.Add(new BoundarySegment(line.Start, line.End));
                    break;

                case PolylineEntity { HasArcSegments: false } polyline:
                    AddPolylineSegments(segments, polyline, tolerance);
                    break;
            }
        }

        return segments;
    }

    private static void AddPolylineSegments(
        List<BoundarySegment> segments,
        PolylineEntity polyline,
        GeometryTolerance tolerance)
    {
        IReadOnlyList<Point2D> vertices = polyline.Vertices;

        if (vertices.Count < 2)
        {
            return;
        }

        int segmentCount = polyline.IsClosed
            ? vertices.Count
            : vertices.Count - 1;

        for (int index = 0; index < segmentCount; index++)
        {
            Point2D start = vertices[index];
            Point2D end = vertices[(index + 1) % vertices.Count];

            if (start.DistanceTo(end) > tolerance.Distance)
            {
                segments.Add(new BoundarySegment(start, end));
            }
        }
    }

    private static PlanarGraph BuildPlanarGraph(
        IReadOnlyList<BoundarySegment> sourceSegments,
        GeometryTolerance tolerance)
    {
        var splitPoints = sourceSegments
            .Select(segment => new List<Point2D> { segment.Start, segment.End })
            .ToList();

        for (int first = 0; first < sourceSegments.Count; first++)
        {
            for (int second = first + 1; second < sourceSegments.Count; second++)
            {
                AddSegmentIntersections(
                    sourceSegments[first],
                    sourceSegments[second],
                    splitPoints[first],
                    splitPoints[second],
                    tolerance);
            }
        }

        var graph = new PlanarGraph(tolerance);

        for (int index = 0; index < sourceSegments.Count; index++)
        {
            BoundarySegment segment = sourceSegments[index];
            IReadOnlyList<Point2D> orderedPoints = DistinctPoints(splitPoints[index], tolerance)
                .OrderBy(point => ParameterAlongSegment(segment, point))
                .ToList();

            for (int pointIndex = 0; pointIndex < orderedPoints.Count - 1; pointIndex++)
            {
                Point2D start = orderedPoints[pointIndex];
                Point2D end = orderedPoints[pointIndex + 1];

                if (start.DistanceTo(end) <= tolerance.Distance)
                {
                    continue;
                }

                graph.AddUndirectedEdge(start, end);
            }
        }

        graph.SortAdjacency();
        return graph;
    }

    private static void AddSegmentIntersections(
        BoundarySegment first,
        BoundarySegment second,
        List<Point2D> firstSplitPoints,
        List<Point2D> secondSplitPoints,
        GeometryTolerance tolerance)
    {
        IntersectionResult intersection = IntersectionService.IntersectSegments(
            first.ToLineSegment(),
            second.ToLineSegment(),
            tolerance);

        if (intersection.Kind == IntersectionKind.Point && intersection.Point is { } point)
        {
            AddDistinct(firstSplitPoints, point, tolerance);
            AddDistinct(secondSplitPoints, point, tolerance);
            return;
        }

        if (intersection.Kind != IntersectionKind.Overlapping)
        {
            return;
        }

        AddOverlappingEndpoint(first.Start, first, second, firstSplitPoints, secondSplitPoints, tolerance);
        AddOverlappingEndpoint(first.End, first, second, firstSplitPoints, secondSplitPoints, tolerance);
        AddOverlappingEndpoint(second.Start, second, first, secondSplitPoints, firstSplitPoints, tolerance);
        AddOverlappingEndpoint(second.End, second, first, secondSplitPoints, firstSplitPoints, tolerance);
    }

    private static void AddOverlappingEndpoint(
        Point2D point,
        BoundarySegment ownSegment,
        BoundarySegment otherSegment,
        List<Point2D> ownSplitPoints,
        List<Point2D> otherSplitPoints,
        GeometryTolerance tolerance)
    {
        if (!IsPointOnSegment(point, otherSegment, tolerance))
        {
            return;
        }

        AddDistinct(ownSplitPoints, point, tolerance);
        AddDistinct(otherSplitPoints, point, tolerance);

        if (IsPointOnSegment(otherSegment.Start, ownSegment, tolerance))
        {
            AddDistinct(ownSplitPoints, otherSegment.Start, tolerance);
        }

        if (IsPointOnSegment(otherSegment.End, ownSegment, tolerance))
        {
            AddDistinct(ownSplitPoints, otherSegment.End, tolerance);
        }
    }

    private static IReadOnlyList<IReadOnlyList<Point2D>> FindInteriorFaces(
        PlanarGraph graph,
        GeometryTolerance tolerance)
    {
        var faces = new List<IReadOnlyList<Point2D>>();
        var visitedDirectedEdges = new HashSet<DirectedEdge>();
        int maxSteps = Math.Max(1, graph.EdgeCount * 2 + 1);

        for (int node = 0; node < graph.Nodes.Count; node++)
        {
            foreach (int neighbor in graph.GetNeighbors(node))
            {
                var startEdge = new DirectedEdge(node, neighbor);

                if (visitedDirectedEdges.Contains(startEdge))
                {
                    continue;
                }

                IReadOnlyList<int> faceNodeIndexes = TraceFace(
                    graph,
                    startEdge,
                    visitedDirectedEdges,
                    maxSteps);

                if (faceNodeIndexes.Count < 3)
                {
                    continue;
                }

                IReadOnlyList<Point2D> face = faceNodeIndexes
                    .Select(index => graph.Nodes[index])
                    .ToList();

                double signedArea = SignedArea(face);

                if (signedArea > tolerance.Distance * tolerance.Distance)
                {
                    faces.Add(face);
                }
            }
        }

        return faces;
    }

    private static IReadOnlyList<int> TraceFace(
        PlanarGraph graph,
        DirectedEdge startEdge,
        HashSet<DirectedEdge> visitedDirectedEdges,
        int maxSteps)
    {
        var face = new List<int>();
        DirectedEdge current = startEdge;

        for (int step = 0; step < maxSteps; step++)
        {
            if (visitedDirectedEdges.Contains(current) && current != startEdge)
            {
                return Array.Empty<int>();
            }

            visitedDirectedEdges.Add(current);
            face.Add(current.From);

            IReadOnlyList<int> neighbors = graph.GetNeighbors(current.To);
            int incomingIndex = IndexOfNeighbor(neighbors, current.From);

            if (incomingIndex < 0 || neighbors.Count == 0)
            {
                return Array.Empty<int>();
            }

            int nextNeighborIndex = (incomingIndex - 1 + neighbors.Count) % neighbors.Count;
            var next = new DirectedEdge(
                current.To,
                neighbors[nextNeighborIndex]);

            if (next == startEdge)
            {
                return face;
            }

            current = next;
        }

        return Array.Empty<int>();
    }

    private static int IndexOfNeighbor(
        IReadOnlyList<int> neighbors,
        int nodeIndex)
    {
        for (int index = 0; index < neighbors.Count; index++)
        {
            if (neighbors[index] == nodeIndex)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool ContainsPoint(
        IReadOnlyList<Point2D> polygon,
        Point2D point,
        GeometryTolerance tolerance)
    {
        bool inside = false;

        for (int currentIndex = 0, previousIndex = polygon.Count - 1;
             currentIndex < polygon.Count;
             previousIndex = currentIndex++)
        {
            Point2D current = polygon[currentIndex];
            Point2D previous = polygon[previousIndex];

            if (IsPointOnSegment(point, new BoundarySegment(previous, current), tolerance))
            {
                return true;
            }

            bool crossesRay = current.Y > point.Y != previous.Y > point.Y;

            if (!crossesRay)
            {
                continue;
            }

            double xAtPointY = (previous.X - current.X) *
                (point.Y - current.Y) /
                (previous.Y - current.Y) +
                current.X;

            if (point.X < xAtPointY)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static IReadOnlyList<Point2D> RemoveCollinearVertices(
        IReadOnlyList<Point2D> vertices,
        GeometryTolerance tolerance)
    {
        if (vertices.Count < 3)
        {
            return vertices;
        }

        var result = new List<Point2D>();

        for (int index = 0; index < vertices.Count; index++)
        {
            Point2D previous = vertices[(index - 1 + vertices.Count) % vertices.Count];
            Point2D current = vertices[index];
            Point2D next = vertices[(index + 1) % vertices.Count];
            Vector2D before = previous.VectorTo(current);
            Vector2D after = current.VectorTo(next);

            if (before.Length <= tolerance.Distance ||
                after.Length <= tolerance.Distance)
            {
                continue;
            }

            if (Math.Abs(before.Cross(after)) <= tolerance.Distance)
            {
                continue;
            }

            result.Add(current);
        }

        return result;
    }

    private static IReadOnlyList<Point2D> DistinctPoints(
        IEnumerable<Point2D> points,
        GeometryTolerance tolerance)
    {
        var result = new List<Point2D>();

        foreach (Point2D point in points)
        {
            AddDistinct(result, point, tolerance);
        }

        return result;
    }

    private static void AddDistinct(
        List<Point2D> points,
        Point2D point,
        GeometryTolerance tolerance)
    {
        if (points.Any(existing => existing.DistanceTo(point) <= tolerance.Distance))
        {
            return;
        }

        points.Add(point);
    }

    private static bool IsPointOnSegment(
        Point2D point,
        BoundarySegment segment,
        GeometryTolerance tolerance)
    {
        Vector2D segmentVector = segment.Start.VectorTo(segment.End);
        Vector2D startToPoint = segment.Start.VectorTo(point);

        if (segmentVector.Length <= tolerance.Distance)
        {
            return point.DistanceTo(segment.Start) <= tolerance.Distance;
        }

        if (Math.Abs(segmentVector.Cross(startToPoint)) > tolerance.Distance)
        {
            return false;
        }

        double dot = startToPoint.Dot(segmentVector);

        return dot >= -tolerance.Distance &&
               dot <= segmentVector.LengthSquared + tolerance.Distance;
    }

    private static double ParameterAlongSegment(
        BoundarySegment segment,
        Point2D point)
    {
        Vector2D segmentVector = segment.Start.VectorTo(segment.End);

        if (segmentVector.LengthSquared <= 0.0)
        {
            return 0.0;
        }

        return segment.Start.VectorTo(point).Dot(segmentVector) / segmentVector.LengthSquared;
    }

    private static double SignedArea(IReadOnlyList<Point2D> vertices)
    {
        double area = 0.0;

        for (int index = 0; index < vertices.Count; index++)
        {
            Point2D current = vertices[index];
            Point2D next = vertices[(index + 1) % vertices.Count];

            area += current.X * next.Y - next.X * current.Y;
        }

        return area / 2.0;
    }

    private readonly record struct BoundarySegment(
        Point2D Start,
        Point2D End)
    {
        public LineSegment2D ToLineSegment() => new(Start, End);
    }

    private readonly record struct DirectedEdge(
        int From,
        int To);

    private sealed class PlanarGraph
    {
        private readonly GeometryTolerance _tolerance;
        private readonly List<Point2D> _nodes = new();
        private readonly Dictionary<int, List<int>> _adjacency = new();
        private readonly HashSet<(int First, int Second)> _edges = new();

        public PlanarGraph(GeometryTolerance tolerance)
        {
            _tolerance = tolerance;
        }

        public IReadOnlyList<Point2D> Nodes => _nodes;

        public int EdgeCount => _edges.Count;

        public void AddUndirectedEdge(
            Point2D start,
            Point2D end)
        {
            int startIndex = GetOrAddNode(start);
            int endIndex = GetOrAddNode(end);

            if (startIndex == endIndex)
            {
                return;
            }

            (int First, int Second) edge = startIndex < endIndex
                ? (startIndex, endIndex)
                : (endIndex, startIndex);

            if (!_edges.Add(edge))
            {
                return;
            }

            _adjacency[startIndex].Add(endIndex);
            _adjacency[endIndex].Add(startIndex);
        }

        public IReadOnlyList<int> GetNeighbors(int nodeIndex)
        {
            return _adjacency.TryGetValue(nodeIndex, out List<int>? neighbors)
                ? neighbors
                : Array.Empty<int>();
        }

        public void SortAdjacency()
        {
            foreach ((int nodeIndex, List<int> neighbors) in _adjacency)
            {
                Point2D origin = _nodes[nodeIndex];

                neighbors.Sort((first, second) =>
                {
                    Point2D firstPoint = _nodes[first];
                    Point2D secondPoint = _nodes[second];

                    double firstAngle = Math.Atan2(
                        firstPoint.Y - origin.Y,
                        firstPoint.X - origin.X);
                    double secondAngle = Math.Atan2(
                        secondPoint.Y - origin.Y,
                        secondPoint.X - origin.X);

                    return firstAngle.CompareTo(secondAngle);
                });
            }
        }

        private int GetOrAddNode(Point2D point)
        {
            for (int index = 0; index < _nodes.Count; index++)
            {
                if (_nodes[index].DistanceTo(point) <= _tolerance.Distance)
                {
                    return index;
                }
            }

            int nodeIndex = _nodes.Count;
            _nodes.Add(point);
            _adjacency.Add(nodeIndex, new List<int>());
            return nodeIndex;
        }
    }
}
