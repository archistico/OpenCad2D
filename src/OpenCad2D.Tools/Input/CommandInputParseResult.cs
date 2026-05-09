using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Result produced by the CAD command-line input parser.
/// </summary>
public sealed class CommandInputParseResult
{
    private CommandInputParseResult(
        CommandInputKind kind,
        Point2D? point = null,
        Vector2D? offset = null,
        double? distance = null,
        string? errorMessage = null)
    {
        Kind = kind;
        Point = point;
        Offset = offset;
        Distance = distance;
        ErrorMessage = errorMessage;
    }

    public CommandInputKind Kind { get; }

    public bool IsValid => Kind != CommandInputKind.Invalid;

    public Point2D? Point { get; }

    public Vector2D? Offset { get; }

    public double? Distance { get; }

    public string? ErrorMessage { get; }

    public static CommandInputParseResult AbsolutePoint(Point2D point)
    {
        return new CommandInputParseResult(
            CommandInputKind.AbsolutePoint,
            point: point);
    }

    public static CommandInputParseResult RelativePoint(Vector2D offset)
    {
        return new CommandInputParseResult(
            CommandInputKind.RelativePoint,
            offset: offset);
    }

    public static CommandInputParseResult DistanceValue(double distance)
    {
        return new CommandInputParseResult(
            CommandInputKind.Distance,
            distance: distance);
    }

    public static CommandInputParseResult Invalid(string errorMessage)
    {
        return new CommandInputParseResult(
            CommandInputKind.Invalid,
            errorMessage: errorMessage);
    }
}
