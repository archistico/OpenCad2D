using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Tools.Input;

/// <summary>
/// Contextual command-line submission after parsing against the current prompt state.
/// </summary>
public sealed class CommandInputSubmission
{
    private CommandInputSubmission(
        CommandInputSubmissionKind kind,
        string rawText,
        Point2D? point = null,
        Vector2D? offset = null,
        double? distance = null,
        double? angleDegrees = null,
        double? number = null,
        string? optionKeyword = null,
        string? commandName = null,
        string? text = null,
        string? errorMessage = null)
    {
        Kind = kind;
        RawText = rawText;
        Point = point;
        Offset = offset;
        Distance = distance;
        AngleDegrees = angleDegrees;
        Number = number;
        OptionKeyword = optionKeyword;
        CommandName = commandName;
        Text = text;
        ErrorMessage = errorMessage;
    }

    public CommandInputSubmissionKind Kind { get; }

    public bool IsValid => Kind != CommandInputSubmissionKind.Invalid;

    public string RawText { get; }

    public Point2D? Point { get; }

    public Vector2D? Offset { get; }

    public double? Distance { get; }

    public double? AngleDegrees { get; }

    public double? Number { get; }

    public string? OptionKeyword { get; }

    public string? CommandName { get; }

    public string? Text { get; }

    public string? ErrorMessage { get; }

    public static CommandInputSubmission Confirm(string rawText)
    {
        return new CommandInputSubmission(CommandInputSubmissionKind.Confirm, rawText);
    }

    public static CommandInputSubmission Command(string rawText, string commandName)
    {
        return new CommandInputSubmission(
            CommandInputSubmissionKind.Command,
            rawText,
            commandName: commandName);
    }

    public static CommandInputSubmission FromPoint(
        string rawText,
        Point2D point,
        Vector2D? offset = null,
        double? distance = null,
        double? angleDegrees = null)
    {
        return new CommandInputSubmission(
            CommandInputSubmissionKind.Point,
            rawText,
            point: point,
            offset: offset,
            distance: distance,
            angleDegrees: angleDegrees);
    }

    public static CommandInputSubmission FromDistance(string rawText, double distance)
    {
        return new CommandInputSubmission(
            CommandInputSubmissionKind.Distance,
            rawText,
            distance: distance);
    }

    public static CommandInputSubmission Angle(string rawText, double angleDegrees)
    {
        return new CommandInputSubmission(
            CommandInputSubmissionKind.Angle,
            rawText,
            angleDegrees: angleDegrees);
    }

    public static CommandInputSubmission FromNumber(string rawText, double number)
    {
        return new CommandInputSubmission(
            CommandInputSubmissionKind.Number,
            rawText,
            number: number);
    }

    public static CommandInputSubmission Option(string rawText, string optionKeyword)
    {
        return new CommandInputSubmission(
            CommandInputSubmissionKind.Option,
            rawText,
            optionKeyword: optionKeyword);
    }

    public static CommandInputSubmission FromText(string rawText, string text)
    {
        return new CommandInputSubmission(
            CommandInputSubmissionKind.Text,
            rawText,
            text: text);
    }

    public static CommandInputSubmission Invalid(string rawText, string errorMessage)
    {
        return new CommandInputSubmission(
            CommandInputSubmissionKind.Invalid,
            rawText,
            errorMessage: errorMessage);
    }
}
