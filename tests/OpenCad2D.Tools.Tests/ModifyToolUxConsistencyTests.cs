using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class ModifyToolUxConsistencyTests
{
    [Fact]
    public void BreakAtPointSnapKind_ShouldUseEntityOnlyWhenSelectingTargetAndGeometricSnapsForBreakPoint()
    {
        ToolContext context = CreateContextWithLine(out _);
        var tool = new BreakAtPointTool();

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(BreakAtPointToolState.WaitingForBreakPoint, tool.State);
        Assert.Equal(SnapKind.Endpoint | SnapKind.Midpoint, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void BreakBetweenPointsSnapKind_ShouldUseEntityOnlyWhenSelectingTargetAndGeometricSnapsForBreakPoints()
    {
        ToolContext context = CreateContextWithLine(out _);
        var tool = new BreakBetweenPointsTool();

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(BreakBetweenPointsToolState.WaitingForFirstBreakPoint, tool.State);
        Assert.Equal(SnapKind.Endpoint | SnapKind.Midpoint, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void ExtendSnapKind_ShouldStayEntityOnlyForBoundaryAndTargetSelection()
    {
        ToolContext context = CreateContextWithLine(out _);
        context.Document.AddEntity(new LineEntity(new Point2D(10, -5), new Point2D(10, 5)));
        var tool = new ExtendTool();

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ExtendToolState.WaitingForTargetEntity, tool.State);
        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void AlignSnapKind_ShouldUseEntityOnlyWithoutSelectionAndDisableSnapsAtScaleConfirmation()
    {
        ToolContext context = CreateContextWithLine(out LineEntity line);
        var tool = new AlignTool();

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));

        context.Selection.Set.Select(line.Id);

        Assert.Equal(SnapKind.Endpoint | SnapKind.Midpoint, tool.GetActiveSnapKind(context));

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(1, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 1)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(1, 1)));

        Assert.Equal(AlignToolState.WaitingForScaleConfirmation, tool.State);
        Assert.Equal(SnapKind.None, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void MoveRightClickConfirmation_ShouldAdvanceEntitySelectionPhase()
    {
        ToolContext context = CreateContextWithLine(out _);
        var tool = new MoveTool();
        var controller = new ToolController(context, tool);

        controller.OnPointerPressed(new PointerInfo(new Point2D(5, 0)));
        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(MoveToolState.WaitingForBasePoint, tool.MoveState);
    }

    [Fact]
    public void CopyRightClickConfirmation_ShouldAdvanceEntitySelectionPhase()
    {
        ToolContext context = CreateContextWithLine(out _);
        var tool = new CopyTool();
        var controller = new ToolController(context, tool);

        controller.OnPointerPressed(new PointerInfo(new Point2D(5, 0)));
        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(MoveToolState.WaitingForBasePoint, tool.CopyState);
    }

    [Fact]
    public void RotateRightClickConfirmation_ShouldAdvanceEntitySelectionPhase()
    {
        ToolContext context = CreateContextWithLine(out _);
        var tool = new RotateTool();
        var controller = new ToolController(context, tool);

        controller.OnPointerPressed(new PointerInfo(new Point2D(5, 0)));
        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(RotateToolState.WaitingForBasePoint, tool.State);
    }

    [Fact]
    public void ScaleRightClickConfirmation_ShouldAdvanceEntitySelectionPhase()
    {
        ToolContext context = CreateContextWithLine(out _);
        var tool = new ScaleTool();
        var controller = new ToolController(context, tool);

        controller.OnPointerPressed(new PointerInfo(new Point2D(5, 0)));
        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForBasePoint, tool.State);
    }

    [Fact]
    public void MirrorRightClickConfirmation_ShouldAdvanceEntitySelectionPhase()
    {
        ToolContext context = CreateContextWithLine(out _);
        var tool = new MirrorTool();
        var controller = new ToolController(context, tool);

        controller.OnPointerPressed(new PointerInfo(new Point2D(5, 0)));
        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(MirrorToolState.WaitingForFirstAxisPoint, tool.State);
    }

    private static ToolContext CreateContextWithLine(out LineEntity line)
    {
        var document = new CadDocument();
        var selection = new SelectionSet();

        var context = new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            selectionSet: selection,
            enabledSnaps: SnapKind.Endpoint | SnapKind.Midpoint,
            snapTolerance: 0.5,
            selectionTolerance: 0.5);

        line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);

        return context;
    }
}
