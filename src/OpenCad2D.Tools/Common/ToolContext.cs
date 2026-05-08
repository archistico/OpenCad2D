using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;

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
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0)
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

        Document = document;
        CommandHistory = commandHistory;
        SnapService = snapService;
        SelectionSet = selectionSet ?? new SelectionSet();
        EnabledSnaps = enabledSnaps;
        SnapTolerance = snapTolerance;
    }

    public CadDocument Document { get; }

    public CommandHistory CommandHistory { get; }

    public SnapService SnapService { get; }

    public SelectionSet SelectionSet { get; }

    public SnapKind EnabledSnaps { get; set; }

    public double SnapTolerance { get; set; }
}