using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Result of an automatic boundary fill search.
/// </summary>
public sealed class BoundaryFillResult
{
    private BoundaryFillResult(
        BoundaryFillStatus status,
        PolylineEntity? polyline,
        string message,
        Point2D? seedPoint,
        IReadOnlyList<Point2D> boundaryVertices,
        BoundaryFillDiagnostics diagnostics)
    {
        Status = status;
        Polyline = polyline;
        Message = message;
        SeedPoint = seedPoint;
        BoundaryVertices = boundaryVertices;
        Diagnostics = diagnostics;
    }

    public BoundaryFillStatus Status { get; }

    public PolylineEntity? Polyline { get; }

    public string Message { get; }

    public Point2D? SeedPoint { get; }

    public IReadOnlyList<Point2D> BoundaryVertices { get; }

    public BoundaryFillDiagnostics Diagnostics { get; }

    public bool Succeeded => Status == BoundaryFillStatus.Success && Polyline is not null;

    public static BoundaryFillResult Success(PolylineEntity polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        return Success(
            polyline,
            seedPoint: null,
            boundaryVertices: polyline.Vertices,
            diagnostics: BoundaryFillDiagnostics.Empty);
    }

    public static BoundaryFillResult Success(
        PolylineEntity polyline,
        Point2D? seedPoint,
        IReadOnlyList<Point2D> boundaryVertices,
        BoundaryFillDiagnostics diagnostics,
        string message = "Boundary fill created.")
    {
        ArgumentNullException.ThrowIfNull(polyline);
        ArgumentNullException.ThrowIfNull(boundaryVertices);
        ArgumentNullException.ThrowIfNull(diagnostics);

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(message));
        }

        return new BoundaryFillResult(
            BoundaryFillStatus.Success,
            polyline,
            message,
            seedPoint,
            boundaryVertices.ToList(),
            diagnostics);
    }

    public static BoundaryFillResult Failure(string message)
    {
        return Failure(
            BoundaryFillStatus.NoClosedBoundary,
            message,
            seedPoint: null,
            diagnostics: BoundaryFillDiagnostics.Empty);
    }

    public static BoundaryFillResult Failure(
        BoundaryFillStatus status,
        string message,
        Point2D? seedPoint = null,
        BoundaryFillDiagnostics? diagnostics = null)
    {
        if (status == BoundaryFillStatus.Success)
        {
            throw new ArgumentException(
                "A failure result cannot use Success status.",
                nameof(status));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(message));
        }

        return new BoundaryFillResult(
            status,
            null,
            message,
            seedPoint,
            Array.Empty<Point2D>(),
            diagnostics ?? BoundaryFillDiagnostics.Empty);
    }
}
