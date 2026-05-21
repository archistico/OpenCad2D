using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Coordinates;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Provides the runtime services required by CAD tools.
/// Keep this class small: group related state into focused sub-contexts.
/// </summary>
public sealed class ToolContext
{
    public ToolContext(
        CadDocument document,
        CommandHistory commandHistory,
        SnapService snapService,
        SelectionSet? selectionSet = null,
        SelectionService? selectionService = null,
        GridSettings? gridSettings = null,
        LayerId? currentLayerId = null,
        DimensionStyleId? currentDimensionStyleId = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0,
        double selectionTolerance = 5,
        double selectionDragThreshold = 1,
        AngleConstraintSettings? angleConstraintSettings = null,
        CoordinateSystem2D? currentUcs = null,
        GeometryTolerance? geometryTolerance = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(commandHistory);
        ArgumentNullException.ThrowIfNull(snapService);

        Document = document;

        Commands = new ToolCommandContext(commandHistory);

        Selection = new ToolSelectionContext(
            selectionSet ?? new SelectionSet(),
            selectionService ?? new SelectionService(),
            selectionTolerance,
            selectionDragThreshold);

        Snapping = new ToolSnapContext(
            snapService,
            enabledSnaps,
            snapTolerance,
            gridSettings ?? new GridSettings());

        Coordinates = new ToolCoordinateContext(
            currentUcs ?? CoordinateSystem2D.World,
            geometryTolerance ?? GeometryTolerance.Default);

        Creation = new ToolCreationContext(
            currentLayerId ?? LayerId.Default,
            currentDimensionStyleId ?? document.CurrentDimensionStyleId);

        AngleConstraintSettings = angleConstraintSettings ?? AngleConstraintSettings.Off;
    }

    public CadDocument Document { get; internal set; }

    public ToolCommandContext Commands { get; }

    public ToolSelectionContext Selection { get; }

    public ToolSnapContext Snapping { get; }

    public ToolCoordinateContext Coordinates { get; }

    public ToolCreationContext Creation { get; }

    /// <summary>
    /// Current point from which command-line relative coordinates and direct distance entry are measured.
    /// Tools that collect a first point should set this value when that first point is accepted.
    /// </summary>
    public Point2D? CurrentBasePoint { get; set; }

    /// <summary>
    /// When enabled, second points for two-point tools are constrained to the
    /// closest horizontal or vertical direction from the current base point.
    /// </summary>
    public bool IsOrthoEnabled { get; set; }

    /// <summary>
    /// Gets or sets polar tracking settings. The constraint is applied after snap resolution.
    /// </summary>
    public AngleConstraintSettings AngleConstraintSettings { get; set; }

    /*
     * Compatibility properties.
     * Keep them temporarily so existing tools continue to compile.
     * New code should prefer Commands, Selection, Snapping, Coordinates and Creation.
     */
    public CommandHistory CommandHistory => Commands.History;

    public SnapService SnapService => Snapping.Service;

    public SelectionSet SelectionSet => Selection.Set;

    public SelectionService SelectionService => Selection.Service;

    public GridSettings GridSettings => Snapping.GridSettings;

    public LayerId CurrentLayerId
    {
        get => Creation.CurrentLayerId;
        set => Creation.CurrentLayerId = value;
    }

    public DimensionStyleId CurrentDimensionStyleId
    {
        get => Creation.CurrentDimensionStyleId;
        set => Creation.CurrentDimensionStyleId = value;
    }

    public SnapKind EnabledSnaps
    {
        get => Snapping.EnabledSnaps;
        set => Snapping.EnabledSnaps = value;
    }

    public double SnapTolerance
    {
        get => Snapping.Tolerance;
        set => Snapping.Tolerance = value;
    }

    public double SelectionTolerance
    {
        get => Selection.Tolerance;
        set => Selection.Tolerance = value;
    }

    public double SelectionDragThreshold
    {
        get => Selection.DragThreshold;
        set => Selection.DragThreshold = value;
    }

    public CoordinateSystem2D CurrentUcs
    {
        get => Coordinates.CurrentUcs;
        set => Coordinates.CurrentUcs = value;
    }

    public GeometryTolerance GeometryTolerance
    {
        get => Coordinates.GeometryTolerance;
        set => Coordinates.GeometryTolerance = value;
    }
}
