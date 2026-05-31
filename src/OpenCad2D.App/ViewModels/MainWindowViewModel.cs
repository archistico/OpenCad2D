using Avalonia;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Dimensions;
using OpenCad2D.Tools.Input;
using OpenCad2D.Tools.Measurements;
using OpenCad2D.Tools.Navigation;
using OpenCad2D.App.ViewModels.Properties;
using OpenCad2D.App.ViewModels.PolarTracking;
using OpenCad2D.App.ViewModels.ImportDrawing;
using OpenCad2D.App.ViewModels.Blocks;
using OpenCad2D.App.ViewModels.Library;
using OpenCad2D.App.Settings;
using System.IO;
using OpenCad2D.Persistence.Dto;
using OpenCad2D.Persistence;
using OpenCad2D.Export.Svg;
using OpenCad2D.Export.Dxf;
using OpenCad2D.Export.Dxf.Import;
using OpenCad2D.Export.Pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OpenCad2D.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const string DefaultTemplateRelativePath = "Templates/default.opencad2d.json";

    private PendingOpenCad2DImport? _pendingOpenCad2DImport;
    private CreateBlockOptions? _pendingCreateBlockBasePointPick;
    private InsertBlockOptions? _pendingBlockInsertion;
    private PendingLibraryBlockInsertion? _pendingLibraryBlockInsertion;
    private BlockEditSession? _activeBlockEditSession;

    private Point2D _mousePosition = Point2D.Origin;
    private Point? _hudScreenPosition;
    private string _lastMessage = "Ready.";
    private SnapCandidate? _currentSnapCandidate;
    private readonly CommandHudInputState _commandHudInputState = new();
    private readonly CommandInputParser _commandInputParser = new();
    private readonly CommandAliasRegistry _commandAliasRegistry = CommandAliasRegistry.CreateDefault();
    private readonly List<string> _commandLineHistory = new();
    private readonly List<string> _visibleCommandHistory = new();
    private const int MaxVisibleCommandHistoryEntries = 8;
    private ToolId? _lastCommandToolId;
    private string? _lastCommandInput;
    private int? _commandHistoryNavigationIndex;
    private static readonly string[] ActionCommandNames =
    {
        "SELECTALL",
        "SA",
        "ALL",
        "SELECTLAST",
        "SL",
        "LAST",
        "DESELECT",
        "CLEARSELECTION",
        "CS",
        "BRINGTOFRONT",
        "BTF",
        "FRONT",
        "SENDTOBACK",
        "STB",
        "BACK",
        "BRINGFORWARD",
        "BF",
        "FORWARD",
        "SENDBACKWARD",
        "SB",
        "BACKWARD",
        "ALIGNLEFT",
        "ALEFT",
        "ALIGNRIGHT",
        "ARIGHT",
        "ALIGNTOP",
        "ATOP",
        "ALIGNBOTTOM",
        "ABOTTOM",
        "DISTRIBUTEHORIZONTAL",
        "DISTRIBUTEHORIZONTALLY",
        "DH",
        "DISTRIBUTEVERTICAL",
        "DISTRIBUTEVERTICALLY",
        "DV"
    };
    private readonly SelectionPropertyPanelBuilder _propertyPanelBuilder = new();
    private readonly IDocumentSerializer _documentSerializer = new JsonDocumentSerializer();
    private readonly ISvgExporter _svgExporter = new SvgExporter();
    private readonly IDxfExporter _dxfExporter = new DxfExporter();
    private readonly IDxfImporter _dxfImporter = new DxfDocumentImporter();
    private readonly IPdfExporter _pdfExporter = new PdfExporter();
    private readonly IApplicationSettingsStore _applicationSettingsStore;
    private ApplicationSettings _applicationSettings;
    private string? _currentFilePath;
    private PropertyPanelViewModel _propertyPanel = new("Properties", Array.Empty<PropertySectionViewModel>());
    private bool _isPropertyPanelVisible = true;
    private PolarTrackingOptionViewModel _selectedPolarTrackingOption;

    public event EventHandler? CommandHudInputOverridesCleared;

    public MainWindowViewModel(
        ITextInputProvider? textInputProvider = null,
        IApplicationSettingsStore? applicationSettingsStore = null)
    {
        _applicationSettingsStore = applicationSettingsStore ?? JsonApplicationSettingsStore.CreateDefault();
        _applicationSettings = _applicationSettingsStore.Load();

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

    public ApplicationSettings ApplicationSettings => _applicationSettings;

    public IReadOnlyList<string> RecentFiles => _applicationSettings.RecentFiles;

    public IReadOnlyList<string> CommandLineHistory => _commandLineHistory;

    public IReadOnlyList<string> VisibleCommandHistory => _visibleCommandHistory;

    public bool CanRepeatLastCommand => _lastCommandToolId is not null;

    public string LastCommandText => _lastCommandInput ?? string.Empty;

    public string NavigateCommandHistoryPrevious()
    {
        if (_commandLineHistory.Count == 0)
        {
            _commandHistoryNavigationIndex = null;
            return string.Empty;
        }

        _commandHistoryNavigationIndex = _commandHistoryNavigationIndex is null
            ? _commandLineHistory.Count - 1
            : Math.Max(0, _commandHistoryNavigationIndex.Value - 1);

        return _commandLineHistory[_commandHistoryNavigationIndex.Value];
    }

    public string NavigateCommandHistoryNext()
    {
        if (_commandLineHistory.Count == 0 || _commandHistoryNavigationIndex is null)
        {
            _commandHistoryNavigationIndex = null;
            return string.Empty;
        }

        if (_commandHistoryNavigationIndex.Value >= _commandLineHistory.Count - 1)
        {
            _commandHistoryNavigationIndex = null;
            return string.Empty;
        }

        _commandHistoryNavigationIndex++;
        return _commandLineHistory[_commandHistoryNavigationIndex.Value];
    }

    public void ResetCommandHistoryNavigation()
    {
        _commandHistoryNavigationIndex = null;
    }

    public string? GetCommandAutocompleteSuggestion(string? input)
    {
        string normalized = input?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized) ||
            !IsLikelyCommandAlias(normalized))
        {
            return null;
        }

        if (normalized.Length <= 2 &&
            _commandAliasRegistry.Aliases.TryGetValue(normalized, out ToolId exactAliasToolId))
        {
            string? preferredAlias = GetPreferredAutocompleteAlias(exactAliasToolId);

            if (preferredAlias is not null &&
                preferredAlias.StartsWith(normalized, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(preferredAlias, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return preferredAlias;
            }
        }

        string? suggestion = GetCommandAutocompleteCandidates()
            .Where(candidate =>
                candidate.StartsWith(
                    normalized,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    candidate,
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => GetAutocompleteCandidateRank(normalized, candidate))
            .ThenBy(candidate => candidate.Length)
            .ThenBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return suggestion;
    }

    private string? GetPreferredAutocompleteAlias(ToolId toolId)
    {
        return _commandAliasRegistry.Aliases
            .Where(pair => pair.Value == toolId && pair.Key.Length >= 3)
            .OrderBy(pair => pair.Key.Length)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key)
            .FirstOrDefault();
    }

    private IEnumerable<string> GetCommandAutocompleteCandidates()
    {
        return _commandAliasRegistry.Aliases.Keys
            .Where(alias => alias.Length >= 3)
            .Concat(ActionCommandNames)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static int GetAutocompleteCandidateRank(
        string normalizedInput,
        string candidate)
    {
        if (candidate.Length <= 2)
        {
            return 2;
        }

        if (candidate.Length <= 4)
        {
            return 0;
        }

        return normalizedInput.Length <= 2 ? 1 : 0;
    }

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

    private string? CommandHudToolName =>
        IsCreateBlockBasePointPickPending
            ? "Create Block"
            : IsBlockInsertionPending
                ? "Insert Block"
                : IsLibraryInsertionPending
                    ? "Library"
                    : IsImportDrawingPlacementPending
                        ? "Import Drawing"
                        : IsCommandHudVisible
                            ? ActiveToolName
                            : null;

    public Point? HudScreenPosition => _hudScreenPosition;

    public bool HasLiveMeasurements =>
        Workspace.Context.CurrentBasePoint is not null;

    public bool IsCommandHudVisible =>
        Workspace.ToolController.ActiveTool is ICommandDrivenTool ||
        IsCreateBlockBasePointPickPending ||
        IsBlockInsertionPending ||
        IsLibraryInsertionPending ||
        IsImportDrawingPlacementPending;

    public bool IsBottomCommandLineVisible => false;

    public double? LiveDistance => GetLiveMeasurement().Distance;

    public double? LiveAngle => GetLiveMeasurement().AngleDegrees;

    public double? LiveDeltaX => GetLiveMeasurement().DeltaX;

    public double? LiveDeltaY => GetLiveMeasurement().DeltaY;

    public CommandPromptState CurrentPromptState => GetCurrentPromptState();

    public CommandHudStateViewModel CommandHudState => new(
        IsCommandHudVisible,
        CommandHudToolName,
        GetCurrentPromptState(),
        BuildCommandHudFields());

    public int EntityCount =>
        Workspace.Document.Entities.Count;

    public int SelectedCount =>
        Workspace.SelectionSet.Count;

    public bool IsPropertyPanelVisible =>
        _isPropertyPanelVisible;

    public PropertyPanelViewModel PropertyPanel =>
        _propertyPanel;

    public bool HasSingleSelectedImageReference =>
        GetSingleSelectedImageReference() is not null;

    public int MissingImageReferenceCount =>
        GetMissingImageReferences().Count;

    public bool HasMissingImageReferences =>
        MissingImageReferenceCount > 0;

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


    public CommandPromptState GetCurrentPromptState()
    {
        if (IsCreateBlockBasePointPickPending)
        {
            return new CommandPromptState(
                "CREATEBLOCK",
                "Specify base point",
                CommandInputKind.Point,
                placeholder: "click base point or enter X/Y");
        }

        if (IsBlockInsertionPending)
        {
            return new CommandPromptState(
                "INSERTBLOCK",
                "Specify insertion point",
                CommandInputKind.Point,
                placeholder: "click insertion point or enter X/Y");
        }

        if (IsLibraryInsertionPending)
        {
            return new CommandPromptState(
                "LIBRARY",
                "Specify insertion point",
                CommandInputKind.Point,
                placeholder: "click insertion point or enter X/Y");
        }

        if (IsImportDrawingPlacementPending)
        {
            return new CommandPromptState(
                "IMPORTDRAWING",
                "Specify insertion point",
                CommandInputKind.Point,
                placeholder: "click insertion point or enter X/Y");
        }

        if (Workspace.ToolController.ActiveTool is ICommandDrivenTool commandDrivenTool)
        {
            return commandDrivenTool.GetPromptState(Workspace.Context);
        }

        return CommandPromptState.Idle;
    }

    private CommandLiveMeasurement GetLiveMeasurement()
    {
        if (Workspace.Context.CurrentBasePoint is null)
        {
            return CommandLiveMeasurement.Empty;
        }

        Point2D basePoint = Workspace.Context.CurrentBasePoint.Value;
        Point2D targetPoint = _currentSnapCandidate?.Point ?? _mousePosition;

        targetPoint = ToolInputConstraintService.ApplyAngleConstraint(
            Workspace.Context,
            basePoint,
            targetPoint);

        if (Workspace.GeometryTolerance.ArePointsEqual(basePoint, targetPoint))
        {
            return new CommandLiveMeasurement(0, 0, 0, 0);
        }

        Point2D baseUserPoint = Workspace.CurrentUcs.WorldToUser(basePoint);
        Point2D targetUserPoint = Workspace.CurrentUcs.WorldToUser(targetPoint);
        Vector2D delta = baseUserPoint.VectorTo(targetUserPoint);

        return new CommandLiveMeasurement(
            delta.Length,
            Math.Atan2(delta.Y, delta.X) * 180.0 / Math.PI,
            delta.X,
            delta.Y);
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildCommandHudFields()
    {
        CommandLiveMeasurement measurement = GetLiveMeasurement();
        ICadTool activeTool = Workspace.ToolController.ActiveTool;

        if (activeTool is AlignTool alignTool &&
            alignTool.State is AlignToolState.WaitingForSourcePoint1 or
                AlignToolState.WaitingForDestinationPoint1 or
                AlignToolState.WaitingForSourcePoint2 or
                AlignToolState.WaitingForDestinationPoint2)
        {
            return BuildCoordinateOverrideFields();
        }

        if (activeTool is OffsetTool { State: OffsetToolState.WaitingForDistance } offsetDistanceTool)
        {
            return BuildSingleDistanceField(
                "distance",
                "Distance",
                _commandHudInputState.Distance ?? offsetDistanceTool.Distance ?? offsetDistanceTool.LastDistance);
        }

        if (activeTool is PolygonTool { State: PolygonToolState.WaitingForSides } polygonSidesTool)
        {
            return BuildSingleNumberField(
                "sides",
                "Sides",
                _commandHudInputState.Sides ?? polygonSidesTool.SideCount);
        }

        if (activeTool is FilletTool { State: FilletToolState.WaitingForRadius } filletTool)
        {
            return BuildSingleDistanceField(
                "radius",
                "Radius",
                _commandHudInputState.Radius ?? filletTool.Radius);
        }

        if (activeTool is ChamferTool { State: ChamferToolState.WaitingForDistance } chamferTool)
        {
            return BuildSingleDistanceField(
                "distance",
                "Distance",
                _commandHudInputState.Distance ?? chamferTool.Distance);
        }

        if (!measurement.HasValue)
        {
            return IsPointExpectedInput(GetCurrentPromptState().ExpectedInput)
                ? BuildCoordinateOverrideFields()
                : Array.Empty<CommandHudFieldViewModel>();
        }

        if (activeTool is LineTool lineTool &&
            lineTool.State == TwoPointToolState.WaitingForSecondPoint)
        {
            return BuildDistanceAngleCoordinateOverrideFields(measurement);
        }

        if (activeTool is PolylineTool polylineTool &&
            polylineTool.State == PolylineToolState.CollectingVertices)
        {
            return BuildDistanceAngleCoordinateOverrideFields(measurement);
        }

        if (activeTool is MoveTool moveTool &&
            moveTool.MoveState == MoveToolState.WaitingForDestinationPoint)
        {
            return BuildDistanceAngleCoordinateOverrideFields(measurement);
        }

        if (activeTool is CopyTool copyTool &&
            copyTool.CopyState == MoveToolState.WaitingForDestinationPoint)
        {
            return BuildDistanceAngleCoordinateOverrideFields(measurement);
        }

        if (activeTool is MeasureDistanceTool measureDistanceTool &&
            measureDistanceTool.State == TwoPointToolState.WaitingForSecondPoint)
        {
            return BuildDistanceAngleCoordinateOverrideFields(measurement);
        }

        if (activeTool is MirrorTool mirrorTool &&
            mirrorTool.State == MirrorToolState.WaitingForSecondAxisPoint)
        {
            return BuildDistanceAngleCoordinateOverrideFields(measurement);
        }

        if (activeTool is BreakAtPointTool breakAtPointTool &&
            breakAtPointTool.State == BreakAtPointToolState.WaitingForBreakPoint)
        {
            return BuildCoordinateOverrideFields();
        }

        if (activeTool is BreakBetweenPointsTool breakBetweenPointsTool)
        {
            if (breakBetweenPointsTool.State == BreakBetweenPointsToolState.WaitingForFirstBreakPoint)
            {
                return BuildCoordinateOverrideFields();
            }

            if (breakBetweenPointsTool.State == BreakBetweenPointsToolState.WaitingForSecondBreakPoint)
            {
                return BuildDistanceAngleCoordinateOverrideFields(measurement);
            }
        }


        if (activeTool is RectangleTool rectangleTool &&
            rectangleTool.State == TwoPointToolState.WaitingForSecondPoint)
        {
            return BuildWidthHeightFields(measurement);
        }

        if (activeTool is RectangleBySidesTool rectangleBySidesTool)
        {
            return BuildRectangleBySidesFields(
                rectangleBySidesTool,
                measurement);
        }

        if (activeTool is CircleTool circleTool &&
            circleTool.State == TwoPointToolState.WaitingForSecondPoint)
        {
            return BuildSingleDistanceField(
                "radius",
                "Radius",
                _commandHudInputState.Distance ?? measurement.Distance,
                CommandHudFieldKind.Distance);
        }

        if (activeTool is ArcTool arcTool)
        {
            return BuildArcFields(
                arcTool,
                measurement);
        }

        if (activeTool is EllipseTool ellipseTool)
        {
            return BuildEllipseFields(
                ellipseTool,
                measurement);
        }

        if (activeTool is PolygonTool polygonTool &&
            polygonTool.State == PolygonToolState.WaitingForVertex)
        {
            return BuildRadiusAngleFields(measurement);
        }

        if (activeTool is RotateTool rotateTool)
        {
            return BuildRotateFields(
                rotateTool,
                measurement);
        }

        if (activeTool is ScaleTool scaleTool)
        {
            return BuildScaleFields(
                scaleTool,
                measurement);
        }

        if (activeTool is OffsetTool offsetTool &&
            offsetTool.State == OffsetToolState.WaitingForDistanceSecondPoint)
        {
            return BuildSingleDistanceCoordinateOverrideField(
                "distance",
                "Distance",
                _commandHudInputState.Distance ?? measurement.Distance);
        }

        if (activeTool is MeasureAngleTool measureAngleTool &&
            measureAngleTool.State == MeasureAngleToolState.WaitingForSecondRayPoint)
        {
            return BuildSingleAngleField(measurement.AngleDegrees);
        }

        if (activeTool is RadialDimensionToolBase radialDimensionTool &&
            radialDimensionTool.PointOnCircle is null)
        {
            return BuildSingleDistanceField(
                "radius",
                "Radius",
                _commandHudInputState.Distance ?? measurement.Distance,
                CommandHudFieldKind.Distance);
        }

        if (activeTool is AngularDimensionTool angularDimensionTool &&
            angularDimensionTool.FirstRayPoint is not null &&
            angularDimensionTool.SecondRayPoint is null)
        {
            return BuildSingleAngleField(measurement.AngleDegrees);
        }

        return BuildFieldsFromPromptKind(
            GetCurrentPromptState().ExpectedInput,
            measurement);
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildDistanceAngleCoordinateOverrideFields(
        CommandLiveMeasurement measurement)
    {
        Point2D userPoint = GetLivePointerUserPoint();

        return new[]
        {
            new CommandHudFieldViewModel(
                "distance",
                "Distance",
                _commandHudInputState.Distance ?? measurement.Distance),
            new CommandHudFieldViewModel(
                "angle",
                "Angle",
                _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees,
                "°"),
            new CommandHudFieldViewModel(
                "x",
                "X",
                _commandHudInputState.X ?? userPoint.X),
            new CommandHudFieldViewModel(
                "y",
                "Y",
                _commandHudInputState.Y ?? userPoint.Y)
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildCoordinateOverrideFields()
    {
        Point2D userPoint = GetLivePointerUserPoint();

        return new[]
        {
            new CommandHudFieldViewModel(
                "x",
                "X",
                _commandHudInputState.X ?? userPoint.X),
            new CommandHudFieldViewModel(
                "y",
                "Y",
                _commandHudInputState.Y ?? userPoint.Y)
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildSingleDistanceCoordinateOverrideField(
        string key,
        string label,
        double? value)
    {
        Point2D userPoint = GetLivePointerUserPoint();

        return new[]
        {
            new CommandHudFieldViewModel(
                key,
                label,
                value),
            new CommandHudFieldViewModel(
                "x",
                "X",
                _commandHudInputState.X ?? userPoint.X),
            new CommandHudFieldViewModel(
                "y",
                "Y",
                _commandHudInputState.Y ?? userPoint.Y)
        };
    }

    private Point2D GetLivePointerUserPoint()
    {
        Point2D targetPoint = _currentSnapCandidate?.Point ?? _mousePosition;
        return Workspace.CurrentUcs.WorldToUser(targetPoint);
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildDistanceAngleFields(
        CommandLiveMeasurement measurement)
    {
        return new[]
        {
            new CommandHudFieldViewModel(
                "distance",
                "Distance",
                _commandHudInputState.Distance ?? measurement.Distance),
            new CommandHudFieldViewModel(
                "angle",
                "Angle",
                _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees,
                "°")
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildWidthHeightFields(
        CommandLiveMeasurement measurement)
    {
        return new[]
        {
            new CommandHudFieldViewModel(
                "width",
                "Width",
                _commandHudInputState.Width ?? (measurement.DeltaX is null
                    ? null
                    : Math.Abs(measurement.DeltaX.Value))),
            new CommandHudFieldViewModel(
                "height",
                "Height",
                _commandHudInputState.Height ?? (measurement.DeltaY is null
                    ? null
                    : Math.Abs(measurement.DeltaY.Value)))
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildRectangleBySidesFields(
        RectangleBySidesTool rectangleBySidesTool,
        CommandLiveMeasurement measurement)
    {
        return rectangleBySidesTool.State switch
        {
            RectangleBySidesToolState.WaitingForFirstSideEndPoint => new[]
            {
                new CommandHudFieldViewModel(
                    "distance",
                    "Distance",
                    _commandHudInputState.Distance ?? measurement.Distance),
                new CommandHudFieldViewModel(
                    "angle",
                    "Angle",
                    _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees,
                    "°")
            },

            RectangleBySidesToolState.WaitingForSecondSidePoint => new[]
            {
                new CommandHudFieldViewModel(
                    "height",
                    "Height",
                    _commandHudInputState.Height ?? measurement.Distance)
            },

            _ => Array.Empty<CommandHudFieldViewModel>()
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildArcFields(
        ArcTool arcTool,
        CommandLiveMeasurement measurement)
    {
        return arcTool.State switch
        {
            ArcToolState.WaitingForStartPoint => new[]
            {
                new CommandHudFieldViewModel(
                    "radius",
                    "Radius",
                    _commandHudInputState.Distance ?? measurement.Distance,
                    kind: CommandHudFieldKind.Distance),
                new CommandHudFieldViewModel(
                    "angle",
                    "Angle",
                    _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees,
                    "°")
            },

            ArcToolState.WaitingForEndPoint => new[]
            {
                new CommandHudFieldViewModel(
                    "angle",
                    "Angle",
                    _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees,
                    "°")
            },

            _ => Array.Empty<CommandHudFieldViewModel>()
        };
    }


    private IReadOnlyList<CommandHudFieldViewModel> BuildRadiusAngleFields(
        CommandLiveMeasurement measurement)
    {
        return new[]
        {
            new CommandHudFieldViewModel(
                "radius",
                "Radius",
                _commandHudInputState.Distance ?? measurement.Distance,
                kind: CommandHudFieldKind.Distance),
            new CommandHudFieldViewModel(
                "angle",
                "Angle",
                _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees,
                "°")
        };
    }

    private static IReadOnlyList<CommandHudFieldViewModel> BuildSingleDistanceField(
        string key,
        string label,
        double? value,
        CommandHudFieldKind? kind = null)
    {
        return new[]
        {
            new CommandHudFieldViewModel(
                key,
                label,
                value,
                kind: kind)
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildSingleAngleField(
        double? value)
    {
        return new[]
        {
            new CommandHudFieldViewModel(
                "angle",
                "Angle",
                _commandHudInputState.AngleDegrees ?? value,
                "°")
        };
    }

    private static IReadOnlyList<CommandHudFieldViewModel> BuildSingleNumberField(
        string key,
        string label,
        double? value)
    {
        return new[]
        {
            new CommandHudFieldViewModel(
                key,
                label,
                value)
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildEllipseFields(
        EllipseTool ellipseTool,
        CommandLiveMeasurement measurement)
    {
        return ellipseTool.State switch
        {
            EllipseToolState.WaitingForMajorAxis => new[]
            {
                new CommandHudFieldViewModel(
                    "major-radius",
                    "Major radius",
                    _commandHudInputState.Distance ?? measurement.Distance,
                    kind: CommandHudFieldKind.Distance),
                new CommandHudFieldViewModel(
                    "angle",
                    "Angle",
                    _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees,
                    "°")
            },

            EllipseToolState.WaitingForMinorRadius => BuildSingleDistanceField(
                "minor-radius",
                "Minor radius",
                _commandHudInputState.Distance ?? measurement.Distance,
                CommandHudFieldKind.Distance),

            _ => Array.Empty<CommandHudFieldViewModel>()
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildRotateFields(
        RotateTool rotateTool,
        CommandLiveMeasurement measurement)
    {
        return rotateTool.State switch
        {
            RotateToolState.WaitingForReferencePoint => BuildDistanceAngleCoordinateOverrideFields(measurement),

            RotateToolState.WaitingForDestinationPoint when rotateTool.HasPreview => BuildAngleCoordinateOverrideFields(
                _commandHudInputState.AngleDegrees ?? rotateTool.CurrentAngle.Degrees),

            RotateToolState.WaitingForDestinationPoint => BuildAngleCoordinateOverrideFields(
                _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees),

            _ => Array.Empty<CommandHudFieldViewModel>()
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildScaleFields(
        ScaleTool scaleTool,
        CommandLiveMeasurement measurement)
    {
        return scaleTool.State switch
        {
            ScaleToolState.WaitingForReferencePoint => BuildDistanceAngleCoordinateOverrideFields(measurement),

            ScaleToolState.WaitingForDestinationPoint when scaleTool.HasPreview => BuildFactorCoordinateOverrideFields(
                _commandHudInputState.Factor ?? scaleTool.CurrentFactor),

            ScaleToolState.WaitingForDestinationPoint => BuildFactorCoordinateOverrideFields(
                _commandHudInputState.Factor),

            _ => Array.Empty<CommandHudFieldViewModel>()
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildAngleCoordinateOverrideFields(double? angleDegrees)
    {
        Point2D userPoint = GetLivePointerUserPoint();

        return new[]
        {
            new CommandHudFieldViewModel(
                "angle",
                "Angle",
                angleDegrees,
                "°"),
            new CommandHudFieldViewModel(
                "x",
                "X",
                _commandHudInputState.X ?? userPoint.X),
            new CommandHudFieldViewModel(
                "y",
                "Y",
                _commandHudInputState.Y ?? userPoint.Y)
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildFactorCoordinateOverrideFields(double? factor)
    {
        Point2D userPoint = GetLivePointerUserPoint();

        return new[]
        {
            new CommandHudFieldViewModel(
                "factor",
                "Factor",
                factor),
            new CommandHudFieldViewModel(
                "x",
                "X",
                _commandHudInputState.X ?? userPoint.X),
            new CommandHudFieldViewModel(
                "y",
                "Y",
                _commandHudInputState.Y ?? userPoint.Y)
        };
    }

    private IReadOnlyList<CommandHudFieldViewModel> BuildFieldsFromPromptKind(
        CommandInputKind expectedInput,
        CommandLiveMeasurement measurement)
    {
        return expectedInput switch
        {
            CommandInputKind.Point or
            CommandInputKind.PointOrOption => BuildCoordinateOverrideFields(),

            CommandInputKind.PointOrDistance or
            CommandInputKind.PointOrDistanceOrOption => BuildDistanceAngleFields(measurement),

            CommandInputKind.PointOrAngle or
            CommandInputKind.PointOrAngleOrOption => BuildSingleAngleField(measurement.AngleDegrees),

            CommandInputKind.Distance or
            CommandInputKind.DistanceOrOption => BuildSingleDistanceField(
                "distance",
                "Distance",
                _commandHudInputState.Distance ?? measurement.Distance),

            CommandInputKind.Angle => BuildSingleAngleField(measurement.AngleDegrees),

            _ => Array.Empty<CommandHudFieldViewModel>()
        };
    }

    public string MeasurementText
    {
        get
        {
            if (Workspace.Context.CurrentBasePoint is null)
            {
                return "Measure: -";
            }

            CommandLiveMeasurement measurement = GetLiveMeasurement();

            if (!measurement.HasValue)
            {
                return "Measure: L 0 | DX 0 | DY 0";
            }

            return $"Measure: L {measurement.Distance:0.###} | DX {measurement.DeltaX:0.###} | DY {measurement.DeltaY:0.###}";
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
                    : "Polyline: specify next point, type distance, press Enter/right-click to finish, or C to close:";
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
            viewportState,
            BuildCurrentDocumentSettings());

        _documentSerializer.SaveToFile(
            dto,
            filePath);

        _currentFilePath = filePath;
        Workspace.MarkSaved();
        RegisterSavedFile(filePath);

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
        ApplyDocumentSettings(recovery.Settings);

        _currentFilePath = filePath;
        RegisterOpenedFile(filePath);

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

        RegisterExportedFile(filePath);

        SetMessage(BuildExportCompletedMessage(
            "SVG",
            Path.GetFileName(filePath),
            $"{result.ExportedEntityCount} entities"));
        OnPropertiesChanged(
            nameof(LastMessage),
            nameof(StatusText));

        return result;
    }


    private string BuildExportCompletedMessage(
        string formatName,
        string fileName,
        string details)
    {
        string message = $"Exported {formatName} '{fileName}' ({details}). This export does not save the editable OpenCad2D project.";

        if (string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            message += " The editable OpenCad2D project has not been saved yet; use Save As to preserve the native drawing.";
        }
        else if (IsDirty)
        {
            message += " Unsaved project changes remain; use Save to preserve the native drawing.";
        }
        else
        {
            message += " The native drawing is already saved.";
        }

        return message;
    }


    public ToolResult AddImageReference(
        string filePath,
        Point2D center,
        double width,
        double height,
        int pixelWidth,
        int pixelHeight)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Image file path cannot be empty.",
                nameof(filePath));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Image width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                "Image height must be greater than zero.");
        }

        Workspace.EnsureCurrentLayerIsUsable();

        var imageReference = new ImageReferenceEntity(
            filePath,
            new Point2D(
                center.X - width / 2.0,
                center.Y - height / 2.0),
            new Vector2D(width, 0),
            new Vector2D(0, height),
            pixelWidth,
            pixelHeight,
            layerId: Workspace.CurrentLayerId);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new AddEntityCommand(imageReference));

        Workspace.SelectionSet.ReplaceWith(new[] { imageReference.Id });

        ToolResult result = ToolResult.Completed(
            $"Linked image '{Path.GetFileName(filePath)}' added as external reference.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult SelectImageReference(EntityId entityId)
    {
        ImageReferenceEntity? imageReference = GetImageReferenceById(entityId);

        if (imageReference is null)
        {
            ToolResult rejected = ToolResult.None(
                "The selected image reference no longer exists in the drawing.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        Workspace.SelectionSet.ReplaceWith(imageReference.Id);

        ToolResult result = ToolResult.Completed(
            $"Selected image reference '{Path.GetFileName(imageReference.FilePath)}'.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult ReplaceImageReference(
        EntityId entityId,
        string filePath,
        int pixelWidth,
        int pixelHeight)
    {
        return ReplaceImageReferenceInternal(
            entityId,
            filePath,
            pixelWidth,
            pixelHeight,
            "Image reference replaced");
    }

    public ToolResult RelinkImageReference(
        EntityId entityId,
        string filePath,
        int pixelWidth,
        int pixelHeight)
    {
        return ReplaceImageReferenceInternal(
            entityId,
            filePath,
            pixelWidth,
            pixelHeight,
            "Image reference relinked");
    }


    public ToolResult SetImageReferenceTransparency(
        EntityId entityId,
        double transparencyPercent)
    {
        ImageReferenceEntity? imageReference = GetImageReferenceById(entityId);

        if (imageReference is null)
        {
            ToolResult rejected = ToolResult.None(
                "The selected image reference no longer exists in the drawing.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        ImageReferenceEntity replacement = imageReference.WithTransparencyPercent(transparencyPercent);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new ReplaceEntitiesCommand(replacement));

        Workspace.SelectionSet.ReplaceWith(replacement.Id);

        ToolResult result = ToolResult.Completed(
            $"Image reference transparency set to {replacement.TransparencyPercent:0.#}%.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    private ToolResult ReplaceImageReferenceInternal(
        EntityId entityId,
        string filePath,
        int pixelWidth,
        int pixelHeight,
        string completedPrefix)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Image file path cannot be empty.",
                nameof(filePath));
        }

        ImageReferenceEntity? imageReference = GetImageReferenceById(entityId);

        if (imageReference is null)
        {
            ToolResult rejected = ToolResult.None(
                "The selected image reference no longer exists in the drawing.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        ImageReferenceEntity replacement = imageReference.WithFilePath(
            filePath,
            pixelWidth,
            pixelHeight);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new ReplaceEntitiesCommand(replacement));

        Workspace.SelectionSet.ReplaceWith(replacement.Id);

        ToolResult result = ToolResult.Completed(
            $"{completedPrefix} to '{Path.GetFileName(filePath)}'.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult ReplaceSelectedImageReference(
        string filePath,
        int pixelWidth,
        int pixelHeight)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Image file path cannot be empty.",
                nameof(filePath));
        }

        ImageReferenceEntity? selectedImageReference = GetSingleSelectedImageReference();

        if (selectedImageReference is null)
        {
            ToolResult rejected = ToolResult.None(
                "Select exactly one image reference before replacing/relinking its file.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        var replacement = selectedImageReference.WithFilePath(
            filePath,
            pixelWidth,
            pixelHeight);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new ReplaceEntitiesCommand(replacement));

        Workspace.SelectionSet.ReplaceWith(replacement.Id);

        ToolResult result = ToolResult.Completed(
            $"Image reference relinked to '{Path.GetFileName(filePath)}'.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }


    public ToolResult RelinkFirstMissingImageReference(
        string filePath,
        int pixelWidth,
        int pixelHeight)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Image file path cannot be empty.",
                nameof(filePath));
        }

        ImageReferenceEntity? imageReference = GetSingleSelectedImageReference();

        if (imageReference is not null && File.Exists(imageReference.FilePath))
        {
            imageReference = null;
        }

        imageReference ??= GetMissingImageReferences().FirstOrDefault();

        if (imageReference is null)
        {
            ToolResult rejected = ToolResult.None(
                "No missing image reference was found in the current drawing.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        ImageReferenceEntity replacement = imageReference.WithFilePath(
            filePath,
            pixelWidth,
            pixelHeight);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new ReplaceEntitiesCommand(replacement));

        Workspace.SelectionSet.ReplaceWith(replacement.Id);

        ToolResult result = ToolResult.Completed(
            $"Missing image reference relinked to '{Path.GetFileName(filePath)}'.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult SelectNextMissingImageReference()
    {
        ImageReferenceEntity? missingImageReference = GetMissingImageReferences().FirstOrDefault();

        if (missingImageReference is null)
        {
            ToolResult rejected = ToolResult.None(
                "No missing image reference was found in the current drawing.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        Workspace.SelectionSet.ReplaceWith(missingImageReference.Id);

        ToolResult result = ToolResult.Completed(
            $"Selected missing image reference '{Path.GetFileName(missingImageReference.FilePath)}'.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult ResetSelectedImageReferenceAspectRatio()
    {
        ImageReferenceEntity? selectedImageReference = GetSingleSelectedImageReference();

        if (selectedImageReference is null)
        {
            ToolResult rejected = ToolResult.None(
                "Select exactly one image reference before resetting its aspect ratio.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        if (!selectedImageReference.HasNaturalAspectRatio)
        {
            ToolResult rejected = ToolResult.None(
                "The selected image reference has no pixel size metadata, so its natural aspect ratio is unknown.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        ImageReferenceEntity replacement = selectedImageReference.WithNaturalAspectRatio();

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new ReplaceEntitiesCommand(replacement));

        Workspace.SelectionSet.ReplaceWith(replacement.Id);

        ToolResult result = ToolResult.Completed(
            "Image aspect ratio reset from the linked raster file metadata.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult CollectExternalImageReferences()
    {
        if (string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            ToolResult rejected = ToolResult.None(
                "Save the drawing before collecting external image references.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        string? documentDirectory = Path.GetDirectoryName(CurrentFilePath);

        if (string.IsNullOrWhiteSpace(documentDirectory))
        {
            ToolResult rejected = ToolResult.None(
                "The current drawing path has no valid directory.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        IReadOnlyList<ImageReferenceEntity> imageReferences = Workspace.Document.Entities.All
            .OfType<ImageReferenceEntity>()
            .ToList();

        if (imageReferences.Count == 0)
        {
            ToolResult rejected = ToolResult.None(
                "The current drawing has no external image references to collect.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        string imagesDirectory = Path.Combine(documentDirectory, "images");
        var replacements = new List<ImageReferenceEntity>();
        var collectedSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int copiedCount = 0;
        int reusedCount = 0;
        int missingCount = 0;

        foreach (ImageReferenceEntity imageReference in imageReferences)
        {
            if (string.IsNullOrWhiteSpace(imageReference.FilePath) || !File.Exists(imageReference.FilePath))
            {
                missingCount++;
                continue;
            }

            string sourcePath = Path.GetFullPath(imageReference.FilePath);

            if (!collectedSources.TryGetValue(sourcePath, out string? targetPath))
            {
                Directory.CreateDirectory(imagesDirectory);

                string sourceFileName = Path.GetFileName(sourcePath);
                targetPath = GetCollectReferenceTargetPath(
                    imagesDirectory,
                    sourceFileName,
                    sourcePath);

                if (AreSamePath(sourcePath, targetPath))
                {
                    reusedCount++;
                }
                else
                {
                    File.Copy(sourcePath, targetPath, overwrite: false);
                    copiedCount++;
                }

                collectedSources[sourcePath] = targetPath;
            }
            else
            {
                reusedCount++;
            }

            ImageReferenceEntity replacement = imageReference.WithFilePath(
                targetPath,
                imageReference.PixelWidth,
                imageReference.PixelHeight);

            replacements.Add(replacement);
        }

        if (replacements.Count == 0)
        {
            ToolResult rejected = ToolResult.None(
                missingCount == 0
                    ? "No image reference needed to be collected."
                    : $"No image reference could be collected because {missingCount} linked image file(s) are missing.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new ReplaceEntitiesCommand(replacements));

        ToolResult result = ToolResult.Completed(
            BuildCollectExternalImageReferencesMessage(
                copiedCount,
                reusedCount,
                missingCount));

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    private static string BuildCollectExternalImageReferencesMessage(
        int copiedCount,
        int reusedCount,
        int missingCount)
    {
        string message = $"Collected external image references: {copiedCount} copied, {reusedCount} already in package.";

        if (missingCount > 0)
        {
            message += $" {missingCount} missing image reference(s) were skipped.";
        }

        message += " Save the drawing to persist relative image paths.";

        return message;
    }

    private static string GetCollectReferenceTargetPath(
        string imagesDirectory,
        string sourceFileName,
        string sourcePath)
    {
        string safeFileName = string.IsNullOrWhiteSpace(sourceFileName)
            ? "image"
            : sourceFileName;

        string targetPath = Path.Combine(imagesDirectory, safeFileName);

        if (!File.Exists(targetPath) || AreSamePath(targetPath, sourcePath))
        {
            return targetPath;
        }

        string name = Path.GetFileNameWithoutExtension(safeFileName);
        string extension = Path.GetExtension(safeFileName);

        for (int index = 2; ; index++)
        {
            string candidate = Path.Combine(
                imagesDirectory,
                $"{name}_{index}{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static bool AreSamePath(
        string first,
        string second)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(first),
                Path.GetFullPath(second),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return string.Equals(
                first,
                second,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private ImageReferenceEntity? GetImageReferenceById(EntityId entityId)
    {
        if (!Workspace.Document.Entities.Contains(entityId))
        {
            return null;
        }

        return Workspace.Document.Entities.GetRequired(entityId) as ImageReferenceEntity;
    }

    private ImageReferenceEntity? GetSingleSelectedImageReference()
    {
        if (Workspace.SelectionSet.SelectedIds.Count != 1)
        {
            return null;
        }

        EntityId entityId = Workspace.SelectionSet.SelectedIds.First();

        if (!Workspace.Document.Entities.Contains(entityId))
        {
            return null;
        }

        return Workspace.Document.Entities.GetRequired(entityId) as ImageReferenceEntity;
    }

    private IReadOnlyList<ImageReferenceEntity> GetMissingImageReferences()
    {
        return Workspace.Document.Entities.All
            .OfType<ImageReferenceEntity>()
            .Where(imageReference => !File.Exists(imageReference.FilePath))
            .ToList();
    }


    public bool IsCreateBlockBasePointPickPending => _pendingCreateBlockBasePointPick is not null;

    public bool IsBlockInsertionPending => _pendingBlockInsertion is not null;

    public bool IsLibraryInsertionPending => _pendingLibraryBlockInsertion is not null;

    public IReadOnlyList<BlockDefinition> BlockDefinitions => Workspace.Document.BlockDefinitions.All;

    public ToolResult BeginInsertLibraryItem(LibraryCatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        DocumentRecoveryResult recovery = _documentSerializer.DeserializeWithRecovery(item.Document);

        try
        {
            var builder = new LibraryBlockDefinitionBuilder();
            LibraryBlockDefinitionPreparation preparation = builder.Prepare(
                Workspace.Document,
                recovery.Document,
                item);

            _pendingLibraryBlockInsertion = new PendingLibraryBlockInsertion(
                item,
                preparation);

            ToolResult result = ToolResult.Started(
                $"Library item '{item.Title}': specify insertion point.");

            SetLastResult(result);
            NotifyDocumentStateChanged();

            return result;
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            ToolResult rejected = ToolResult.None(exception.Message);

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }
    }

    public ToolResult CommitPendingLibraryInsertion(Point2D insertionPoint)
    {
        if (_pendingLibraryBlockInsertion is null)
        {
            return ToolResult.None();
        }

        PendingLibraryBlockInsertion pending = _pendingLibraryBlockInsertion;
        _pendingLibraryBlockInsertion = null;

        Workspace.EnsureCurrentLayerIsUsable();

        var reference = new BlockReferenceEntity(
            pending.Preparation.BlockDefinitionId,
            insertionPoint,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            pending.Preparation.Definition.GetBoundingBox(),
            layerId: Workspace.CurrentLayerId);

        var commands = new List<ICadCommand>(pending.Preparation.DefinitionCommands)
        {
            new AddEntityCommand(reference)
        };

        ICadCommand command = commands.Count == 1
            ? commands[0]
            : new CompositeCommand(
                "Insert library item",
                commands);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            command);

        Workspace.SelectionSet.ReplaceWith(reference.Id);

        ToolResult result = ToolResult.Completed(
            $"Library item '{pending.Item.Title}' inserted as block '{pending.Preparation.BlockName}'.");

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();

        return result;
    }

    public ToolResult CancelPendingLibraryInsertion()
    {
        if (_pendingLibraryBlockInsertion is null)
        {
            return ToolResult.None();
        }

        _pendingLibraryBlockInsertion = null;

        ToolResult result = ToolResult.None("Library insertion cancelled.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult BeginInsertBlockPlacement(InsertBlockOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Workspace.Document.BlockDefinitions.Contains(options.BlockDefinitionId))
        {
            ToolResult rejected = ToolResult.None(
                $"Block '{options.BlockName}' does not exist.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        _pendingBlockInsertion = options;

        ToolResult result = ToolResult.Started(
            $"Insert block '{options.BlockName}': specify insertion point.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult CommitPendingBlockInsertion(Point2D insertionPoint)
    {
        if (_pendingBlockInsertion is null)
        {
            return ToolResult.None();
        }

        InsertBlockOptions options = _pendingBlockInsertion;
        _pendingBlockInsertion = null;

        if (!Workspace.Document.BlockDefinitions.TryGet(options.BlockDefinitionId, out BlockDefinition? definition) ||
            definition is null)
        {
            ToolResult rejected = ToolResult.None(
                $"Block '{options.BlockName}' does not exist.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        Workspace.EnsureCurrentLayerIsUsable();

        double rotationRadians = options.RotationDegrees * Math.PI / 180.0;
        double cos = Math.Cos(rotationRadians);
        double sin = Math.Sin(rotationRadians);
        double scale = options.Scale;

        var reference = new BlockReferenceEntity(
            definition.Id,
            insertionPoint,
            new Vector2D(cos * scale, sin * scale),
            new Vector2D(-sin * scale, cos * scale),
            definition.GetBoundingBox(),
            layerId: Workspace.CurrentLayerId);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new AddEntityCommand(reference));

        Workspace.SelectionSet.ReplaceWith(reference.Id);

        ToolResult result = ToolResult.Completed(
            $"Block '{definition.Name}' inserted.");

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();

        return result;
    }

    public ToolResult CancelPendingBlockInsertion()
    {
        if (_pendingBlockInsertion is null)
        {
            return ToolResult.None();
        }

        _pendingBlockInsertion = null;

        ToolResult result = ToolResult.None("Insert block cancelled.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public bool IsBlockEditSessionActive => _activeBlockEditSession is not null;

    public string ActiveBlockEditText => _activeBlockEditSession is null
        ? "No active block edit"
        : $"Editing block '{_activeBlockEditSession.BlockName}'";

    public ToolResult BeginEditSelectedBlock()
    {
        if (_activeBlockEditSession is not null)
        {
            ToolResult rejected = ToolResult.None(
                $"Finish or cancel the current block edit before editing another block.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        if (Workspace.SelectionSet.SelectedIds.Count != 1)
        {
            ToolResult rejected = ToolResult.None(
                "Select exactly one block reference before editing a block.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        EntityId selectedId = Workspace.SelectionSet.SelectedIds.Single();

        if (!Workspace.Document.Entities.TryGet(selectedId, out CadEntity? selectedEntity) ||
            selectedEntity is not BlockReferenceEntity blockReference ||
            !Workspace.Document.IsEntitySelectable(blockReference))
        {
            ToolResult rejected = ToolResult.None(
                "Select an editable block reference before editing a block.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        if (!Workspace.Document.BlockDefinitions.TryGet(blockReference.BlockDefinitionId, out BlockDefinition? definition) ||
            definition is null)
        {
            ToolResult rejected = ToolResult.None(
                "The selected block reference points to a missing block definition.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        IReadOnlyList<CadEntity> editEntities = definition.Entities
            .Select(entity => blockReference.TransformContainedEntity(entity).WithId(EntityId.New()))
            .ToList();

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new ModifyEntitiesCommand(
                new[] { blockReference },
                editEntities,
                "Start block edit"));

        _activeBlockEditSession = new BlockEditSession(
            definition.Id,
            definition.Name,
            blockReference,
            editEntities.Select(entity => entity.Id).ToList());

        Workspace.SelectionSet.ReplaceWith(editEntities.Select(entity => entity.Id));

        ToolResult result = ToolResult.Completed(
            $"Block '{definition.Name}' opened for editing. Modify the selected entities, then use Save Block Edit or Cancel Block Edit.");

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();
        OnPropertiesChanged(nameof(IsBlockEditSessionActive), nameof(ActiveBlockEditText));

        return result;
    }

    public ToolResult SaveActiveBlockEdit()
    {
        if (_activeBlockEditSession is null)
        {
            ToolResult rejected = ToolResult.None("No block edit session is active.");
            SetLastResult(rejected);
            NotifyDocumentStateChanged();
            return rejected;
        }

        BlockEditSession session = _activeBlockEditSession;

        if (!Workspace.Document.BlockDefinitions.TryGet(session.BlockDefinitionId, out BlockDefinition? oldDefinition) ||
            oldDefinition is null)
        {
            ToolResult rejected = ToolResult.None(
                $"Cannot save block edit because block '{session.BlockName}' no longer exists.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();
            return rejected;
        }

        IReadOnlyList<CadEntity> editedEntities = GetBlockEditSourceEntities(session);
        IReadOnlyList<CadEntity> entitiesToRemove = GetBlockEditEntitiesToRemove(session, editedEntities);

        if (editedEntities.Count == 0)
        {
            ToolResult rejected = ToolResult.None(
                "Select at least one editable non-block entity or keep at least one original edit entity before saving the block edit.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();
            return rejected;
        }

        Matrix2D worldToBlock = session.OriginalReference.LocalToWorldMatrix.Invert();
        IReadOnlyList<CadEntity> localEntities = editedEntities
            .Select(entity => entity.Transform(worldToBlock).WithId(EntityId.New()))
            .ToList();

        BlockDefinition newDefinition = oldDefinition.WithEntities(localEntities);
        BoundingBox2D updatedDefinitionBounds = newDefinition.GetBoundingBox();
        BlockReferenceEntity restoredReference = WithDefinitionBounds(
            session.OriginalReference,
            updatedDefinitionBounds);

        IReadOnlyList<BlockReferenceEntity> updatedExistingReferences = Workspace.Document.Entities.All
            .OfType<BlockReferenceEntity>()
            .Where(reference => reference.BlockDefinitionId == session.BlockDefinitionId)
            .Select(reference => WithDefinitionBounds(reference, updatedDefinitionBounds))
            .ToList();

        List<BlockDefinition> updatedDefinitions = Workspace.Document.BlockDefinitions.All
            .Select(definition => definition.Id == session.BlockDefinitionId ? newDefinition : definition)
            .ToList();

        var commands = new List<ICadCommand>
        {
            new UpdateBlockDefinitionsCommand(
                Workspace.Document.BlockDefinitions.All.ToList(),
                updatedDefinitions)
        };

        if (updatedExistingReferences.Count > 0)
        {
            commands.Add(new ReplaceEntitiesCommand(updatedExistingReferences));
        }

        commands.Add(new ModifyEntitiesCommand(
            entitiesToRemove,
            new[] { restoredReference },
            "Close block edit"));

        var command = new CompositeCommand(
            "Save block edit",
            commands);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            command);

        _activeBlockEditSession = null;
        Workspace.SelectionSet.ReplaceWith(restoredReference.Id);

        ToolResult result = ToolResult.Completed(
            $"Block '{session.BlockName}' updated from {editedEntities.Count} entity/entities.");

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();
        OnPropertiesChanged(nameof(IsBlockEditSessionActive), nameof(ActiveBlockEditText));

        return result;
    }

    public ToolResult CancelActiveBlockEdit()
    {
        if (_activeBlockEditSession is null)
        {
            ToolResult rejected = ToolResult.None("No block edit session is active.");
            SetLastResult(rejected);
            NotifyDocumentStateChanged();
            return rejected;
        }

        BlockEditSession session = _activeBlockEditSession;
        IReadOnlyList<CadEntity> editEntities = GetExistingEntities(session.EditEntityIds);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            new ModifyEntitiesCommand(
                editEntities,
                new[] { session.OriginalReference },
                "Cancel block edit"));

        _activeBlockEditSession = null;
        Workspace.SelectionSet.ReplaceWith(session.OriginalReference.Id);

        ToolResult result = ToolResult.Completed(
            $"Block edit for '{session.BlockName}' cancelled.");

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();
        OnPropertiesChanged(nameof(IsBlockEditSessionActive), nameof(ActiveBlockEditText));

        return result;
    }

    private IReadOnlyList<CadEntity> GetBlockEditSourceEntities(BlockEditSession session)
    {
        IReadOnlyList<CadEntity> selectedEditableEntities = Workspace.SelectionSet.SelectedIds
            .Select(id => Workspace.Document.Entities.TryGet(id, out CadEntity? entity) ? entity : null)
            .OfType<CadEntity>()
            .Where(entity => entity is not BlockReferenceEntity)
            .Where(Workspace.Document.IsEntitySelectable)
            .ToList();

        if (selectedEditableEntities.Count > 0)
        {
            return selectedEditableEntities;
        }

        return GetExistingEntities(session.EditEntityIds);
    }

    private IReadOnlyList<CadEntity> GetBlockEditEntitiesToRemove(
        BlockEditSession session,
        IReadOnlyList<CadEntity> sourceEntities)
    {
        Dictionary<EntityId, CadEntity> entities = GetExistingEntities(session.EditEntityIds)
            .Concat(sourceEntities)
            .GroupBy(entity => entity.Id)
            .ToDictionary(group => group.Key, group => group.First());

        return entities.Values.ToList();
    }

    private static BlockReferenceEntity WithDefinitionBounds(
        BlockReferenceEntity reference,
        BoundingBox2D definitionBounds)
    {
        return new BlockReferenceEntity(
            reference.BlockDefinitionId,
            reference.InsertionPoint,
            reference.XAxis,
            reference.YAxis,
            definitionBounds,
            reference.Id,
            reference.LayerId,
            reference.Style,
            reference.IsVisible,
            reference.IsLocked,
            reference.DrawOrder);
    }

    private IReadOnlyList<CadEntity> GetExistingEntities(IEnumerable<EntityId> entityIds)
    {
        return entityIds
            .Select(id => Workspace.Document.Entities.TryGet(id, out CadEntity? entity) ? entity : null)
            .OfType<CadEntity>()
            .ToList();
    }

    public ToolResult BeginCreateBlockBasePointPick(CreateBlockOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string blockName = options.Name.Trim();

        if (string.IsNullOrWhiteSpace(blockName))
        {
            ToolResult rejected = ToolResult.None(
                "Enter a block name before creating the block.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        _pendingCreateBlockBasePointPick = options with
        {
            Name = blockName,
            PickBasePointFromDrawing = false
        };

        ToolResult result = ToolResult.Started(
            $"Create block '{blockName}': specify base point.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult CommitCreateBlockBasePointPick(Point2D basePoint)
    {
        if (_pendingCreateBlockBasePointPick is null)
        {
            return ToolResult.None();
        }

        CreateBlockOptions pendingOptions = _pendingCreateBlockBasePointPick;
        _pendingCreateBlockBasePointPick = null;

        return CreateBlockFromSelection(pendingOptions with
        {
            BasePointX = basePoint.X,
            BasePointY = basePoint.Y,
            PickBasePointFromDrawing = false
        });
    }

    public ToolResult CancelCreateBlockBasePointPick()
    {
        if (_pendingCreateBlockBasePointPick is null)
        {
            return ToolResult.None();
        }

        _pendingCreateBlockBasePointPick = null;

        ToolResult result = ToolResult.None("Create block cancelled.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }


    public bool IsImportDrawingPlacementPending => _pendingOpenCad2DImport is not null;

    public ToolResult BeginImportDrawingPlacementFromFile(
        string filePath,
        OpenCad2DImportPlacementOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "OpenCad2D import file path cannot be empty.",
                nameof(filePath));
        }

        DocumentDto dto = _documentSerializer.LoadFromFile(filePath);
        DocumentRecoveryResult recovery = _documentSerializer.DeserializeWithRecovery(dto);

        _pendingOpenCad2DImport = new PendingOpenCad2DImport(
            filePath,
            recovery,
            options ?? OpenCad2DImportPlacementOptions.Default);

        ToolResult result = ToolResult.Started(
            $"Import drawing '{Path.GetFileName(filePath)}': specify insertion point.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult CommitPendingImportDrawing(Point2D insertionPoint)
    {
        if (_pendingOpenCad2DImport is null)
        {
            return ToolResult.None();
        }

        PendingOpenCad2DImport pendingImport = _pendingOpenCad2DImport;
        _pendingOpenCad2DImport = null;

        OpenCad2DImportMergeResult mergeResult = MergeImportedDocument(
            pendingImport.Recovery.Document,
            insertionPoint,
            pendingImport.Options);

        ApplyImportedEntitiesSelection(mergeResult);

        string recoverySuffix = pendingImport.Recovery.HasIssues
            ? $" Recovery: {pendingImport.Recovery.RecoveredEntityCount} recovered, {pendingImport.Recovery.SkippedEntityCount} skipped."
            : string.Empty;

        ToolResult result = ToolResult.Completed(
            $"Imported OpenCad2D drawing '{Path.GetFileName(pendingImport.FilePath)}' " +
            $"({mergeResult.ImportedEntityCount} entities, " +
            $"{mergeResult.AddedLayerCount} layers, " +
            $"{mergeResult.AddedLineFormatCount} line formats, " +
            $"scale {pendingImport.Options.Scale:0.###}, " +
            $"rotation {pendingImport.Options.RotationDegrees:0.###}°)." +
            recoverySuffix);

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();

        return result;
    }

    public ToolResult CancelPendingImportDrawing()
    {
        if (_pendingOpenCad2DImport is null)
        {
            return ToolResult.None();
        }

        _pendingOpenCad2DImport = null;

        ToolResult result = ToolResult.Cancelled("Import drawing cancelled.");

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult ImportDrawingFromFile(string filePath)
    {
        return ImportDrawingFromFile(
            filePath,
            Point2D.Origin,
            OpenCad2DImportPlacementOptions.Default);
    }

    public ToolResult ImportDrawingFromFile(
        string filePath,
        Point2D insertionPoint,
        OpenCad2DImportPlacementOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "OpenCad2D import file path cannot be empty.",
                nameof(filePath));
        }

        DocumentDto dto = _documentSerializer.LoadFromFile(filePath);
        DocumentRecoveryResult recovery = _documentSerializer.DeserializeWithRecovery(dto);
        OpenCad2DImportPlacementOptions placementOptions = options ?? OpenCad2DImportPlacementOptions.Default;

        OpenCad2DImportMergeResult mergeResult = MergeImportedDocument(
            recovery.Document,
            insertionPoint,
            placementOptions);

        ApplyImportedEntitiesSelection(mergeResult);

        string recoverySuffix = recovery.HasIssues
            ? $" Recovery: {recovery.RecoveredEntityCount} recovered, {recovery.SkippedEntityCount} skipped."
            : string.Empty;

        ToolResult result = ToolResult.Completed(
            $"Imported OpenCad2D drawing '{Path.GetFileName(filePath)}' " +
            $"({mergeResult.ImportedEntityCount} entities, " +
            $"{mergeResult.AddedLayerCount} layers, " +
            $"{mergeResult.AddedLineFormatCount} line formats)." +
            recoverySuffix);

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();

        return result;
    }

    private OpenCad2DImportMergeResult MergeImportedDocument(
        CadDocument importedDocument,
        Point2D insertionPoint,
        OpenCad2DImportPlacementOptions options)
    {
        var merger = new OpenCad2DImportMerger();
        OpenCad2DImportMergeResult mergeResult = merger.Merge(
            Workspace.Document,
            importedDocument,
            insertionPoint,
            options.Scale,
            options.RotationDegrees);

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            mergeResult.Command);

        Workspace.EnsureCurrentLayerIsUsable();

        return mergeResult;
    }

    private void ApplyImportedEntitiesSelection(OpenCad2DImportMergeResult mergeResult)
    {
        if (mergeResult.ImportedEntities.Count > 0)
        {
            Workspace.SelectionSet.ReplaceWith(
                mergeResult.ImportedEntities.Select(entity => entity.Id));
        }
        else
        {
            Workspace.SelectionSet.Clear();
        }
    }



    public ToolResult CreateBlockFromSelection(CreateBlockOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string blockName = options.Name.Trim();

        if (string.IsNullOrWhiteSpace(blockName))
        {
            ToolResult rejected = ToolResult.None(
                "Enter a block name before creating the block.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        if (Workspace.Document.BlockDefinitions.ContainsName(blockName))
        {
            ToolResult rejected = ToolResult.None(
                $"A block named '{blockName}' already exists.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        IReadOnlyList<CadEntity> selectedEntities = Workspace.SelectionSet.SelectedIds
            .Select(id => Workspace.Document.Entities.TryGet(id, out CadEntity? entity) ? entity : null)
            .OfType<CadEntity>()
            .Where(Workspace.Document.IsEntitySelectable)
            .ToList();

        if (selectedEntities.Count == 0)
        {
            ToolResult rejected = ToolResult.None(
                "Select one or more editable entities before creating a block.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        if (selectedEntities.Any(entity => entity is BlockReferenceEntity))
        {
            ToolResult rejected = ToolResult.None(
                "Nested blocks are not supported yet. Explode existing block references before creating a new block.");

            SetLastResult(rejected);
            NotifyDocumentStateChanged();

            return rejected;
        }

        Workspace.EnsureCurrentLayerIsUsable();

        Point2D basePoint = new(options.BasePointX, options.BasePointY);
        Matrix2D worldToBlock = Matrix2D.Translation(-basePoint.X, -basePoint.Y);

        IReadOnlyList<CadEntity> definitionEntities = selectedEntities
            .Select(entity => entity.Transform(worldToBlock))
            .ToList();

        var blockDefinition = new BlockDefinition(
            BlockDefinitionId.New(),
            blockName,
            definitionEntities);

        var blockReference = new BlockReferenceEntity(
            blockDefinition.Id,
            basePoint,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            blockDefinition.GetBoundingBox(),
            layerId: Workspace.CurrentLayerId);

        var command = new CompositeCommand(
            "Create block",
            new ICadCommand[]
            {
                new AddBlockDefinitionCommand(blockDefinition),
                new ModifyEntitiesCommand(
                    selectedEntities,
                    new[] { blockReference },
                    "Create block reference")
            });

        Workspace.CommandHistory.Execute(
            Workspace.Document,
            command);

        Workspace.SelectionSet.ReplaceWith(blockReference.Id);

        ToolResult result = ToolResult.Completed(
            $"Block '{blockName}' created from {selectedEntities.Count} selected entities.");

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyFileStateChanged();

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

        RegisterExportedFile(filePath);

        SetMessage(BuildExportCompletedMessage(
            "DXF",
            Path.GetFileName(filePath),
            $"{result.ExportedEntityCount} entities"));
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

        RegisterExportedFile(filePath);

        SetMessage(BuildExportCompletedMessage(
            "PDF",
            Path.GetFileName(filePath),
            $"{result.ExportedEntityCount} entities, " +
            $"{options.PageSize} {options.Orientation}, " +
            $"margin {options.MarginMillimeters:0.##} mm"));
        OnPropertiesChanged(
            nameof(LastMessage),
            nameof(StatusText));

        return result;
    }


    public bool TryCommitCommandHudFieldInput(
        CommandHudFieldKind fieldKind,
        string? input,
        bool confirm,
        out ToolResult result)
    {
        result = ToolResult.None();

        string normalizedInput = input?.Trim() ?? string.Empty;

        if (!TryCommitCommandHudFieldOverride(
                fieldKind,
                normalizedInput,
                confirm,
                out result,
                out bool handled))
        {
            return false;
        }

        if (!handled)
        {
            return false;
        }

        return true;
    }

    public bool TrySubmitCommandHudFieldInput(
        CommandHudFieldKind fieldKind,
        string? input,
        out ToolResult result)
    {
        return TryCommitCommandHudFieldInput(
            fieldKind,
            input,
            confirm: true,
            out result);
    }

    public async Task<(bool Handled, ToolResult Result)> TryCommitCommandHudFieldInputAsync(
        CommandHudFieldKind fieldKind,
        string? input,
        bool confirm)
    {
        if (!confirm ||
            Workspace.ToolController.ActiveTool is not IAsyncCadTool)
        {
            bool handled = TryCommitCommandHudFieldInput(
                fieldKind,
                input,
                confirm,
                out ToolResult syncResult);

            return (handled, syncResult);
        }

        ToolResult result = ToolResult.None();
        string normalizedInput = input?.Trim() ?? string.Empty;

        if (fieldKind is not (
            CommandHudFieldKind.Distance or
            CommandHudFieldKind.Angle or
            CommandHudFieldKind.Width or
            CommandHudFieldKind.Height or
            CommandHudFieldKind.Radius or
            CommandHudFieldKind.Factor or
            CommandHudFieldKind.Sides or
            CommandHudFieldKind.X or
            CommandHudFieldKind.Y))
        {
            return (false, result);
        }

        if (!TryParseHudDouble(normalizedInput, out double value))
        {
            result = ToolResult.None($"Invalid {fieldKind.ToString().ToLowerInvariant()} value.");
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyCommandInputStateChanged();
            return (true, result);
        }

        if (!ValidateCommandHudScalarField(
                fieldKind,
                value,
                out string? validationMessage))
        {
            result = ToolResult.None(validationMessage);
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyCommandInputStateChanged();
            return (true, result);
        }

        FreezeComplementaryPolarHudValueIfNeeded(fieldKind);

        _commandHudInputState.SetOverride(
            fieldKind,
            value);

        if (TryHandleDedicatedScalarCommandHudOverride(
                fieldKind,
                value,
                normalizedInput,
                confirm,
                out result))
        {
            return (true, result);
        }

        AppendVisibleCommandHistoryLine($"> {normalizedInput}");

        if (!IsCommandHudPointInputTargetActive())
        {
            NotifyPointerDrivenStateChanged();
            return (true, result);
        }

        if (!TryResolveCommandHudOverridePoint(
                requireCompleteCoordinates: true,
                out Point2D worldPoint,
                out string? errorMessage))
        {
            result = ToolResult.None(errorMessage);
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyCommandInputStateChanged();
            return (true, result);
        }

        result = TrySubmitPendingPlacementHudPoint(
            worldPoint,
            out ToolResult pendingResult)
            ? pendingResult
            : await Workspace.SubmitPointFromCommandLineAsync(worldPoint).ConfigureAwait(true);

        ClearCommandHudInputOverrides();
        SetLastResult(result);
        AppendToolResultToVisibleHistory(result);
        NotifyDocumentStateChanged();
        NotifyCommandInputStateChanged();
        return (true, result);
    }

    public bool TryConfirmCommandHudInputOverrides(out ToolResult result)
    {
        result = ToolResult.None();

        if (!_commandHudInputState.HasAnyOverride)
        {
            return false;
        }

        if (!TryResolveCommandHudOverridePoint(
                requireCompleteCoordinates: true,
                out Point2D worldPoint,
                out string? errorMessage))
        {
            result = ToolResult.None(errorMessage);
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyCommandInputStateChanged();
            return true;
        }

        result = TrySubmitPendingPlacementHudPoint(
            worldPoint,
            out ToolResult pendingResult)
            ? pendingResult
            : Workspace.SubmitPointFromCommandLine(worldPoint);
        ClearCommandHudInputOverrides();
        SetLastResult(result);
        AppendToolResultToVisibleHistory(result);
        NotifyDocumentStateChanged();
        NotifyCommandInputStateChanged();
        return true;
    }

    public void ClearCommandHudInputOverridesForNextInput()
    {
        if (!_commandHudInputState.HasAnyOverride)
        {
            return;
        }

        ClearCommandHudInputOverrides();
        NotifyPointerDrivenStateChanged();
    }

    public ToolResult SubmitCommandInput(string? input)
    {
        ResetCommandHistoryNavigation();

        string normalizedInput = input?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            AppendVisibleCommandHistoryLine("> Enter");

            if (ShouldRouteInputToActiveCommand(normalizedInput) &&
                TrySubmitCommandDrivenInput(normalizedInput, out ToolResult activeCommandResult))
            {
                return activeCommandResult;
            }

            ToolResult repeatResult = RepeatLastCommand();
            AppendToolResultToVisibleHistory(repeatResult);
            return repeatResult;
        }

        AppendVisibleCommandHistoryLine($"> {normalizedInput}");

        if (ShouldRouteInputToActiveCommand(normalizedInput) &&
            TrySubmitCommandDrivenInput(normalizedInput, out ToolResult activeTextCommandResult))
        {
            return activeTextCommandResult;
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

    private bool ShouldRouteInputToActiveCommand(string normalizedInput)
    {
        if (Workspace.ToolController.ActiveTool is not ICommandDrivenTool commandDrivenTool)
        {
            return false;
        }

        CommandPromptState promptState = commandDrivenTool.GetPromptState(Workspace.Context);

        if (promptState.ExpectedInput != CommandInputKind.CommandName)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(normalizedInput) && promptState.AcceptsEmptyEnter;
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
        else if (submission.Kind == CommandInputSubmissionKind.Distance &&
                 submission.Distance is not null &&
                 ShouldResolveDistanceAsPoint(promptState.ExpectedInput))
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

    private static bool ShouldResolveDistanceAsPoint(CommandInputKind expectedInput)
    {
        return expectedInput is
            CommandInputKind.PointOrDistance or
            CommandInputKind.PointOrDistanceOrOption;
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


    private bool TryCommitCommandHudFieldOverride(
        CommandHudFieldKind fieldKind,
        string input,
        bool confirm,
        out ToolResult result,
        out bool handled)
    {
        result = ToolResult.None();
        handled = false;

        if (fieldKind is not (
            CommandHudFieldKind.Distance or
            CommandHudFieldKind.Angle or
            CommandHudFieldKind.Width or
            CommandHudFieldKind.Height or
            CommandHudFieldKind.Radius or
            CommandHudFieldKind.Factor or
            CommandHudFieldKind.Sides or
            CommandHudFieldKind.X or
            CommandHudFieldKind.Y))
        {
            return true;
        }

        handled = true;

        if (!TryParseHudDouble(input, out double value))
        {
            result = ToolResult.None($"Invalid {fieldKind.ToString().ToLowerInvariant()} value.");
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyCommandInputStateChanged();
            return true;
        }

        if (!ValidateCommandHudScalarField(
                fieldKind,
                value,
                out string? validationMessage))
        {
            result = ToolResult.None(validationMessage);
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyCommandInputStateChanged();
            return true;
        }

        FreezeComplementaryPolarHudValueIfNeeded(fieldKind);

        _commandHudInputState.SetOverride(
            fieldKind,
            value);

        if (TryHandleDedicatedScalarCommandHudOverride(
                fieldKind,
                value,
                input,
                confirm,
                out result))
        {
            return true;
        }

        if (confirm)
        {
            AppendVisibleCommandHistoryLine($"> {input}");
        }

        if (!IsCommandHudPointInputTargetActive())
        {
            NotifyPointerDrivenStateChanged();
            return true;
        }

        if (!TryResolveCommandHudOverridePoint(
                confirm,
                out Point2D worldPoint,
                out string? errorMessage))
        {
            result = ToolResult.None(errorMessage);
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyCommandInputStateChanged();
            return true;
        }

        if (confirm)
        {
            result = TrySubmitPendingPlacementHudPoint(
                worldPoint,
                out ToolResult pendingResult)
                ? pendingResult
                : Workspace.SubmitPointFromCommandLine(worldPoint);
            ClearCommandHudInputOverrides();
            SetLastResult(result);
            AppendToolResultToVisibleHistory(result);
            NotifyDocumentStateChanged();
            NotifyCommandInputStateChanged();
            return true;
        }

        if (IsCommandHudPointOverrideTargetActive())
        {
            result = Workspace.PreviewPointFromCommandLine(worldPoint);
            SetLastResult(result);
        }

        NotifyPointerDrivenStateChanged();
        return true;
    }



    private bool ValidateCommandHudScalarField(
        CommandHudFieldKind fieldKind,
        double value,
        out string? message)
    {
        message = null;

        if (Workspace.ToolController.ActiveTool is FilletTool { State: FilletToolState.WaitingForRadius } &&
            fieldKind == CommandHudFieldKind.Radius)
        {
            if (value < 0)
            {
                message = "Fillet radius cannot be negative.";
                return false;
            }

            return true;
        }

        if (Workspace.ToolController.ActiveTool is ChamferTool { State: ChamferToolState.WaitingForDistance } &&
            fieldKind == CommandHudFieldKind.Distance)
        {
            if (value < 0)
            {
                message = "Chamfer distance cannot be negative.";
                return false;
            }

            return true;
        }

        if (fieldKind == CommandHudFieldKind.Distance && value <= 0)
        {
            message = "Distance must be greater than zero.";
            return false;
        }

        if (fieldKind == CommandHudFieldKind.Sides)
        {
            if (value < 3 || Math.Abs(value - Math.Round(value)) > 0.000001)
            {
                message = "Sides must be a whole number greater than or equal to 3.";
                return false;
            }

            return true;
        }

        if (IsPositiveCommandHudField(fieldKind) && value <= 0)
        {
            message = $"{fieldKind} must be greater than zero.";
            return false;
        }

        return true;
    }

    private static bool IsPositiveCommandHudField(CommandHudFieldKind fieldKind)
    {
        return fieldKind is
            CommandHudFieldKind.Width or
            CommandHudFieldKind.Height or
            CommandHudFieldKind.Radius or
            CommandHudFieldKind.Factor;
    }

    private bool TryHandleDedicatedScalarCommandHudOverride(
        CommandHudFieldKind fieldKind,
        double value,
        string input,
        bool confirm,
        out ToolResult result)
    {
        result = ToolResult.None();


        if (Workspace.ToolController.ActiveTool is PolygonTool { State: PolygonToolState.WaitingForSides } &&
            fieldKind == CommandHudFieldKind.Sides)
        {
            if (!confirm)
            {
                NotifyPointerDrivenStateChanged();
                return true;
            }

            result = SubmitCommandInput(((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture));
            ClearCommandHudInputOverrides();
            NotifyCommandInputStateChanged();
            return true;
        }

        if (Workspace.ToolController.ActiveTool is RotateTool { State: RotateToolState.WaitingForDestinationPoint } &&
            fieldKind == CommandHudFieldKind.Angle)
        {
            if (!confirm)
            {
                NotifyPointerDrivenStateChanged();
                return true;
            }

            result = SubmitCommandInput(FormatHudNumber(value));
            ClearCommandHudInputOverrides();
            NotifyCommandInputStateChanged();
            return true;
        }

        if (Workspace.ToolController.ActiveTool is ScaleTool { State: ScaleToolState.WaitingForDestinationPoint } &&
            fieldKind == CommandHudFieldKind.Factor)
        {
            if (!confirm)
            {
                NotifyPointerDrivenStateChanged();
                return true;
            }

            result = SubmitCommandInput(FormatHudNumber(value));
            ClearCommandHudInputOverrides();
            NotifyCommandInputStateChanged();
            return true;
        }

        if (Workspace.ToolController.ActiveTool is RectangleBySidesTool { State: RectangleBySidesToolState.WaitingForSecondSidePoint } &&
            fieldKind == CommandHudFieldKind.Height)
        {
            if (!confirm)
            {
                NotifyPointerDrivenStateChanged();
                return true;
            }

            result = SubmitCommandInput(FormatHudNumber(value));
            ClearCommandHudInputOverrides();
            NotifyCommandInputStateChanged();
            return true;
        }

        if (Workspace.ToolController.ActiveTool is OffsetTool { State: OffsetToolState.WaitingForDistance or OffsetToolState.WaitingForDistanceSecondPoint } &&
            fieldKind == CommandHudFieldKind.Distance)
        {
            if (!confirm)
            {
                NotifyPointerDrivenStateChanged();
                return true;
            }

            result = SubmitCommandInput(FormatHudNumber(value));
            ClearCommandHudInputOverrides();
            NotifyCommandInputStateChanged();
            return true;
        }

        if (Workspace.ToolController.ActiveTool is FilletTool { State: FilletToolState.WaitingForRadius } &&
            fieldKind == CommandHudFieldKind.Radius)
        {
            if (!confirm)
            {
                NotifyPointerDrivenStateChanged();
                return true;
            }

            result = SubmitCommandInput(FormatHudNumber(value));
            ClearCommandHudInputOverrides();
            NotifyCommandInputStateChanged();
            return true;
        }

        if (Workspace.ToolController.ActiveTool is ChamferTool { State: ChamferToolState.WaitingForDistance } &&
            fieldKind == CommandHudFieldKind.Distance)
        {
            if (!confirm)
            {
                NotifyPointerDrivenStateChanged();
                return true;
            }

            result = SubmitCommandInput(FormatHudNumber(value));
            ClearCommandHudInputOverrides();
            NotifyCommandInputStateChanged();
            return true;
        }

        return false;
    }

    private static string FormatHudNumber(double value)
    {
        return value.ToString("0.############", CultureInfo.InvariantCulture);
    }


    private bool IsCommandHudPointOverrideTargetActive()
    {
        ICadTool activeTool = Workspace.ToolController.ActiveTool;

        if (activeTool is LineTool { State: TwoPointToolState.WaitingForSecondPoint } ||
            activeTool is PolylineTool { State: PolylineToolState.CollectingVertices } ||
            activeTool is MoveTool { MoveState: MoveToolState.WaitingForDestinationPoint } ||
            activeTool is CopyTool { CopyState: MoveToolState.WaitingForDestinationPoint } ||
            activeTool is MeasureDistanceTool { State: TwoPointToolState.WaitingForSecondPoint } ||
            activeTool is MirrorTool { State: MirrorToolState.WaitingForSecondAxisPoint } ||
            activeTool is BreakBetweenPointsTool { State: BreakBetweenPointsToolState.WaitingForSecondBreakPoint } ||
            activeTool is RectangleBySidesTool { State: RectangleBySidesToolState.WaitingForFirstSideEndPoint } ||
            activeTool is EllipseTool { State: EllipseToolState.WaitingForMajorAxis } ||
            activeTool is OffsetTool { State: OffsetToolState.WaitingForDistanceSecondPoint } ||
            activeTool is RotateTool { State: RotateToolState.WaitingForReferencePoint } ||
            activeTool is ScaleTool { State: ScaleToolState.WaitingForReferencePoint })
        {
            return true;
        }

        return IsPolarPointExpectedInput(GetCurrentPromptState().ExpectedInput) &&
               Workspace.Context.CurrentBasePoint is not null;
    }

    private static bool IsPolarPointExpectedInput(CommandInputKind expectedInput)
    {
        return expectedInput is
            CommandInputKind.PointOrDistance or
            CommandInputKind.PointOrDistanceOrOption or
            CommandInputKind.PointOrAngle or
            CommandInputKind.PointOrAngleOrOption;
    }

    private bool IsCommandHudPointInputTargetActive()
    {
        if (IsCreateBlockBasePointPickPending ||
            IsBlockInsertionPending ||
            IsLibraryInsertionPending ||
            IsImportDrawingPlacementPending)
        {
            return true;
        }

        if (IsCommandHudPointOverrideTargetActive())
        {
            return true;
        }

        if (Workspace.ToolController.ActiveTool is not ICommandDrivenTool commandDrivenTool)
        {
            return false;
        }

        return IsPointExpectedInput(
            commandDrivenTool.GetPromptState(Workspace.Context).ExpectedInput);
    }

    private static bool IsPointExpectedInput(CommandInputKind expectedInput)
    {
        return expectedInput is
            CommandInputKind.Point or
            CommandInputKind.PointOrOption or
            CommandInputKind.PointOrDistance or
            CommandInputKind.PointOrDistanceOrOption or
            CommandInputKind.PointOrAngle or
            CommandInputKind.PointOrAngleOrOption or
            CommandInputKind.PointOrNumber or
            CommandInputKind.PointOrNumberOrOption;
    }

    private bool TrySubmitPendingPlacementHudPoint(
        Point2D worldPoint,
        out ToolResult result)
    {
        if (IsCreateBlockBasePointPickPending)
        {
            result = CommitCreateBlockBasePointPick(worldPoint);
            return true;
        }

        if (IsBlockInsertionPending)
        {
            result = CommitPendingBlockInsertion(worldPoint);
            return true;
        }

        if (IsLibraryInsertionPending)
        {
            result = CommitPendingLibraryInsertion(worldPoint);
            return true;
        }

        if (IsImportDrawingPlacementPending)
        {
            result = CommitPendingImportDrawing(worldPoint);
            return true;
        }

        result = ToolResult.None();
        return false;
    }

    private bool TryResolveCommandHudSizeOverridePoint(
        bool requireCompleteSize,
        out Point2D worldPoint,
        out string? errorMessage)
    {
        worldPoint = Point2D.Origin;
        errorMessage = null;

        var rectangleTool = Workspace.ToolController.ActiveTool as RectangleTool;

        if (rectangleTool is null ||
            rectangleTool.State != TwoPointToolState.WaitingForSecondPoint ||
            rectangleTool.FirstPoint is null)
        {
            return false;
        }

        if (requireCompleteSize &&
            (_commandHudInputState.Width is null || _commandHudInputState.Height is null))
        {
            errorMessage = "Enter both Width and Height before confirming the rectangle.";
            return false;
        }

        Point2D firstWorldPoint = rectangleTool.FirstPoint.Value;
        Point2D firstUserPoint = Workspace.CurrentUcs.WorldToUser(firstWorldPoint);
        Point2D liveUserPoint = GetLivePointerUserPoint();

        double liveDeltaX = liveUserPoint.X - firstUserPoint.X;
        double liveDeltaY = liveUserPoint.Y - firstUserPoint.Y;

        double width = _commandHudInputState.Width ?? Math.Abs(liveDeltaX);
        double height = _commandHudInputState.Height ?? Math.Abs(liveDeltaY);

        if (width <= 0 || height <= 0)
        {
            errorMessage = "Move the cursor or enter both Width and Height before confirming the rectangle.";
            return false;
        }

        double xSign = liveDeltaX < 0 ? -1.0 : 1.0;
        double ySign = liveDeltaY < 0 ? -1.0 : 1.0;

        var oppositeUserPoint = new Point2D(
            firstUserPoint.X + width * xSign,
            firstUserPoint.Y + height * ySign);

        worldPoint = Workspace.CurrentUcs.UserToWorld(oppositeUserPoint);
        return true;
    }

    private bool TryResolveCommandHudOverridePoint(
        bool requireCompleteCoordinates,
        out Point2D worldPoint,
        out string? errorMessage)
    {
        worldPoint = Point2D.Origin;
        errorMessage = null;

        if (!IsCommandHudPointInputTargetActive())
        {
            errorMessage = "The active command is not waiting for point input.";
            return false;
        }

        if (_commandHudInputState.HasSizeOverride &&
            TryResolveCommandHudSizeOverridePoint(
                requireCompleteCoordinates,
                out worldPoint,
                out errorMessage))
        {
            return true;
        }

        if (_commandHudInputState.HasCoordinateOverride)
        {
            Point2D liveUserPoint = GetLivePointerUserPoint();

            if (requireCompleteCoordinates &&
                (_commandHudInputState.X is null || _commandHudInputState.Y is null))
            {
                errorMessage = "Enter both X and Y before confirming the point.";
                return false;
            }

            double x = _commandHudInputState.X ?? liveUserPoint.X;
            double y = _commandHudInputState.Y ?? liveUserPoint.Y;
            worldPoint = Workspace.CurrentUcs.UserToWorld(new Point2D(x, y));
            return true;
        }

        if (TryResolveDedicatedCommandHudOverridePoint(
                out worldPoint,
                out errorMessage))
        {
            return true;
        }

        if (_commandHudInputState.HasSizeOverride)
        {
            errorMessage = "The active command cannot resolve size input.";
            return false;
        }

        if (!IsCommandHudPointOverrideTargetActive())
        {
            errorMessage = "Distance/angle input requires a base point.";
            return false;
        }

        CommandLiveMeasurement measurement = GetLiveMeasurement();
        double distance = _commandHudInputState.Distance ?? measurement.Distance ?? 0.0;
        double angleDegrees = _commandHudInputState.AngleDegrees ?? measurement.AngleDegrees ?? 0.0;

        if (distance <= 0)
        {
            errorMessage = "Move the cursor or enter a distance before confirming the point.";
            return false;
        }

        return TryResolveDistanceAnglePoint(
            distance,
            angleDegrees,
            out worldPoint,
            out errorMessage);
    }


    private bool TryResolveDedicatedCommandHudOverridePoint(
        out Point2D worldPoint,
        out string? errorMessage)
    {
        worldPoint = Point2D.Origin;
        errorMessage = null;

        if (Workspace.ToolController.ActiveTool is ArcTool
            {
                State: ArcToolState.WaitingForEndPoint,
                CenterPoint: { } centerPoint,
                StartPoint: { } startPoint
            } &&
            _commandHudInputState.AngleDegrees is not null)
        {
            double radius = centerPoint.DistanceTo(startPoint);
            if (radius <= 0)
            {
                errorMessage = "Arc radius must be greater than zero.";
                return false;
            }

            return TryResolveDistanceAnglePointFromBasePoint(
                centerPoint,
                radius,
                _commandHudInputState.AngleDegrees.Value,
                out worldPoint,
                out errorMessage);
        }

        if (Workspace.ToolController.ActiveTool is EllipseTool
            {
                State: EllipseToolState.WaitingForMinorRadius,
                Center: { } ellipseCenter,
                MajorAxisPoint: { } majorAxisPoint
            } &&
            _commandHudInputState.Distance is not null)
        {
            Vector2D majorAxis = ellipseCenter.VectorTo(majorAxisPoint);
            if (majorAxis.Length <= 0)
            {
                errorMessage = "Ellipse major radius must be greater than zero.";
                return false;
            }

            Vector2D perpendicular = new Vector2D(-majorAxis.Y, majorAxis.X).Normalize();
            Vector2D liveSide = ellipseCenter.VectorTo(_currentSnapCandidate?.Point ?? _mousePosition);
            if (perpendicular.Dot(liveSide) < 0)
            {
                perpendicular = perpendicular * -1.0;
            }

            worldPoint = ellipseCenter + perpendicular * _commandHudInputState.Distance.Value;
            return true;
        }

        return false;
    }

    private void ApplyCommandHudInputOverridesToPreview()
    {
        if (!_commandHudInputState.HasAnyOverride)
        {
            return;
        }

        if (!IsCommandHudPointInputTargetActive())
        {
            ClearCommandHudInputOverrides();
            return;
        }

        if (TryResolveCommandHudOverridePoint(
                requireCompleteCoordinates: false,
                out Point2D worldPoint,
                out _))
        {
            Workspace.PreviewPointFromCommandLine(worldPoint);
        }
    }

    public void CancelCommandHudInputOverrides()
    {
        ClearCommandHudInputOverrides();
        NotifyPointerDrivenStateChanged();
    }

    private void ClearCommandHudInputOverrides()
    {
        _commandHudInputState.Clear();
        CommandHudInputOverridesCleared?.Invoke(this, EventArgs.Empty);
    }

    private static bool TryParseHudDouble(
        string input,
        out double value)
    {
        string normalized = input.Trim().Replace(',', '.');

        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private void FreezeComplementaryPolarHudValueIfNeeded(CommandHudFieldKind fieldKind)
    {
        if (!IsCommandHudPointOverrideTargetActive())
        {
            return;
        }

        CommandLiveMeasurement measurement = GetLiveMeasurement();

        if (fieldKind == CommandHudFieldKind.Distance &&
            _commandHudInputState.AngleDegrees is null &&
            measurement.AngleDegrees is not null)
        {
            _commandHudInputState.AngleDegrees = measurement.AngleDegrees;
        }
        else if (fieldKind == CommandHudFieldKind.Angle &&
                 _commandHudInputState.Distance is null &&
                 measurement.Distance is not null)
        {
            _commandHudInputState.Distance = measurement.Distance;
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

        return TryResolveDistanceAnglePointFromBasePoint(
            Workspace.Context.CurrentBasePoint.Value,
            distance,
            angleDegrees,
            out worldPoint,
            out errorMessage);
    }

    private bool TryResolveDistanceAnglePointFromBasePoint(
        Point2D basePoint,
        double distance,
        double angleDegrees,
        out Point2D worldPoint,
        out string? errorMessage)
    {
        worldPoint = Point2D.Origin;
        errorMessage = null;

        double radians = angleDegrees * Math.PI / 180.0;
        var userOffset = new Vector2D(
            distance * Math.Cos(radians),
            distance * Math.Sin(radians));

        Vector2D worldOffset = Workspace.CurrentUcs.UserVectorToWorld(userOffset);
        worldPoint = basePoint + worldOffset;
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
            nameof(HasSingleSelectedImageReference),
            nameof(MissingImageReferenceCount),
            nameof(HasMissingImageReferences),
            nameof(IsDirty),
            nameof(TitleText),
            nameof(FileStatusText),
            nameof(LastMessage),
            nameof(StatusText));
    }

    public void SetMousePosition(Point2D point)
    {
        _mousePosition = point;

        ApplyCommandHudInputOverridesToPreview();
        NotifyPointerDrivenStateChanged(nameof(MousePositionText));
    }

    public void SetHudScreenPosition(Point? point)
    {
        _hudScreenPosition = point;

        OnPropertiesChanged(
            nameof(HudScreenPosition),
            nameof(CommandHudState),
            nameof(IsBottomCommandLineVisible));
    }

    public void SetCurrentSnapCandidate(SnapCandidate? candidate)
    {
        _currentSnapCandidate = candidate;

        ApplyCommandHudInputOverridesToPreview();
        NotifyPointerDrivenStateChanged(nameof(SnapText));
    }

    private void NotifyPointerDrivenStateChanged(params string[] additionalPropertyNames)
    {
        OnPropertiesChanged(
            additionalPropertyNames
                .Concat(new[]
                {
                    nameof(HasLiveMeasurements),
                    nameof(LiveDistance),
                    nameof(LiveAngle),
                    nameof(LiveDeltaX),
                    nameof(LiveDeltaY),
                    nameof(MeasurementText),
                    nameof(CommandHudState),
                    nameof(StatusText)
                })
                .Distinct()
                .ToArray());
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
        Workspace.MarkDocumentChanged();

        SetMessage($"Current layer changed to '{layer.Name}'.");

        NotifyLayerStateChanged();
        NotifyFileStateChanged();
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

    public ToolResult DeselectAll()
    {
        ToolResult result = Workspace.ActionController.DeselectAll();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifySelectionStateChanged();

        return result;
    }

    public ToolResult BringSelectionToFront()
    {
        ToolResult result = Workspace.ActionController.BringSelectionToFront();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult SendSelectionToBack()
    {
        ToolResult result = Workspace.ActionController.SendSelectionToBack();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult BringSelectionForward()
    {
        ToolResult result = Workspace.ActionController.BringSelectionForward();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult SendSelectionBackward()
    {
        ToolResult result = Workspace.ActionController.SendSelectionBackward();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }


    public ToolResult AlignSelectionLeft()
    {
        ToolResult result = Workspace.ActionController.AlignSelectionLeft();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult AlignSelectionRight()
    {
        ToolResult result = Workspace.ActionController.AlignSelectionRight();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult AlignSelectionTop()
    {
        ToolResult result = Workspace.ActionController.AlignSelectionTop();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult AlignSelectionBottom()
    {
        ToolResult result = Workspace.ActionController.AlignSelectionBottom();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }



    public ToolResult DistributeSelectionHorizontally()
    {
        ToolResult result = Workspace.ActionController.DistributeSelectionHorizontally();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult DistributeSelectionVertically()
    {
        ToolResult result = Workspace.ActionController.DistributeSelectionVertically();

        SetLastResult(result);
        RefreshPropertyPanel();
        NotifyDocumentStateChanged();

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

            case "DESELECT":
            case "CLEARSELECTION":
            case "CS":
                result = DeselectAll();
                return true;

            case "BRINGTOFRONT":
            case "BTF":
            case "FRONT":
                result = BringSelectionToFront();
                return true;

            case "SENDTOBACK":
            case "STB":
            case "BACK":
                result = SendSelectionToBack();
                return true;

            case "BRINGFORWARD":
            case "BF":
            case "FORWARD":
                result = BringSelectionForward();
                return true;

            case "SENDBACKWARD":
            case "SB":
            case "BACKWARD":
                result = SendSelectionBackward();
                return true;

            case "ALIGNLEFT":
            case "ALEFT":
                result = AlignSelectionLeft();
                return true;

            case "ALIGNRIGHT":
            case "ARIGHT":
                result = AlignSelectionRight();
                return true;

            case "ALIGNTOP":
            case "ATOP":
                result = AlignSelectionTop();
                return true;

            case "ALIGNBOTTOM":
            case "ABOTTOM":
                result = AlignSelectionBottom();
                return true;

            case "DISTRIBUTEHORIZONTAL":
            case "DISTRIBUTEHORIZONTALLY":
            case "DH":
                result = DistributeSelectionHorizontally();
                return true;

            case "DISTRIBUTEVERTICAL":
            case "DISTRIBUTEVERTICALLY":
            case "DV":
                result = DistributeSelectionVertically();
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
            nameof(CurrentPromptState),
            nameof(CommandHudState),
            nameof(IsCommandHudVisible),
            nameof(IsBottomCommandLineVisible),
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

        ClearBlockEditSessionAfterHistoryNavigation();
        Workspace.EnsureCurrentLayerIsUsable();
        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    public ToolResult Redo()
    {
        ToolResult result = Workspace.ActionController.Redo();

        ClearBlockEditSessionAfterHistoryNavigation();
        Workspace.EnsureCurrentLayerIsUsable();
        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }

    private void ClearBlockEditSessionAfterHistoryNavigation()
    {
        if (_activeBlockEditSession is null)
        {
            return;
        }

        _activeBlockEditSession = null;
        OnPropertiesChanged(nameof(IsBlockEditSessionActive), nameof(ActiveBlockEditText));
    }

    public ToolResult DeleteSelection()
    {
        ToolResult result;

        if (Workspace.SelectionSet.IsEmpty)
        {
            result = Workspace.SetActiveTool(ToolId.Delete);
            result = ToolResult.Started("Delete tool started. Select entities to delete, then press Enter or right-click.");
        }
        else
        {
            result = Workspace.ActionController.DeleteSelection();
        }

        SetLastResult(result);
        NotifyDocumentStateChanged();
        NotifyCommandInputStateChanged();

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
        ClearCommandHudInputOverrides();

        if (IsCreateBlockBasePointPickPending)
        {
            return CancelCreateBlockBasePointPick();
        }

        if (IsBlockInsertionPending)
        {
            return CancelPendingBlockInsertion();
        }

        if (IsLibraryInsertionPending)
        {
            return CancelPendingLibraryInsertion();
        }

        if (IsImportDrawingPlacementPending)
        {
            return CancelPendingImportDrawing();
        }

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

        Workspace.MarkDocumentChanged();
        CaptureAndSaveDraftingDefaults();

        SetMessage($"Snap settings updated: {Workspace.Context.EnabledSnaps}");

        OnPropertiesChanged(
            nameof(SnapText),
            nameof(StatusText),
            nameof(IsDirty),
            nameof(TitleText),
            nameof(FileStatusText));
    }



    public ToolResult ApplyLineFormatChanges(IEnumerable<LineFormat> lineFormats)
    {
        ToolResult result = Workspace.ApplyLineFormatChanges(lineFormats);

        SetLastResult(result);
        NotifyLayerStateChanged();
        NotifyDocumentStateChanged();

        return result;
    }


    public ToolResult ApplyBlockDefinitionChanges(IEnumerable<BlockDefinition> blockDefinitions)
    {
        ToolResult result = Workspace.ApplyBlockDefinitionChanges(blockDefinitions);

        SetLastResult(result);
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


    public ToolResult ApplyDimensionStyleChanges(
        IEnumerable<DimensionStyle> dimensionStyles,
        DimensionStyleId currentDimensionStyleId)
    {
        ToolResult result = Workspace.ApplyDimensionStyleChanges(
            dimensionStyles,
            currentDimensionStyleId);

        SetLastResult(result);
        NotifyDocumentStateChanged();

        return result;
    }


    public ToolResult ApplyGridSettings(GridSettings gridSettings)
    {
        ToolResult result = Workspace.SetGridSettings(gridSettings);
        Workspace.MarkDocumentChanged();
        CaptureAndSaveDraftingDefaults();

        SetLastResult(result);
        OnPropertiesChanged(
            nameof(StatusText),
            nameof(SnapText),
            nameof(IsDirty),
            nameof(TitleText),
            nameof(FileStatusText));

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

        Workspace.MarkDocumentChanged();

        SetMessage(isEnabled
            ? "Ortho mode enabled."
            : "Ortho mode disabled.");

        OnPropertiesChanged(
            nameof(IsOrthoEnabled),
            nameof(SelectedPolarTrackingOption),
            nameof(PolarTrackingText),
            nameof(MeasurementText),
            nameof(StatusText),
            nameof(IsDirty),
            nameof(TitleText),
            nameof(FileStatusText));
    }

    public void SetPolarTracking(PolarTrackingOptionViewModel option)
    {
        ArgumentNullException.ThrowIfNull(option);

        _selectedPolarTrackingOption = option;
        Workspace.Context.IsOrthoEnabled = false;
        Workspace.AngleConstraintSettings = option.Settings;
        Workspace.MarkDocumentChanged();

        SetMessage(option.IsOff
            ? "Polar tracking disabled."
            : $"Polar tracking set to {option.StepDegrees:0.###}°.");

        OnPropertiesChanged(
            nameof(IsOrthoEnabled),
            nameof(SelectedPolarTrackingOption),
            nameof(PolarTrackingText),
            nameof(MeasurementText),
            nameof(StatusText),
            nameof(IsDirty),
            nameof(TitleText),
            nameof(FileStatusText));
    }

    public void NotifyDocumentStateChanged()
    {
        RefreshPropertyPanel();

        OnPropertiesChanged(
            nameof(StatusText),
            nameof(EntityCount),
            nameof(SelectedCount),
            nameof(ActiveToolName),
            nameof(CurrentPromptState),
            nameof(CommandHudState),
            nameof(IsCommandHudVisible),
            nameof(IsBottomCommandLineVisible),
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
            nameof(HasSingleSelectedImageReference),
            nameof(MissingImageReferenceCount),
            nameof(HasMissingImageReferences),
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

    private readonly record struct CommandLiveMeasurement(
        double? Distance,
        double? AngleDegrees,
        double? DeltaX,
        double? DeltaY)
    {
        public static CommandLiveMeasurement Empty { get; } = new(null, null, null, null);

        public bool HasValue => Distance is not null;
    }

    private void NotifySelectionStateChanged()
    {
        OnPropertiesChanged(
            nameof(SelectedCount),
            nameof(HasSingleSelectedImageReference),
            nameof(PropertyPanel),
            nameof(LastMessage),
            nameof(StatusText));
    }

    private void NotifyCommandInputStateChanged()
    {
        OnPropertiesChanged(
            nameof(CommandPromptText),
            nameof(CommandInputPlaceholderText),
            nameof(CurrentPromptState),
            nameof(CommandHudState),
            nameof(IsCommandHudVisible),
            nameof(IsBottomCommandLineVisible),
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
            nameof(CurrentPromptState),
            nameof(CommandHudState),
            nameof(IsCommandHudVisible),
            nameof(IsBottomCommandLineVisible),
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

    private DocumentSettingsDto BuildCurrentDocumentSettings()
    {
        GridSettings grid = Workspace.GridSettings;
        AngleConstraintSettings polar = Workspace.AngleConstraintSettings;

        return new DocumentSettingsDto
        {
            CurrentLayerId = Workspace.CurrentLayerId.Value,
            CurrentTextFormatId = Workspace.Context.Creation.CurrentTextFormatId.Value,
            Grid = new DocumentGridSettingsDto
            {
                Kind = grid.Kind.ToString(),
                IsVisible = grid.IsVisible,
                MinorStep = grid.MinorStep,
                MajorStep = grid.MajorStep,
                OriginX = grid.OriginX,
                OriginY = grid.OriginY,
                MinimumScreenSpacing = grid.MinimumScreenSpacing,
                MaximumScreenSpacing = grid.MaximumScreenSpacing,
                IsometricAngleDegrees = grid.IsometricAngleDegrees
            },
            Snapping = new DocumentSnapSettingsDto
            {
                IsEnabled = Workspace.Context.EnabledSnaps != SnapKind.None,
                EnabledModes = GetEnabledSnapModeNames(Workspace.Context.EnabledSnaps).ToList(),
                Tolerance = Workspace.Context.SnapTolerance
            },
            Drafting = new DocumentDraftingSettingsDto
            {
                IsOrthoEnabled = Workspace.Context.IsOrthoEnabled,
                PolarTracking = new DocumentPolarTrackingSettingsDto
                {
                    IsEnabled = polar.IsEnabled,
                    StepDegrees = polar.StepDegrees
                }
            }
        };
    }

    private void ApplyDocumentSettings(DocumentSettingsDto? settings)
    {
        if (settings is null)
        {
            return;
        }

        Workspace.SetGridSettings(CreateGridSettings(settings.Grid));
        Workspace.Context.EnabledSnaps = CreateSnapKind(settings.Snapping);
        Workspace.Context.SnapTolerance = GetPositiveOrDefault(settings.Snapping?.Tolerance, Workspace.Context.SnapTolerance);
        Workspace.Context.IsOrthoEnabled = settings.Drafting?.IsOrthoEnabled == true;
        Workspace.AngleConstraintSettings = CreateAngleConstraintSettings(settings.Drafting?.PolarTracking);
        _selectedPolarTrackingOption = ResolvePolarTrackingOption(Workspace.AngleConstraintSettings);

        if (!string.IsNullOrWhiteSpace(settings.CurrentTextFormatId))
        {
            var textFormatId = new TextFormatId(settings.CurrentTextFormatId);
            if (Workspace.Document.TextFormats.Contains(textFormatId))
            {
                Workspace.Context.Creation.CurrentTextFormatId = textFormatId;
            }
        }
    }

    private static GridSettings CreateGridSettings(DocumentGridSettingsDto? dto)
    {
        if (dto is null)
        {
            return new GridSettings();
        }

        GridKind kind = Enum.TryParse(dto.Kind, ignoreCase: true, out GridKind parsedKind)
            ? parsedKind
            : GridKind.Rectangular;

        try
        {
            return new GridSettings(
                step: GetPositiveOrDefault(dto.MinorStep, 10),
                originX: dto.OriginX,
                originY: dto.OriginY,
                isVisible: dto.IsVisible,
                majorStep: GetPositiveOrDefault(dto.MajorStep, Math.Max(dto.MinorStep, 10)),
                minimumScreenSpacing: GetPositiveOrDefault(dto.MinimumScreenSpacing, 8),
                maximumScreenSpacing: GetPositiveOrDefault(dto.MaximumScreenSpacing, 220),
                kind: kind,
                isometricAngleDegrees: GetPositiveOrDefault(dto.IsometricAngleDegrees, 30));
        }
        catch (ArgumentOutOfRangeException)
        {
            return new GridSettings();
        }
    }

    private static SnapKind CreateSnapKind(DocumentSnapSettingsDto? dto)
    {
        if (dto is null)
        {
            return SnapKind.Endpoint |
                   SnapKind.Midpoint |
                   SnapKind.Center |
                   SnapKind.Quadrant |
                   SnapKind.Intersection |
                   SnapKind.Perpendicular |
                   SnapKind.Tangent |
                   SnapKind.Grid;
        }

        if (!dto.IsEnabled)
        {
            return SnapKind.None;
        }

        SnapKind result = SnapKind.None;

        foreach (string mode in dto.EnabledModes ?? Enumerable.Empty<string>())
        {
            if (Enum.TryParse(mode, ignoreCase: true, out SnapKind parsed) &&
                parsed != SnapKind.Entity &&
                parsed != SnapKind.EntityOnly)
            {
                result |= parsed;
            }
        }

        return result;
    }

    private static IEnumerable<string> GetEnabledSnapModeNames(SnapKind snapKind)
    {
        SnapKind[] modes =
        {
            SnapKind.Endpoint,
            SnapKind.Midpoint,
            SnapKind.Center,
            SnapKind.Quadrant,
            SnapKind.Intersection,
            SnapKind.Nearest,
            SnapKind.Perpendicular,
            SnapKind.Tangent,
            SnapKind.Grid
        };

        foreach (SnapKind mode in modes)
        {
            if (snapKind.HasFlag(mode))
            {
                yield return mode.ToString();
            }
        }
    }

    private static AngleConstraintSettings CreateAngleConstraintSettings(DocumentPolarTrackingSettingsDto? dto)
    {
        if (dto is null || !dto.IsEnabled)
        {
            return AngleConstraintSettings.Off;
        }

        try
        {
            return AngleConstraintSettings.FromStep(dto.StepDegrees);
        }
        catch (ArgumentOutOfRangeException)
        {
            return AngleConstraintSettings.Off;
        }
    }

    private PolarTrackingOptionViewModel ResolvePolarTrackingOption(AngleConstraintSettings settings)
    {
        PolarTrackingOptionViewModel? matching = PolarTrackingOptions.FirstOrDefault(option =>
            option.Settings.IsEnabled == settings.IsEnabled &&
            Math.Abs(option.Settings.StepDegrees - settings.StepDegrees) < 0.000001);

        return matching ?? PolarTrackingOptions[0];
    }

    private static double GetPositiveOrDefault(
        double? value,
        double fallback)
    {
        return value is > 0 && double.IsFinite(value.Value)
            ? value.Value
            : fallback;
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
            ApplyDocumentSettings(dto.Settings);
            ApplyApplicationDraftingDefaults();

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
        ApplyApplicationDraftingDefaults();
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


    private void ApplyApplicationDraftingDefaults()
    {
        if (_applicationSettings.DefaultGrid is not null)
        {
            Workspace.SetGridSettings(_applicationSettings.DefaultGrid.ToGridSettings());
        }

        if (_applicationSettings.DefaultSnapping is not null)
        {
            Workspace.Context.EnabledSnaps = _applicationSettings.DefaultSnapping.ToSnapKind();
            Workspace.Context.SnapTolerance = GetPositiveOrDefault(
                _applicationSettings.DefaultSnapping.Tolerance,
                Workspace.Context.SnapTolerance);
        }
    }

    private void CaptureAndSaveDraftingDefaults()
    {
        _applicationSettings.CaptureDraftingDefaults(
            Workspace.GridSettings,
            Workspace.Context.EnabledSnaps,
            Workspace.Context.SnapTolerance);
        SaveApplicationSettings();
    }

    private void RegisterOpenedFile(string filePath)
    {
        _applicationSettings.RegisterOpenedFile(filePath);
        SaveApplicationSettings();
    }

    private void RegisterSavedFile(string filePath)
    {
        _applicationSettings.RegisterSavedFile(filePath);
        SaveApplicationSettings();
    }

    private void RegisterExportedFile(string filePath)
    {
        _applicationSettings.RegisterExportedFile(filePath);
        SaveApplicationSettings();
    }

    private void SaveApplicationSettings()
    {
        try
        {
            _applicationSettingsStore.Save(_applicationSettings);
        }
        catch (IOException)
        {
            // Local settings must never block drawing workflows.
        }
        catch (UnauthorizedAccessException)
        {
            // Local settings must never block drawing workflows.
        }

        OnPropertiesChanged(nameof(ApplicationSettings), nameof(RecentFiles));
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

    private sealed class PendingOpenCad2DImport
    {
        public PendingOpenCad2DImport(
            string filePath,
            DocumentRecoveryResult recovery,
            OpenCad2DImportPlacementOptions options)
        {
            FilePath = filePath;
            Recovery = recovery;
            Options = options;
        }

        public string FilePath { get; }

        public DocumentRecoveryResult Recovery { get; }

        public OpenCad2DImportPlacementOptions Options { get; }
    }

    private sealed class PendingLibraryBlockInsertion
    {
        public PendingLibraryBlockInsertion(
            LibraryCatalogItem item,
            LibraryBlockDefinitionPreparation preparation)
        {
            Item = item;
            Preparation = preparation;
        }

        public LibraryCatalogItem Item { get; }

        public LibraryBlockDefinitionPreparation Preparation { get; }
    }

}
