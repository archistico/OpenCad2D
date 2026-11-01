using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Editing;

/// <summary>
/// Joins selected lines, arcs and open polylines into one or more polyline entities.
/// Arc geometry is preserved through DXF-compatible polyline bulges.
/// Clicks add or remove entities from the selection; Enter/right-click joins the selection and ends the command,
/// or ends it directly when nothing is selected.
/// </summary>
public sealed class JoinTool : ICadTool, ICommandDrivenTool, ISnapModeProvider
{
    public string Name => "Join";

    public bool IsCommandFinished { get; private set; }

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
                ? "1 entity selected. Click to add or remove entities; Enter or right-click joins lines, arcs and open polylines."
                : $"{count} entities selected. Click to add or remove entities; Enter or right-click joins lines, arcs and open polylines.";

            return new CommandPromptState(
                "JOIN",
                message,
                CommandInputKind.Selection,
                acceptsEmptyEnter: true,
                placeholder: "Enter/right-click to join");
        }

        return new CommandPromptState(
            "JOIN",
            "Select lines, arcs and open polylines to join, or press Enter to finish",
            CommandInputKind.Selection,
            acceptsEmptyEnter: true,
            placeholder: "Select entities, then press Enter/right-click");
    }

    public ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (input.Kind == CommandInputSubmissionKind.Confirm)
        {
            return Confirm(context);
        }

        return ToolResult.None("Select lines, arcs and open polylines to join, then press Enter or right-click.");
    }

    public ToolResult OnPointerPressed(
        ToolContext context,
        PointerInfo pointer)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pointer);

        // A click always picks: with a preselection it adds or removes the clicked entity
        // instead of joining immediately, so the click target is never ignored.
        EntityId? selectedId = context.Selection.Service.SelectByPoint(
            context.Document,
            pointer.ModelPoint,
            context.Selection.Tolerance);

        if (selectedId is null)
        {
            return ToolResult.None("Select lines, arcs and open polylines to join, then press Enter or right-click.");
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
            return ToolResult.Updated("Entity removed from join selection. Select lines, arcs and open polylines to join.");
        }

        return ToolResult.Updated(count == 1
            ? "1 entity selected for join. Select more joinable entities or press Enter/right-click to join."
            : $"{count} entities selected for join. Select more joinable entities or press Enter/right-click to join.");
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

        IsCommandFinished = false;

        return ToolResult.Cancelled("Join command cancelled.");
    }

    public ToolResult Deactivate(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ToolResult.None("Join tool deactivated.");
    }

    private ToolResult Confirm(ToolContext context)
    {
        if (!context.Selection.HasSelection)
        {
            IsCommandFinished = true;
            return ToolResult.Completed("Join command finished.");
        }

        int skipped = EditableSelection.Prune(context);
        if (!context.Selection.HasSelection)
        {
            return ToolResult.None(
                "The selected entities were deleted or are on hidden or locked layers. Select lines, arcs and open polylines to join.");
        }

        ToolResult result = Execute(context, skipped);
        if (result.Kind == ToolResultKind.Completed)
        {
            IsCommandFinished = true;
        }

        return result;
    }

    private static ToolResult Execute(
        ToolContext context,
        int skippedCount)
    {
        double tolerance = context.GeometryTolerance.Distance;
        IReadOnlyList<CadEntity> selectedEntities = EditableSelection.GetEntities(context);

        IReadOnlyList<CadEntity> unsupportedEntities = selectedEntities
            .Where(entity => !IsSupportedJoinEntity(entity))
            .ToList();

        if (unsupportedEntities.Count > 0)
        {
            return ToolResult.None("Only lines, arcs and open polylines can be joined.");
        }

        if (selectedEntities.OfType<PolylineEntity>().Any(polyline => polyline.IsClosed))
        {
            return ToolResult.None("Closed polylines cannot be joined.");
        }

        if (selectedEntities.Any(entity => entity.IsLocked || !entity.IsVisible))
        {
            return ToolResult.None("Only editable visible lines, arcs and open polylines can be joined.");
        }

        // Zero-length segments (a degenerate line or a repeated polyline vertex) carry no
        // geometry; as graph edges they would look like a branching junction.
        IReadOnlyList<JoinSegment> segments = selectedEntities
            .SelectMany(CreateSegments)
            .Where(segment => !AreSamePoint(segment.Start, segment.End, tolerance))
            .ToList();

        if (segments.Count < 2)
        {
            return ToolResult.None("Select at least two joinable entities.");
        }

        int joinableEntityCount = segments
            .Select(segment => segment.SourceEntity.Id)
            .Distinct()
            .Count();

        if (joinableEntityCount < 2)
        {
            return ToolResult.None("Select at least two joinable entities.");
        }

        JoinStyleKey styleKey = JoinStyleKey.FromEntity(segments[0].SourceEntity);
        if (segments.Any(segment => !AreCompatible(segment.SourceEntity, styleKey)))
        {
            return ToolResult.None("Selected entities use different layers or styles and cannot be joined.");
        }

        BuildGraphResult graphResult = BuildGraph(segments, tolerance);
        if (graphResult.HasBranchingNode)
        {
            return ToolResult.None("Selected entities create a branching junction and cannot be joined into a single polyline.");
        }

        IReadOnlyList<JoinedSegmentChain> chains = BuildChains(
            graphResult.Segments,
            tolerance);

        IReadOnlyList<JoinedSegmentChain> joinableChains = chains
            .Where(chain => chain.SourceIds.Count >= 2 && chain.Segments.Count >= 2)
            .Where(chain => !IsDegenerateClosedChain(chain))
            .ToList();

        if (joinableChains.Count == 0)
        {
            return ToolResult.None("Selected entities do not touch at endpoints.");
        }

        var polylines = joinableChains
            .Select(chain => CreatePolyline(chain, styleKey))
            .ToList();

        IReadOnlyList<EntityId> consumedIds = joinableChains
            .SelectMany(chain => chain.SourceIds)
            .Distinct()
            .ToList();

        context.Commands.Execute(
            context.Document,
            new CompositeCommand(
                "Join entities",
                new ICadCommand[]
                {
                    new DeleteEntitiesCommand(consumedIds),
                    new AddEntityCommand(polylines)
                }));

        context.Selection.Set.Clear();

        return ToolResult.Completed(
            GetCompletedMessage(consumedIds.Count, polylines) +
            EditableSelection.FormatSkippedSuffix(skippedCount));
    }

    /// <summary>
    /// Two segments that close on each other while one is the reverse of the other describe the
    /// same curve twice (for example two overlapping copies of a line): the result would be a
    /// zero-area closed polyline. A line and an arc closing a D shape remain valid.
    /// </summary>
    private static bool IsDegenerateClosedChain(JoinedSegmentChain chain)
    {
        if (chain.Segments.Count != 2)
        {
            return false;
        }

        OrientedJoinSegment first = chain.Segments[0];
        OrientedJoinSegment second = chain.Segments[1];

        return first.Start == second.End &&
               first.End == second.Start &&
               Tolerance.IsZero(first.Bulge + second.Bulge);
    }

    private static bool IsSupportedJoinEntity(CadEntity entity)
    {
        return entity is LineEntity
               || entity is ArcEntity
               || entity is PolylineEntity;
    }

    private static IEnumerable<JoinSegment> CreateSegments(CadEntity entity)
    {
        switch (entity)
        {
            case LineEntity line:
                yield return new JoinSegment(entity, line.Start, line.End, 0.0);
                yield break;

            case ArcEntity arc:
                yield return new JoinSegment(
                    entity,
                    arc.Geometry.StartPoint,
                    arc.Geometry.EndPoint,
                    GetBulgeFromArc(arc.Geometry));
                yield break;

            case PolylineEntity polyline:
                for (int index = 0; index < polyline.SegmentCount; index++)
                {
                    Point2D start = polyline.Vertices[index];
                    Point2D end = polyline.Vertices[(index + 1) % polyline.Vertices.Count];
                    double bulge = index < polyline.SegmentBulges.Count
                        ? polyline.SegmentBulges[index]
                        : 0.0;

                    yield return new JoinSegment(entity, start, end, bulge);
                }

                yield break;
        }
    }

    private static PolylineEntity CreatePolyline(
        JoinedSegmentChain chain,
        JoinStyleKey styleKey)
    {
        IReadOnlyList<OrientedJoinSegment> orientedSegments = chain.Segments;
        bool isClosed = AreSamePoint(
            orientedSegments[0].Start,
            orientedSegments[^1].End,
            Tolerance.Default);

        var vertices = new List<Point2D>
        {
            orientedSegments[0].Start
        };
        var bulges = new List<double>();

        foreach (OrientedJoinSegment segment in orientedSegments)
        {
            bulges.Add(segment.Bulge);
            vertices.Add(segment.End);
        }

        if (isClosed)
        {
            vertices.RemoveAt(vertices.Count - 1);
        }

        return new PolylineEntity(
            vertices,
            isClosed,
            layerId: styleKey.LayerId,
            style: styleKey.Style,
            isVisible: styleKey.IsVisible,
            isLocked: styleKey.IsLocked,
            drawOrder: styleKey.DrawOrder,
            segmentBulges: bulges);
    }

    private static bool AreCompatible(
        CadEntity entity,
        JoinStyleKey styleKey)
    {
        return entity.LayerId == styleKey.LayerId
               && entity.Style == styleKey.Style
               && entity.IsVisible == styleKey.IsVisible
               && entity.IsLocked == styleKey.IsLocked;
    }

    private static BuildGraphResult BuildGraph(
        IReadOnlyList<JoinSegment> segments,
        double tolerance)
    {
        var nodes = new List<JoinNode>();
        var graphSegments = new List<GraphJoinSegment>();

        foreach (JoinSegment segment in segments)
        {
            JoinNode start = GetOrCreateNode(nodes, segment.Start, tolerance);
            JoinNode end = GetOrCreateNode(nodes, segment.End, tolerance);
            var graphSegment = new GraphJoinSegment(segment, start, end);
            graphSegments.Add(graphSegment);
            start.Segments.Add(graphSegment);
            end.Segments.Add(graphSegment);
        }

        bool hasBranchingNode = nodes.Any(node => node.Segments.Count > 2);

        return new BuildGraphResult(graphSegments, hasBranchingNode);
    }

    private static IReadOnlyList<JoinedSegmentChain> BuildChains(
        IReadOnlyList<GraphJoinSegment> segments,
        double tolerance)
    {
        var remaining = segments.ToList();
        var chains = new List<JoinedSegmentChain>();

        while (remaining.Count > 0)
        {
            GraphJoinSegment seed = remaining[0];
            remaining.RemoveAt(0);

            var orientedSegments = new List<OrientedJoinSegment>
            {
                seed.AsOriented(seed.Start, seed.End)
            };

            bool changed;
            do
            {
                changed = false;

                for (int index = 0; index < remaining.Count; index++)
                {
                    GraphJoinSegment candidate = remaining[index];

                    if (TryAppendOrPrepend(orientedSegments, candidate, tolerance))
                    {
                        remaining.RemoveAt(index);
                        changed = true;
                        break;
                    }
                }
            }
            while (changed);

            chains.Add(new JoinedSegmentChain(orientedSegments));
        }

        return chains;
    }

    private static bool TryAppendOrPrepend(
        List<OrientedJoinSegment> chain,
        GraphJoinSegment candidate,
        double tolerance)
    {
        Point2D first = chain[0].Start;
        Point2D last = chain[^1].End;

        if (AreSamePoint(last, candidate.Start.Point, tolerance))
        {
            chain.Add(candidate.AsOriented(candidate.Start, candidate.End));
            return true;
        }

        if (AreSamePoint(last, candidate.End.Point, tolerance))
        {
            chain.Add(candidate.AsOriented(candidate.End, candidate.Start));
            return true;
        }

        if (AreSamePoint(first, candidate.End.Point, tolerance))
        {
            chain.Insert(0, candidate.AsOriented(candidate.Start, candidate.End));
            return true;
        }

        if (AreSamePoint(first, candidate.Start.Point, tolerance))
        {
            chain.Insert(0, candidate.AsOriented(candidate.End, candidate.Start));
            return true;
        }

        return false;
    }

    private static JoinNode GetOrCreateNode(
        List<JoinNode> nodes,
        Point2D point,
        double tolerance)
    {
        JoinNode? existing = nodes.FirstOrDefault(node => AreSamePoint(node.Point, point, tolerance));
        if (existing is not null)
        {
            return existing;
        }

        var created = new JoinNode(point);
        nodes.Add(created);
        return created;
    }

    private static string GetCompletedMessage(
        int consumedEntityCount,
        IReadOnlyList<PolylineEntity> polylines)
    {
        int mixedPolylineCount = polylines.Count(polyline => polyline.HasArcSegments);

        if (polylines.Count == 1)
        {
            return mixedPolylineCount == 1
                ? $"{consumedEntityCount} entities joined into 1 mixed polyline."
                : $"{consumedEntityCount} entities joined into 1 polyline.";
        }

        return $"{consumedEntityCount} entities joined into {polylines.Count} polylines.";
    }

    private static double GetBulgeFromArc(Arc2D arc)
    {
        double sweep = GetPositiveSweep(arc);
        double bulge = Math.Tan(sweep / 4.0);

        return arc.IsCounterClockwise
            ? -bulge
            : bulge;
    }

    private static double GetPositiveSweep(Arc2D arc)
    {
        double start = arc.StartAngle.NormalizePositive().Radians;
        double end = arc.EndAngle.NormalizePositive().Radians;

        if (arc.IsCounterClockwise)
        {
            double sweep = end - start;
            return sweep < 0.0
                ? sweep + (2.0 * Math.PI)
                : sweep;
        }

        double clockwiseSweep = start - end;
        return clockwiseSweep < 0.0
            ? clockwiseSweep + (2.0 * Math.PI)
            : clockwiseSweep;
    }

    private static bool AreSamePoint(
        Point2D first,
        Point2D second,
        double tolerance)
    {
        return first.DistanceTo(second) <= tolerance;
    }

    private sealed record JoinSegment(
        CadEntity SourceEntity,
        Point2D Start,
        Point2D End,
        double Bulge);

    private sealed class JoinNode
    {
        public JoinNode(Point2D point)
        {
            Point = point;
        }

        public Point2D Point { get; }

        public List<GraphJoinSegment> Segments { get; } = new();
    }

    private sealed record GraphJoinSegment(
        JoinSegment Segment,
        JoinNode Start,
        JoinNode End)
    {
        public OrientedJoinSegment AsOriented(
            JoinNode orientedStart,
            JoinNode orientedEnd)
        {
            if (ReferenceEquals(orientedStart, Start) && ReferenceEquals(orientedEnd, End))
            {
                return new OrientedJoinSegment(
                    Segment.SourceEntity.Id,
                    Start.Point,
                    End.Point,
                    Segment.Bulge);
            }

            return new OrientedJoinSegment(
                Segment.SourceEntity.Id,
                End.Point,
                Start.Point,
                -Segment.Bulge);
        }
    }

    private sealed record OrientedJoinSegment(
        EntityId SourceId,
        Point2D Start,
        Point2D End,
        double Bulge);

    private sealed record JoinedSegmentChain(IReadOnlyList<OrientedJoinSegment> Segments)
    {
        public IReadOnlySet<EntityId> SourceIds { get; } = Segments
            .Select(segment => segment.SourceId)
            .ToHashSet();
    }

    private sealed record BuildGraphResult(
        IReadOnlyList<GraphJoinSegment> Segments,
        bool HasBranchingNode);

    private sealed record JoinStyleKey(
        LayerId LayerId,
        EntityStyle Style,
        bool IsVisible,
        bool IsLocked,
        int DrawOrder)
    {
        public static JoinStyleKey FromEntity(CadEntity entity)
        {
            return new JoinStyleKey(
                entity.LayerId,
                entity.Style,
                entity.IsVisible,
                entity.IsLocked,
                entity.DrawOrder);
        }
    }
}
