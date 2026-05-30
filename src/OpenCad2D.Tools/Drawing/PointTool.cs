using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Drawing;

/// <summary>
/// Interactive tool used to insert point entities.
/// </summary>
public sealed class PointTool : ICadTool, ICommandDrivenTool
{
    public string Name => "Point";

    public Point2D? LastCreatedPosition { get; private set; }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CommandPromptState(
            "POINT",
            "Specify point",
            CommandInputKind.Point,
            placeholder: "100,50   |   @50,0");
    }


    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(
                input.ErrorMessage ?? $"{Name} expects a point input.");
        }

        return CreatePoint(
            context,
            input.Point.Value);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        Point2D position = ApplySnap(
            context,
            pointer.ModelPoint);

        return CreatePoint(
            context,
            position);
    }


    private ToolResult CreatePoint(
        ToolContext context,
        Point2D position)
    {
        var point = new PointEntity(
            position,
            layerId: context.Creation.CurrentLayerId);

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(point));

        LastCreatedPosition = position;
        context.CurrentBasePoint = position;

        return ToolResult.Completed("Point created.");
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return ToolResult.None();
    }

    private static Point2D ApplySnap(
        ToolContext context,
        Point2D cursorPoint)
    {
        if (context.EnabledSnaps == SnapKind.None ||
            Tolerance.IsZero(context.SnapTolerance))
        {
            return cursorPoint;
        }

        var request = new SnapRequest(
            context.Document,
            cursorPoint,
            context.SnapTolerance,
            context.EnabledSnaps,
            context.CurrentBasePoint,
            context.GridSettings);

        SnapCandidate? candidate = context.SnapService.Snap(request);

        return candidate?.Point ?? cursorPoint;
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastCreatedPosition = null;
        context.CurrentBasePoint = null;

        return ToolResult.Cancelled("Point cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        LastCreatedPosition = null;
        context.CurrentBasePoint = null;

        return ToolResult.None();
    }
}
