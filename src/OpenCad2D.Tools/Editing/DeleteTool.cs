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
    private bool _isSelectingObjects;

    public string Name => "Delete";

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.EntityOnly;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string message;
        string placeholder;

        if (context.Selection.HasSelection)
        {
            int count = context.Selection.SelectedIds.Count;
            message = count == 1
                ? "1 entity selected. Press Enter or right-click to delete."
                : $"{count} entities selected. Press Enter or right-click to delete.";
            placeholder = "Enter/right-click to delete";
        }
        else
        {
            message = "Select objects to delete";
            placeholder = "Select objects, then press Enter/right-click";
        }

        return new CommandPromptState(
            "DELETE",
            message,
            CommandInputKind.Selection,
            acceptsEmptyEnter: true,
            placeholder: placeholder);
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

        return ToolResult.None("Select entities to delete, then press Enter or right-click.");
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
        _isSelectingObjects = false;

        return ToolResult.Completed(selectedIds.Count == 1
            ? "Selected entity deleted."
            : $"{selectedIds.Count} selected entities deleted.");
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
            return ToolResult.None("Select entities to delete, then press Enter or right-click.");
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
            return ToolResult.Updated("Entity removed from delete selection. Select objects to delete.");
        }

        return ToolResult.Updated(count == 1
            ? "1 entity selected for deletion. Select more entities or press Enter/right-click to delete."
            : $"{count} entities selected for deletion. Select more entities or press Enter/right-click to delete.");
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

        return ToolResult.Cancelled("Delete command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _isSelectingObjects = false;

        return ToolResult.None("Delete tool deactivated.");
    }
}