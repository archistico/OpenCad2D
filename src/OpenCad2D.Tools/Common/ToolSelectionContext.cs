using OpenCad2D.Core.Identifiers;
using OpenCad2D.Interaction.Selection;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Provides selection state and selection settings for CAD tools.
/// </summary>
public sealed class ToolSelectionContext
{
    public ToolSelectionContext(
        SelectionSet selectionSet,
        SelectionService selectionService,
        double tolerance,
        double dragThreshold)
    {
        ArgumentNullException.ThrowIfNull(selectionSet);
        ArgumentNullException.ThrowIfNull(selectionService);

        if (tolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "Selection tolerance cannot be negative.");
        }

        if (dragThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dragThreshold),
                "Selection drag threshold cannot be negative.");
        }

        Set = selectionSet;
        Service = selectionService;
        Tolerance = tolerance;
        DragThreshold = dragThreshold;
    }

    public SelectionSet Set { get; }

    public SelectionService Service { get; }

    public double Tolerance { get; set; }

    public double DragThreshold { get; set; }

    public bool HasSelection => !Set.IsEmpty;

    public IReadOnlyCollection<EntityId> SelectedIds => Set.SelectedIds;
}