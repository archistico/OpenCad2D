using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Explodes selected straight-segment polylines into individual line entities and block references into their world-space entities.
/// </summary>
public sealed class ExplodeTool : ICadTool, ICommandDrivenTool, ISnapModeProvider
{
    private bool _isSelectingObjects;

    public string Name => "Explode";

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.EntityOnly;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Selection.HasSelection)
        {
            int count = context.Selection.SelectedIds.Count;
            string message = count == 1
                ? "1 entity selected. Press Enter or right-click to explode polylines or blocks."
                : $"{count} entities selected. Press Enter or right-click to explode polylines or blocks.";

            return new CommandPromptState(
                "EXPLODE",
                message,
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Enter/right-click to explode");
        }

        return new CommandPromptState(
            "EXPLODE",
            "Select polylines or blocks to explode",
            CommandInputKind.Selection,
            acceptsEmptyEnter: true,
            placeholder: "Select polylines/blocks, then press Enter/right-click");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return Execute(context);
        }

        return ToolResult.None("Select polylines or blocks to explode, then press Enter or right-click.");
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (!_isSelectingObjects && context.Selection.HasSelection)
        {
            return Execute(context);
        }

        _isSelectingObjects = true;

        EntityId? selectedId = context.Selection.Service.SelectByPoint(
            context.Document,
            pointer.ModelPoint,
            context.Selection.Tolerance);

        if (selectedId is null)
        {
            return ToolResult.None("Select polylines or blocks to explode, then press Enter or right-click.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);
        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        context.Selection.Set.Toggle(selectedId.Value);

        int count = context.Selection.SelectedIds.Count;
        if (count == 0)
        {
            return ToolResult.Updated("Entity removed from explode selection. Select polylines or blocks to explode.");
        }

        return ToolResult.Updated(count == 1
            ? "1 entity selected for explode. Select more entities or press Enter/right-click to explode."
            : $"{count} entities selected for explode. Select more entities or press Enter/right-click to explode.");
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

        _isSelectingObjects = false;

        return ToolResult.Cancelled("Explode command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _isSelectingObjects = false;

        return ToolResult.None("Explode tool deactivated.");
    }

    private ToolResult Execute(ToolContext context)
    {
        IReadOnlyList<CadEntity> selectedEntities = context.Selection.SelectedIds
            .Select(context.Document.Entities.GetRequired)
            .ToList();

        var polylines = selectedEntities
            .OfType<PolylineEntity>()
            .Where(polyline => polyline.Vertices.Count >= 2)
            .ToList();

        var blockReferences = selectedEntities
            .OfType<BlockReferenceEntity>()
            .Where(blockReference => context.Document.BlockDefinitions.Contains(blockReference.BlockDefinitionId))
            .ToList();

        if (polylines.Count == 0 && blockReferences.Count == 0)
        {
            return ToolResult.None("No explodable polylines or blocks selected.");
        }

        var newEntities = new List<CadEntity>();
        newEntities.AddRange(polylines.SelectMany(CreateLineSegments));
        newEntities.AddRange(blockReferences.SelectMany(blockReference => CreateBlockEntities(context, blockReference)));

        if (newEntities.Count == 0)
        {
            return ToolResult.None("Selected entities do not contain geometry to explode.");
        }

        var removedEntities = polylines
            .Cast<CadEntity>()
            .Concat(blockReferences)
            .ToList();

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                removedEntities,
                newEntities,
                "Explode entities"));

        context.Selection.Set.Clear();
        _isSelectingObjects = false;

        return ToolResult.Completed(CreateCompletedMessage(polylines.Count, blockReferences.Count, newEntities.Count));
    }

    private static IEnumerable<CadEntity> CreateBlockEntities(
        ToolContext context,
        BlockReferenceEntity blockReference)
    {
        BlockDefinition definition = context.Document.BlockDefinitions.GetRequired(blockReference.BlockDefinitionId);

        foreach (CadEntity entity in definition.Entities)
        {
            yield return blockReference
                .TransformContainedEntity(entity)
                .WithId(EntityId.New());
        }
    }

    private static string CreateCompletedMessage(
        int polylineCount,
        int blockReferenceCount,
        int createdEntityCount)
    {
        if (blockReferenceCount == 0)
        {
            return polylineCount == 1
                ? $"Polyline exploded into {createdEntityCount} lines."
                : $"{polylineCount} polylines exploded into {createdEntityCount} lines.";
        }

        if (polylineCount == 0)
        {
            return blockReferenceCount == 1
                ? $"Block exploded into {createdEntityCount} entities."
                : $"{blockReferenceCount} blocks exploded into {createdEntityCount} entities.";
        }

        return $"{polylineCount} polylines and {blockReferenceCount} blocks exploded into {createdEntityCount} entities.";
    }

    private static IEnumerable<LineEntity> CreateLineSegments(PolylineEntity polyline)
    {
        for (int i = 0; i < polyline.Vertices.Count - 1; i++)
        {
            yield return CreateLine(polyline, polyline.Vertices[i], polyline.Vertices[i + 1]);
        }

        if (polyline.IsClosed && polyline.Vertices.Count > 2)
        {
            yield return CreateLine(polyline, polyline.Vertices[^1], polyline.Vertices[0]);
        }
    }

    private static LineEntity CreateLine(
        PolylineEntity source,
        Point2D start,
        Point2D end)
    {
        return new LineEntity(
            start,
            end,
            layerId: source.LayerId,
            style: source.Style,
            isVisible: source.IsVisible,
            isLocked: source.IsLocked,
            drawOrder: source.DrawOrder);
    }
}
