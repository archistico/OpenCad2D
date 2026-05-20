using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Joins selected line entities into one or more straight-segment polylines.
/// </summary>
public sealed class JoinTool : ICadTool, ICommandDrivenTool, ISnapModeProvider
{
    private bool _isSelectingObjects;

    public string Name => "Join";

    public SnapKind GetActiveSnapKind(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return SnapKind.EntityOnly;
    }

    public CommandPromptState GetPromptState(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Selection.HasSelection)
        {
            int count = context.Selection.SelectedIds.Count;
            string message = count == 1
                ? "1 entity selected. Press Enter or right-click to join connected lines into polylines."
                : $"{count} entities selected. Press Enter or right-click to join connected lines into polylines.";

            return new CommandPromptState(
                "JOIN",
                message,
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Enter/right-click to join");
        }

        return new CommandPromptState(
            "JOIN",
            "Select connected lines to join into polylines",
            CommandInputKind.Selection,
            acceptsEmptyEnter: true,
            placeholder: "Select lines, then press Enter/right-click");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return Execute(context);
        }

        return ToolResult.None("Select connected lines to join, then press Enter or right-click.");
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        if (!_isSelectingObjects && context.Selection.HasSelection)
        {
            return Execute(context);
        }

        _isSelectingObjects = true;

        EntityId? selectedId = context.Selection.Service.SelectByPoint(
            context.Document,
            pointer.ModelPoint,
            context.Selection.Tolerance);

        if (selectedId is null)
        {
            return ToolResult.None("Select connected lines to join, then press Enter or right-click.");
        }

        CadEntity entity = context.Document.Entities.GetRequired(selectedId.Value);
        if (!context.Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Target entity is not editable.");
        }

        context.Selection.Set.Toggle(selectedId.Value);

        int count = context.Selection.SelectedIds.Count;
        if (count == 0)
        {
            return ToolResult.Updated("Entity removed from join selection. Select connected lines to join.");
        }

        return ToolResult.Updated(count == 1
            ? "1 entity selected for join. Select more connected lines or press Enter/right-click to join."
            : $"{count} entities selected for join. Select more connected lines or press Enter/right-click to join.");
    }

    public ToolResult OnPointerMoved(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        return ToolResult.None();
    }

    public ToolResult Cancel(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _isSelectingObjects = false;

        return ToolResult.Cancelled("Join command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _isSelectingObjects = false;

        return ToolResult.None("Join tool deactivated.");
    }

    private ToolResult Execute(ToolContext context)
    {
        IReadOnlyList<LineEntity> selectedLines = context.Selection.SelectedIds
            .Select(context.Document.Entities.GetRequired)
            .OfType<LineEntity>()
            .ToList();

        if (selectedLines.Count < 2)
        {
            return ToolResult.None("Select at least two lines to join.");
        }

        IReadOnlyList<JoinedLineChain> chains = BuildChains(
            selectedLines,
            context.GeometryTolerance.Distance);

        var polylines = chains
            .Where(chain => chain.Lines.Count >= 2 && chain.Vertices.Count >= 2)
            .Select(CreatePolyline)
            .ToList();

        if (polylines.Count == 0)
        {
            return ToolResult.None("Selected lines do not share endpoints and cannot be joined.");
        }

        IReadOnlyList<EntityId> consumedLineIds = chains
            .Where(chain => chain.Lines.Count >= 2 && chain.Vertices.Count >= 2)
            .SelectMany(chain => chain.Lines.Select(line => line.Id))
            .Distinct()
            .ToList();

        context.Commands.Execute(
            context.Document,
            new CompositeCommand(
                "Join lines",
                new ICadCommand[]
                {
                    new DeleteEntitiesCommand(consumedLineIds),
                    new AddEntityCommand(polylines)
                }));

        context.Selection.Set.Clear();
        _isSelectingObjects = false;

        return ToolResult.Completed(polylines.Count == 1
            ? $"{consumedLineIds.Count} lines joined into 1 polyline."
            : $"{consumedLineIds.Count} lines joined into {polylines.Count} polylines.");
    }

    private static PolylineEntity CreatePolyline(JoinedLineChain chain)
    {
        LineEntity source = chain.Lines[0];

        return new PolylineEntity(
            chain.Vertices,
            chain.IsClosed,
            layerId: source.LayerId,
            style: source.Style,
            isVisible: source.IsVisible,
            isLocked: source.IsLocked,
            drawOrder: source.DrawOrder);
    }

    private static IReadOnlyList<JoinedLineChain> BuildChains(
        IReadOnlyList<LineEntity> lines,
        double tolerance)
    {
        var remaining = lines.ToList();
        var chains = new List<JoinedLineChain>();

        while (remaining.Count > 0)
        {
            LineEntity seed = remaining[0];
            remaining.RemoveAt(0);

            var chainLines = new List<LineEntity> { seed };
            var vertices = new List<Point2D> { seed.Start, seed.End };
            bool changed;

            do
            {
                changed = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    LineEntity candidate = remaining[i];
                    if (!AreCompatible(seed, candidate))
                    {
                        continue;
                    }

                    if (TryAppendOrPrepend(vertices, candidate, tolerance))
                    {
                        chainLines.Add(candidate);
                        remaining.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }
            while (changed);

            bool isClosed = vertices.Count > 2 && AreSamePoint(vertices[0], vertices[^1], tolerance);
            if (isClosed)
            {
                vertices.RemoveAt(vertices.Count - 1);
            }

            chains.Add(new JoinedLineChain(chainLines, vertices, isClosed));
        }

        return chains;
    }

    private static bool TryAppendOrPrepend(
        List<Point2D> vertices,
        LineEntity candidate,
        double tolerance)
    {
        Point2D first = vertices[0];
        Point2D last = vertices[^1];

        if (AreSamePoint(last, candidate.Start, tolerance))
        {
            vertices.Add(candidate.End);
            return true;
        }

        if (AreSamePoint(last, candidate.End, tolerance))
        {
            vertices.Add(candidate.Start);
            return true;
        }

        if (AreSamePoint(first, candidate.End, tolerance))
        {
            vertices.Insert(0, candidate.Start);
            return true;
        }

        if (AreSamePoint(first, candidate.Start, tolerance))
        {
            vertices.Insert(0, candidate.End);
            return true;
        }

        return false;
    }

    private static bool AreCompatible(
        LineEntity first,
        LineEntity second)
    {
        return first.LayerId == second.LayerId
               && first.Style == second.Style
               && first.IsVisible == second.IsVisible
               && first.IsLocked == second.IsLocked;
    }

    private static bool AreSamePoint(
        Point2D first,
        Point2D second,
        double tolerance)
    {
        return first.DistanceTo(second) <= tolerance;
    }

    private sealed record JoinedLineChain(
        IReadOnlyList<LineEntity> Lines,
        IReadOnlyList<Point2D> Vertices,
        bool IsClosed);
}
