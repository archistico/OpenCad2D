using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw line entities.
/// </summary>
public sealed class LineTool : TwoPointToolBase, ICommandDrivenTool
{
    public override string Name => "Line";


    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State == TwoPointToolState.WaitingForFirstPoint
            ? new CommandPromptState(
                "LINE",
                "Specify first point",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50")
            : new CommandPromptState(
                "LINE",
                "Specify second point",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50   |   @50,0   |   @100<45   |   distance");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "LINE expects a point input.");
        }

        return SubmitResolvedPoint(context, input.Point.Value);
    }

    public LineEntity? GetPreviewEntity()
    {
        if (!HasPreview || FirstPoint is null || CurrentPoint is null)
        {
            return null;
        }

        return new LineEntity(
            FirstPoint.Value,
            CurrentPoint.Value);
    }

    protected override ToolResult OnFirstPointSelected(
        ToolContext context,
        Point2D firstPoint)
    {
        return ToolResult.Started("Specify next point.");
    }

    protected override ToolResult OnPreviewUpdated(
        ToolContext context,
        Point2D firstPoint,
        Point2D currentPoint)
    {
        return ToolResult.Updated();
    }

    protected override ToolResult OnSecondPointSelected(
        ToolContext context,
        Point2D firstPoint,
        Point2D secondPoint)
    {
        var line = new LineEntity(
            firstPoint,
            secondPoint,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(line));

        return ToolResult.Completed("Line created.");
    }
}