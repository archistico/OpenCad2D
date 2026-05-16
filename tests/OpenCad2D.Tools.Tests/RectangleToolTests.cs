using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Input;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;

namespace OpenCad2D.Tools.Tests;

public sealed class RectangleToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstPoint()
    {
        var tool = new RectangleTool();

        Assert.Equal("Rectangle", tool.Name);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_ShouldStoreFirstCorner()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

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
    public void PointerMove_AfterFirstCorner_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(10, 20), tool.CurrentPoint);

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.True(preview.IsClosed);
        Assert.Equal(4, preview.Vertices.Count);

        Assert.Equal(new Point2D(1, 2), preview.Vertices[0]);
        Assert.Equal(new Point2D(10, 2), preview.Vertices[1]);
        Assert.Equal(new Point2D(10, 20), preview.Vertices[2]);
        Assert.Equal(new Point2D(1, 20), preview.Vertices[3]);
    }

    [Fact]
    public void PointerMove_WithSameX_ShouldNotCreatePreviewEntity()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(1, 20)));

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.Null(preview);
    }

    [Fact]
    public void PointerMove_WithSameY_ShouldNotCreatePreviewEntity()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 2)));

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.Null(preview);
    }

    [Fact]
    public void SecondPointerPress_ShouldCreateClosedPolylineRectangle()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

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

        var rectangle = Assert.Single(
            context.Document.Entities.All.OfType<PolylineEntity>());

        Assert.True(rectangle.IsClosed);
        Assert.Equal(4, rectangle.Vertices.Count);

        Assert.Equal(new Point2D(1, 2), rectangle.Vertices[0]);
        Assert.Equal(new Point2D(10, 2), rectangle.Vertices[1]);
        Assert.Equal(new Point2D(10, 20), rectangle.Vertices[2]);
        Assert.Equal(new Point2D(1, 20), rectangle.Vertices[3]);
    }

    [Fact]
    public void SecondPointerPress_WithOppositeDirection_ShouldCreateClosedPolylineRectangle()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        var rectangle = Assert.Single(
            context.Document.Entities.All.OfType<PolylineEntity>());

        Assert.True(rectangle.IsClosed);
        Assert.Equal(4, rectangle.Vertices.Count);

        Assert.Equal(new Point2D(10, 20), rectangle.Vertices[0]);
        Assert.Equal(new Point2D(1, 20), rectangle.Vertices[1]);
        Assert.Equal(new Point2D(1, 2), rectangle.Vertices[2]);
        Assert.Equal(new Point2D(10, 2), rectangle.Vertices[3]);
    }

    [Fact]
    public void SecondPointerPress_WithSameX_ShouldNotCreateRectangleAndShouldKeepWaitingForSecondPoint()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 20)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(new Point2D(1, 2), tool.FirstPoint);
    }

    [Fact]
    public void SecondPointerPress_WithSameY_ShouldNotCreateRectangleAndShouldKeepWaitingForSecondPoint()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(new Point2D(1, 2), tool.FirstPoint);
    }

    [Fact]
    public void Rectangle_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(1, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void FirstPointerPress_WithEndpointSnap_ShouldUseSnappedFirstCorner()
    {
        CadDocument document = new();

        var existingLine = new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(existingLine);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        Assert.Equal(new Point2D(100, 100), tool.FirstPoint);
    }

    [Fact]
    public void SecondPointerPress_WithEndpointSnap_ShouldUseSnappedOppositeCorner()
    {
        CadDocument document = new();

        var existingLine = new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(existingLine);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(199, 101)));

        var rectangle = Assert.Single(
            document.Entities.All.OfType<PolylineEntity>());

        Assert.Equal(new Point2D(0, 0), rectangle.Vertices[0]);
        Assert.Equal(new Point2D(200, 0), rectangle.Vertices[1]);
        Assert.Equal(new Point2D(200, 100), rectangle.Vertices[2]);
        Assert.Equal(new Point2D(0, 100), rectangle.Vertices[3]);
    }

    [Fact]
    public void Cancel_ShouldResetWithoutCreatingRectangle()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
        Assert.Equal(0, context.Document.Entities.Count);
    }

    [Fact]
    public void SecondPointerPress_ShouldCreateRectangleOnCurrentLayer()
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

        var tool = new RectangleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 5)));

        var rectangle = Assert.Single(
            document.Entities.All.OfType<PolylineEntity>());

        Assert.Equal(layerId, rectangle.LayerId);
        Assert.True(rectangle.IsClosed);
    }


    [Fact]
    public void CommandInput_ShouldCreateRectangleFromTwoCorners()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        ToolResult firstResult = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("1,2", new Point2D(1, 2)),
            context);
        ToolResult secondResult = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("@9,18", new Point2D(10, 20)),
            context);

        Assert.Equal(ToolResultKind.Started, firstResult.Kind);
        Assert.Equal(ToolResultKind.Completed, secondResult.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);

        var rectangle = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.True(rectangle.IsClosed);
        Assert.Equal(new Point2D(1, 2), rectangle.Vertices[0]);
        Assert.Equal(new Point2D(10, 20), rectangle.Vertices[2]);
    }

    [Fact]
    public void GetPromptState_ShouldExposeRectangleCommandSteps()
    {
        var context = CreateContext();
        var tool = new RectangleTool();

        CommandPromptState firstPrompt = tool.GetPromptState(context);

        Assert.Equal("RECTANGLE", firstPrompt.CommandName);
        Assert.Equal(CommandInputKind.Point, firstPrompt.ExpectedInput);

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("1,2", new Point2D(1, 2)),
            context);

        CommandPromptState secondPrompt = tool.GetPromptState(context);

        Assert.Equal("RECTANGLE", secondPrompt.CommandName);
        Assert.Equal(CommandInputKind.PointOrDistance, secondPrompt.ExpectedInput);
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