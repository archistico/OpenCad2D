using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class EllipseToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForCenter()
    {
        var tool = new EllipseTool();

        Assert.Equal("Ellipse", tool.Name);
        Assert.Equal(EllipseToolState.WaitingForCenter, tool.State);
        Assert.Null(tool.Center);
        Assert.Null(tool.MajorAxisPoint);
    }

    [Fact]
    public void PointerPresses_ShouldCreateEllipseEntity()
    {
        var context = CreateContext();
        var tool = new EllipseTool();

        ToolResult center = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        ToolResult major = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        ToolResult minor = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 4)));

        Assert.Equal(ToolResultKind.Started, center.Kind);
        Assert.Equal(ToolResultKind.Started, major.Kind);
        Assert.Equal(ToolResultKind.Completed, minor.Kind);
        Assert.Equal(EllipseToolState.WaitingForCenter, tool.State);
        Assert.Null(context.CurrentBasePoint);

        EllipseEntity ellipse = Assert.Single(context.Document.Entities.All.OfType<EllipseEntity>());
        Assert.Equal(new Point2D(0, 0), ellipse.Center);
        Assert.Equal(new Vector2D(10, 0), ellipse.MajorAxis);
        Assert.Equal(4, ellipse.MinorRadius);
    }

    [Fact]
    public void PointerMove_AfterMajorAxis_ShouldUpdatePreview()
    {
        var context = CreateContext();
        var tool = new EllipseTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 4)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);

        EllipseEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        Assert.Equal(new Vector2D(10, 0), preview.MajorAxis);
        Assert.Equal(4, preview.MinorRadius);
    }

    [Fact]
    public void ThirdPointerPress_WithZeroMinorRadius_ShouldNotCreateEllipse()
    {
        var context = CreateContext();
        var tool = new EllipseTool();

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
        Assert.Equal(0, context.Document.Entities.Count);
        Assert.Equal(EllipseToolState.WaitingForMinorRadius, tool.State);
    }

    [Fact]
    public void CreatedEllipse_ShouldBeUndoable()
    {
        var context = CreateContext();
        var tool = new EllipseTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 4)));

        Assert.Equal(1, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void CommandInput_ShouldCreateEllipse()
    {
        var context = CreateContext();
        var tool = new EllipseTool();

        ToolResult first = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)),
            context);
        ToolResult second = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)),
            context);
        ToolResult third = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,4", new Point2D(0, 4)),
            context);

        Assert.Equal(ToolResultKind.Started, first.Kind);
        Assert.Equal(ToolResultKind.Started, second.Kind);
        Assert.Equal(ToolResultKind.Completed, third.Kind);

        EllipseEntity ellipse = Assert.Single(context.Document.Entities.All.OfType<EllipseEntity>());
        Assert.Equal(new Vector2D(10, 0), ellipse.MajorAxis);
        Assert.Equal(4, ellipse.MinorRadius);
    }

    [Fact]
    public void GetPromptState_ShouldExposeEllipseCommandSteps()
    {
        var context = CreateContext();
        var tool = new EllipseTool();

        CommandPromptState firstPrompt = tool.GetPromptState(context);
        Assert.Equal("ELLIPSE", firstPrompt.CommandName);
        Assert.Equal(CommandInputKind.Point, firstPrompt.ExpectedInput);

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)),
            context);
        CommandPromptState secondPrompt = tool.GetPromptState(context);
        Assert.Equal(CommandInputKind.Point, secondPrompt.ExpectedInput);

        tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("10,0", new Point2D(10, 0)),
            context);
        CommandPromptState thirdPrompt = tool.GetPromptState(context);
        Assert.Equal(CommandInputKind.PointOrDistance, thirdPrompt.ExpectedInput);
    }

    [Fact]
    public void MajorAxisPointerPress_WithEndpointSnap_ShouldUseSnappedAxisPoint()
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

        var tool = new EllipseTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(199, 101)));

        Assert.Equal(new Point2D(200, 100), tool.MajorAxisPoint);
        Assert.Equal(new Point2D(200, 100), tool.CurrentPoint);
    }

    [Fact]
    public void MajorAxisPointerMove_WithEndpointSnap_ShouldPreviewSnappedAxisPoint()
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

        var tool = new EllipseTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(199, 101)));

        Assert.Equal(new Point2D(200, 100), tool.CurrentPoint);

        EllipseEntity preview = Assert.IsType<EllipseEntity>(
            Assert.Single(tool.GetPreviewEntities(context)));

        Assert.Equal(new Vector2D(200, 100), preview.MajorAxis);
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
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance);
    }
}
