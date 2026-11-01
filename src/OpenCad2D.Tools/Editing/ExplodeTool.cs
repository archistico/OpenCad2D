using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Explodes selected polylines into individual line and arc entities and block references into their world-space entities.
/// Clicks add or remove entities from the selection; Enter/right-click explodes the selection and ends the command,
/// or ends it directly when nothing is selected.
/// </summary>
public sealed class ExplodeTool : ICadTool, ICommandDrivenTool, ISnapModeProvider
{
    public string Name => "Explode";

    public bool IsCommandFinished { get; private set; }

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
                ? "1 entity selected. Click to add or remove entities; Enter or right-click explodes polylines and blocks."
                : $"{count} entities selected. Click to add or remove entities; Enter or right-click explodes polylines and blocks.";

            return new CommandPromptState(
                "EXPLODE",
                message,
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Enter/right-click to explode");
        }

        return new CommandPromptState(
            "EXPLODE",
            "Select polylines or blocks to explode, or press Enter to finish",
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
            return Confirm(context);
        }

        return ToolResult.None("Select polylines or blocks to explode, then press Enter or right-click.");
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        // A click always picks: with a preselection it adds or removes the clicked entity
        // instead of exploding immediately, so the click target is never ignored.
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

        IsCommandFinished = false;

        return ToolResult.Cancelled("Explode command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ToolResult.None("Explode tool deactivated.");
    }

    private ToolResult Confirm(ToolContext context)
    {
        if (!context.Selection.HasSelection)
        {
            IsCommandFinished = true;
            return ToolResult.Completed("Explode command finished.");
        }

        int skipped = EditableSelection.Prune(context);
        if (!context.Selection.HasSelection)
        {
            return ToolResult.None(
                "The selected entities were deleted or are on hidden or locked layers. Select polylines or blocks to explode.");
        }

        ToolResult result = Execute(context, skipped);
        if (result.Kind == ToolResultKind.Completed)
        {
            IsCommandFinished = true;
        }

        return result;
    }

    private static ToolResult Execute(
        ToolContext context,
        int skippedCount)
    {
        GeometryTolerance tolerance = context.GeometryTolerance;
        IReadOnlyList<CadEntity> selectedEntities = EditableSelection.GetEntities(context);

        var removedEntities = new List<CadEntity>();
        var newEntities = new List<CadEntity>();
        int polylineCount = 0;
        int blockReferenceCount = 0;
        int reassignedCount = 0;
        int droppedCount = 0;

        foreach (CadEntity entity in selectedEntities)
        {
            switch (entity)
            {
                case PolylineEntity polyline:
                    List<CadEntity> segments = CreatePolylineSegments(polyline, tolerance).ToList();
                    if (segments.Count == 0)
                    {
                        continue;
                    }

                    removedEntities.Add(polyline);
                    newEntities.AddRange(segments);
                    polylineCount++;
                    break;

                case BlockReferenceEntity blockReference
                    when context.Document.BlockDefinitions.Contains(blockReference.BlockDefinitionId):
                    List<CadEntity> children = CreateBlockEntities(
                        context,
                        blockReference,
                        ref reassignedCount,
                        ref droppedCount).ToList();
                    if (children.Count == 0)
                    {
                        continue;
                    }

                    removedEntities.Add(blockReference);
                    newEntities.AddRange(children);
                    blockReferenceCount++;
                    break;
            }
        }

        if (removedEntities.Count == 0)
        {
            return ToolResult.None(polylineCount == 0 && blockReferenceCount == 0 &&
                                   selectedEntities.Any(entity => entity is PolylineEntity or BlockReferenceEntity)
                ? "Selected entities do not contain geometry to explode."
                : "No explodable polylines or blocks selected.");
        }

        context.Commands.Execute(
            context.Document,
            new ModifyEntitiesCommand(
                removedEntities,
                newEntities,
                "Explode entities"));

        context.Selection.Set.Clear();

        string message = CreateCompletedMessage(polylineCount, blockReferenceCount, newEntities.Count);

        if (reassignedCount > 0)
        {
            message += reassignedCount == 1
                ? " 1 block entity whose layer no longer exists was placed on the block reference layer."
                : $" {reassignedCount} block entities whose layer no longer exists were placed on the block reference layer.";
        }

        if (droppedCount > 0)
        {
            message += droppedCount == 1
                ? " 1 nested block reference with a missing definition was skipped."
                : $" {droppedCount} nested block references with a missing definition were skipped.";
        }

        return ToolResult.Completed(message + EditableSelection.FormatSkippedSuffix(skippedCount));
    }

