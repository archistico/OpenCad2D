using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Tests;

public sealed class TwoPointToolBaseTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstPoint()
    {
        var tool = new FakeTwoPointTool();

        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreFirstPoint()
    {
        var context = CreateContext();
        var tool = new FakeTwoPointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(new Point2D(1, 2), tool.FirstPoint);
        Assert.Equal(new Point2D(1, 2), tool.CurrentPoint);
        Assert.Equal(new Point2D(1, 2), tool.FirstPointReceived);
    }

    [Fact]
    public void PointerMove_AfterFirstPoint_ShouldUpdateCurrentPoint()
    {
        var context = CreateContext();
        var tool = new FakeTwoPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(10, 20), tool.CurrentPoint);
        Assert.Equal(new Point2D(1, 2), tool.PreviewFirstPointReceived);
        Assert.Equal(new Point2D(10, 20), tool.PreviewCurrentPointReceived);
    }

    [Fact]
    public void SecondPointerPress_ShouldCallSecondPointHandlerAndReset()
    {
        var context = CreateContext();
        var tool = new FakeTwoPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(new Point2D(1, 2), tool.SecondHandlerFirstPointReceived);
        Assert.Equal(new Point2D(10, 20), tool.SecondHandlerSecondPointReceived);

        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void SecondPointerPress_WithSamePoint_ShouldNotCallSecondPointHandler()
    {
        var context = CreateContext();
        var tool = new FakeTwoPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Null(tool.SecondHandlerFirstPointReceived);
        Assert.Null(tool.SecondHandlerSecondPointReceived);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContext();
        var tool = new FakeTwoPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_WithSnap_ShouldUseSnappedPoint()
    {
        var document = new CadDocument();

        var existingLine = new OpenCad2D.Core.Entities.LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(existingLine);

        var context = CreateContext(
            document,
            SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new FakeTwoPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        Assert.Equal(new Point2D(100, 100), tool.FirstPoint);
        Assert.Equal(new Point2D(100, 100), tool.FirstPointReceived);
    }

    [Fact]
    public void SecondPointerPress_WithSnap_ShouldUseSnappedPoint()
    {
        var document = new CadDocument();

        var existingLine = new OpenCad2D.Core.Entities.LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(existingLine);

        var context = CreateContext(
            document,
            SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new FakeTwoPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(199, 101)));

        Assert.Equal(new Point2D(0, 0), tool.SecondHandlerFirstPointReceived);
        Assert.Equal(new Point2D(200, 100), tool.SecondHandlerSecondPointReceived);
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

    private sealed class FakeTwoPointTool : TwoPointToolBase
    {
        public override string Name => "Fake";

        public Point2D? FirstPointReceived { get; private set; }

        public Point2D? PreviewFirstPointReceived { get; private set; }

        public Point2D? PreviewCurrentPointReceived { get; private set; }

        public Point2D? SecondHandlerFirstPointReceived { get; private set; }

        public Point2D? SecondHandlerSecondPointReceived { get; private set; }

        protected override ToolResult OnFirstPointSelected(
            ToolContext context,
            Point2D firstPoint)
        {
            FirstPointReceived = firstPoint;

            return ToolResult.Started("First point selected.");
        }

        protected override ToolResult OnPreviewUpdated(
            ToolContext context,
            Point2D firstPoint,
            Point2D currentPoint)
        {
            PreviewFirstPointReceived = firstPoint;
            PreviewCurrentPointReceived = currentPoint;

            return ToolResult.Updated();
        }

        protected override ToolResult OnSecondPointSelected(
            ToolContext context,
            Point2D firstPoint,
            Point2D secondPoint)
        {
            SecondHandlerFirstPointReceived = firstPoint;
            SecondHandlerSecondPointReceived = secondPoint;

            return ToolResult.Completed("Completed.");
        }
    }
}