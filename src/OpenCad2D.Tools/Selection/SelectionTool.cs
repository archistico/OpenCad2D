using OpenCad2D.Core.Identifiers;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Selection;

/// <summary>
/// Interactive tool used to select entities by point.
/// </summary>
public sealed class SelectionTool : ICadTool
{
    public string Name => "Selection";

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        EntityId? selectedId = context.SelectionService.SelectByPoint(
            context.Document,
            pointer.ModelPoint,
            context.SelectionTolerance);

        if (selectedId is null)
        {
            if (!pointer.IsShiftPressed)
            {
                context.SelectionSet.Clear();
                return ToolResult.Updated("Selection cleared.");
            }

            return ToolResult.None("Nothing selected.");
        }

        if (pointer.IsShiftPressed)
        {
            context.SelectionSet.Toggle(selectedId.Value);
            return ToolResult.Updated("Selection toggled.");
        }

        context.SelectionSet.ReplaceWith(selectedId.Value);
        return ToolResult.Updated("Entity selected.");
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

        context.SelectionSet.Clear();

        return ToolResult.Cancelled("Selection cleared.");
    }
}