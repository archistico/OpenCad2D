using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;
using OpenCad2D.Tools.Measurements;
using OpenCad2D.Tools.Navigation;
using OpenCad2D.App.ViewModels.Properties;
using OpenCad2D.App.ViewModels.PolarTracking;
using System.IO;
using OpenCad2D.Persistence.Dto;
using OpenCad2D.Persistence;
using OpenCad2D.Export.Svg;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Export.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCad2D.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const string DefaultTemplateRelativePath = "Templates/default.opencad2d.json";

    private Point2D _mousePosition = Point2D.Origin;
    private string _lastMessage = "Ready.";
    private SnapCandidate? _currentSnapCandidate;
    private readonly CommandInputParser _commandInputParser = new();
    private readonly CommandAliasRegistry _commandAliasRegistry = CommandAliasRegistry.CreateDefault();
    private readonly List<string> _commandLineHistory = new();
    private readonly List<string> _visibleCommandHistory = new();
    private const int MaxVisibleCommandHistoryEntries = 8;
    private ToolId? _lastCommandToolId;
    private string? _lastCommandInput;
    private readonly SelectionPropertyPanelBuilder _propertyPanelBuilder = new();
    private readonly IDocumentSerializer _documentSerializer = new JsonDocumentSerializer();
    private readonly ISvgExporter _svgExporter = new SvgExporter();
    private readonly IDxfExporter _dxfExporter = new DxfExporter();
    private readonly IDxfImporter _dxfImporter = new DxfDocumentImporter();
    private readonly IPdfExporter _pdfExporter = new PdfExporter();
    private string? _currentFilePath;
    private PropertyPanelViewModel _propertyPanel = new("Properties", Array.Empty<PropertySectionViewModel>());
    private bool _isPropertyPanelVisible = true;
    private PolarTrackingOptionViewModel _selectedPolarTrackingOption;

    public MainWindowViewModel(ITextInputProvider? textInputProvider = null)
    {
        PolarTrackingOptions = CreatePolarTrackingOptions();
        _selectedPolarTrackingOption = PolarTrackingOptions[0];

        Workspace = new CadWorkspace(
            toolRegistry: new ToolRegistry(textInputProvider),
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

        LoadDefaultTemplate();
        RefreshPropertyPanel();
    }

    public CadWorkspace Workspace { get; }

    public string? CurrentFilePath => _currentFilePath;

    public IReadOnlyList<string> CommandLineHistory => _commandLineHistory;

    public IReadOnlyList<string> VisibleCommandHistory => _visibleCommandHistory;

    public bool CanRepeatLastCommand => _lastCommandToolId is not null;

    public string LastCommandText => _lastCommandInput ?? string.Empty;

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

    public IReadOnlyList<PolarTrackingOptionViewModel> PolarTrackingOptions { get; }

    public PolarTrackingOptionViewModel SelectedPolarTrackingOption
    {
        get => _selectedPolarTrackingOption;
        set
        {
            if (ReferenceEquals(_selectedPolarTrackingOption, value) || value is null)
            {
                return;
            }

            SetPolarTracking(value);
        }
    }

    public string PolarTrackingText => _selectedPolarTrackingOption.IsOff
        ? "Polar: Off"
        : $"Polar: {_selectedPolarTrackingOption.StepDegrees:0.###}°";

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

            targetPoint = ToolInputConstraintService.ApplyAngleConstraint(
                Workspace.Context,
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
            if (Workspace.ToolController.ActiveTool is ICommandDrivenTool commandDrivenTool)
            {
                return commandDrivenTool.GetPromptState(Workspace.Context).FormatPrompt();
            }

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

            if (Workspace.ToolController.ActiveTool is ArcTool arcTool)
            {
                return arcTool.State switch
                {
                    ArcToolState.WaitingForCenterPoint => "Arc: specify center point or type coordinates:",
                    ArcToolState.WaitingForStartPoint => "Arc: specify start point/radius or type coordinates:",
                    ArcToolState.WaitingForEndPoint => "Arc: specify end point/direction or type coordinates:",
                    _ => "Arc: specify point:"
                };
            }

            if (Workspace.ToolController.ActiveTool is ArcThreePointsTool arcThreePointsTool)
            {
                return arcThreePointsTool.State switch
                {
                    ArcThreePointsToolState.WaitingForStartPoint => "Arc 3P: specify start point or type coordinates:",
                    ArcThreePointsToolState.WaitingForPointOnArc => "Arc 3P: specify point on arc or type coordinates:",
                    ArcThreePointsToolState.WaitingForEndPoint => "Arc 3P: specify end point or type coordinates:",
                    _ => "Arc 3P: specify point:"
                };
            }

            if (Workspace.ToolController.ActiveTool is ZoomWindowTool zoomWindowTool)
            {
                return zoomWindowTool.FirstPoint is null
                    ? "Zoom Window: specify first corner:"
                    : "Zoom Window: specify opposite corner:";
            }

            if (Workspace.ToolController.ActiveTool is PointTool)
            {
                return "Point: specify point or type coordinates:";
            }

            if (Workspace.ToolController.ActiveTool is TextTool)
            {
                return "Text: specify insertion point or type coordinates:";
            }

            if (Workspace.ToolController.ActiveTool is MeasureDistanceTool measureDistanceTool)
            {
                return measureDistanceTool.State == TwoPointToolState.WaitingForFirstPoint
                    ? "Measure distance: specify first point or type coordinates:"
                    : "Measure distance: specify second point, type coordinates, relative coordinates, or distance:";
            }

            if (Workspace.ToolController.ActiveTool is MeasureEntityTool)
            {
                return "Measure entity: click an entity. Use Ctrl+click to cycle overlapping entities:";
            }

            if (Workspace.ToolController.ActiveTool is MeasureAreaTool)
            {
                return "Measure area: click a closed polyline. Use Ctrl+click to cycle overlapping entities:";
            }

            if (Workspace.ToolController.ActiveTool is MeasureAngleTool measureAngleTool)
            {
                return measureAngleTool.State switch
                {
                    MeasureAngleToolState.WaitingForFirstRayPoint => "Measure angle: specify first point or type coordinates:",
                    MeasureAngleToolState.WaitingForVertex => "Measure angle: specify vertex point, type coordinates, relative coordinates, or distance:",
                    MeasureAngleToolState.WaitingForSecondRayPoint => "Measure angle: specify second point, type coordinates, relative coordinates, or distance:",
                    _ => "Measure angle: specify point:"
                };
            }

            return Workspace.Context.CurrentBasePoint is null
                ? "Specify first point or type coordinates:"
                : "Specify second point, type coordinates, relative coordinates, or distance:";
        }
    }

    public string CommandInputPlaceholderText
    {
        get
        {
            if (Workspace.ToolController.ActiveTool is ICommandDrivenTool commandDrivenTool)
            {
                return commandDrivenTool.GetPromptState(Workspace.Context).Placeholder ?? "Command input";
            }

            return Workspace.Context.CurrentBasePoint is null
                ? "100,50   |   @50,0   |   @100<45"
                : "100,50   |   @50,0   |   @100<45   |   distance";
        }
    }

    public string StatusText =>
        $"Tool: {ActiveToolName} | " +
        $"{CurrentLayerText} | " +
        $"Entities: {EntityCount} | " +
        $"Selected: {SelectedCount} | " +
        $"{PolarTrackingText} | " +
        $"{MousePositionText} | " +
        $"{MeasurementText} | " +
        $"{SnapText} | " +
        $"{LastMessage}";



    public void NewDocument()
    {
        LoadDefaultTemplate();
        _currentFilePath = null;
        SetMessage("New document created from default template.");
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

        DocumentRecoveryResult recovery = _documentSerializer.DeserializeWithRecovery(dto);

        Workspace.LoadDocument(
            recovery.Document,
            new LayerId(recovery.CurrentLayerId));

        _currentFilePath = filePath;

        if (recovery.HasIssues)
        {
            SetMessage($"Opened '{CurrentFileName}' with recovery: {recovery.RecoveredEntityCount} recovered, {recovery.SkippedEntityCount} skipped.");
        }
        else
        {
            SetMessage($"Opened '{CurrentFileName}'.");
        }
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();

        return recovery.Viewport;
    }

    public SvgExportResult ExportSvgToFile(string filePath)
    {
        var options = new SvgExportOptions
        {
            Title = CurrentFileName == "Untitled"
                ? "OpenCad2D export"
                : CurrentFileName
        };

        return ExportSvgToFile(
            filePath,
            options);
    }

    public SvgExportResult ExportSvgToFile(
        string filePath,
        SvgExportOptions options)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "SVG export file path cannot be empty.",
                nameof(filePath));
        }

        ArgumentNullException.ThrowIfNull(options);

        SvgExportResult result = _svgExporter.Export(
            Workspace.Document,
            options);

        _svgExporter.ExportToFile(
            Workspace.Document,
            filePath,
            options);

        SetMessage($"Exported SVG '{Path.GetFileName(filePath)}' ({result.ExportedEntityCount} entities). Use Save to save the native drawing.");
        OnPropertiesChanged(
            nameof(LastMessage),
            nameof(StatusText));

        return result;
    }


    public DxfImportResult ImportDxfFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "DXF import file path cannot be empty.",
                nameof(filePath));
        }

        DxfImportResult result = _dxfImporter.ImportFile(filePath);

        if (result.HasErrors)
        {
            SetMessage($"DXF import failed: {Path.GetFileName(filePath)}.");
            OnPropertiesChanged(
                nameof(LastMessage),
                nameof(StatusText));

            return result;
        }

        Workspace.LoadDocument(
            result.Document,
            LayerId.Default,
            markAsSaved: false);
        Workspace.MarkDocumentChanged();
        Workspace.EnsureCurrentLayerIsUsable();

        _currentFilePath = null;

        SetMessage(
            $"Imported DXF '{Path.GetFileName(filePath)}' " +
            $"({result.Statistics.TotalImportedEntities} entities, " +
            $"{result.Statistics.ImportedLayerCount} layers, " +
            $"{result.Statistics.WarningCount} warnings).");

        NotifyDocumentStateChanged();
        NotifyFileStateChanged();

        return result;
    }

    public DxfExportResult ExportDxfToFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "DXF export file path cannot be empty.",
                nameof(filePath));
        }

        DxfExportResult result = _dxfExporter.Export(
            Workspace.Document,
            DxfExportOptions.Default);

        _dxfExporter.ExportToFile(
            Workspace.Document,
            filePath,
            DxfExportOptions.Default);

        SetMessage($"Exported DXF '{Path.GetFileName(filePath)}' ({result.ExportedEntityCount} entities). Use Save to save the native drawing.");
        OnPropertiesChanged(
            nameof(LastMessage),
            nameof(StatusText));

        return result;
    }

    public PdfExportResult ExportPdfToFile(string filePath)
    {
        return ExportPdfToFile(
            filePath,
            PdfExportOptions.Default);
    }

    public PdfExportResult ExportPdfToFile(
        string filePath,
        PdfExportOptions options)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "PDF export file path cannot be empty.",
                nameof(filePath));
        }

        ArgumentNullException.ThrowIfNull(options);

        PdfExportResult result = _pdfExporter.Export(
            Workspace.Document,
            options);

        _pdfExporter.ExportToFile(
            Workspace.Document,
            filePath,
            options);

        SetMessage(
            $"Exported PDF '{Path.GetFileName(filePath)}' " +
            $"({result.ExportedEntityCount} entities, " +
            $"{options.PageSize} {options.Orientation}, " +
            $"margin {options.MarginMillimeters:0.##} mm). Use Save to save the native drawing.");
        OnPropertiesChanged(
            nameof(LastMessage),
            nameof(StatusText));

        return result;
    }

    public ToolResult SubmitCommandInput(string? input)
    {
        string normalizedInput = input?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            AppendVisibleCommandHistoryLine("> Enter");
            ToolResult repeatResult = RepeatLastCommand();
            AppendToolResultToVisibleHistory(repeatResult);
            return repeatResult;
        }

        AppendVisibleCommandHistoryLine($"> {normalizedInput}");

        if (Workspace.Context.CurrentBasePoint is not null &&
            TrySubmitCommandDrivenInput(normalizedInput, out ToolResult activeCommandResult))
        {
            return activeCommandResult;
        }

        if (TryExecuteActionCommand(normalizedInput, out ToolResult actionResult))
        {
            _commandLineHistory.Add(normalizedInput);
            AppendToolResultToVisibleHistory(actionResult);
            NotifyCommandInputStateChanged();
            return actionResult;
        }

        if (_commandAliasRegistry.TryResolve(normalizedInput, out ToolId toolId))
        {
            _commandLineHistory.Add(normalizedInput);

            ToolResult result = SetTool(
                toolId,
                rememberAsLastCommand: true,
                commandInput: normalizedInput);

            NotifyCommandInputStateChanged();
            return result;
        }

        if (TrySubmitCommandDrivenInput(normalizedInput, out ToolResult commandDrivenResult))
        {
            return commandDrivenResult;
        }

        CommandInputParseResult parseResult = _commandInputParser.Parse(normalizedInput);

        if (!parseResult.IsValid)
        {
            string message = IsLikelyCommandAlias(normalizedInput)
                ? $"Unknown command or alias '{normalizedInput}'."
                : parseResult.ErrorMessage ?? "Invalid command input.";

            ToolResult invalidResult = ToolResult.None(message);
            SetLastResult(invalidResult);
            AppendToolResultToVisibleHistory(invalidResult);
            NotifyCommandInputStateChanged();
            return invalidResult;
        }

        if (!TryResolveCommandInputPoint(parseResult, out Point2D worldPoint, out string? errorMessage))
        {
            ToolResult invalidResult = ToolResult.None(errorMessage);
            SetLastResult(invalidResult);
            AppendToolResultToVisibleHistory(invalidResult);
            NotifyCommandInputStateChanged();
            return invalidResult;
        }

        ToolResult pointResult = Workspace.SubmitPointFromCommandLine(worldPoint);

        SetLastResult(pointResult);
        AppendToolResultToVisibleHistory(pointResult);
        NotifyDocumentStateChanged();
        NotifyCommandInputStateChanged();

        return pointResult;
    }

    private bool TrySubmitCommandDrivenInput(
        string normalizedInput,
        out ToolResult result)
    {
        result = ToolResult.None();

        if (Workspace.ToolController.ActiveTool is not ICommandDrivenTool commandDrivenTool)
        {
            return false;
        }

        CommandPromptState promptState = commandDrivenTool.GetPromptState(Workspace.Context);
        Point2D? referenceUserPoint = Workspace.Context.CurrentBasePoint is null
            ? null
            : Workspace.CurrentUcs.WorldToUser(Workspace.Context.CurrentBasePoint.Value);

        CommandInputSubmission submission = _commandInputParser.Parse(
            normalizedInput,
            promptState,
            referenceUserPoint);

        if (!submission.IsValid)
        {
            result = ToolResult.None(submission.ErrorMessage ?? "Invalid command input.");
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyCommandInputStateChanged();
            return true;
        }

        CommandInputSubmission toolSubmission = submission;

        if (submission.Kind == CommandInputSubmissionKind.Point && submission.Point is not null)
        {
            Point2D worldPoint = Workspace.CurrentUcs.UserToWorld(submission.Point.Value);
            toolSubmission = CommandInputSubmission.FromPoint(
                submission.RawText,
                worldPoint,
                offset: submission.Offset is null
                    ? null
                    : Workspace.CurrentUcs.UserVectorToWorld(submission.Offset.Value),
                distance: submission.Distance,
                angleDegrees: submission.AngleDegrees);
        }
        else if (submission.Kind == CommandInputSubmissionKind.Distance && submission.Distance is not null)
        {
            if (!TryResolveDirectDistancePoint(
                    submission.Distance.Value,
                    out Point2D directDistancePoint,
                    out string? errorMessage))
            {
                result = ToolResult.None(errorMessage);
                SetLastResult(result);
                AppendToolResultToVisibleHistory(result);
                NotifyCommandInputStateChanged();
                return true;
            }

            toolSubmission = CommandInputSubmission.FromPoint(
                submission.RawText,
                directDistancePoint,
                distance: submission.Distance);
        }

        result = commandDrivenTool.HandleCommandInput(
            toolSubmission,
            Workspace.Context);

        SetLastResult(result);
        AppendToolResultToVisibleHistory(result);
        NotifyDocumentStateChanged();
        NotifyCommandInputStateChanged();

        return true;
    }

    private static bool IsLikelyCommandAlias(string input)
    {
        return input.All(character =>
            char.IsLetter(character) ||
            char.IsDigit(character) ||
            character == '_');
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
            case CommandInputParseKind.AbsolutePoint:
                worldPoint = Workspace.CurrentUcs.UserToWorld(parseResult.Point!.Value);
                return true;

            case CommandInputParseKind.RelativePoint:
                if (Workspace.Context.CurrentBasePoint is null)
                {
                    errorMessage = "Relative coordinates require a base point.";
                    return false;
                }

                Vector2D worldOffset = Workspace.CurrentUcs.UserVectorToWorld(parseResult.Offset!.Value);
                worldPoint = Workspace.Context.CurrentBasePoint.Value + worldOffset;
                return true;

            case CommandInputParseKind.Distance:
                return TryResolveDirectDistancePoint(
                    parseResult.Distance!.Value,
                    out worldPoint,
                    out errorMessage);

            case CommandInputParseKind.DistanceAngle:
                return TryResolveDistanceAnglePoint(
                    parseResult.Distance!.Value,
                    parseResult.AngleDegrees!.Value,
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

        directionPoint = ToolInputConstraintService.ApplyAngleConstraint(
            Workspace.Context,
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


    private bool TryResolveDistanceAnglePoint(
        double distance,
        double angleDegrees,
        out Point2D worldPoint,
        out string? errorMessage)
    {
        worldPoint = Point2D.Origin;
        errorMessage = null;

        if (Workspace.Context.CurrentBasePoint is null)
        {
            errorMessage = "Distance-angle input requires a base point.";
            return false;
        }

        double radians = angleDegrees * Math.PI / 180.0;
        var userOffset = new Vector2D(
            distance * Math.Cos(radians),
            distance * Math.Sin(radians));

        Vector2D worldOffset = Workspace.CurrentUcs.UserVectorToWorld(userOffset);
        worldPoint = Workspace.Context.CurrentBasePoint.Value + worldOffset;
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
        _propertyPanel = _propertyPanelBuilder.Build(
            Workspace,
            SetMessage,
            RefreshAfterPropertyEdit);

        OnPropertiesChanged(nameof(PropertyPanel));
    }


    private void RefreshAfterPropertyEdit()
    {
        RefreshPropertyPanel();

        OnPropertiesChanged(
            nameof(EntityCount),
            nameof(SelectedCount),
            nameof(IsDirty),
            nameof(TitleText),
            nameof(FileStatusText),
            nameof(LastMessage),
            nameof(StatusText));
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


    public ToolResult AssignSelectedEntitiesToCurrentLayer()
    {
        ToolResult result = Workspace.AssignSelectedEntitiesToCurrentLayer();

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
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

    public ToolResult SetCurrentLayerVisibility(bool isVisible)
    {
        ToolResult result = Workspace.SetCurrentLayerVisibility(isVisible);

        SetLastResult(result);
        NotifyLayerStateChanged();
        NotifyDocumentStateChanged();

        return result;
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

    public ToolResult SelectAll()
    {
        ToolResult result = Workspace.ActionController.SelectAll();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifySelectionStateChanged();

        return result;
    }

    public ToolResult SelectLast()
    {
        ToolResult result = Workspace.ActionController.SelectLast();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifySelectionStateChanged();

        return result;
    }

    private bool TryExecuteActionCommand(
        string input,
        out ToolResult result)
    {
        result = ToolResult.None();

        string normalized = input.Trim().ToUpperInvariant();

        switch (normalized)
        {
            case "SELECTALL":
            case "SA":
            case "ALL":
                result = SelectAll();
                return true;

            case "SELECTLAST":
            case "SL":
            case "LAST":
                result = SelectLast();
                return true;

            default:
                return false;
        }
    }

    public ToolResult SetTool(ToolId toolId)
    {
        return SetTool(
            toolId,
            rememberAsLastCommand: true,
            commandInput: null);
    }

    public ToolResult RepeatLastCommand()
    {
        return RepeatLastCommandCore(requireIdleWorkspace: false);
    }

    public ToolResult RepeatLastCommandFromCanvas()
    {
        return RepeatLastCommandCore(requireIdleWorkspace: true);
    }

    private ToolResult RepeatLastCommandCore(bool requireIdleWorkspace)
    {
        if (_lastCommandToolId is null)
        {
            ToolResult result = ToolResult.None("No command to repeat.");
            SetLastResult(result);
            NotifyCommandInputStateChanged();
            return result;
        }

        if (requireIdleWorkspace && Workspace.Context.CurrentBasePoint is not null)
        {
            ToolResult result = ToolResult.None("Finish or cancel the current command before repeating the last command.");
            SetLastResult(result);
            NotifyCommandInputStateChanged();
            return result;
        }

        ToolResult repeated = SetTool(
            _lastCommandToolId.Value,
            rememberAsLastCommand: false,
            commandInput: null);

        SetMessage($"Repeated command: {Workspace.ToolController.ActiveToolName}.");
        AppendVisibleCommandHistoryLine(LastMessage);
        NotifyCommandInputStateChanged();

        return repeated;
    }

    private ToolResult SetTool(
        ToolId toolId,
        bool rememberAsLastCommand,
        string? commandInput)
    {
        ToolResult result = Workspace.SetActiveTool(toolId);

        if (rememberAsLastCommand)
        {
            _lastCommandToolId = toolId;
            _lastCommandInput = string.IsNullOrWhiteSpace(commandInput)
                ? Workspace.ToolController.ActiveToolName
                : commandInput.Trim();
        }

        SetLastResult(result);
        SetMessage($"Tool changed to {Workspace.ToolController.ActiveToolName}.");
        AppendVisibleCommandHistoryLine($"Command: {Workspace.ToolController.ActiveToolName}");
        AppendVisibleCommandHistoryLine(CommandPromptText);

        OnPropertiesChanged(
            nameof(ActiveToolName),
            nameof(CommandPromptText),
            nameof(CommandInputPlaceholderText),
            nameof(VisibleCommandHistory),
            nameof(CanRepeatLastCommand),
            nameof(LastCommandText),
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



    public ToolResult ApplyLineFormatChanges(IEnumerable<LineFormat> lineFormats)
    {
        ToolResult result = Workspace.ApplyLineFormatChanges(lineFormats);

        SetLastResult(result);
        NotifyLayerStateChanged();
        NotifyDocumentStateChanged();

        return result;
    }


    public ToolResult ApplyTextFormatChanges(IEnumerable<TextFormat> textFormats)
    {
        ToolResult result = Workspace.ApplyTextFormatChanges(textFormats);

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }


    public ToolResult ApplyGridSettings(GridSettings gridSettings)
    {
        ToolResult result = Workspace.SetGridSettings(gridSettings);

        SetLastResult(result);
        OnPropertiesChanged(
            nameof(StatusText),
            nameof(SnapText));

        return result;
    }

    public void SetOrthoEnabled(bool isEnabled)
    {
        Workspace.Context.IsOrthoEnabled = isEnabled;

        if (isEnabled)
        {
            Workspace.AngleConstraintSettings = AngleConstraintSettings.Off;
            _selectedPolarTrackingOption = PolarTrackingOptions[0];
        }

        SetMessage(isEnabled
            ? "Ortho mode enabled."
            : "Ortho mode disabled.");

        OnPropertiesChanged(
            nameof(IsOrthoEnabled),
            nameof(SelectedPolarTrackingOption),
            nameof(PolarTrackingText),
            nameof(MeasurementText),
            nameof(StatusText));
    }

    public void SetPolarTracking(PolarTrackingOptionViewModel option)
    {
        ArgumentNullException.ThrowIfNull(option);

        _selectedPolarTrackingOption = option;
        Workspace.Context.IsOrthoEnabled = false;
        Workspace.AngleConstraintSettings = option.Settings;

        SetMessage(option.IsOff
            ? "Polar tracking disabled."
            : $"Polar tracking set to {option.StepDegrees:0.###}°.");

        OnPropertiesChanged(
            nameof(IsOrthoEnabled),
            nameof(SelectedPolarTrackingOption),
            nameof(PolarTrackingText),
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
            nameof(SelectedPolarTrackingOption),
            nameof(PolarTrackingText),
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

    private void AppendToolResultToVisibleHistory(ToolResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            AppendVisibleCommandHistoryLine(result.Message!);
        }
    }

    private void AppendVisibleCommandHistoryLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _visibleCommandHistory.Add(line.Trim());

        while (_visibleCommandHistory.Count > MaxVisibleCommandHistoryEntries)
        {
            _visibleCommandHistory.RemoveAt(0);
        }
    }

    private void NotifySelectionStateChanged()
    {
        OnPropertiesChanged(
            nameof(SelectedCount),
            nameof(PropertyPanel),
            nameof(LastMessage),
            nameof(StatusText));
    }

    private void NotifyCommandInputStateChanged()
    {
        OnPropertiesChanged(
            nameof(CommandPromptText),
            nameof(CommandInputPlaceholderText),
            nameof(MeasurementText),
            nameof(StatusText),
            nameof(LastMessage),
            nameof(CommandLineHistory),
            nameof(VisibleCommandHistory),
            nameof(CanRepeatLastCommand),
            nameof(LastCommandText));
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
            nameof(SelectedPolarTrackingOption),
            nameof(PolarTrackingText),
            nameof(CurrentLayerText),
            nameof(CommandPromptText),
            nameof(CommandInputPlaceholderText),
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

    private static IReadOnlyList<PolarTrackingOptionViewModel> CreatePolarTrackingOptions()
    {
        return new[]
        {
            new PolarTrackingOptionViewModel("Off", AngleConstraintSettings.Off),
            new PolarTrackingOptionViewModel("90°", AngleConstraintSettings.FromStep(90)),
            new PolarTrackingOptionViewModel("45°", AngleConstraintSettings.FromStep(45)),
            new PolarTrackingOptionViewModel("30°", AngleConstraintSettings.FromStep(30)),
            new PolarTrackingOptionViewModel("15°", AngleConstraintSettings.FromStep(15))
        };
    }

    private void LoadDefaultTemplate()
    {
        string templatePath = GetDefaultTemplatePath();

        try
        {
            DocumentDto dto = _documentSerializer.LoadFromFile(templatePath);

            CadDocument document = _documentSerializer.Deserialize(
                dto,
                out string currentLayerId,
                out _);

            Workspace.LoadDocument(
                document,
                new LayerId(currentLayerId));

            _currentFilePath = null;
            SetMessage("Default template loaded.");
        }
        catch (DocumentLoadException)
        {
            LoadInternalDefaultDocument();
        }
        catch (InvalidOperationException)
        {
            LoadInternalDefaultDocument();
        }
    }

    private static string GetDefaultTemplatePath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            DefaultTemplateRelativePath);
    }

    private void LoadInternalDefaultDocument()
    {
        Workspace.NewDocument();
        EnsureDefaultCadLayers();
        Workspace.MarkSaved();
        _currentFilePath = null;
        SetMessage("Default template unavailable; using internal defaults.");
    }

    private void EnsureDefaultCadLayers()
    {
        AddLayerIfMissing(Layer.Default);
        AddLayerIfMissing(Layer.Annotations);
        AddLayerIfMissing(Layer.Walls);
        AddLayerIfMissing(Layer.Axis);
        AddLayerIfMissing(Layer.ConstructionLines);
    }

    private void AddLayerIfMissing(Layer layer)
    {
        if (Workspace.Document.Layers.Contains(layer.Id))
        {
            return;
        }

        Workspace.Document.Layers.Add(layer);
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