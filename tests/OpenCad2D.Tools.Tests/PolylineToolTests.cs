using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.Tools.Tests;

public sealed class PolylineToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstPoint()
    {
        var tool = new PolylineTool();

        Assert.Equal("Polyline", tool.Name);
        Assert.Equal(PolylineToolState.WaitingForFirstPoint, tool.State);
        Assert.Empty(tool.Vertices);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreFirstVertex()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(PolylineToolState.CollectingVertices, tool.State);
        Assert.Single(tool.Vertices);
        Assert.Equal(new Point2D(10, 20), tool.Vertices[0]);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void PointerMove_AfterFirstVertex_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(10, 0), tool.CurrentPoint);

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.False(preview.IsClosed);
        Assert.Equal(2, preview.Vertices.Count);
        Assert.Equal(new Point2D(0, 0), preview.Vertices[0]);
        Assert.Equal(new Point2D(10, 0), preview.Vertices[1]);
    }

    [Fact]
    public void AdditionalPointerPress_ShouldAddVertexAndUpdateBasePoint()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(2, tool.Vertices.Count);
        Assert.Equal(new Point2D(10, 0), tool.Vertices[1]);
        Assert.Equal(new Point2D(10, 0), context.CurrentBasePoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void CompleteOpen_WithTwoVertices_ShouldCreateOpenPolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.CompleteOpen(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(PolylineToolState.WaitingForFirstPoint, tool.State);
        Assert.Empty(tool.Vertices);
        Assert.Null(context.CurrentBasePoint);

        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.False(polyline.IsClosed);
        Assert.Equal(2, polyline.Vertices.Count);
    }

    [Fact]
    public void CompleteClosed_WithThreeVertices_ShouldCreateClosedPolyline()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 10)));

        ToolResult result = tool.CompleteClosed(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        PolylineEntity polyline = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(polyline.IsClosed);
        Assert.Equal(3, polyline.Vertices.Count);
    }

    [Fact]
    public void CompleteOpen_WithLessThanTwoVertices_ShouldNotCreateEntity()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.CompleteOpen(context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(PolylineToolState.CollectingVertices, tool.State);
    }

    [Fact]
    public void CompleteClosed_WithLessThanThreeVertices_ShouldNotCreateEntity()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.CompleteClosed(context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(PolylineToolState.CollectingVertices, tool.State);
    }

    [Fact]
    public void PointerMove_WithOrthoEnabled_ShouldConstrainPreviewFromLastVertex()
    {
        var context = CreateContext();
        context.IsOrthoEnabled = true;
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(16, 8)));

        Assert.Equal(new Point2D(10, 8), tool.CurrentPoint);

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Point2D(10, 8), preview.Vertices[^1]);
    }

    [Fact]
    public void CompleteOpen_ShouldCreatePolylineOnCurrentLayer()
    {
        CadDocument document = new();
        var layerId = new LayerId("Polylines");

        document.Layers.Add(
            new Layer(
                layerId,
                "Polylines",
                CadColor.FromRgb(255, 0, 0),
                LineWeight.FromMillimeters(0.25)));

        var context = new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            currentLayerId: layerId);

        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.CompleteOpen(context);

        PolylineEntity polyline = Assert.Single(document.Entities.All.OfType<PolylineEntity>());

        Assert.Equal(layerId, polyline.LayerId);
    }

    [Fact]
    public void CreatedPolyline_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.CompleteOpen(context);

        Assert.Equal(1, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void Cancel_ShouldClearVerticesAndBasePoint()
    {
        var context = CreateContext();
        var tool = new PolylineTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(PolylineToolState.WaitingForFirstPoint, tool.State);
        Assert.Empty(tool.Vertices);
        Assert.Null(context.CurrentBasePoint);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }
}
