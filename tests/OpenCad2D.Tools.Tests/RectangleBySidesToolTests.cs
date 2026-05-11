using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.Tools.Tests;

public sealed class RectangleBySidesToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForStartPoint()
    {
        var tool = new RectangleBySidesTool();

        Assert.Equal("Rectangle Sides", tool.Name);
        Assert.Equal(RectangleBySidesToolState.WaitingForStartPoint, tool.State);
        Assert.Null(tool.StartPoint);
        Assert.Null(tool.FirstSideEndPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreStartPoint()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(RectangleBySidesToolState.WaitingForFirstSideEndPoint, tool.State);
        Assert.Equal(new Point2D(1, 2), tool.StartPoint);
        Assert.Equal(new Point2D(1, 2), tool.CurrentPoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }


    [Fact]
    public void PointerMove_AfterStartPoint_ShouldShowFirstSidePreview()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));

        LineEntity? preview = tool.GetFirstSidePreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Point2D(0, 0), preview.Start);
        Assert.Equal(new Point2D(10, 0), preview.End);
    }

    [Fact]
    public void SecondPointerPress_ShouldStoreFirstSideEndPoint()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(11, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(RectangleBySidesToolState.WaitingForSecondSidePoint, tool.State);
        Assert.Equal(new Point2D(11, 2), tool.FirstSideEndPoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void PointerMove_AfterFirstSide_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(3, 5)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.True(preview.IsClosed);
        Assert.Equal(4, preview.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), preview.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), preview.Vertices[1]);
        Assert.Equal(new Point2D(10, 5), preview.Vertices[2]);
        Assert.Equal(new Point2D(0, 5), preview.Vertices[3]);
    }

    [Fact]
    public void ThirdPointerPress_ShouldCreateClosedPolylineRectangle()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, context.Document.Entities.Count);
        Assert.Equal(RectangleBySidesToolState.WaitingForStartPoint, tool.State);
        Assert.Null(tool.StartPoint);
        Assert.Null(tool.FirstSideEndPoint);
        Assert.Null(tool.CurrentPoint);

        var rectangle = Assert.Single(
            context.Document.Entities.All.OfType<PolylineEntity>());

        Assert.True(rectangle.IsClosed);
        Assert.Equal(4, rectangle.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), rectangle.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), rectangle.Vertices[1]);
        Assert.Equal(new Point2D(10, 5), rectangle.Vertices[2]);
        Assert.Equal(new Point2D(0, 5), rectangle.Vertices[3]);
    }

    [Fact]
    public void ThirdPointerPress_WithNegativeSide_ShouldCreateRectangleOnOppositeSide()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(3, -5)));

        var rectangle = Assert.Single(
            context.Document.Entities.All.OfType<PolylineEntity>());

        Assert.Equal(new Point2D(0, 0), rectangle.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), rectangle.Vertices[1]);
        Assert.Equal(new Point2D(10, -5), rectangle.Vertices[2]);
        Assert.Equal(new Point2D(0, -5), rectangle.Vertices[3]);
    }

    [Fact]
    public void ThirdPointerPress_WithRotatedFirstSide_ShouldCreateOrientedRectangle()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 5)));

        var horizontal = Assert.Single(
            context.Document.Entities.All.OfType<PolylineEntity>());

        Assert.Equal(new Point2D(10, 5), horizontal.Vertices[2]);

        var context2 = CreateContext();
        var rotatedTool = new RectangleBySidesTool();

        rotatedTool.OnPointerPressed(
            context2,
            new PointerInfo(new Point2D(0, 0)));

        rotatedTool.OnPointerPressed(
            context2,
            new PointerInfo(new Point2D(0, 10)));

        rotatedTool.OnPointerPressed(
            context2,
            new PointerInfo(new Point2D(-5, 2)));

        var rectangle = Assert.Single(
            context2.Document.Entities.All.OfType<PolylineEntity>());

        Assert.Equal(new Point2D(0, 0), rectangle.Vertices[0]);
        Assert.Equal(new Point2D(0, 10), rectangle.Vertices[1]);
        Assert.Equal(new Point2D(-5, 10), rectangle.Vertices[2]);
        Assert.Equal(new Point2D(-5, 0), rectangle.Vertices[3]);
    }

    [Fact]
    public void SecondPointerPress_WithSameStartPoint_ShouldKeepWaitingForFirstSideEndPoint()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RectangleBySidesToolState.WaitingForFirstSideEndPoint, tool.State);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void ThirdPointerPress_WithZeroHeight_ShouldKeepWaitingForSecondSidePoint()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RectangleBySidesToolState.WaitingForSecondSidePoint, tool.State);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void SecondPointerPress_WithPolarTracking_ShouldConstrainFirstSide()
    {
        var context = CreateContext();
        context.AngleConstraintSettings = AngleConstraintSettings.FromStep(45);
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 1)));

        Assert.Equal(new Point2D(Math.Sqrt(101), 0), tool.FirstSideEndPoint);
    }

    [Fact]
    public void ThirdPointerPress_ShouldCreateRectangleOnCurrentLayer()
    {
        CadDocument document = new();
        var layerId = new LayerId("Rooms");

        document.Layers.Add(
            new Layer(
                layerId,
                "Rooms",
                OpenCad2D.Core.Styling.CadColor.FromRgb(0, 180, 255),
                OpenCad2D.Core.Styling.LineWeight.FromMillimeters(0.25)));

        var context = new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            currentLayerId: layerId);

        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 5)));

        var rectangle = Assert.Single(
            document.Entities.All.OfType<PolylineEntity>());

        Assert.Equal(layerId, rectangle.LayerId);
    }

    [Fact]
    public void Cancel_ShouldResetWithoutCreatingRectangle()
    {
        var context = CreateContext();
        var tool = new RectangleBySidesTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(RectangleBySidesToolState.WaitingForStartPoint, tool.State);
        Assert.Null(tool.StartPoint);
        Assert.Null(tool.FirstSideEndPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.Equal(0, context.Document.Entities.Count);
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
