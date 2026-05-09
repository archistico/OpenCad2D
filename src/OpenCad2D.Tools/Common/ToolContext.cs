using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Geometry.Coordinates;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Provides shared services and state required by CAD tools.
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
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0,
        double selectionTolerance = 5,
        double selectionDragThreshold = 1,
        CoordinateSystem2D? currentUcs = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(commandHistory);
        ArgumentNullException.ThrowIfNull(snapService);

        if (snapTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(snapTolerance),
                "Snap tolerance cannot be negative.");
        }

        if (selectionTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectionTolerance),
                "Selection tolerance cannot be negative.");
        }

        if (selectionDragThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectionDragThreshold),
                "Selection drag threshold cannot be negative.");
        }

        Document = document;
        CommandHistory = commandHistory;
        SnapService = snapService;
        SelectionSet = selectionSet ?? new SelectionSet();
        SelectionService = selectionService ?? new SelectionService();
        GridSettings = gridSettings ?? new GridSettings();
        CurrentLayerId = currentLayerId ?? LayerId.Default;
        EnabledSnaps = enabledSnaps;
        SnapTolerance = snapTolerance;
        SelectionTolerance = selectionTolerance;
        SelectionDragThreshold = selectionDragThreshold;
        CurrentUcs = currentUcs ?? CoordinateSystem2D.World;
    }

    public CadDocument Document { get; }

    public CommandHistory CommandHistory { get; }

    public SnapService SnapService { get; }

    public SelectionSet SelectionSet { get; }

    public SelectionService SelectionService { get; }

    public GridSettings GridSettings { get; }

    public LayerId CurrentLayerId { get; set; }

    public SnapKind EnabledSnaps { get; set; }

    public double SnapTolerance { get; set; }

    public double SelectionTolerance { get; set; }

    public double SelectionDragThreshold { get; set; }

    public CoordinateSystem2D CurrentUcs { get; set; }
}