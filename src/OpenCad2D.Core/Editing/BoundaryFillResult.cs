using OpenCad2D.Core.Entities;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Result of an automatic boundary fill search.
/// </summary>
public sealed class BoundaryFillResult
{
    private BoundaryFillResult(
        PolylineEntity? polyline,
        string message)
    {
        Polyline = polyline;
        Message = message;
    }

    public PolylineEntity? Polyline { get; }

    public string Message { get; }

    public bool Succeeded => Polyline is not null;

    public static BoundaryFillResult Success(PolylineEntity polyline)
    {
        ArgumentNullException.ThrowIfNull(polyline);

        return new BoundaryFillResult(
            polyline,
            "Boundary fill created.");
    }

    public static BoundaryFillResult Failure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(message));
        }

        return new BoundaryFillResult(
            null,
            message);
    }
}
