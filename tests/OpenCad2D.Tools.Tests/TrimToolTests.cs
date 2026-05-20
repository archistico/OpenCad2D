using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class TrimToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForBoundaryEntity()
    {
        var tool = new TrimTool();

        Assert.Equal("Trim", tool.Name);
        Assert.Equal(TrimToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
        Assert.False(tool.HasPreview);
    }


    [Fact]
    public void GetActiveSnapKind_ShouldUseEntityOnlySnap()
    {
        var context = CreateContext();
        var tool = new TrimTool();

        SnapKind activeSnaps = tool.GetActiveSnapKind(context);

        Assert.Equal(SnapKind.EntityOnly, activeSnaps);
    }

    [Fact]
    public void GetActiveSnapKind_AfterBoundarySelection_ShouldKeepEntityOnlySnap()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        SnapKind activeSnaps = tool.GetActiveSnapKind(context);

        Assert.Equal(SnapKind.EntityOnly, activeSnaps);
    }

    [Fact]
    public void FirstPointerPress_WithoutLine_ShouldNotStartTool()
    {
        var context = CreateContext();
        var tool = new TrimTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(TrimToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
    }

    [Fact]
    public void FirstPointerPress_OnLine_ShouldSelectBoundaryLine()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out LineEntity boundary,
            out _);
        var tool = new TrimTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(TrimToolState.WaitingForTargetEntity, tool.State);
        Assert.Equal(boundary.Id, tool.BoundaryEntityId);
        Assert.NotNull(context.CurrentBasePoint);
    }

    [Fact]
    public void PointerMove_AfterBoundary_ShouldUpdatePreview()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(8, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        LineEntity line = Assert.IsType<LineEntity>(Assert.Single(preview));

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(5, 0), line.End);
    }


    [Fact]
    public void PointerMove_AfterBoundary_ShouldExposeHighlightedRemovedSegment()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(8, 0)));

        IReadOnlyList<CadEntity> highlighted = tool.GetHighlightedPreviewEntities();
        LineEntity removed = Assert.IsType<LineEntity>(Assert.Single(highlighted));

        Assert.Equal(new Point2D(5, 0), removed.Start);
        Assert.Equal(new Point2D(10, 0), removed.End);
    }

    [Fact]
    public void SecondPointerPress_OnRightSide_ShouldTrimRightSide()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out LineEntity boundary,
            out LineEntity target);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(8, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(TrimToolState.WaitingForTargetEntity, tool.State);
        Assert.False(context.Document.Entities.Contains(target.Id));

        LineEntity trimmed = Assert.Single(
            context.Document.Entities.All.OfType<LineEntity>(),
            line => !line.Id.Equals(target.Id) && !line.Id.Equals(boundary.Id));

        Assert.Equal(new Point2D(0, 0), trimmed.Start);
        Assert.Equal(new Point2D(5, 0), trimmed.End);
    }

    [Fact]
    public void SecondPointerPress_OnLeftSide_ShouldTrimLeftSide()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out LineEntity boundary,
            out LineEntity target);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(2, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(context.Document.Entities.Contains(target.Id));

        LineEntity trimmed = Assert.Single(
            context.Document.Entities.All.OfType<LineEntity>(),
            line => !line.Id.Equals(target.Id) && !line.Id.Equals(boundary.Id));

        Assert.Equal(new Point2D(5, 0), trimmed.Start);
        Assert.Equal(new Point2D(10, 0), trimmed.End);
    }

    [Fact]
    public void SecondPointerPress_WhenIntersectionIsOutsideTarget_ShouldNotTrim()
    {
        var context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(15, -5),
            new Point2D(15, 5));

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(15, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(8, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.True(context.Document.Entities.Contains(target.Id));

        LineEntity unchanged = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), unchanged.Start);
        Assert.Equal(new Point2D(10, 0), unchanged.End);
    }

    [Fact]
    public void Trim_ShouldBeUndoable()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out LineEntity target);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(8, 0)));

        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        LineEntity restored = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), restored.Start);
        Assert.Equal(new Point2D(10, 0), restored.End);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(TrimToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
        Assert.Null(context.CurrentBasePoint);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 0.5);
    }

    private static ToolContext CreateContextWithBoundaryAndTarget(
        out LineEntity boundary,
        out LineEntity target)
    {
        ToolContext context = CreateContext();

        boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));

        target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        return context;
    }
}

