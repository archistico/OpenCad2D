using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCad2D.App.ViewModels;

public sealed class MainWindowViewModel
{
    private Point2D _mousePosition = Point2D.Origin;
    private string _lastMessage = "Ready.";
    private SnapCandidate? _currentSnapCandidate;

    public IReadOnlyList<string> LayerNames =>
    Workspace.Document.Layers.All
        .OrderBy(layer => layer.Name)
        .Select(layer => layer.Name)
        .ToList();

    public MainWindowViewModel()
    {
        Workspace = new CadWorkspace(
            enabledSnaps:
                SnapKind.Endpoint |
                SnapKind.Midpoint |
                SnapKind.Center |
                SnapKind.Quadrant |
                SnapKind.Intersection |
                SnapKind.Perpendicular |
                SnapKind.Tangent |
                SnapKind.Grid,
            snapTolerance: 8,
            selectionTolerance: 6,
            selectionDragThreshold: 4);

        EnsureDemoLayers();
        SeedDemoDrawing();
    }

    public CadWorkspace Workspace { get; }

    public IReadOnlyList<Layer> Layers =>
        Workspace.Document.Layers.All
            .OrderBy(layer => layer.Name)
            .ToList();

    public Layer CurrentLayer =>
        Workspace.Document.Layers.GetRequired(Workspace.CurrentLayerId);

    public string ActiveToolName => Workspace.ToolController.ActiveToolName;

    public int EntityCount => Workspace.Document.Entities.Count;

    public int SelectedCount => Workspace.SelectionSet.Count;

    public string LastMessage => _lastMessage;

    public string MousePositionText
    {
        get
        {
            Point2D userPoint = Workspace.CurrentUcs.WorldToUser(_mousePosition);

            return
                $"WCS X: {_mousePosition.X:0.###} Y: {_mousePosition.Y:0.###} | " +
                $"UCS X: {userPoint.X:0.###} Y: {userPoint.Y:0.###}";
        }
    }

    public string SnapText =>
        _currentSnapCandidate is null
            ? "Snap: -"
            : $"Snap: {_currentSnapCandidate.Kind}";

    public string CurrentLayerText =>
        $"Layer: {CurrentLayer.Name}";

    public string StatusText =>
        $"Tool: {ActiveToolName} | " +
        $"Layer: {CurrentLayer.Name} | " +
        $"Entities: {EntityCount} | " +
        $"Selected: {SelectedCount} | " +
        $"{MousePositionText} | " +
        $"{SnapText} | " +
        $"{LastMessage}";

    public void SetMousePosition(Point2D point)
    {
        _mousePosition = point;
    }

    public void SetCurrentSnapCandidate(SnapCandidate? candidate)
    {
        _currentSnapCandidate = candidate;
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

    public void SetCurrentLayer(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        Workspace.CurrentLayerId = layer.Id;

        SetMessage($"Current layer changed to '{layer.Name}'.");
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

    public bool IsSnapEnabled(SnapKind snapKind)
    {
        return Workspace.Context.EnabledSnaps.HasFlag(snapKind);
    }

    public void SetSnapEnabled(
        SnapKind snapKind,
        bool isEnabled)
    {
        if (isEnabled)
        {
            Workspace.Context.EnabledSnaps |= snapKind;
        }
        else
        {
            Workspace.Context.EnabledSnaps &= ~snapKind;
        }

        SetMessage($"Snap settings updated: {Workspace.Context.EnabledSnaps}");
    }

    private void EnsureDemoLayers()
    {
        AddLayerIfMissing(
            new LayerId("Walls"),
            "Walls",
            CadColor.FromRgb(230, 120, 80));

        AddLayerIfMissing(
            new LayerId("Furniture"),
            "Furniture",
            CadColor.FromRgb(120, 200, 120));

        AddLayerIfMissing(
            new LayerId("Annotations"),
            "Annotations",
            CadColor.FromRgb(120, 170, 240));
    }

    private void AddLayerIfMissing(
        LayerId layerId,
        string name,
        CadColor color)
    {
        if (Workspace.Document.Layers.Contains(layerId))
        {
            return;
        }

        Workspace.Document.Layers.Add(
            new Layer(
                layerId,
                name,
                color,
                LineWeight.FromMillimeters(0.25)));
    }

    public void SetCurrentLayerByName(string layerName)
    {
        Layer? layer = Workspace.Document.Layers.All
            .FirstOrDefault(layer => layer.Name == layerName);

        if (layer is null)
        {
            return;
        }

        SetCurrentLayer(layer);
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
}