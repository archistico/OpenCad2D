using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;
using OpenCad2D.Tools.Grips;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private Point2D _mousePosition = Point2D.Origin;
    private string _lastMessage = "Ready.";
    private SnapCandidate? _currentSnapCandidate;
    private readonly CommandInputParser _commandInputParser = new();

    public MainWindowViewModel()
    {
        Workspace = new CadWorkspace(
            enabledSnaps: SnapKind.Endpoint |
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

    public IReadOnlyList<string> LayerNames => Workspace.Document.Layers.All
        .OrderBy(layer => layer.Name)
        .Select(layer => layer.Name)
        .ToList();

    public IReadOnlyList<Layer> Layers => Workspace.Document.Layers.All
        .OrderBy(layer => layer.Name)
        .ToList();

    public Layer CurrentLayer =>
        Workspace.Document.Layers.GetRequired(Workspace.CurrentLayerId);

    public bool CurrentLayerIsVisible => CurrentLayer.IsVisible;

    public bool CurrentLayerIsLocked => CurrentLayer.IsLocked;

    public bool IsOrthoEnabled => Workspace.Context.IsOrthoEnabled;

    public bool IsGridVisible => Workspace.GridSettings.IsVisible;

    public double GridMinorStep => Workspace.GridSettings.MinorStep;

    public double GridMajorStep => Workspace.GridSettings.MajorStep;

    public double GridMinimumScreenSpacing => Workspace.GridSettings.MinimumScreenSpacing;

    public double GridMaximumScreenSpacing => Workspace.GridSettings.MaximumScreenSpacing;

    public string ActiveToolName =>
        Workspace.ToolController.ActiveToolName;

    public int EntityCount =>
        Workspace.Document.Entities.Count;

    public int SelectedCount =>
        Workspace.SelectionSet.Count;

    public string LastMessage =>
        _lastMessage;

    public string MousePositionText
    {
        get
        {
            Point2D userPoint = Workspace.CurrentUcs.WorldToUser(_mousePosition);

            return $"WCS X: {_mousePosition.X:0.###} Y: {_mousePosition.Y:0.###} | " +
                   $"UCS X: {userPoint.X:0.###} Y: {userPoint.Y:0.###}";
        }
    }

    public string SnapText =>
        _currentSnapCandidate is null
            ? "Snap: -"
            : $"Snap: {_currentSnapCandidate.Kind}";

    public string CurrentLayerText
    {
        get
        {
            string visibility = CurrentLayer.IsVisible
                ? "Visible"
                : "Hidden";

            string locked = CurrentLayer.IsLocked
                ? "Locked"
                : "Unlocked";

            return $"Layer: {CurrentLayer.Name} ({visibility}, {locked})";
        }
    }


    public string MeasurementText
    {
        get
        {
            if (Workspace.Context.CurrentBasePoint is null)
            {
                return "Measure: -";
            }

            Point2D basePoint = Workspace.Context.CurrentBasePoint.Value;
            Point2D targetPoint = _currentSnapCandidate?.Point ?? _mousePosition;

            bool shouldApplyOrtho = true;

            if (Workspace.ToolController.ActiveTool is GripEditTool gripEditTool &&
                gripEditTool.ActiveGripKind == GripKind.ResizeRadius)
            {
                shouldApplyOrtho = false;
            }

            targetPoint = ToolInputConstraintService.ApplyOrtho(
                Workspace.Context.IsOrthoEnabled && shouldApplyOrtho,
                basePoint,
                targetPoint);

            if (Workspace.GeometryTolerance.ArePointsEqual(basePoint, targetPoint))
            {
                return "Measure: L 0 | DX 0 | DY 0";
            }

            Point2D baseUserPoint = Workspace.CurrentUcs.WorldToUser(basePoint);
            Point2D targetUserPoint = Workspace.CurrentUcs.WorldToUser(targetPoint);

            Vector2D delta = baseUserPoint.VectorTo(targetUserPoint);
            double length = delta.Length;

            return $"Measure: L {length:0.###} | DX {delta.X:0.###} | DY {delta.Y:0.###}";
        }
    }

    public string CommandPromptText
    {
        get
        {
            if (Workspace.ToolController.ActiveTool is GripEditTool gripEditTool)
            {
                if (gripEditTool.WarmGripIndex is not null)
                {
                    string gripName = GetGripDescription(
                        gripEditTool,
                        gripEditTool.WarmGripIndex.Value);

                    return $"Grip active ({gripName}). Specify destination, coordinates, relative coordinates, or distance:";
                }

                if (gripEditTool.HotGripIndex is not null)
                {
                    string gripName = GetGripDescription(
                        gripEditTool,
                        gripEditTool.HotGripIndex.Value);

                    return $"Grip Edit: click highlighted grip ({gripName}) or press ESC to exit:";
                }

                return "Grip Edit: click a grip, or press ESC to exit:";
            }

            return Workspace.Context.CurrentBasePoint is null
                ? "Specify first point or type coordinates:"
                : "Specify second point, type coordinates, relative coordinates, or distance:";
        }
    }

    public string GripStatusText
    {
        get
        {
            if (Workspace.ToolController.ActiveTool is not GripEditTool gripEditTool)
            {
                return string.Empty;
            }

            if (gripEditTool.WarmGripIndex is not null)
            {
                string gripName = GetGripDescription(
                    gripEditTool,
                    gripEditTool.WarmGripIndex.Value);

                return $"Grip: active {gripName}";
            }

            if (gripEditTool.HotGripIndex is not null)
            {
                string gripName = GetGripDescription(
                    gripEditTool,
                    gripEditTool.HotGripIndex.Value);

                return $"Grip: hot {gripName}";
            }

            return "Grip: select grip";
        }
    }

    public string GridStatusText
    {
        get
        {
            string state = IsGridVisible
                ? "on"
                : "off";

            return $"Grid: {state} | minor {GridMinorStep:0.###} | major {GridMajorStep:0.###}";
        }
    }

    public string StatusText
    {
        get
        {
            string gripStatus = string.IsNullOrWhiteSpace(GripStatusText)
                ? string.Empty
                : $"{GripStatusText} | ";

            return $"Tool: {ActiveToolName} | " +
                   gripStatus +
                   $"{CurrentLayerText} | " +
                   $"{GridStatusText} | " +
                   $"Entities: {EntityCount} | " +
                   $"Selected: {SelectedCount} | " +
                   $"{MousePositionText} | " +
                   $"{MeasurementText} | " +
                   $"{SnapText} | " +
                   $"{LastMessage}";
        }
    }


    private static string GetGripDescription(
        GripEditTool gripEditTool,
        int gripListIndex)
    {
        if (gripListIndex < 0 || gripListIndex >= gripEditTool.CurrentGrips.Count)
        {
            return "unknown";
        }

        GripPoint grip = gripEditTool.CurrentGrips[gripListIndex];

        return grip.Kind switch
        {
            GripKind.MoveVertex => $"vertex {grip.GripIndex}",
            GripKind.MoveEntity => "move",
            GripKind.ResizeRadius => "radius",
            _ => grip.Kind.ToString()
        };
    }


    public ToolResult SubmitCommandInput(string? input)
    {
        CommandInputParseResult parseResult = _commandInputParser.Parse(input);

        if (!parseResult.IsValid)
        {
            ToolResult invalidResult = ToolResult.None(parseResult.ErrorMessage);
            SetLastResult(invalidResult);
            NotifyCommandInputStateChanged();
            return invalidResult;
        }

        if (!TryResolveCommandInputPoint(parseResult, out Point2D worldPoint, out string? errorMessage))
        {
            ToolResult invalidResult = ToolResult.None(errorMessage);
            SetLastResult(invalidResult);
            NotifyCommandInputStateChanged();
            return invalidResult;
        }

        ToolResult result = Workspace.SubmitPointFromCommandLine(worldPoint);

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyCommandInputStateChanged();

        return result;
    }

    private bool TryResolveCommandInputPoint(
        CommandInputParseResult parseResult,
        out Point2D worldPoint,
        out string? errorMessage)
    {
        worldPoint = Point2D.Origin;
        errorMessage = null;

        switch (parseResult.Kind)
        {
            case CommandInputKind.AbsolutePoint:
                worldPoint = Workspace.CurrentUcs.UserToWorld(parseResult.Point!.Value);
                return true;

            case CommandInputKind.RelativePoint:
                if (Workspace.Context.CurrentBasePoint is null)
                {
                    errorMessage = "Relative coordinates require a base point.";
                    return false;
                }

                Vector2D worldOffset = Workspace.CurrentUcs.UserVectorToWorld(parseResult.Offset!.Value);
                worldPoint = Workspace.Context.CurrentBasePoint.Value + worldOffset;
                return true;

            case CommandInputKind.Distance:
                return TryResolveDirectDistancePoint(
                    parseResult.Distance!.Value,
                    out worldPoint,
                    out errorMessage);

            default:
                errorMessage = "Unsupported command input.";
                return false;
        }
    }

    private bool TryResolveDirectDistancePoint(
        double distance,
        out Point2D worldPoint,
        out string? errorMessage)
    {
        worldPoint = Point2D.Origin;
        errorMessage = null;

        if (Workspace.Context.CurrentBasePoint is null)
        {
            errorMessage = "Direct distance requires a base point.";
            return false;
        }

        Point2D basePoint = Workspace.Context.CurrentBasePoint.Value;
        Point2D directionPoint = _currentSnapCandidate?.Point ?? _mousePosition;

        bool shouldApplyOrtho = true;

        if (Workspace.ToolController.ActiveTool is GripEditTool gripEditTool &&
            gripEditTool.ActiveGripKind == GripKind.ResizeRadius)
        {
            shouldApplyOrtho = false;
        }

        directionPoint = ToolInputConstraintService.ApplyOrtho(
            Workspace.Context.IsOrthoEnabled && shouldApplyOrtho,
            basePoint,
            directionPoint);

        Vector2D direction = basePoint.VectorTo(directionPoint);

        if (Workspace.GeometryTolerance.ArePointsEqual(basePoint, directionPoint))
        {
            errorMessage = "Move the cursor to indicate a direction.";
            return false;
        }

        worldPoint = basePoint + direction.Normalize() * distance;
        return true;
    }

    public void SetMousePosition(Point2D point)
    {
        _mousePosition = point;

        OnPropertiesChanged(
            nameof(MousePositionText),
            nameof(MeasurementText),
            nameof(GripStatusText),
            nameof(StatusText));
    }

    public void SetCurrentSnapCandidate(SnapCandidate? candidate)
    {
        _currentSnapCandidate = candidate;

        OnPropertiesChanged(
            nameof(SnapText),
            nameof(MeasurementText),
            nameof(GripStatusText),
            nameof(StatusText));
    }

    public void SetLastResult(ToolResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Message))
        {
            return;
        }

        _lastMessage = result.Message;

        OnPropertiesChanged(
            nameof(LastMessage),
            nameof(GripStatusText),
            nameof(StatusText));
    }

    public void SetMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _lastMessage = message;

        OnPropertiesChanged(
            nameof(LastMessage),
            nameof(GripStatusText),
            nameof(StatusText));
    }

    public void SetCurrentLayer(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        Workspace.CurrentLayerId = layer.Id;

        SetMessage($"Current layer changed to '{layer.Name}'.");

        NotifyLayerStateChanged();
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

    public void SetCurrentLayerVisibility(bool isVisible)
    {
        Layer currentLayer = CurrentLayer;

        Workspace.Document.Layers.SetVisibility(
            currentLayer.Id,
            isVisible);

        SetMessage(
            isVisible
                ? $"Layer '{currentLayer.Name}' visible."
                : $"Layer '{currentLayer.Name}' hidden.");

        NotifyLayerStateChanged();
        NotifyDocumentStateChanged();
    }

    public ToolResult SetCurrentLayerLocked(bool isLocked)
    {
        string layerName = CurrentLayer.Name;

        ToolResult result = Workspace.SetCurrentLayerLocked(isLocked);

        SetLastResult(result);

        SetMessage(
            isLocked
                ? $"Layer '{layerName}' locked."
                : $"Layer '{layerName}' unlocked.");

        NotifyLayerStateChanged();
        NotifyDocumentStateChanged();

        return result;
    }


    public ToolResult EnterGripEditModeForSelection()
    {
        ToolResult result = Workspace.EnterGripEditModeForSelection();

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult SetTool(ToolId toolId)
    {
        ToolResult result = Workspace.SetActiveTool(toolId);

        SetLastResult(result);
        SetMessage($"Tool changed to {Workspace.ToolController.ActiveToolName}.");

        OnPropertiesChanged(
            nameof(ActiveToolName),
            nameof(GripStatusText),
            nameof(StatusText));

        return result;
    }

    public ToolResult Undo()
    {
        ToolResult result = Workspace.ActionController.Undo();

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult Redo()
    {
        ToolResult result = Workspace.ActionController.Redo();

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult DeleteSelection()
    {
        ToolResult result = Workspace.ActionController.DeleteSelection();

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult Cancel()
    {
        ToolResult result = Workspace.ActionController.CancelActiveTool();

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult Escape()
    {
        ToolResult result = Workspace.Escape();

        SetLastResult(result);
        NotifyDocumentStateChanged();

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

        OnPropertiesChanged(
            nameof(SnapText),
            nameof(GripStatusText),
            nameof(StatusText));
    }

    public void SetOrthoEnabled(bool isEnabled)
    {
        Workspace.Context.IsOrthoEnabled = isEnabled;

        SetMessage(isEnabled
            ? "Ortho mode enabled."
            : "Ortho mode disabled.");

        OnPropertiesChanged(
            nameof(IsOrthoEnabled),
            nameof(MeasurementText),
            nameof(GripStatusText),
            nameof(StatusText));
    }

    public void SetGridVisible(bool isVisible)
    {
        ToolResult result = Workspace.SetGridVisible(isVisible);

        SetLastResult(result);
        SetMessage(isVisible
            ? "Grid display enabled."
            : "Grid display disabled.");

        NotifyGridStateChanged();
    }

    public bool TrySetGridSettings(
        double minorStep,
        double majorStep,
        double minimumScreenSpacing,
        double maximumScreenSpacing,
        out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            GridSettings gridSettings = Workspace.GridSettings
                .WithSpacing(
                    minorStep,
                    majorStep)
                .WithScreenSpacingRange(
                    minimumScreenSpacing,
                    maximumScreenSpacing);

            ToolResult result = Workspace.SetGridSettings(gridSettings);

            SetLastResult(result);
            SetMessage("Grid settings updated.");

            NotifyGridStateChanged();

            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            errorMessage = ex.Message;
            SetMessage(errorMessage);
            NotifyGridStateChanged();
            return false;
        }
    }

    private void NotifyGridStateChanged()
    {
        OnPropertiesChanged(
            nameof(IsGridVisible),
            nameof(GridMinorStep),
            nameof(GridMajorStep),
            nameof(GridMinimumScreenSpacing),
            nameof(GridMaximumScreenSpacing),
            nameof(GridStatusText),
            nameof(StatusText),
            nameof(LastMessage));
    }

    public void NotifyDocumentStateChanged()
    {
        OnPropertiesChanged(
            nameof(StatusText),
            nameof(GripStatusText),
            nameof(EntityCount),
            nameof(SelectedCount),
            nameof(ActiveToolName),
            nameof(LayerNames),
            nameof(Layers),
            nameof(CurrentLayer),
            nameof(CurrentLayerIsVisible),
            nameof(CurrentLayerIsLocked),
            nameof(IsOrthoEnabled),
            nameof(IsGridVisible),
            nameof(GridMinorStep),
            nameof(GridMajorStep),
            nameof(GridMinimumScreenSpacing),
            nameof(GridMaximumScreenSpacing),
            nameof(GridStatusText),
            nameof(CurrentLayerText),
            nameof(MousePositionText),
            nameof(MeasurementText),
            nameof(SnapText),
            nameof(LastMessage),
            nameof(CommandPromptText));
    }

    private void NotifyCommandInputStateChanged()
    {
        OnPropertiesChanged(
            nameof(CommandPromptText),
            nameof(MeasurementText),
            nameof(GripStatusText),
            nameof(StatusText),
            nameof(LastMessage));
    }

    private void NotifyLayerStateChanged()
    {
        OnPropertiesChanged(
            nameof(LayerNames),
            nameof(Layers),
            nameof(CurrentLayer),
            nameof(CurrentLayerIsVisible),
            nameof(CurrentLayerIsLocked),
            nameof(IsOrthoEnabled),
            nameof(IsGridVisible),
            nameof(GridMinorStep),
            nameof(GridMajorStep),
            nameof(GridMinimumScreenSpacing),
            nameof(GridMaximumScreenSpacing),
            nameof(GridStatusText),
            nameof(CurrentLayerText),
            nameof(CommandPromptText),
            nameof(MeasurementText),
            nameof(GripStatusText),
            nameof(StatusText));
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private void OnPropertiesChanged(params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}