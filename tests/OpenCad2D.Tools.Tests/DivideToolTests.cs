using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class DivideToolTests
{
    [Fact]
    public void Constructor_ShouldExposeAutoCadCommandName()
    {
        var tool = new DivideTool();

        Assert.Equal("Divide", tool.Name);
    }

    [Fact]
    public void Prompt_WithNoTarget_ShouldAskForSelection()
    {
        ToolContext context = CreateContext();
        var tool = new DivideTool();

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Equal("DIVIDE", prompt.CommandName);
        Assert.Equal(CommandInputKind.Selection, prompt.ExpectedInput);
    }

    [Fact]
    public void Prompt_WithSingleSelectedDividableEntity_ShouldAskForSegmentCount()
    {
        var document = new CadDocument();
        var selection = new SelectionSet();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(300, 0));
        document.AddEntity(line);
        selection.Select(line.Id);

        ToolContext context = CreateContext(document, selection);
        var tool = new DivideTool();

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Equal(CommandInputKind.Number, prompt.ExpectedInput);
    }

    [Fact]
    public void NumberInput_WithSelectedLine_ShouldCreatePersistentPointsOnCurrentLayer()
    {
        LayerId constructionLayerId = new("Construction");
        var document = new CadDocument();
        document.Layers.Add(new Layer(constructionLayerId, "Construction"));
        var selection = new SelectionSet();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(300, 0));
        document.AddEntity(line);
        selection.Select(line.Id);

        ToolContext context = CreateContext(
            document,
            selection,
            currentLayerId: constructionLayerId);
        var tool = new DivideTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("3", 3),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.Count);

        IReadOnlyList<PointEntity> points = document.Entities.All.OfType<PointEntity>().ToList();
        Assert.Equal(2, points.Count);
        Assert.Contains(points, point => IsPoint(point.Position, new Point2D(100, 0)));
        Assert.Contains(points, point => IsPoint(point.Position, new Point2D(200, 0)));
        Assert.All(points, point => Assert.Equal(constructionLayerId, point.LayerId));
        Assert.Contains(document.Entities.All, entity => entity.Id == line.Id);
    }

    [Fact]
    public void PointerThenNumberInput_ShouldDividePickedEntity()
    {
        var document = new CadDocument();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(300, 0));
        document.AddEntity(line);

        ToolContext context = CreateContext(document);
        var tool = new DivideTool();

        ToolResult pickResult = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(150, 0)));

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("3", 3),
            context);

        Assert.Equal(ToolResultKind.Updated, pickResult.Kind);
        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.OfType<PointEntity>().Count());
    }

    [Fact]
    public void NumberInput_ShouldBeUndoableAsSingleOperation()
    {
        var document = new CadDocument();
        var history = new CommandHistory();
        var selection = new SelectionSet();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(300, 0));
        document.AddEntity(line);
        selection.Select(line.Id);

        ToolContext context = CreateContext(
            document,
            selection,
            history);
        var tool = new DivideTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("3", 3),
            context);

        Assert.Equal(3, document.Entities.Count);
        Assert.True(history.CanUndo);

        history.Undo(document);

        Assert.Single(document.Entities.All);
        Assert.Contains(document.Entities.All, entity => entity.Id == line.Id);

        history.Redo(document);

        Assert.Equal(3, document.Entities.Count);
    }

    [Fact]
    public void NumberInput_WithDecimalSegmentCount_ShouldNotCreatePoints()
    {
        var document = new CadDocument();
        var selection = new SelectionSet();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(300, 0));
        document.AddEntity(line);
        selection.Select(line.Id);

        ToolContext context = CreateContext(document, selection);
        var tool = new DivideTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("3.5", 3.5),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(document.Entities.All.OfType<PointEntity>());
    }

    [Fact]
    public void NumberInput_WithMultipleSelection_ShouldNotCreatePoints()
    {
        var document = new CadDocument();
        var selection = new SelectionSet();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(0, 10), new Point2D(10, 10));
        document.AddEntity(first);
        document.AddEntity(second);
        selection.Select(first.Id);
        selection.Select(second.Id);

        ToolContext context = CreateContext(document, selection);
        var tool = new DivideTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("3", 3),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(document.Entities.All.OfType<PointEntity>());
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SelectionSet? selection = null,
        CommandHistory? history = null,
        LayerId? currentLayerId = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            history ?? new CommandHistory(),
            new SnapService(),
            selectionSet: selection ?? new SelectionSet(),
            currentLayerId: currentLayerId,
            selectionTolerance: 6);
    }

    private static bool IsPoint(Point2D actual, Point2D expected)
    {
        return Math.Abs(actual.X - expected.X) < 1e-8 &&
               Math.Abs(actual.Y - expected.Y) < 1e-8;
    }
}
