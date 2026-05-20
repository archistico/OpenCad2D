using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
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
