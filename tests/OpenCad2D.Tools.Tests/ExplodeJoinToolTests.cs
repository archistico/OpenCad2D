using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class ExplodeJoinToolTests
{
    [Fact]
    public void Explode_SelectedOpenPolyline_ShouldReplacePolylineWithLines()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5)
        });

        document.AddEntity(polyline);
        selection.Select(polyline.Id);

        var context = CreateContext(document, history, selection);
        var tool = new ExplodeTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(document.Entities.Contains(polyline.Id));

        List<LineEntity> lines = document.Entities.All.OfType<LineEntity>().ToList();
        Assert.Equal(2, lines.Count);
        Assert.Contains(lines, line => line.Start == new Point2D(0, 0) && line.End == new Point2D(10, 0));
        Assert.Contains(lines, line => line.Start == new Point2D(10, 0) && line.End == new Point2D(10, 5));
        Assert.Empty(selection.SelectedIds);

        history.Undo(document);

        Assert.True(document.Entities.Contains(polyline.Id));
        Assert.Empty(document.Entities.All.OfType<LineEntity>());
    }

    [Fact]
    public void Explode_SelectedClosedPolyline_ShouldCreateClosingLine()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5)
            },
            isClosed: true);

        document.AddEntity(polyline);
        selection.Select(polyline.Id);

        var context = CreateContext(document, history, selection);
        var tool = new ExplodeTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        List<LineEntity> lines = document.Entities.All.OfType<LineEntity>().ToList();
        Assert.Equal(3, lines.Count);
        Assert.Contains(lines, line => line.Start == new Point2D(10, 5) && line.End == new Point2D(0, 0));
    }

    [Fact]
    public void Explode_SelectedMixedPolyline_ShouldReplacePolylineWithLinesAndArcs()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0)
            },
            segmentBulges: new[] { 0.0, -1.0 });

        document.AddEntity(polyline);
        selection.Select(polyline.Id);

        var context = CreateContext(document, history, selection);
        var tool = new ExplodeTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(document.Entities.Contains(polyline.Id));

        LineEntity line = Assert.Single(document.Entities.All.OfType<LineEntity>());
        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);

        ArcEntity arc = Assert.Single(document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(10.0, arc.Geometry.StartPoint.X, 12);
        Assert.Equal(0.0, arc.Geometry.StartPoint.Y, 12);
        Assert.Equal(20.0, arc.Geometry.EndPoint.X, 12);
        Assert.Equal(0.0, arc.Geometry.EndPoint.Y, 12);
        Assert.True(arc.IsCounterClockwise);
        Assert.Equal(polyline.LayerId, arc.LayerId);
        Assert.Equal(polyline.Style, arc.Style);

        Assert.Empty(selection.SelectedIds);

        history.Undo(document);

        Assert.True(document.Entities.Contains(polyline.Id));
        Assert.Empty(document.Entities.All.OfType<LineEntity>());
        Assert.Empty(document.Entities.All.OfType<ArcEntity>());
    }

    [Fact]
    public void Explode_SelectedClosedMixedPolyline_ShouldCreateClosingArcFromLastBulge()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: true,
            segmentBulges: new[] { 0.0, 0.0, 1.0 });

        document.AddEntity(polyline);
        selection.Select(polyline.Id);

        var context = CreateContext(document, history, selection);
        var tool = new ExplodeTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        Assert.Equal(2, document.Entities.All.OfType<LineEntity>().Count());
        ArcEntity closingArc = Assert.Single(document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(10.0, closingArc.Geometry.StartPoint.X, 12);
        Assert.Equal(10.0, closingArc.Geometry.StartPoint.Y, 12);
        Assert.Equal(0.0, closingArc.Geometry.EndPoint.X, 12);
        Assert.Equal(0.0, closingArc.Geometry.EndPoint.Y, 12);
        Assert.False(closingArc.IsCounterClockwise);
    }

    [Fact]
    public void Explode_SelectedBlockReference_ShouldReplaceReferenceWithWorldSpaceEntities()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var definition = new BlockDefinition(
            BlockDefinitionId.New(),
            "Door",
            new CadEntity[]
            {
                new LineEntity(new Point2D(0, 0), new Point2D(10, 0))
            });

        document.BlockDefinitions.Add(definition);

        var blockReference = new BlockReferenceEntity(
            definition.Id,
            new Point2D(5, 10),
            new Vector2D(2, 0),
            new Vector2D(0, 2),
            definition.GetBoundingBox());

        document.AddEntity(blockReference);
        selection.Select(blockReference.Id);

        var context = CreateContext(document, history, selection);
        var tool = new ExplodeTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(document.Entities.Contains(blockReference.Id));

        LineEntity line = Assert.Single(document.Entities.All.OfType<LineEntity>());
        Assert.Equal(new Point2D(5, 10), line.Start);
        Assert.Equal(new Point2D(25, 10), line.End);
        Assert.NotEqual(definition.Entities[0].Id, line.Id);
        Assert.Empty(selection.SelectedIds);

        history.Undo(document);

        Assert.True(document.Entities.Contains(blockReference.Id));
        Assert.Empty(document.Entities.All.OfType<LineEntity>());
    }

    [Fact]
    public void Join_SelectedConnectedLines_ShouldReplaceLinesWithPolyline()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 5));
        var third = new LineEntity(new Point2D(10, 5), new Point2D(15, 5));

        document.AddEntities(new CadEntity[] { first, second, third });
        selection.Select(first.Id);
        selection.Select(second.Id);
        selection.Select(third.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(document.Entities.Contains(first.Id));
        Assert.False(document.Entities.Contains(second.Id));
        Assert.False(document.Entities.Contains(third.Id));

        PolylineEntity polyline = Assert.Single(document.Entities.All.OfType<PolylineEntity>());
        Assert.False(polyline.IsClosed);
        Assert.Equal(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(15, 5)
            },
            polyline.Vertices);
        Assert.Empty(selection.SelectedIds);

        history.Undo(document);

        Assert.Equal(3, document.Entities.All.OfType<LineEntity>().Count());
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void Join_SelectedClosedConnectedLines_ShouldCreateClosedPolyline()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 5));
        var third = new LineEntity(new Point2D(10, 5), new Point2D(0, 0));

        document.AddEntities(new CadEntity[] { first, second, third });
        selection.Select(first.Id);
        selection.Select(second.Id);
        selection.Select(third.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        PolylineEntity polyline = Assert.Single(document.Entities.All.OfType<PolylineEntity>());
        Assert.True(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
    }

    [Fact]
    public void Join_WithDisconnectedLines_ShouldCreateSeparatePolylinesForConnectedChains()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var a = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var b = new LineEntity(new Point2D(10, 0), new Point2D(20, 0));
        var c = new LineEntity(new Point2D(100, 0), new Point2D(110, 0));
        var d = new LineEntity(new Point2D(110, 0), new Point2D(120, 0));

        document.AddEntities(new CadEntity[] { a, b, c, d });
        foreach (LineEntity line in new[] { a, b, c, d })
        {
            selection.Select(line.Id);
        }

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.OfType<PolylineEntity>().Count());
        Assert.Empty(document.Entities.All.OfType<LineEntity>());
    }

    [Fact]
    public void Join_WithUnconnectedLinesOnly_ShouldNotModifyDocument()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(20, 0), new Point2D(30, 0));

        document.AddEntities(new CadEntity[] { first, second });
        selection.Select(first.Id);
        selection.Select(second.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(2, document.Entities.All.OfType<LineEntity>().Count());
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());
        Assert.False(history.CanUndo);
    }



    [Fact]
    public void Join_SelectedOpenPolylineAndLine_ShouldPreserveExistingBulges()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0)
            },
            segmentBulges: new[] { 0.25, 0.0 });
        var line = new LineEntity(new Point2D(20, 0), new Point2D(30, 0));

        document.AddEntities(new CadEntity[] { polyline, line });
        selection.Select(polyline.Id);
        selection.Select(line.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity joined = Assert.Single(document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0),
                new Point2D(30, 0)
            },
            joined.Vertices);
        Assert.Equal(new[] { 0.25, 0.0, 0.0 }, joined.SegmentBulges);
    }

    [Fact]
    public void Join_SelectedLineAndArc_ShouldCreateMixedPolyline()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var arc = new ArcEntity(
            new Point2D(10, 5),
            5,
            Angle.FromDegrees(270),
            Angle.FromDegrees(0),
            isCounterClockwise: true);

        document.AddEntities(new CadEntity[] { line, arc });
        selection.Select(line.Id);
        selection.Select(arc.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity joined = Assert.Single(document.Entities.All.OfType<PolylineEntity>());
        Assert.True(joined.HasArcSegments);
        Assert.Equal(3, joined.Vertices.Count);
        Assert.Equal(2, joined.SegmentBulges.Count);
        Assert.Equal(0.0, joined.SegmentBulges[0]);
        Assert.NotEqual(0.0, joined.SegmentBulges[1]);
        Assert.Empty(document.Entities.All.OfType<LineEntity>());
        Assert.Empty(document.Entities.All.OfType<ArcEntity>());

        history.Undo(document);

        Assert.Single(document.Entities.All.OfType<LineEntity>());
        Assert.Single(document.Entities.All.OfType<ArcEntity>());
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void Join_SelectedReversedArc_ShouldInvertBulge()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var arc = new ArcEntity(
            new Point2D(0, 5),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(270),
            isCounterClockwise: true);
        var line = new LineEntity(new Point2D(5, 5), new Point2D(15, 5));

        document.AddEntities(new CadEntity[] { arc, line });
        selection.Select(line.Id);
        selection.Select(arc.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity joined = Assert.Single(document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(0.0, joined.Vertices[0].X, 12);
        Assert.Equal(0.0, joined.Vertices[0].Y, 12);
        Assert.True(joined.SegmentBulges[0] > 0.0);
        Assert.Equal(0.0, joined.SegmentBulges[1]);
    }

    [Fact]
    public void Join_WithClosedPolyline_ShouldExplainClosedPolylinesCannotBeJoined()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            },
            isClosed: true);
        var line = new LineEntity(new Point2D(10, 10), new Point2D(20, 10));

        document.AddEntities(new CadEntity[] { polyline, line });
        selection.Select(polyline.Id);
        selection.Select(line.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Closed polylines cannot be joined.", result.Message);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Join_WithUnsupportedEntity_ShouldExplainOnlyLinesArcsOpenPolylines()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var circle = new CircleEntity(new Point2D(10, 0), 5);

        document.AddEntities(new CadEntity[] { line, circle });
        selection.Select(line.Id);
        selection.Select(circle.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal("Only lines, arcs and open polylines can be joined.", result.Message);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Join_WithBranchingEntities_ShouldExplainBranchingJunction()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selection = new();

        var a = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var b = new LineEntity(new Point2D(10, 0), new Point2D(20, 0));
        var c = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));

        document.AddEntities(new CadEntity[] { a, b, c });
        selection.Select(a.Id);
        selection.Select(b.Id);
        selection.Select(c.Id);

        var context = CreateContext(document, history, selection);
        var tool = new JoinTool();

        ToolResult result = tool.HandleCommandInput(
            OpenCad2D.Tools.Input.CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(
            "Selected entities create a branching junction and cannot be joined into a single polyline.",
            result.Message);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Tools_ShouldUseEntityOnlySnapMode()
    {
        ToolContext context = CreateContext(new CadDocument(), new CommandHistory(), new SelectionSet());

        Assert.Equal(SnapKind.EntityOnly, ((ISnapModeProvider)new ExplodeTool()).GetActiveSnapKind(context));
        Assert.Equal(SnapKind.EntityOnly, ((ISnapModeProvider)new JoinTool()).GetActiveSnapKind(context));
    }

    private static ToolContext CreateContext(
        CadDocument document,
        CommandHistory history,
        SelectionSet selection)
    {
        return new ToolContext(
            document,
            history,
            new SnapService(),
            selectionSet: selection,
            selectionTolerance: 1);
    }
}
