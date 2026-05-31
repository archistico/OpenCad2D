using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Editing;

/// <summary>
/// Result returned by the AutoCAD-style DIVIDE calculation.
/// </summary>
public sealed class DivideEntityResult
{
    private DivideEntityResult(
        bool succeeded,
        string message,
        IReadOnlyList<Point2D>? points = null)
    {
        Succeeded = succeeded;
        Message = message;
        Points = points ?? Array.Empty<Point2D>();
    }

    public bool Succeeded { get; }

    public string Message { get; }

    public IReadOnlyList<Point2D> Points { get; }

    public static DivideEntityResult Success(IReadOnlyList<Point2D> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        return new DivideEntityResult(
            true,
            points.Count == 1
                ? "1 divide point calculated."
                : $"{points.Count} divide points calculated.",
            points);
    }

    public static DivideEntityResult Failure(string message)
    {
        return new DivideEntityResult(
            false,
            string.IsNullOrWhiteSpace(message)
                ? "Cannot divide selected entity."
                : message.Trim());
    }
}
