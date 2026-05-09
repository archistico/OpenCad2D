using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Interactive tool used to move selected entities.
/// </summary>
public sealed class MoveTool : TwoPointToolBase
{
    public override string Name => "Move";

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!HasPreview || FirstPoint is null || CurrentPoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        Vector2D displacement = FirstPoint.Value.VectorTo(CurrentPoint.Value);

        return context.Document.Entities
            .GetByIds(context.Selection.SelectedIds)
            .Select(entity => entity.Transform(
                OpenCad2D.Geometry.Transformations.Matrix2D.Translation(
                    displacement.X,
                    displacement.Y)))
            .ToList();
    }

    protected override ToolResult OnFirstPointSelected(
    ToolContext context,
    Point2D firstPoint)
    {
        if (!context.Selection.HasSelection)
        {
            Reset();

            return ToolResult.None("No entities selected.");
        }

        return ToolResult.Started("Specify destination point.");
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
        if (!context.Selection.HasSelection)
        {
            return ToolResult.None("No entities selected.");
        }

        Vector2D displacement = firstPoint.VectorTo(secondPoint);

        IReadOnlyList<EntityId> selectedIds =
            context.Selection.SelectedIds.ToList();

        context.Commands.Execute(
            context.Document,
            new MoveEntitiesCommand(
                selectedIds,
                displacement));

        return ToolResult.Completed("Entities moved.");
    }
}