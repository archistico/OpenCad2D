using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Coordinates;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Navigation;

namespace OpenCad2D.Tools.Tests;

public sealed class ZoomWindowToolTests
{
    [Fact]
    public void OnPointerPressed_FirstPoint_ShouldStartWindow()
    {
        var tool = new ZoomWindowTool();
        ToolContext context = CreateContext();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(new Point2D(1, 2), tool.FirstPoint);
        Assert.Equal(new Point2D(1, 2), tool.CurrentPoint);
        Assert.Null(tool.CompletedWindow);
    }

    [Fact]
    public void OnPointerMoved_AfterFirstPoint_ShouldExposePreviewWindow()
    {
        var tool = new ZoomWindowTool();
        ToolContext context = CreateContext();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(8, 10)));

        BoundingBox2D? preview = tool.GetPreviewWindow();

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.NotNull(preview);
        Assert.Equal(1, preview.Value.MinX);
        Assert.Equal(2, preview.Value.MinY);
        Assert.Equal(8, preview.Value.MaxX);
        Assert.Equal(10, preview.Value.MaxY);
    }

    [Fact]
    public void OnPointerReleased_AfterFirstPoint_ShouldCompleteWindowAndResetPreview()
    {
        var tool = new ZoomWindowTool();
        ToolContext context = CreateContext();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 10)));

        ToolResult result = tool.OnPointerReleased(
            context,
            new PointerInfo(new Point2D(2, 4)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.Null(tool.GetPreviewWindow());
        Assert.NotNull(tool.CompletedWindow);
        Assert.Equal(2, tool.CompletedWindow.Value.MinX);
        Assert.Equal(4, tool.CompletedWindow.Value.MinY);
        Assert.Equal(10, tool.CompletedWindow.Value.MaxX);
        Assert.Equal(10, tool.CompletedWindow.Value.MaxY);
    }

    [Fact]
    public void Cancel_ShouldClearPendingAndCompletedWindow()
    {
        var tool = new ZoomWindowTool();
        ToolContext context = CreateContext();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 5)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.Null(tool.CompletedWindow);
        Assert.Null(tool.GetPreviewWindow());
    }

    private static ToolContext CreateContext()
    {
        CadDocument document = new();
        CommandHistory history = new();
        SelectionSet selectionSet = new();

        return new ToolContext(
            document,
            history,
            new SnapService(),
            selectionSet: selectionSet,
            selectionService: new SelectionService(),
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 8,
            selectionTolerance: 6,
            selectionDragThreshold: 4,
            currentLayerId: LayerId.Default,
            currentUcs: CoordinateSystem2D.World);
    }
}
