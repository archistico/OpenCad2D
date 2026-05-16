using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to draw circle entities by center and radius point.
/// </summary>
public sealed class CircleTool : TwoPointToolBase, ICommandDrivenTool
{
    public override string Name => "Circle";

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return State == TwoPointToolState.WaitingForFirstPoint
            ? new CommandPromptState(
                "CIRCLE",
                "Specify center point",
                CommandInputKind.Point,
                placeholder: "100,50")
            : new CommandPromptState(
                "CIRCLE",
                "Specify radius point or type radius",
                CommandInputKind.PointOrDistance,
                placeholder: "100,50   |   @50,0   |   @100<45   |   radius");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "CIRCLE expects a point or radius input.");
        }

        return SubmitResolvedPoint(context, input.Point.Value);
    }

    public CircleEntity? GetPreviewEntity()
    {
        if (!HasPreview || FirstPoint is null || CurrentPoint is null)
        {
            return null;
        }

        double radius = FirstPoint.Value.DistanceTo(CurrentPoint.Value);

        if (radius <= 0)
        {
            return null;
        }

        return new CircleEntity(
            FirstPoint.Value,
            radius);
    }

    protected override ToolResult OnFirstPointSelected(
        ToolContext context,
        Point2D firstPoint)
    {
        return ToolResult.Started(
            "Specify radius point or type radius.");
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
        double radius = firstPoint.DistanceTo(secondPoint);

        if (context.GeometryTolerance.AreDistancesEqual(radius, 0))
        {
            return ToolResult.None(
                "Circle radius must be greater than zero.");
        }

        var circle = new CircleEntity(
            firstPoint,
            radius,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(circle));

        return ToolResult.Completed("Circle created.");
    }
}
