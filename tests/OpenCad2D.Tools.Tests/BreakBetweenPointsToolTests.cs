using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class BreakBetweenPointsToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForTargetEntity()
    {
        var tool = new BreakBetweenPointsTool();

        Assert.Equal("Break Segment", tool.Name);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(tool.TargetEntityId);
        Assert.Null(tool.FirstBreakPoint);
        Assert.Null(tool.CurrentSecondBreakPoint);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void FirstPointerPress_WithoutLine_ShouldNotStartTool()
    {
        var context = CreateContext();
        var tool = new BreakBetweenPointsTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(tool.TargetEntityId);
    }

    [Fact]
    public void FirstPointerPress_OnLine_ShouldSelectTargetLine()
    {
        var context = CreateContextWithLine(out LineEntity line);
        var tool = new BreakBetweenPointsTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0.1)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForFirstBreakPoint, tool.State);
        Assert.Equal(line.Id, tool.TargetEntityId);
        Assert.Equal(new Point2D(5, 0), context.CurrentBasePoint);
    }

    [Fact]
    public void SecondPointerPress_ShouldAcceptFirstBreakPoint()
    {
        var context = CreateContextWithLine(out _);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForSecondBreakPoint, tool.State);
        Assert.Equal(new Point2D(3, 0), tool.FirstBreakPoint);
        Assert.Equal(new Point2D(3, 0), context.CurrentBasePoint);
    }

    [Fact]
    public void PointerMove_AfterFirstBreakPoint_ShouldUpdatePreview()
    {
        var context = CreateContextWithLine(out _);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(7, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(7, 0), tool.CurrentSecondBreakPoint);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        Assert.Equal(2, preview.Count);
    }

    [Fact]
    public void ThirdPointerPress_ShouldRemoveSegmentBetweenBreakPoints()
    {
        var context = CreateContextWithLine(out LineEntity originalLine);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(7, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(context.CurrentBasePoint);
        Assert.False(context.Document.Entities.Contains(originalLine.Id));

        IReadOnlyList<LineEntity> lines = context.Document.Entities.All
            .OfType<LineEntity>()
            .OrderBy(line => line.Start.X)
            .ToList();

        Assert.Equal(2, lines.Count);
        Assert.Equal(new Point2D(0, 0), lines[0].Start);
        Assert.Equal(new Point2D(3, 0), lines[0].End);
        Assert.Equal(new Point2D(7, 0), lines[1].Start);
        Assert.Equal(new Point2D(10, 0), lines[1].End);
    }

    [Fact]
    public void ThirdPointerPress_WithReversedBreakPoints_ShouldRemoveSegmentBetweenBreakPoints()
    {
        var context = CreateContextWithLine(out _);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(7, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 0)));

        IReadOnlyList<LineEntity> lines = context.Document.Entities.All
            .OfType<LineEntity>()
            .OrderBy(line => line.Start.X)
            .ToList();

        Assert.Equal(2, lines.Count);
        Assert.Equal(new Point2D(0, 0), lines[0].Start);
        Assert.Equal(new Point2D(3, 0), lines[0].End);
        Assert.Equal(new Point2D(7, 0), lines[1].Start);
        Assert.Equal(new Point2D(10, 0), lines[1].End);
    }

    [Fact]
    public void ThirdPointerPress_WithSameBreakPoint_ShouldNotModifyLine()
    {
        var context = CreateContextWithLine(out LineEntity originalLine);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForSecondBreakPoint, tool.State);
        Assert.True(context.Document.Entities.Contains(originalLine.Id));
        Assert.Single(context.Document.Entities.All.OfType<LineEntity>());
    }

    [Fact]
    public void BreakBetweenPoints_ShouldBeUndoable()
    {
        var context = CreateContextWithLine(out LineEntity originalLine);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(7, 0)));

        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        LineEntity line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(originalLine.Id, line.Id);
        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContextWithLine(out _);
        var tool = new BreakBetweenPointsTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(3, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(tool.TargetEntityId);
        Assert.Null(tool.FirstBreakPoint);
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

    private static ToolContext CreateContextWithLine(out LineEntity line)
    {
        var document = new CadDocument();
        var commandHistory = new CommandHistory();
        var context = new ToolContext(
            document,
            commandHistory,
            new SnapService(),
            selectionTolerance: 0.5);

        line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        return context;
    }
}
