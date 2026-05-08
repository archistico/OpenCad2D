using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Tests;

public sealed class ToolControllerTests
{
    [Fact]
    public void Constructor_ShouldSetInitialTool()
    {
        ToolContext context = CreateContext();
        var tool = new FakeTool("First");

        var controller = new ToolController(
            context,
            tool);

        Assert.Equal(tool, controller.ActiveTool);
        Assert.Equal("First", controller.ActiveToolName);
        Assert.Equal(ToolResultKind.None, controller.LastResult.Kind);
    }

    [Fact]
    public void OnPointerPressed_ShouldForwardEventToActiveTool()
    {
        ToolContext context = CreateContext();
        var tool = new FakeTool("First");

        var controller = new ToolController(
            context,
            tool);

        ToolResult result = controller.OnPointerPressed(
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ToolResultKind.Started, controller.LastResult.Kind);
        Assert.Equal(1, tool.PointerPressedCount);
        Assert.Equal(new Point2D(1, 2), tool.LastPointerPoint);
    }

    [Fact]
    public void OnPointerMoved_ShouldForwardEventToActiveTool()
    {
        ToolContext context = CreateContext();
        var tool = new FakeTool("First");

        var controller = new ToolController(
            context,
            tool);

        ToolResult result = controller.OnPointerMoved(
            new PointerInfo(new Point2D(5, 6)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(1, tool.PointerMovedCount);
        Assert.Equal(new Point2D(5, 6), tool.LastPointerPoint);
    }

    [Fact]
    public void OnPointerReleased_ShouldForwardEventToActiveTool()
    {
        ToolContext context = CreateContext();
        var tool = new FakeTool("First");

        var controller = new ToolController(
            context,
            tool);

        ToolResult result = controller.OnPointerReleased(
            new PointerInfo(new Point2D(7, 8)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, tool.PointerReleasedCount);
        Assert.Equal(new Point2D(7, 8), tool.LastPointerPoint);
    }

    [Fact]
    public void CancelActiveTool_ShouldCancelCurrentTool()
    {
        ToolContext context = CreateContext();
        var tool = new FakeTool("First");

        var controller = new ToolController(
            context,
            tool);

        ToolResult result = controller.CancelActiveTool();

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(1, tool.CancelCount);
        Assert.Equal(ToolResultKind.Cancelled, controller.LastResult.Kind);
    }

    [Fact]
    public void SetActiveTool_ShouldReplaceActiveTool()
    {
        ToolContext context = CreateContext();
        var first = new FakeTool("First");
        var second = new FakeTool("Second");

        var controller = new ToolController(
            context,
            first);

        controller.SetActiveTool(second);

        Assert.Equal(second, controller.ActiveTool);
        Assert.Equal("Second", controller.ActiveToolName);
    }

    [Fact]
    public void SetActiveTool_ByDefault_ShouldCancelPreviousTool()
    {
        ToolContext context = CreateContext();
        var first = new FakeTool("First");
        var second = new FakeTool("Second");

        var controller = new ToolController(
            context,
            first);

        ToolResult result = controller.SetActiveTool(second);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(1, first.CancelCount);
        Assert.Equal(0, second.CancelCount);
        Assert.Equal(second, controller.ActiveTool);
    }

    [Fact]
    public void SetActiveTool_WithCancelCurrentToolFalse_ShouldNotCancelPreviousTool()
    {
        ToolContext context = CreateContext();
        var first = new FakeTool("First");
        var second = new FakeTool("Second");

        var controller = new ToolController(
            context,
            first);

        ToolResult result = controller.SetActiveTool(
            second,
            cancelCurrentTool: false);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, first.CancelCount);
        Assert.Equal(second, controller.ActiveTool);
    }

    [Fact]
    public void EventsAfterToolChange_ShouldGoToNewActiveTool()
    {
        ToolContext context = CreateContext();
        var first = new FakeTool("First");
        var second = new FakeTool("Second");

        var controller = new ToolController(
            context,
            first);

        controller.SetActiveTool(second);

        controller.OnPointerPressed(
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(0, first.PointerPressedCount);
        Assert.Equal(1, second.PointerPressedCount);
        Assert.Equal(new Point2D(10, 20), second.LastPointerPoint);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }

    private sealed class FakeTool : ICadTool
    {
        public FakeTool(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public int PointerPressedCount { get; private set; }

        public int PointerMovedCount { get; private set; }

        public int PointerReleasedCount { get; private set; }

        public int CancelCount { get; private set; }

        public Point2D? LastPointerPoint { get; private set; }

        public ToolResult OnPointerPressed(
            ToolContext context,
            PointerInfo pointer)
        {
            PointerPressedCount++;
            LastPointerPoint = pointer.ModelPoint;

            return ToolResult.Started("Pressed.");
        }

        public ToolResult OnPointerMoved(
            ToolContext context,
            PointerInfo pointer)
        {
            PointerMovedCount++;
            LastPointerPoint = pointer.ModelPoint;

            return ToolResult.Updated("Moved.");
        }

        public ToolResult OnPointerReleased(
            ToolContext context,
            PointerInfo pointer)
        {
            PointerReleasedCount++;
            LastPointerPoint = pointer.ModelPoint;

            return ToolResult.Completed("Released.");
        }

        public ToolResult Cancel(ToolContext context)
        {
            CancelCount++;

            return ToolResult.Cancelled("Cancelled.");
        }
    }
}