public sealed class TrimToolTwoBoundaryTests
{
    [Fact]
    public void ControlClick_AfterFirstBoundary_ShouldSelectSecondCuttingEdge()
    {
        ToolContext context = CreateContextWithTwoBoundariesAndTarget(
            out _,
            out LineEntity rightBoundary,
            out _);
        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(
                new Point2D(7, 2),
                PointerModifiers.Control));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(rightBoundary.Id, tool.SecondBoundaryEntityId);
        Assert.Equal(TrimToolState.WaitingForTargetEntity, tool.State);
    }

    [Fact]
    public void PointerMove_WithTwoBoundaries_ShouldPreviewTwoOuterFragmentsAndHighlightedMiddleSegment()
    {
        ToolContext context = CreateContextWithTwoBoundariesAndTarget(
            out _,
            out _,
            out _);
        var tool = new TrimTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 2)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(7, 2), PointerModifiers.Control));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(2, tool.GetPreviewEntities().OfType<LineEntity>().Count());

        LineEntity highlighted = Assert.IsType<LineEntity>(
            Assert.Single(tool.GetHighlightedPreviewEntities()));

        Assert.Equal(new Point2D(3, 0), highlighted.Start);
        Assert.Equal(new Point2D(7, 0), highlighted.End);
    }

    [Fact]
    public void PointerPress_WithTwoBoundaries_WhenPickedMiddle_ShouldTrimMiddleSegment()
    {
        ToolContext context = CreateContextWithTwoBoundariesAndTarget(
            out LineEntity leftBoundary,
            out LineEntity rightBoundary,
            out LineEntity target);
        var tool = new TrimTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 2)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(7, 2), PointerModifiers.Control));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(context.Document.Entities.Contains(target.Id));

        List<LineEntity> fragments = context.Document.Entities.All
            .OfType<LineEntity>()
            .Where(line => !line.Id.Equals(leftBoundary.Id) &&
                           !line.Id.Equals(rightBoundary.Id))
            .OrderBy(line => line.Start.X)
            .ToList();

        Assert.Equal(2, fragments.Count);
        Assert.Equal(new Point2D(0, 0), fragments[0].Start);
        Assert.Equal(new Point2D(3, 0), fragments[0].End);
        Assert.Equal(new Point2D(7, 0), fragments[1].Start);
        Assert.Equal(new Point2D(10, 0), fragments[1].End);
    }

    [Fact]
    public void PointerPress_WithTwoBoundaries_WhenPickedOuterSide_ShouldTrimOuterSegment()
    {
        ToolContext context = CreateContextWithTwoBoundariesAndTarget(
            out LineEntity leftBoundary,
            out LineEntity rightBoundary,
            out LineEntity target);
        var tool = new TrimTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 2)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(7, 2), PointerModifiers.Control));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(context.Document.Entities.Contains(target.Id));

        LineEntity fragment = Assert.Single(
            context.Document.Entities.All
                .OfType<LineEntity>()
                .Where(line => !line.Id.Equals(leftBoundary.Id) &&
                               !line.Id.Equals(rightBoundary.Id)));

        Assert.Equal(new Point2D(3, 0), fragment.Start);
        Assert.Equal(new Point2D(10, 0), fragment.End);
    }

    [Fact]
    public void TrimWithTwoBoundaries_ShouldBeUndoable()
    {
        ToolContext context = CreateContextWithTwoBoundariesAndTarget(
            out _,
            out _,
            out LineEntity target);
        var tool = new TrimTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 2)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(7, 2), PointerModifiers.Control));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        LineEntity restored = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), restored.Start);
        Assert.Equal(new Point2D(10, 0), restored.End);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 0.5);
    }

    private static ToolContext CreateContextWithTwoBoundariesAndTarget(
        out LineEntity leftBoundary,
        out LineEntity rightBoundary,
        out LineEntity target)
    {
        ToolContext context = CreateContext();

        leftBoundary = new LineEntity(
            new Point2D(3, -5),
            new Point2D(3, 5));

        rightBoundary = new LineEntity(
            new Point2D(7, -5),
            new Point2D(7, 5));

        target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(leftBoundary);
        context.Document.AddEntity(rightBoundary);
        context.Document.AddEntity(target);

        return context;
    }
}

public sealed class TrimToolAdvancedCommandInputTests
{
    [Fact]
    public void HandleCommandInput_All_ShouldUseAllVisibleSupportedEntitiesAsCuttingEdges()
    {
        ToolContext context = CreateContextWithTwoBoundariesAndTarget(
            out _,
            out _,
            out _);
        var tool = new TrimTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Option("A", "All"),
            context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(TrimToolState.WaitingForTargetEntity, tool.State);
        Assert.NotNull(tool.BoundaryEntityId);
        Assert.NotNull(tool.SecondBoundaryEntityId);
    }

    [Fact]
    public void HandleCommandInput_Undo_ShouldUndoLastTrimInsideCurrentCommand()
    {
        ToolContext context = CreateContextWithTwoBoundariesAndTarget(
            out LineEntity leftBoundary,
            out LineEntity rightBoundary,
            out LineEntity target);
        var tool = new TrimTool();

        tool.HandleCommandInput(
            CommandInputSubmission.Option("A", "All"),
            context);
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.False(context.Document.Entities.Contains(target.Id));

        ToolResult undoResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("U", "Undo"),
            context);

        Assert.Equal(ToolResultKind.Updated, undoResult.Kind);
        Assert.True(context.Document.Entities.Contains(target.Id));
        Assert.True(context.Document.Entities.Contains(leftBoundary.Id));
        Assert.True(context.Document.Entities.Contains(rightBoundary.Id));
    }

    [Fact]
    public void HandleCommandInput_ConfirmWhileTrimming_ShouldFinishAndResetCommand()
    {
        ToolContext context = CreateContextWithTwoBoundariesAndTarget(
            out _,
            out _,
            out _);
        var tool = new TrimTool();

        tool.HandleCommandInput(
            CommandInputSubmission.Option("A", "All"),
            context);

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(TrimToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 0.5);
    }

    private static ToolContext CreateContextWithTwoBoundariesAndTarget(
        out LineEntity leftBoundary,
        out LineEntity rightBoundary,
        out LineEntity target)
    {
        ToolContext context = CreateContext();

        leftBoundary = new LineEntity(
            new Point2D(3, -5),
            new Point2D(3, 5));

        rightBoundary = new LineEntity(
            new Point2D(7, -5),
            new Point2D(7, 5));

        target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(leftBoundary);
        context.Document.AddEntity(rightBoundary);
        context.Document.AddEntity(target);

        return context;
    }
}
