using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class ExtendToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForBoundaryEntity()
    {
        var tool = new ExtendTool();

        Assert.Equal("Extend", tool.Name);
        Assert.Equal(ExtendToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void FirstPointerPress_WithoutLine_ShouldNotStartTool()
    {
        var context = CreateContext();
        var tool = new ExtendTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
    }

    [Fact]
    public void FirstPointerPress_OnLine_ShouldSelectBoundaryLine()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out LineEntity boundary,
            out _);
        var tool = new ExtendTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForTargetEntity, tool.State);
        Assert.Equal(boundary.Id, tool.BoundaryEntityId);
        Assert.NotNull(context.CurrentBasePoint);
    }

    [Fact]
    public void PointerMove_AfterBoundary_ShouldUpdatePreview()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        LineEntity line = Assert.IsType<LineEntity>(Assert.Single(preview));

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    [Fact]
    public void SecondPointerPress_NearEnd_ShouldExtendLineToBoundary()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out LineEntity target);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForTargetEntity, tool.State);
        Assert.True(context.Document.Entities.Contains(target.Id));

        LineEntity extended = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), extended.Start);
        Assert.Equal(new Point2D(10, 0), extended.End);
    }

    [Fact]
    public void SecondPointerPress_NearStart_ShouldExtendStartToBoundary()
    {
        var context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(-5, -5),
            new Point2D(-5, 5));

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(-5, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        LineEntity extended = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(-5, 0), extended.Start);
        Assert.Equal(new Point2D(5, 0), extended.End);
    }

    [Fact]
    public void SecondPointerPress_WhenIntersectionIsInsideTargetSegment_ShouldNotExtend()
    {
        var context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);

        LineEntity unchanged = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), unchanged.Start);
        Assert.Equal(new Point2D(10, 0), unchanged.End);
    }

    [Fact]
    public void Extend_ShouldBeUndoable()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out LineEntity target);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        LineEntity restored = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), restored.Start);
        Assert.Equal(new Point2D(5, 0), restored.End);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForBoundaryEntity, tool.State);
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
            new Point2D(10, -5),
            new Point2D(10, 5));

        target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        return context;
    }
}
