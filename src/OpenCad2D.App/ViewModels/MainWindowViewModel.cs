using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.App.ViewModels;

public sealed class MainWindowViewModel
{
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

    public void SetTool(ToolId toolId)
    {
        Workspace.SetActiveTool(toolId);
    }

    public void Undo()
    {
        Workspace.ActionController.Undo();
    }

    public void Redo()
    {
        Workspace.ActionController.Redo();
    }

    public void DeleteSelection()
    {
        Workspace.ActionController.DeleteSelection();
    }

    public void Cancel()
    {
        Workspace.ActionController.CancelActiveTool();
    }
}