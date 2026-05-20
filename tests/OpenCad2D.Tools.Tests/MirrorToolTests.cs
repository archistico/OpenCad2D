using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class MirrorToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstAxisPoint()
    {
        var tool = new MirrorTool();

        Assert.Equal("Mirror", tool.Name);
        Assert.Equal(MirrorToolState.WaitingForFirstAxisPoint, tool.State);
        Assert.Null(tool.FirstAxisPoint);
        Assert.Null(tool.SecondAxisPoint);
    }

    [Fact]
    public void Prompt_WithNoSelection_ShouldAskForEntitySelection()
    {
        var context = CreateContext();
        var tool = new MirrorTool();

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Equal(MirrorToolState.WaitingForEntitySelection, tool.State);
        Assert.Equal("MIRROR", prompt.CommandName);
        Assert.Equal(CommandInputKind.Selection, prompt.ExpectedInput);
    }

    [Fact]
    public void PointerFlow_WithDefaultNo_ShouldCreateMirroredCopyAndKeepSource()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(1, 2),
            new Point2D(3, 2));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MirrorTool();

        ToolResult first = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult second = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        ToolResult complete = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Started, first.Kind);
        Assert.Equal(ToolResultKind.Started, second.Kind);
        Assert.Equal(ToolResultKind.Completed, complete.Kind);
        Assert.Equal(2, document.Entities.All.Count);

        LineEntity source = (LineEntity)document.Entities.GetRequired(line.Id);
        Assert.Equal(new Point2D(1, 2), source.Start);
        Assert.Equal(new Point2D(3, 2), source.End);

        LineEntity mirrored = document.Entities.All
            .OfType<LineEntity>()
            .Single(entity => entity.Id != line.Id);

        AssertPointNear(new Point2D(-1, 2), mirrored.Start);
        AssertPointNear(new Point2D(-3, 2), mirrored.End);
    }

    [Fact]
    public void CommandInput_WithYes_ShouldMirrorSourceEntitiesInPlace()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(1, 2),
            new Point2D(3, 2));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MirrorTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)),
            context);

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,10", new Point2D(0, 10)),
            context);

        ToolResult complete = tool.HandleCommandInput(
            CommandInputSubmission.Option("Y", "Yes"),
            context);

        Assert.Equal(ToolResultKind.Completed, complete.Kind);
        Assert.Single(document.Entities.All);

        LineEntity mirrored = (LineEntity)document.Entities.GetRequired(line.Id);
        AssertPointNear(new Point2D(-1, 2), mirrored.Start);
        AssertPointNear(new Point2D(-3, 2), mirrored.End);
    }

    [Fact]
    public void PointerMove_AfterFirstAxisPoint_ShouldExposePreview()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(1, 2),
            new Point2D(3, 2));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MirrorTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult moved = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 10)));

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities(context);

        Assert.Equal(ToolResultKind.Updated, moved.Kind);
        Assert.Single(preview);

        var mirrored = Assert.IsType<LineEntity>(preview[0]);
        AssertPointNear(new Point2D(-1, 2), mirrored.Start);
        AssertPointNear(new Point2D(-3, 2), mirrored.End);
        Assert.Equal(line.Id, document.Entities.All.Single().Id);
    }

    [Fact]
    public void SecondAxisPoint_MatchingFirstPoint_ShouldReturnWarningAndStayInSecondPointPhase()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(1, 2),
            new Point2D(3, 2));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MirrorTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(MirrorToolState.WaitingForSecondAxisPoint, tool.State);
    }

    [Fact]
    public void MirrorCopy_ShouldBeUndoable()
    {
        CadDocument document = new();
        SelectionSet selection = new();
        CommandHistory history = new();

        var line = new LineEntity(
            new Point2D(1, 2),
            new Point2D(3, 2));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection, history);
        var tool = new MirrorTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));
        tool.HandleCommandInput(CommandInputSubmission.Confirm(string.Empty), context);

        Assert.Equal(2, document.Entities.All.Count);

        history.Undo(document);

        Assert.Single(document.Entities.All);
        Assert.Equal(line.Id, document.Entities.All.Single().Id);
    }


    [Fact]
    public void ToolControllerConfirmActiveToolCommand_AtDeleteSourcePrompt_ShouldKeepSourceObjects()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(1, 2),
            new Point2D(3, 2));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MirrorTool();
        var controller = new ToolController(context, tool);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.Count);
        Assert.Contains(document.Entities.All, entity => entity.Id == line.Id);
    }
    private static ToolContext CreateContext(
        CadDocument? document = null,
        SelectionSet? selectionSet = null,
        CommandHistory? history = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            history ?? new CommandHistory(),
            new SnapService(),
            selectionSet: selectionSet,
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance);
    }

    private static void AssertPointNear(
        Point2D expected,
        Point2D actual,
        double tolerance = 1e-9)
    {
        Assert.Equal(expected.X, actual.X, tolerance);
        Assert.Equal(expected.Y, actual.Y, tolerance);
    }
}
