using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Aggregates the main CAD runtime services used by the application.
/// </summary>
public sealed class CadWorkspace
{
    public CadWorkspace(
        CadDocument? document = null,
        CommandHistory? commandHistory = null,
        SelectionSet? selectionSet = null,
        SnapService? snapService = null,
        SelectionService? selectionService = null,
        ToolRegistry? toolRegistry = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0,
        double selectionTolerance = 5,
        double selectionDragThreshold = 1,
        ToolId initialToolId = ToolId.Selection)
    {
        Document = document ?? new CadDocument();
        CommandHistory = commandHistory ?? new CommandHistory();
        SelectionSet = selectionSet ?? new SelectionSet();
        SnapService = snapService ?? new SnapService();
        SelectionService = selectionService ?? new SelectionService();
        ToolRegistry = toolRegistry ?? new ToolRegistry();

        Context = new ToolContext(
            Document,
            CommandHistory,
            SnapService,
            selectionSet: SelectionSet,
            selectionService: SelectionService,
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance,
            selectionTolerance: selectionTolerance,
            selectionDragThreshold: selectionDragThreshold);

        ToolController = new ToolController(
            Context,
            ToolRegistry.Create(initialToolId));

        ActionController = new CadActionController(
            Context,
            ToolController);
    }

    public CadDocument Document { get; }

    public CommandHistory CommandHistory { get; }

    public SelectionSet SelectionSet { get; }

    public SnapService SnapService { get; }

    public SelectionService SelectionService { get; }

    public ToolRegistry ToolRegistry { get; }

    public ToolContext Context { get; }

    public ToolController ToolController { get; }

    public CadActionController ActionController { get; }

    public ToolResult SetActiveTool(ToolId toolId)
    {
        ICadTool tool = ToolRegistry.Create(toolId);

        return ToolController.SetActiveTool(tool);
    }

    public ToolResult SetActiveToolWithoutDeactivating(ToolId toolId)
    {
        ICadTool tool = ToolRegistry.Create(toolId);

        return ToolController.SetActiveToolWithoutDeactivating(tool);
    }
}