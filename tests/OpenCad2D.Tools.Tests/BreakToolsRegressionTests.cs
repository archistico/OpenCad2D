using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class BreakToolsRegressionTests
{
    private static readonly LayerId ReferenceLayerId = new("Reference");

    [Fact]
    public void BreakPoint_WhenTargetWasDeletedAfterPick_ShouldAskForTargetAgainWithoutThrowing()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 0)));
        document.RemoveEntity(line.Id);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
        Assert.Empty(document.Entities.All);
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void BreakPoint_WhenTargetLayerWasLockedAfterPick_ShouldNotThrowOrModifyTarget()
    {
        ToolContext context = CreateContext(out CadDocument document, out _);
        LineEntity line = AddLineOnReferenceLayer(document);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 5)));
        document.Layers.SetLocked(ReferenceLayerId, true);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Same(line, document.Entities.GetRequired(line.Id));
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void BreakPoint_WhenTargetWasChangedAfterPick_ShouldNotRestoreItsOldGeometry()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 0)));
        var moved = new LineEntity(new Point2D(0, 20), new Point2D(10, 20), id: line.Id);
        document.ReplaceEntity(moved);
        ToolResult move = tool.OnPointerMoved(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.False(tool.HasPreview);
        Assert.NotEqual(ToolResultKind.Updated, move.Kind);
        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Same(moved, Assert.Single(document.Entities.All));
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void BreakSegment_WhenTargetWasDeletedBeforeSecondPoint_ShouldAskForTargetAgainWithoutThrowing()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(4, 0)));
        document.RemoveEntity(line.Id);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(6, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(context.CurrentBasePoint);
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void BreakSegment_WhenTargetWasChangedAfterPick_ShouldNotRestoreItsOldGeometry()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(4, 0)));
        var moved = new LineEntity(new Point2D(0, 20), new Point2D(10, 20), id: line.Id);
        document.ReplaceEntity(moved);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(6, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Same(moved, Assert.Single(document.Entities.All));
    }

    [Fact]
    public void BreakSegment_TypedFirstPointAwayFromEntity_ShouldBeProjectedOntoIt()
    {
        ToolContext context = CreateContext(out CadDocument document, out _);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 0)));
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("4,80", new Point2D(4, 80)), context);

        Assert.Equal(new Point2D(4, 0), tool.FirstBreakPoint);
    }

    [Theory]
    [InlineData("Point")]
    [InlineData("Segment")]
    public void ConfirmAtTargetPrompt_ShouldFinishCommand(string toolName)
    {
        ToolContext context = CreateContext(out _, out _);
        ICadTool tool = toolName == "Point" ? new BreakAtPointTool() : new BreakBetweenPointsTool();
        var controller = new ToolController(context, tool);

        Assert.True(((ICommandDrivenTool)tool).GetPromptState(context).AcceptsEmptyEnter);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsType<OpenCad2D.Tools.Selection.SelectionTool>(controller.ActiveTool);
    }

    [Fact]
    public void BreakPoint_WithSinglePreselectedLine_ShouldStartAtBreakPointAndClearStaleSelection()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        context.Selection.Set.Select(line.Id);
        var tool = new BreakAtPointTool();

        Assert.Equal(SnapKind.None, tool.GetActiveSnapKind(context));
        Assert.Equal(BreakAtPointToolState.WaitingForBreakPoint, tool.State);
        Assert.Equal(line.Id, tool.TargetEntityId);

        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromPoint("5,0", new Point2D(5, 0)), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.OfType<LineEntity>().Count());
        Assert.DoesNotContain(line.Id, context.Selection.SelectedIds);
    }

    [Fact]
    public void BreakSegment_WithSinglePreselectedLine_ShouldStartAtFirstBreakPoint()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        context.Selection.Set.Select(line.Id);
        var tool = new BreakBetweenPointsTool();

        Assert.Contains("first break point", tool.GetPromptState(context).Prompt);

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("4,0", new Point2D(4, 0)), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromPoint("6,0", new Point2D(6, 0)), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.OfType<LineEntity>().Count());
    }

    [Fact]
    public void BreakPoint_WithPreselectedCircle_ShouldStillAskForTarget()
    {
        ToolContext context = CreateContext(out CadDocument document, out _);
        var circle = new CircleEntity(new Point2D(50, 50), 5);
        document.AddEntity(circle);
        context.Selection.Set.Select(circle.Id);
        var tool = new BreakAtPointTool();

        tool.GetPromptState(context);

        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
    }

    [Fact]
    public void BreakSegment_WithSeveralPreselectedEntities_ShouldStillAskForTarget()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        var other = new LineEntity(new Point2D(0, 30), new Point2D(10, 30));
        document.AddEntity(other);
        context.Selection.Set.Select(line.Id);
        context.Selection.Set.Select(other.Id);
        var tool = new BreakBetweenPointsTool();

        tool.GetPromptState(context);

        Assert.Equal(BreakBetweenPointsToolState.WaitingForTargetEntity, tool.State);
    }

    [Fact]
    public void BreakPoint_WithPreselectedLockedLine_ShouldStillAskForTarget()
    {
        ToolContext context = CreateContext(out CadDocument document, out _);
        LineEntity locked = AddLineOnReferenceLayer(document);
        document.Layers.SetLocked(ReferenceLayerId, true);
        context.Selection.Set.Select(locked.Id);
        var tool = new BreakAtPointTool();

        tool.GetPromptState(context);

        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
    }

    private static LineEntity AddLineOnReferenceLayer(CadDocument document)
    {
        document.Layers.Add(new Layer(ReferenceLayerId, "Reference"));
        var line = new LineEntity(new Point2D(0, 5), new Point2D(10, 5), layerId: ReferenceLayerId);
        document.AddEntity(line);
        return line;
    }

    private static ToolContext CreateContext(out CadDocument document, out LineEntity line)
    {
        document = new CadDocument();
        line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);

        return new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: SnapKind.None,
            selectionTolerance: 1);
    }
}
