using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using System.Globalization;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Parses CAD command-line input. The legacy <see cref="Parse(string?)" /> method is still
/// used by the current tools; the contextual overload is the v0.8 foundation for guided commands.
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
            string relativeValue = value[1..];

            if (relativeValue.Contains('<'))
            {
                return ParseDistanceAngle(relativeValue);
            }

            return ParseRelativePoint(relativeValue);
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

    public CommandInputSubmission Parse(
        string? input,
        CommandPromptState promptState,
        Point2D? referencePoint = null)
    {
        ArgumentNullException.ThrowIfNull(promptState);

        string rawText = input ?? string.Empty;
        string value = rawText.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return promptState.AcceptsEmptyEnter
                ? CommandInputSubmission.Confirm(rawText)
                : CommandInputSubmission.Invalid(rawText, "Input is required for the current command step.");
        }

        if (PromptAcceptsOptions(promptState.ExpectedInput))
        {
            CommandOption? option = promptState.Options.FirstOrDefault(option => option.Matches(value));
            if (option is not null)
            {
                return CommandInputSubmission.Option(rawText, option.Keyword);
            }
        }

        if (promptState.ExpectedInput == CommandInputKind.CommandName)
        {
            return IsLikelyCommandName(value)
                ? CommandInputSubmission.Command(rawText, value.ToUpperInvariant())
                : CommandInputSubmission.Invalid(rawText, $"Invalid command name: {value}.");
        }

        CommandInputSubmission? pointSubmission = null;

        if (PromptAcceptsPoint(promptState.ExpectedInput))
        {
            pointSubmission = ParsePointSubmission(
                rawText,
                value,
                referencePoint);

            if (pointSubmission.IsValid ||
                !PromptAcceptsScalarFallback(promptState.ExpectedInput) ||
                value.StartsWith('@') ||
                value.Contains('<'))
            {
                return pointSubmission;
            }
        }

        if (PromptAcceptsDistance(promptState.ExpectedInput))
        {
            CommandInputParseResult distanceResult = Parse(value);

            if (distanceResult.Kind == CommandInputParseKind.Distance && distanceResult.Distance is not null)
            {
                return CommandInputSubmission.FromDistance(rawText, distanceResult.Distance.Value);
            }

            if (pointSubmission is not null)
            {
                return pointSubmission;
            }

            return CommandInputSubmission.Invalid(rawText, "Expected a distance value.");
        }

        if (PromptAcceptsAngle(promptState.ExpectedInput))
        {
            if (!TryParseDouble(value, out double angleDegrees))
            {
                if (pointSubmission is not null)
                {
                    return pointSubmission;
                }

                return CommandInputSubmission.Invalid(rawText, "Expected an angle value in degrees.");
            }

            return CommandInputSubmission.Angle(rawText, NormalizeAngle(angleDegrees));
        }

        if (PromptAcceptsNumber(promptState.ExpectedInput))
        {
            if (!TryParseDouble(value, out double number))
            {
                if (pointSubmission is not null)
                {
                    return pointSubmission;
                }

                return CommandInputSubmission.Invalid(rawText, "Expected a numeric value.");
            }

            return CommandInputSubmission.FromNumber(rawText, number);
        }

        if (promptState.ExpectedInput == CommandInputKind.Text)
        {
            return CommandInputSubmission.FromText(rawText, value);
        }

        if (promptState.ExpectedInput == CommandInputKind.Option)
        {
            return CommandInputSubmission.Invalid(rawText, BuildUnknownOptionMessage(value, promptState));
        }

        if (PromptAcceptsSelection(promptState.ExpectedInput))
        {
            return CommandInputSubmission.Invalid(rawText, "Selection input must come from the drawing canvas.");
        }

        return CommandInputSubmission.Invalid(rawText, "Unsupported command input for the current prompt.");
    }

    private static CommandInputSubmission ParsePointSubmission(
        string rawText,
        string value,
        Point2D? referencePoint)
    {
        CommandInputParseResult result = ParsePointLikeValue(value);

        switch (result.Kind)
        {
            case CommandInputParseKind.AbsolutePoint:
                return CommandInputSubmission.FromPoint(rawText, result.Point!.Value);

            case CommandInputParseKind.RelativePoint:
                if (referencePoint is null)
                {
                    return CommandInputSubmission.Invalid(
                        rawText,
                        "Relative coordinate input requires a reference point.");
                }

                Vector2D offset = result.Offset!.Value;
                return CommandInputSubmission.FromPoint(
                    rawText,
                    referencePoint.Value + offset,
                    offset: offset);

            case CommandInputParseKind.DistanceAngle:
                if (referencePoint is null)
                {
                    return CommandInputSubmission.Invalid(
                        rawText,
                        "Polar coordinate input requires a reference point.");
                }

                double distance = result.Distance!.Value;
                double angleDegrees = result.AngleDegrees!.Value;
                double radians = angleDegrees * Math.PI / 180.0;
                var polarOffset = new Vector2D(
                    distance * Math.Cos(radians),
                    distance * Math.Sin(radians));

                return CommandInputSubmission.FromPoint(
                    rawText,
                    referencePoint.Value + polarOffset,
                    offset: polarOffset,
                    distance: distance,
                    angleDegrees: angleDegrees);

            default:
                return CommandInputSubmission.Invalid(
                    rawText,
                    result.ErrorMessage ?? "Expected a point value.");
        }
    }

    private static CommandInputParseResult ParsePointLikeValue(string value)
    {
        if (value.StartsWith('@'))
        {
            string relativeValue = value[1..];

            if (relativeValue.Contains('<'))
            {
                return ParseDistanceAngle(relativeValue);
            }

            return ParseRelativePoint(relativeValue);
        }

        if (value.Contains(','))
        {
            return ParseAbsolutePoint(value);
        }

        if (value.Contains('<'))
        {
            return ParseDistanceAngle(value);
        }

        return CommandInputParseResult.Invalid(
            "Invalid point format. Use x,y, @x,y or @distance<angle.");
    }

    private static bool PromptAcceptsScalarFallback(CommandInputKind kind)
    {
        return PromptAcceptsDistance(kind) ||
               PromptAcceptsAngle(kind) ||
               PromptAcceptsNumber(kind);
    }


    private static bool PromptAcceptsPoint(CommandInputKind kind)
    {
        return kind is
            CommandInputKind.Point or
            CommandInputKind.PointOrOption or
            CommandInputKind.PointOrDistance or
            CommandInputKind.PointOrDistanceOrOption or
            CommandInputKind.PointOrAngle or
            CommandInputKind.PointOrAngleOrOption or
            CommandInputKind.PointOrNumber or
            CommandInputKind.PointOrNumberOrOption;
    }

    private static bool PromptAcceptsDistance(CommandInputKind kind)
    {
        return kind is
            CommandInputKind.Distance or
            CommandInputKind.DistanceOrOption or
            CommandInputKind.PointOrDistance or
            CommandInputKind.PointOrDistanceOrOption;
    }

    private static bool PromptAcceptsAngle(CommandInputKind kind)
    {
        return kind is
            CommandInputKind.Angle or
            CommandInputKind.PointOrAngle or
            CommandInputKind.PointOrAngleOrOption;
    }

    private static bool PromptAcceptsNumber(CommandInputKind kind)
    {
        return kind is
            CommandInputKind.Number or
            CommandInputKind.PointOrNumber or
            CommandInputKind.PointOrNumberOrOption;
    }

    private static bool PromptAcceptsOptions(CommandInputKind kind)
    {
        return kind is CommandInputKind.Option or
            CommandInputKind.PointOrOption or
            CommandInputKind.DistanceOrOption or
            CommandInputKind.PointOrDistanceOrOption or
            CommandInputKind.PointOrAngleOrOption or
            CommandInputKind.PointOrNumberOrOption or
            CommandInputKind.SelectionOrOption;
    }

    private static bool PromptAcceptsSelection(CommandInputKind kind)
    {
        return kind is CommandInputKind.Selection or CommandInputKind.SelectionOrOption;
    }

    private static bool IsLikelyCommandName(string value)
    {
        return value.All(character =>
            char.IsLetter(character) ||
            char.IsDigit(character) ||
            character == '_' ||
            character == '-');
    }

    private static string BuildUnknownOptionMessage(
        string value,
        CommandPromptState promptState)
    {
        if (promptState.Options.Count == 0)
        {
            return $"Unknown option: {value}.";
        }

        return $"Unknown option: {value}. Available options: " +
            string.Join(", ", promptState.Options.Select(option => option.Keyword)) +
            ".";
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
