using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Interactive tool used to copy selected entities.
/// </summary>
public sealed class CopyTool : TwoPointToolBase
{
    public override string Name => "Copy";

    public IReadOnlyList<CadEntity> GetPreviewEntities(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!HasPreview || FirstPoint is null || CurrentPoint is null)
        {
            return Array.Empty<CadEntity>();
        }

        Vector2D displacement = FirstPoint.Value.VectorTo(CurrentPoint.Value);

        Matrix2D matrix = Matrix2D.Translation(
            displacement.X,
            displacement.Y);

        return context.Document.Entities
            .GetByIds(context.Selection.SelectedIds)
            .Select(entity => entity.Transform(matrix).WithId(EntityId.New()))
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
            new CopyEntitiesCommand(
                selectedIds,
                displacement));

        return ToolResult.Completed("Entities copied.");
    }
}