    private static IEnumerable<CadEntity> CreateBlockEntities(
        ToolContext context,
        BlockReferenceEntity blockReference,
        ref int reassignedCount,
        ref int droppedCount)
    {
        BlockDefinition definition = context.Document.BlockDefinitions.GetRequired(blockReference.BlockDefinitionId);
        var entities = new List<CadEntity>(definition.Entities.Count);

        foreach (CadEntity entity in definition.Entities)
        {
            if (entity is BlockReferenceEntity nested &&
                !context.Document.BlockDefinitions.Contains(nested.BlockDefinitionId))
            {
                droppedCount++;
                continue;
            }

            CadEntity exploded = blockReference
                .TransformContainedEntity(entity)
                .WithId(EntityId.New());

            // A block definition can outlive a layer used by its entities. Adding such an entity
            // would fail half-way through the command, so it is placed on the reference's layer.
            if (!context.Document.Layers.Contains(exploded.LayerId))
            {
                exploded = exploded.WithLayer(blockReference.LayerId);
                reassignedCount++;
            }

            entities.Add(exploded);
        }

        return entities;
    }

    private static string CreateCompletedMessage(
        int polylineCount,
        int blockReferenceCount,
        int createdEntityCount)
    {
        if (blockReferenceCount == 0)
        {
            return polylineCount == 1
                ? $"Polyline exploded into {createdEntityCount} entities."
                : $"{polylineCount} polylines exploded into {createdEntityCount} entities.";
        }

        if (polylineCount == 0)
        {
            return blockReferenceCount == 1
                ? $"Block exploded into {createdEntityCount} entities."
                : $"{blockReferenceCount} blocks exploded into {createdEntityCount} entities.";
        }

        return $"{polylineCount} polylines and {blockReferenceCount} blocks exploded into {createdEntityCount} entities.";
    }

    private static IEnumerable<CadEntity> CreatePolylineSegments(
        PolylineEntity polyline,
        GeometryTolerance tolerance)
    {
        for (int index = 0; index < polyline.SegmentCount; index++)
        {
            Point2D start = polyline.Vertices[index];
            Point2D end = polyline.Vertices[(index + 1) % polyline.Vertices.Count];

            // Repeated vertices (including a closing vertex repeated at the end) would
            // otherwise become zero-length lines or degenerate arcs.
            if (tolerance.ArePointsEqual(start, end))
            {
                continue;
            }

            double bulge = index < polyline.SegmentBulges.Count
                ? polyline.SegmentBulges[index]
                : 0.0;

            yield return CreateSegmentEntity(polyline, start, end, bulge);
        }
    }

    private static CadEntity CreateSegmentEntity(
        PolylineEntity source,
        Point2D start,
        Point2D end,
        double bulge)
    {
        if (Tolerance.IsZero(bulge))
        {
            return CreateLine(source, start, end);
        }

        return CreateArc(source, start, end, bulge);
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

    private static CadEntity CreateArc(
        PolylineEntity source,
        Point2D start,
        Point2D end,
        double bulge)
    {
        double chordLength = start.DistanceTo(end);
        double sweep = -4.0 * Math.Atan(bulge);
        double includedAngle = Math.Abs(sweep);

        if (Tolerance.IsZero(includedAngle))
        {
            return CreateLine(source, start, end);
        }

        double radius = chordLength / (2.0 * Math.Sin(includedAngle / 2.0));
        Point2D midpoint = new(
            (start.X + end.X) / 2.0,
            (start.Y + end.Y) / 2.0);

        Vector2D chord = start.VectorTo(end).Normalize();
        Vector2D leftNormal = new(-chord.Y, chord.X);
        double centerOffset = chordLength * (1.0 - bulge * bulge) / (4.0 * bulge);
        Point2D center = midpoint - leftNormal * centerOffset;

        Angle startAngle = Angle.FromRadians(Math.Atan2(
            start.Y - center.Y,
            start.X - center.X));
        Angle endAngle = Angle.FromRadians(Math.Atan2(
            end.Y - center.Y,
            end.X - center.X));

        bool isCounterClockwise = bulge < 0.0;

        return new ArcEntity(
            center,
            radius,
            startAngle,
            endAngle,
            isCounterClockwise,
            layerId: source.LayerId,
            style: source.Style,
            isVisible: source.IsVisible,
            isLocked: source.IsLocked,
            drawOrder: source.DrawOrder);
    }
}
