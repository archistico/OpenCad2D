using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Creates a filled closed polyline from the linear boundary around a picked point.
/// </summary>
public sealed class BoundaryFillTool : ICadTool, ICommandDrivenTool, ISnapModeProvider
{
    private readonly BoundaryFillService _boundaryFillService = new();

    public string Name => "Boundary Fill";

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.Grid;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CommandPromptState(
            "BFILL",
            "Pick inside a closed linear boundary",
            CommandInputKind.Point,
            placeholder: "click inside boundary or 100,50");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind != CommandInputSubmissionKind.Point || input.Point is null)
        {
            return ToolResult.None(input.ErrorMessage ?? "BFILL expects a point inside a closed boundary.");
        }

        return CreateBoundaryFill(
            context,
            input.Point.Value);
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return CreateBoundaryFill(
            context,
            pointer.ModelPoint);
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return ToolResult.None();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ToolResult.Cancelled("Boundary fill cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ToolResult.None();
    }

    private ToolResult CreateBoundaryFill(
        ToolContext context,
        OpenCad2D.Geometry.Primitives.Point2D seedPoint)
    {
        BoundaryFillResult result = _boundaryFillService.CreateFilledPolyline(
            context.Document.GetVisibleEntities(),
            seedPoint,
            context.Creation.CurrentLayerId,
            context.Coordinates.GeometryTolerance);

        if (!result.Succeeded || result.Polyline is null)
        {
            return ToolResult.None(result.Message);
        }

        PolylineEntity polyline = result.Polyline;

        context.Commands.Execute(
            context.Document,
            new AddEntityCommand(polyline));

        return ToolResult.Completed(result.Message);
    }
}
