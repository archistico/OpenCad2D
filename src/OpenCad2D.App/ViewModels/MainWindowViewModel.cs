using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.App.ViewModels;

public sealed class MainWindowViewModel
{
    private Point2D _mousePosition = Point2D.Origin;
    private string _lastMessage = "Ready.";

    public MainWindowViewModel()
    {
        Workspace = new CadWorkspace(
            enabledSnaps:
                SnapKind.Endpoint |
                SnapKind.Midpoint |
                SnapKind.Center |
                SnapKind.Quadrant |
                SnapKind.Intersection,
            snapTolerance: 8,
            selectionTolerance: 6,
            selectionDragThreshold: 4);

        SeedDemoDrawing();
    }

    public CadWorkspace Workspace { get; }

    public string ActiveToolName =>
        Workspace.ToolController.ActiveToolName;

    public int EntityCount =>
        Workspace.Document.Entities.Count;

    public int SelectedCount =>
        Workspace.SelectionSet.Count;

    public string LastMessage =>
        _lastMessage;

    public string MousePositionText =>
        $"X: {_mousePosition.X:0.###}   Y: {_mousePosition.Y:0.###}";

    public string StatusText =>
        $"Tool: {ActiveToolName}   |   Entities: {EntityCount}   |   Selected: {SelectedCount}   |   {MousePositionText}   |   {LastMessage}";

    public void SetMousePosition(Point2D point)
    {
        _mousePosition = point;
    }

    public void SetLastResult(ToolResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            _lastMessage = result.Message;
        }
    }

    public void SetMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _lastMessage = message;
        }
    }

    private void SeedDemoDrawing()
    {
        Workspace.Document.AddEntity(
            new LineEntity(
                new Point2D(50, 50),
                new Point2D(250, 50)));

        Workspace.Document.AddEntity(
            new LineEntity(
                new Point2D(250, 50),
                new Point2D(250, 180)));

        Workspace.Document.AddEntity(
            new CircleEntity(
                new Point2D(400, 150),
                60));

        Workspace.Document.AddEntity(
            new PolylineEntity(
                new[]
                {
                    new Point2D(80, 260),
                    new Point2D(220, 260),
                    new Point2D(220, 360),
                    new Point2D(80, 360)
                },
                isClosed: true));
    }

    public ToolResult SetTool(ToolId toolId)
    {
        ToolResult result = Workspace.SetActiveTool(toolId);
        SetLastResult(result);
        SetMessage($"Tool changed to {Workspace.ToolController.ActiveToolName}.");

        return result;
    }

    public ToolResult Undo()
    {
        ToolResult result = Workspace.ActionController.Undo();
        SetLastResult(result);

        return result;
    }

    public ToolResult Redo()
    {
        ToolResult result = Workspace.ActionController.Redo();
        SetLastResult(result);

        return result;
    }

    public ToolResult DeleteSelection()
    {
        ToolResult result = Workspace.ActionController.DeleteSelection();
        SetLastResult(result);

        return result;
    }

    public ToolResult Cancel()
    {
        ToolResult result = Workspace.ActionController.CancelActiveTool();
        SetLastResult(result);

        return result;
    }
}