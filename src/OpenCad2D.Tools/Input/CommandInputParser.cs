using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using System.Globalization;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Parses the minimal CAD command-line point input supported by OpenCad2D.
/// </summary>
public sealed class CommandInputParser
{
    private static readonly CultureInfo ParsingCulture = CultureInfo.InvariantCulture;

    public CommandInputParseResult Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return CommandInputParseResult.Invalid("Command input cannot be empty.");
        }

        string value = input.Trim();

        if (value.StartsWith('@'))
        {
            return ParseRelativePoint(value[1..]);
        }

        if (value.Contains(','))
        {
            return ParseAbsolutePoint(value);
        }

        if (value.Contains('<'))
        {
            return ParseDistanceAngle(value);
        }

        return ParseDistance(value);
    }

    private static CommandInputParseResult ParseAbsolutePoint(string input)
    {
        if (!TryParsePoint(input, out Point2D point))
        {
            return CommandInputParseResult.Invalid(
                "Invalid absolute coordinate format. Use x,y for example: 100,50.");
        }

        return CommandInputParseResult.AbsolutePoint(point);
    }

    private static CommandInputParseResult ParseRelativePoint(string input)
    {
        if (!TryParsePoint(input, out Point2D point))
        {
            return CommandInputParseResult.Invalid(
                "Invalid relative coordinate format. Use @x,y for example: @50,0.");
        }

        return CommandInputParseResult.RelativePoint(
            new Vector2D(point.X, point.Y));
    }

    private static CommandInputParseResult ParseDistance(string input)
    {
        if (!TryParseDouble(input, out double distance))
        {
            return CommandInputParseResult.Invalid(
                "Invalid distance format. Use a positive number, for example: 5.");
        }

        if (distance <= 0 || Tolerance.IsZero(distance))
        {
            return CommandInputParseResult.Invalid(
                "Distance must be greater than zero.");
        }

        return CommandInputParseResult.DistanceValue(distance);
    }


    private static CommandInputParseResult ParseDistanceAngle(string input)
    {
        string[] parts = input.Split(
            '<',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2 ||
            !TryParseDouble(parts[0], out double distance) ||
            !TryParseDouble(parts[1], out double angleDegrees))
        {
            return CommandInputParseResult.Invalid(
                "Invalid distance-angle format. Use distance<angle for example: 100<45.");
        }

        if (distance <= 0 || Tolerance.IsZero(distance))
        {
            return CommandInputParseResult.Invalid(
                "Distance must be greater than zero.");
        }

        return CommandInputParseResult.DistanceAngleValue(
            distance,
            NormalizeAngle(angleDegrees));
    }

    private static double NormalizeAngle(double angleDegrees)
    {
        double normalized = angleDegrees % 360.0;

        return normalized < 0
            ? normalized + 360.0
            : normalized;
    }

    private static bool TryParsePoint(
        string input,
        out Point2D point)
    {
        point = Point2D.Origin;

        string[] parts = input.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryParseDouble(parts[0], out double x) ||
            !TryParseDouble(parts[1], out double y))
        {
            return false;
        }

        point = new Point2D(x, y);
        return true;
    }

    private static bool TryParseDouble(
        string input,
        out double value)
    {
        return double.TryParse(
            input,
            NumberStyles.Float,
            ParsingCulture,
            out value);
    }
}
