using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Tool used to delete selected entities.
/// </summary>
public sealed class DeleteTool : ICadTool, ICommandDrivenTool, ISnapModeProvider
{
    public string Name => "Delete";

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.EntityOnly;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new CommandPromptState(
            "DELETE",
            context.Selection.HasSelection
                ? "Press Enter to delete selected entities"
                : "Select objects to delete",
            CommandInputKind.Selection,
            acceptsEmptyEnter: true,
            placeholder: context.Selection.HasSelection
                ? "Enter"
                : "Select objects, then press Enter");
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

        return ToolResult.None("Select entities to delete, then press Enter.");
    }

    public ToolResult Execute(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Selection.HasSelection)
        {
            return ToolResult.None("No entities selected.");
        }

        IReadOnlyList<EntityId> selectedIds =
            context.Selection.SelectedIds.ToList();

        context.Commands.Execute(
            context.Document,
            new DeleteEntitiesCommand(selectedIds));

        context.Selection.Set.Clear();

        return ToolResult.Completed("Selected entities deleted.");
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (context.Selection.HasSelection)
        {
            return Execute(context);
        }

        EntityId? selectedId = context.Selection.Service.SelectByPoint(
            context.Document,
            pointer.ModelPoint,
            context.Selection.Tolerance);

        if (selectedId is null)
        {
            return ToolResult.None("Select entities to delete, then press Enter.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);
        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        context.Selection.Set.ReplaceWith(selectedId.Value);

        return ToolResult.Updated("Entity selected. Press Enter to delete.");
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