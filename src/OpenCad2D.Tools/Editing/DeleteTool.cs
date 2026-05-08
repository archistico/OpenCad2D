using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Tool used to delete selected entities.
/// </summary>
public sealed class DeleteTool : ICadTool
{
    public string Name => "Delete";

    public ToolResult Execute(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.SelectionSet.IsEmpty)
        {
            return ToolResult.None("No entities selected.");
        }

        IReadOnlyList<EntityId> selectedIds =
            context.SelectionSet.SelectedIds.ToList();

        context.CommandHistory.Execute(
            context.Document,
            new DeleteEntitiesCommand(selectedIds));

        context.SelectionSet.Clear();

        return ToolResult.Completed("Selected entities deleted.");
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return Execute(context);
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

        return ToolResult.Cancelled("Delete command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ToolResult.None("Delete tool deactivated.");
    }
}