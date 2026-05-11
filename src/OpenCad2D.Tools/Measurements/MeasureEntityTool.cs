using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Measurements;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Measurements;

/// <summary>
/// Non-destructive tool that reports measurements for the entity under the cursor.
/// </summary>
public sealed class MeasureEntityTool : ICadTool, ISnapModeProvider
{
    private EntityId? _lastMeasuredEntityId;

    public string Name => "Measure Entity";

    public EntityId? LastMeasuredEntityId => _lastMeasuredEntityId;

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.EntityOnly;
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
            return ToolResult.None("No measurable entity found.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(entityId.Value);
        EntityMeasurement measurement = MeasurementService.MeasureEntity(entity);
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
        return ToolResult.Cancelled("Measure entity cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _lastMeasuredEntityId = null;
        return ToolResult.None();
    }
}
