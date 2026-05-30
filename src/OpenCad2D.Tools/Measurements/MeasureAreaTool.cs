using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Measurements;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Measurements;

/// <summary>
/// Non-destructive tool that reports area information for a closed polyline.
/// </summary>
public sealed class MeasureAreaTool : ICadTool, ICommandDrivenTool, ISnapModeProvider
{
    private EntityId? _lastMeasuredEntityId;

    public string Name => "Measure Area";

    public EntityId? LastMeasuredEntityId => _lastMeasuredEntityId;

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CommandPromptState(
            "MEASURE AREA",
            "Select closed polyline",
            CommandInputKind.Selection,
            placeholder: "Click closed polyline; Ctrl+click cycles overlaps");
    }

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.EntityOnly;
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

        return OnPointerPressed(
            context,
            new PointerInfo(input.Point.Value));
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        EntityId? entityId = pointer.IsControlPressed
            ? context.Selection.Service.SelectNextByPoint(
                context.Document,
                pointer.ModelPoint,
                context.Selection.Tolerance,
                _lastMeasuredEntityId)
            : context.Selection.Service.SelectByPoint(
                context.Document,
                pointer.ModelPoint,
                context.Selection.Tolerance);

        if (entityId is null)
        {
            _lastMeasuredEntityId = null;
            return ToolResult.None("No closed polyline found.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(entityId.Value);

        if (entity is not PolylineEntity polyline || !polyline.IsClosed)
        {
            _lastMeasuredEntityId = entityId;
            return ToolResult.None("Area can only be measured from a closed polyline.");
        }

        EntityMeasurement measurement = MeasurementService.MeasureEntity(polyline);
        _lastMeasuredEntityId = entityId;

        return ToolResult.Completed(MeasurementFormatter.FormatEntity(measurement));
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

        _lastMeasuredEntityId = null;
        return ToolResult.Cancelled("Measure area cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _lastMeasuredEntityId = null;
        return ToolResult.None();
    }
}
