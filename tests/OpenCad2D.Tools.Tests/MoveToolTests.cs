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

public sealed class MoveToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForBasePoint()
    {
        var tool = new MoveTool();

        Assert.Equal("Move", tool.Name);
        Assert.Equal(MoveToolState.WaitingForBasePoint, tool.MoveState);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void ActiveSnapKind_WithNoSelection_ShouldUseEntitySnapOnly()
    {
        var context = CreateContext(enabledSnaps: SnapKind.Endpoint | SnapKind.Midpoint);
        var tool = new MoveTool();

        SnapKind result = tool.GetActiveSnapKind(context);

        Assert.Equal(SnapKind.EntityOnly, result);
        Assert.Equal(MoveToolState.WaitingForEntitySelection, tool.MoveState);
    }

    [Fact]
    public void ActiveSnapKind_WithSelection_ShouldUseGeometricSnaps()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(
            document,
            selection,
            enabledSnaps: SnapKind.Endpoint | SnapKind.Midpoint);

        var tool = new MoveTool();

        SnapKind result = tool.GetActiveSnapKind(context);

        Assert.Equal(SnapKind.Endpoint | SnapKind.Midpoint, result);
        Assert.Equal(MoveToolState.WaitingForBasePoint, tool.MoveState);
    }

    [Fact]
    public void FirstPointerPress_WithNoSelection_ShouldSelectEntityToMove()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(MoveToolState.WaitingForEntitySelection, tool.MoveState);
        Assert.True(selection.Contains(line.Id));
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_WithNoSelectionAndNoEntity_ShouldKeepSelectionPhase()
    {
        var context = CreateContext();
        var tool = new MoveTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(MoveToolState.WaitingForEntitySelection, tool.MoveState);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }


    [Fact]
    public void ConfirmEntitySelection_WithSelectedEntity_ShouldStartBasePointPhase()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.ConfirmEntitySelection(context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(MoveToolState.WaitingForBasePoint, tool.MoveState);
    }

    [Fact]
    public void EntitySelection_WithControlPressed_ShouldCycleOverOverlappingEntities()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var lower = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            drawOrder: 1);

        var upper = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            drawOrder: 2);

        document.AddEntity(lower);
        document.AddEntity(upper);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(
                new Point2D(5, 0),
                PointerModifiers.Control));

        Assert.True(selection.Contains(upper.Id));

        tool.OnPointerPressed(
            context,
            new PointerInfo(
                new Point2D(5, 0),
                PointerModifiers.Control));

        Assert.True(selection.Contains(lower.Id));
        Assert.False(selection.Contains(upper.Id));
    }

    [Fact]
    public void FirstPointerPress_WithSelection_ShouldStoreBasePoint()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(MoveToolState.WaitingForDestinationPoint, tool.MoveState);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(new Point2D(1, 2), tool.FirstPoint);
        Assert.Equal(new Point2D(1, 2), tool.CurrentPoint);
    }

    [Fact]
    public void PointerMove_AfterBasePoint_ShouldUpdatePreview()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(5, 2), tool.CurrentPoint);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities(context);

        Assert.Single(preview);

        var previewLine = Assert.IsType<LineEntity>(preview[0]);

        Assert.Equal(new Point2D(5, 2), previewLine.Start);
        Assert.Equal(new Point2D(15, 2), previewLine.End);

        var originalLine = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), originalLine.Start);
        Assert.Equal(new Point2D(10, 0), originalLine.End);
    }

    [Fact]
    public void MoveWithoutInitialSelection_ShouldSelectEntityThenMoveIt()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        tool.ConfirmEntitySelection(context);

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(MoveToolState.WaitingForBasePoint, tool.MoveState);

        var moved = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 2), moved.Start);
        Assert.Equal(new Point2D(15, 2), moved.End);
    }

    [Fact]
    public void SecondPointerPress_ShouldMoveSelectedEntity()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(MoveToolState.WaitingForBasePoint, tool.MoveState);

        var moved = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 2), moved.Start);
        Assert.Equal(new Point2D(15, 2), moved.End);
    }

    [Fact]
    public void SecondPointerPress_ShouldMoveMultipleSelectedEntities()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var circle = new CircleEntity(
            new Point2D(0, 0),
            5);

        document.AddEntity(line);
        document.AddEntity(circle);

        selection.Select(line.Id);
        selection.Select(circle.Id);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 3)));

        var movedLine = (LineEntity)document.Entities.GetRequired(line.Id);
        var movedCircle = (CircleEntity)document.Entities.GetRequired(circle.Id);

        Assert.Equal(new Point2D(10, 3), movedLine.Start);
        Assert.Equal(new Point2D(20, 3), movedLine.End);
        Assert.Equal(new Point2D(10, 3), movedCircle.Center);
    }

    [Fact]
    public void Move_ShouldBeUndoable()
    {
        CadDocument document = new();
        SelectionSet selection = new();
        CommandHistory history = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection, history);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.True(history.CanUndo);

        history.Undo(document);

        var restored = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), restored.Start);
        Assert.Equal(new Point2D(10, 0), restored.End);
    }

    [Fact]
    public void Move_ShouldKeepSelection()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.True(selection.Contains(line.Id));
        Assert.Equal(1, selection.Count);
    }

    [Fact]
    public void SecondPointerPress_WithEndpointSnap_ShouldUseSnappedDestination()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var selectedLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var snapLine = new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(selectedLine);
        document.AddEntity(snapLine);

        selection.Select(selectedLine.Id);

        var context = CreateContext(
            document,
            selection,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        var moved = (LineEntity)document.Entities.GetRequired(selectedLine.Id);

        Assert.Equal(new Point2D(100, 100), moved.Start);
        Assert.Equal(new Point2D(110, 100), moved.End);
    }

    [Fact]
    public void Cancel_ShouldResetWithoutMovingEntity()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(MoveToolState.WaitingForBasePoint, tool.MoveState);

        var unchanged = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), unchanged.Start);
        Assert.Equal(new Point2D(10, 0), unchanged.End);
    }


    [Fact]
    public void CommandInput_ShouldMoveSelectedEntityWithResolvedPoints()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new MoveTool();

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)),
            context);

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("@5,2", new Point2D(5, 2)),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        var moved = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(5, 2), moved.Start);
        Assert.Equal(new Point2D(15, 2), moved.End);
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
}
