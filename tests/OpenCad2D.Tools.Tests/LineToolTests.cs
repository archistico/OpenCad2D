using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.Tools.Tests;

public sealed class LineToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstPoint()
    {
        var tool = new LineTool();

        Assert.Equal("Line", tool.Name);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreStartPoint()
    {
        var context = CreateContext();
        var tool = new LineTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(new Point2D(1, 2), tool.FirstPoint);
        Assert.Equal(new Point2D(1, 2), tool.CurrentPoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void PointerMove_AfterFirstPoint_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(10, 20), tool.CurrentPoint);

        LineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Point2D(1, 2), preview.Start);
        Assert.Equal(new Point2D(10, 20), preview.End);
    }

    [Fact]
    public void SecondPointerPress_ShouldCreateLineEntity()
    {
        var context = CreateContext();
        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, context.Document.Entities.Count);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(1, 2), line.Start);
        Assert.Equal(new Point2D(10, 20), line.End);
    }

    [Fact]
    public void SecondPointerPress_WithSamePoint_ShouldNotCreateLine()
    {
        var context = CreateContext();
        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContext();
        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void CreatedLine_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(1, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void FirstPointerPress_WithEndpointSnap_ShouldUseSnappedPoint()
    {
        var document = new CadDocument();

        var existingLine = new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(existingLine);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        Assert.Equal(new Point2D(100, 100), tool.FirstPoint);
    }

    [Fact]
    public void SecondPointerPress_WithEndpointSnap_ShouldUseSnappedPoint()
    {
        var document = new CadDocument();

        var existingLine = new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(existingLine);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(199, 101)));

        var created = context.Document.Entities.All
            .OfType<LineEntity>()
            .Single(line => line.Id != existingLine.Id);

        Assert.Equal(new Point2D(0, 0), created.Start);
        Assert.Equal(new Point2D(200, 100), created.End);
    }

    [Fact]
    public void PointerMove_WithEndpointSnap_ShouldUpdatePreviewWithSnappedPoint()
    {
        var document = new CadDocument();

        var existingLine = new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(existingLine);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(199, 101)));

        Assert.Equal(new Point2D(200, 100), tool.CurrentPoint);

        LineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Point2D(0, 0), preview.Start);
        Assert.Equal(new Point2D(200, 100), preview.End);
    }

    [Fact]
    public void SecondPointerPress_WithPerpendicularSnap_ShouldCreatePerpendicularLine()
    {
        var document = new CadDocument();

        var baseLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(baseLine);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Perpendicular,
            snapTolerance: 2);

        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 5)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5.1, 0.2)));

        var created = context.Document.Entities.All
            .OfType<LineEntity>()
            .Single(line => line.Id != baseLine.Id);

        Assert.Equal(new Point2D(5, 5), created.Start);
        Assert.Equal(new Point2D(5, 0), created.End);
    }

    [Fact]
    public void SecondPointerPress_WithGridSnap_ShouldUseSnappedPoint()
    {
        var context = CreateContext(
            enabledSnaps: SnapKind.Grid,
            snapTolerance: 10);

        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(23.2, 46.8)));

        var line = Assert.Single(
            context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(20, 50), line.End);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionSet: null,
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance);
    }
}