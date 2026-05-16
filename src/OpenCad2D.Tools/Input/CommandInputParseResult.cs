using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Result produced by the CAD command-line input parser.
/// </summary>
public sealed class CommandInputParseResult
{
    private CommandInputParseResult(
        CommandInputParseKind kind,
        Point2D? point = null,
        Vector2D? offset = null,
        double? distance = null,
        double? angleDegrees = null,
        string? errorMessage = null)
    {
        Kind = kind;
        Point = point;
        Offset = offset;
        Distance = distance;
        AngleDegrees = angleDegrees;
        ErrorMessage = errorMessage;
    }

    public CommandInputParseKind Kind { get; }

    public bool IsValid => Kind != CommandInputParseKind.Invalid;

    public Point2D? Point { get; }

    public Vector2D? Offset { get; }

    public double? Distance { get; }

    public double? AngleDegrees { get; }

    public string? ErrorMessage { get; }

    public static CommandInputParseResult AbsolutePoint(Point2D point)
    {
        return new CommandInputParseResult(
            CommandInputParseKind.AbsolutePoint,
            point: point);
    }

    public static CommandInputParseResult RelativePoint(Vector2D offset)
    {
        return new CommandInputParseResult(
            CommandInputParseKind.RelativePoint,
            offset: offset);
    }

    public static CommandInputParseResult DistanceValue(double distance)
    {
        return new CommandInputParseResult(
            CommandInputParseKind.Distance,
            distance: distance);
    }

    public static CommandInputParseResult DistanceAngleValue(
        double distance,
        double angleDegrees)
    {
        return new CommandInputParseResult(
            CommandInputParseKind.DistanceAngle,
            distance: distance,
            angleDegrees: angleDegrees);
    }

    public static CommandInputParseResult Invalid(string errorMessage)
    {
        return new CommandInputParseResult(
            CommandInputParseKind.Invalid,
            errorMessage: errorMessage);
    }
}
