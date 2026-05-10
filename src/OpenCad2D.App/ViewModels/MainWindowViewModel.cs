using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;
using OpenCad2D.App.ViewModels.Properties;
using System.IO;
using OpenCad2D.Persistence.Dto;
using OpenCad2D.Persistence;
using OpenCad2D.Export.Svg;
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
    private readonly SelectionPropertyPanelBuilder _propertyPanelBuilder = new();
    private readonly IDocumentSerializer _documentSerializer = new JsonDocumentSerializer();
    private readonly ISvgExporter _svgExporter = new SvgExporter();
    private string? _currentFilePath;
    private PropertyPanelViewModel _propertyPanel = new("Properties", Array.Empty<PropertySectionViewModel>());
    private bool _isPropertyPanelVisible = true;

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
        RefreshPropertyPanel();
    }

    public CadWorkspace Workspace { get; }

    public string? CurrentFilePath => _currentFilePath;

    public string CurrentFileName => string.IsNullOrWhiteSpace(_currentFilePath)
        ? "Untitled"
        : Path.GetFileName(_currentFilePath);

    public bool IsDirty => Workspace.IsDirty;

    public string TitleText => IsDirty
        ? $"OpenCad2D - {CurrentFileName} *"
        : $"OpenCad2D - {CurrentFileName}";

    public string FileStatusText => IsDirty
        ? $"{CurrentFileName} *"
        : CurrentFileName;

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

    public string ActiveToolName =>
        Workspace.ToolController.ActiveToolName;

    public int EntityCount =>
        Workspace.Document.Entities.Count;

    public int SelectedCount =>
        Workspace.SelectionSet.Count;

    public bool IsPropertyPanelVisible =>
        _isPropertyPanelVisible;

    public PropertyPanelViewModel PropertyPanel =>
        _propertyPanel;

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

            targetPoint = ToolInputConstraintService.ApplyOrtho(
                Workspace.Context.IsOrthoEnabled,
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
            if (Workspace.ToolController.ActiveTool is AlignTool alignTool &&
                alignTool.State == AlignToolState.WaitingForScaleConfirmation)
            {
                return "Apply scale? Press Y for Yes, or N/Enter for No:";
            }

            if (Workspace.ToolController.ActiveTool is PolylineTool polylineTool)
            {
                return polylineTool.State == PolylineToolState.WaitingForFirstPoint
                    ? "Polyline: specify first point or type coordinates:"
                    : "Polyline: specify next point, type distance, press Enter to finish, or C to close:";
            }

            return Workspace.Context.CurrentBasePoint is null
                ? "Specify first point or type coordinates:"
                : "Specify second point, type coordinates, relative coordinates, or distance:";
        }
    }

    public string StatusText =>
        $"Tool: {ActiveToolName} | " +
        $"{CurrentLayerText} | " +
        $"Entities: {EntityCount} | " +
        $"Selected: {SelectedCount} | " +
        $"{MousePositionText} | " +
        $"{MeasurementText} | " +
        $"{SnapText} | " +
        $"{LastMessage}";



    public void NewDocument()
    {
        Workspace.NewDocument();
        _currentFilePath = null;
        SetMessage("New document created.");
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();
    }

    public void SaveToFile(
        string filePath,
        ViewportStateDto viewportState)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(filePath));
        }

        DocumentDto dto = _documentSerializer.Serialize(
            Workspace.Document,
            Workspace.CurrentLayerId.Value,
            viewportState);

        _documentSerializer.SaveToFile(
            dto,
            filePath);

        _currentFilePath = filePath;
        Workspace.MarkSaved();

        SetMessage($"Saved '{CurrentFileName}'.");
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();
    }

    public ViewportStateDto OpenFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "File path cannot be empty.",
                nameof(filePath));
        }

        DocumentDto dto = _documentSerializer.LoadFromFile(filePath);

        CadDocument document = _documentSerializer.Deserialize(
            dto,
            out string currentLayerId,
            out ViewportStateDto viewportState);

        Workspace.LoadDocument(
            document,
            new LayerId(currentLayerId));

        _currentFilePath = filePath;

        SetMessage($"Opened '{CurrentFileName}'.");
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();

        return viewportState;
    }

    public SvgExportResult ExportSvgToFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "SVG export file path cannot be empty.",
                nameof(filePath));
        }

        var options = new SvgExportOptions
        {
            Title = CurrentFileName == "Untitled"
                ? "OpenCad2D export"
                : CurrentFileName
        };

        SvgExportResult result = _svgExporter.Export(
            Workspace.Document,
            options);

        _svgExporter.ExportToFile(
            Workspace.Document,
            filePath,
            options);

        SetMessage($"Exported SVG '{Path.GetFileName(filePath)}' ({result.ExportedEntityCount} entities)." );
        OnPropertiesChanged(
            nameof(LastMessage),
            nameof(StatusText));

        return result;
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

        directionPoint = ToolInputConstraintService.ApplyOrtho(
            Workspace.Context.IsOrthoEnabled,
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

    public void TogglePropertyPanel()
    {
        _isPropertyPanelVisible = !_isPropertyPanelVisible;

        OnPropertiesChanged(
            nameof(IsPropertyPanelVisible),
            nameof(StatusText));
    }

    public void SetPropertyPanelVisible(bool isVisible)
    {
        if (_isPropertyPanelVisible == isVisible)
        {
            return;
        }

        _isPropertyPanelVisible = isVisible;

        OnPropertiesChanged(
            nameof(IsPropertyPanelVisible),
            nameof(StatusText));
    }

    public void RefreshPropertyPanel()
    {
        _propertyPanel = _propertyPanelBuilder.Build(Workspace);

        OnPropertiesChanged(nameof(PropertyPanel));
    }

    public void SetMousePosition(Point2D point)
    {
        _mousePosition = point;

        OnPropertiesChanged(
            nameof(MousePositionText),
            nameof(MeasurementText),
            nameof(StatusText));
    }

    public void SetCurrentSnapCandidate(SnapCandidate? candidate)
    {
        _currentSnapCandidate = candidate;

        OnPropertiesChanged(
            nameof(SnapText),
            nameof(MeasurementText),
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
            nameof(StatusText));
    }

    public void SetCurrentLayer(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (!layer.IsVisible || layer.IsLocked)
        {
            SetMessage("The current layer must be visible and unlocked.");
            NotifyLayerStateChanged();
            return;
        }

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


    public ToolResult ApplyLayerChanges(
        IEnumerable<Layer> layers,
        LayerId currentLayerId)
    {
        ToolResult result = Workspace.ApplyLayerChanges(
            layers,
            currentLayerId);

        SetLastResult(result);
        NotifyLayerStateChanged();
        NotifyDocumentStateChanged();

        return result;
    }

    public void SetCurrentLayerVisibility(bool isVisible)
    {
        Layer currentLayer = CurrentLayer;

        if (!isVisible)
        {
            SetMessage("The current layer must remain visible.");
            NotifyLayerStateChanged();
            return;
        }

        Workspace.Document.Layers.SetVisibility(
            currentLayer.Id,
            isVisible);

        SetMessage($"Layer '{currentLayer.Name}' visible.");

        NotifyLayerStateChanged();
        NotifyDocumentStateChanged();
    }

    public ToolResult SetCurrentLayerLocked(bool isLocked)
    {
        string layerName = CurrentLayer.Name;

        if (isLocked)
        {
            ToolResult rejected = ToolResult.None("The current layer must remain unlocked.");

            SetLastResult(rejected);
            NotifyLayerStateChanged();

            return rejected;
        }

        ToolResult result = Workspace.SetCurrentLayerLocked(false);

        SetLastResult(result);
        SetMessage($"Layer '{layerName}' unlocked.");

        NotifyLayerStateChanged();
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
            nameof(StatusText));

        return result;
    }

    public ToolResult Undo()
    {
        ToolResult result = Workspace.ActionController.Undo();

        Workspace.EnsureCurrentLayerIsUsable();
        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult Redo()
    {
        ToolResult result = Workspace.ActionController.Redo();

        Workspace.EnsureCurrentLayerIsUsable();
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
            nameof(StatusText));
    }

    public void NotifyDocumentStateChanged()
    {
        RefreshPropertyPanel();

        OnPropertiesChanged(
            nameof(StatusText),
            nameof(EntityCount),
            nameof(SelectedCount),
            nameof(ActiveToolName),
            nameof(LayerNames),
            nameof(Layers),
            nameof(CurrentLayer),
            nameof(CurrentLayerIsVisible),
            nameof(CurrentLayerIsLocked),
            nameof(IsOrthoEnabled),
            nameof(CurrentLayerText),
            nameof(MousePositionText),
            nameof(MeasurementText),
            nameof(SnapText),
            nameof(LastMessage),
            nameof(CommandPromptText),
            nameof(IsPropertyPanelVisible),
            nameof(PropertyPanel),
            nameof(IsDirty),
            nameof(TitleText),
            nameof(FileStatusText),
            nameof(CurrentFilePath),
            nameof(CurrentFileName));
    }

    private void NotifyCommandInputStateChanged()
    {
        OnPropertiesChanged(
            nameof(CommandPromptText),
            nameof(MeasurementText),
            nameof(StatusText),
            nameof(LastMessage));
    }

    private void NotifyLayerStateChanged()
    {
        RefreshPropertyPanel();

        OnPropertiesChanged(
            nameof(LayerNames),
            nameof(Layers),
            nameof(CurrentLayer),
            nameof(CurrentLayerIsVisible),
            nameof(CurrentLayerIsLocked),
            nameof(IsOrthoEnabled),
            nameof(CurrentLayerText),
            nameof(CommandPromptText),
            nameof(MeasurementText),
            nameof(StatusText));
    }


    private void NotifyFileStateChanged()
    {
        OnPropertiesChanged(
            nameof(CurrentFilePath),
            nameof(CurrentFileName),
            nameof(IsDirty),
            nameof(TitleText),
            nameof(FileStatusText),
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