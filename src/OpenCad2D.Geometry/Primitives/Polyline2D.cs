namespace OpenCad2D.Geometry.Primitives;

/// <summary>
/// Represents a 2D polyline made of straight line segments.
/// </summary>
public sealed class Polyline2D
{
    private readonly List<Point2D> _vertices;

    public Polyline2D(IEnumerable<Point2D> vertices, bool isClosed = false)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        _vertices = vertices.ToList();

        if (_vertices.Count < 2)
        {
            throw new ArgumentException(
                "A polyline must contain at least two vertices.",
                nameof(vertices));
        }

        IsClosed = isClosed;
    }

    public IReadOnlyList<Point2D> Vertices => _vertices;

    public bool IsClosed { get; }

    public int VertexCount => _vertices.Count;

    public int SegmentCount
    {
        get
        {
            if (IsClosed)
            {
                return _vertices.Count;
            }

            return _vertices.Count - 1;
        }
    }

    public double Length
    {
        get
        {
            double length = 0;

            foreach (LineSegment2D segment in GetSegments())
            {
                length += segment.Length;
            }

            return length;
        }
    }

    public IReadOnlyList<LineSegment2D> GetSegments()
    {
        var segments = new List<LineSegment2D>();

        for (int index = 0; index < _vertices.Count - 1; index++)
        {
            segments.Add(new LineSegment2D(
                _vertices[index],
                _vertices[index + 1]));
        }

        if (IsClosed)
        {
            segments.Add(new LineSegment2D(
                _vertices[^1],
                _vertices[0]));
        }

        return segments;
    }

    public BoundingBox2D GetBoundingBox()
    {
        double minX = _vertices.Min(point => point.X);
        double minY = _vertices.Min(point => point.Y);
        double maxX = _vertices.Max(point => point.X);
        double maxY = _vertices.Max(point => point.Y);

        return new BoundingBox2D(minX, minY, maxX, maxY);
    }

    public bool ContainsVertex(Point2D point, double tolerance = Tolerance.Default)
    {
        return _vertices.Any(vertex =>
            Tolerance.AreEqual(vertex.X, point.X, tolerance) &&
            Tolerance.AreEqual(vertex.Y, point.Y, tolerance));
    }

    public Polyline2D Reverse()
    {
        var reversed = _vertices
            .AsEnumerable()
            .Reverse()
            .ToList();

        return new Polyline2D(reversed, IsClosed);
    }

    public Polyline2D AddVertex(Point2D point)
    {
        var vertices = _vertices.ToList();
        vertices.Add(point);

        return new Polyline2D(vertices, IsClosed);
    }
}