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

    [Fact]
    public void MajorAxisPointerMove_WithOrthoEnabled_ShouldConstrainPreviewAxis()
    {
        var context = CreateContext(isOrthoEnabled: true);
        var tool = new EllipseTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 4)));

        Assert.Equal(new Point2D(10, 0), tool.CurrentPoint);

        EllipseEntity preview = Assert.IsType<EllipseEntity>(
            Assert.Single(tool.GetPreviewEntities(context)));

        Assert.Equal(new Vector2D(10, 0), preview.MajorAxis);
    }

    [Fact]
    public void MajorAxisPointerPress_WithEndpointSnapAndPolarTracking_ShouldApplySnapThenPolar()
    {
        var document = new CadDocument();

        var snapSource = new LineEntity(
            new Point2D(10, 10),
            new Point2D(20, 20));

        document.AddEntity(snapSource);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5,
            angleConstraintSettings: AngleConstraintSettings.FromStep(90));

        var tool = new EllipseTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(11, 9)));

        AssertPointNear(
            new Point2D(0, Math.Sqrt(200)),
            tool.MajorAxisPoint!.Value);
        AssertPointNear(
            new Point2D(0, Math.Sqrt(200)),
            tool.CurrentPoint!.Value);
    }

    [Fact]
    public void GetPreviewDescriptor_WhileSelectingMajorAxis_ShouldExposeAxisLineAndMarkers()
    {
        var context = CreateContext();
        var tool = new EllipseTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 0)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        Assert.Single(descriptor.Entities.OfType<EllipseEntity>());

        ToolPreviewLine axis = Assert.Single(descriptor.Lines);
        Assert.Equal(ToolPreviewLineKind.Axis, axis.Kind);
        Assert.Equal(Point2D.Origin, axis.Start);
        Assert.Equal(new Point2D(10, 0), axis.End);

        Assert.Contains(
            descriptor.Markers,
            marker => marker.Position == Point2D.Origin);
        Assert.Contains(
            descriptor.Markers,
            marker => marker.Position == new Point2D(10, 0));
    }

    [Fact]
    public void GetPreviewDescriptor_WhileSelectingMinorRadius_ShouldExposeMajorAndMinorAxisLines()
    {
        var context = CreateContext();
        var tool = new EllipseTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(2, 4)));

        var provider = Assert.IsAssignableFrom<IToolPreviewDescriptorProvider>(tool);
        ToolPreviewDescriptor descriptor = provider.GetPreviewDescriptor(context);

        Assert.Single(descriptor.Entities.OfType<EllipseEntity>());
        Assert.Equal(2, descriptor.Lines.Count);

        Assert.Contains(
            descriptor.Lines,
            line => line.Start == new Point2D(-10, 0) &&
                line.End == new Point2D(10, 0) &&
                line.Kind == ToolPreviewLineKind.Axis);
        Assert.Contains(
            descriptor.Lines,
            line => line.Start == Point2D.Origin &&
                line.End == new Point2D(0, 4) &&
                line.Kind == ToolPreviewLineKind.Axis);

        Assert.Contains(
            descriptor.Markers,
            marker => marker.Position == new Point2D(10, 0));
        Assert.Contains(
            descriptor.Markers,
            marker => marker.Position == new Point2D(0, 4));
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0,
        bool isOrthoEnabled = false,
        AngleConstraintSettings? angleConstraintSettings = null)
    {
        var context = new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance,
            angleConstraintSettings: angleConstraintSettings);

        context.IsOrthoEnabled = isOrthoEnabled;

        return context;
    }

    private static void AssertPointNear(
        Point2D expected,
        Point2D actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 10);
        Assert.Equal(expected.Y, actual.Y, precision: 10);
    }
}
