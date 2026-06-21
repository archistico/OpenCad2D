using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Architectural;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class WindowToolTests
{
    [Fact]
    public void Constructor_ShouldExposeToolName()
    {
        var tool = new WindowTool();

        Assert.Equal("Window", tool.Name);
        Assert.Null(tool.LastInsertionPoint);
        Assert.Equal(AnchorPoint.MiddleLeft, tool.CurrentAnchor);
        Assert.True(tool.CurrentMaskWallOpening);
    }

    [Fact]
    public void Defaults_ShouldUseArchitecturalCentimeterValues()
    {
        Assert.Equal(120.0, WindowTool.DefaultWidth);
        Assert.Equal(20.0, WindowTool.DefaultWallThickness);
        Assert.Equal(4.0, WindowTool.DefaultFrameOffset);
        Assert.Equal(AnchorPoint.MiddleLeft, WindowTool.DefaultAnchor);
        Assert.True(WindowTool.DefaultMaskWallOpening);
    }

    [Fact]
    public void PointerPress_ShouldInsertWindowOnCurrentLayer()
    {
        LayerId layerId = new("Architecture");
        var document = new CadDocument();
        document.Layers.Add(new Layer(layerId, "Architecture"));

        ToolContext context = CreateContext(
            document,
            currentLayerId: layerId);

        var tool = new WindowTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Window inserted.", result.Message);
        Assert.Equal(new Point2D(10, 20), tool.LastInsertionPoint);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);

        WindowEntity window = Assert.Single(document.Entities.All.OfType<WindowEntity>());
        Assert.Equal(layerId, window.LayerId);
        Assert.Equal(new Point2D(10, 20), window.InsertionPoint);
        Assert.Equal(WindowTool.DefaultWidth, window.Width);
        Assert.Equal(WindowTool.DefaultWallThickness, window.WallThickness);
        Assert.Equal(WindowTool.DefaultFrameOffset, window.FrameOffset);
        Assert.Equal(AnchorPoint.MiddleLeft, window.Anchor);
        Assert.True(window.MaskWallOpening);
    }

    [Fact]
    public void HandleCommandInput_WithMaskOption_ShouldToggleInsertedWindowMask()
    {
        ToolContext context = CreateContext();
        var tool = new WindowTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("M", "Mask"),
            context);

        Assert.Equal(ToolResultKind.Updated, optionResult.Kind);
        Assert.False(tool.CurrentMaskWallOpening);

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        WindowEntity window = Assert.Single(context.Document.Entities.All.OfType<WindowEntity>());
        Assert.False(window.MaskWallOpening);
    }

    [Fact]
    public void SetAnchor_ShouldUpdateInsertedWindowAnchor()
    {
        ToolContext context = CreateContext();
        var tool = new WindowTool();

        ToolResult anchorResult = tool.SetAnchor(AnchorPoint.BottomCenter);

        Assert.Equal(ToolResultKind.Updated, anchorResult.Kind);
        Assert.Equal(AnchorPoint.BottomCenter, tool.CurrentAnchor);

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        WindowEntity window = Assert.Single(context.Document.Entities.All.OfType<WindowEntity>());
        Assert.Equal(AnchorPoint.BottomCenter, window.Anchor);
    }

    [Fact]
    public void PointerMove_ShouldExposePreviewEntity()
    {
        ToolContext context = CreateContext();
        var tool = new WindowTool();

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(15, 25)));

        WindowEntity preview = Assert.Single(tool.GetPreviewEntities(context).OfType<WindowEntity>());
        Assert.Equal(new Point2D(15, 25), preview.InsertionPoint);
        Assert.True(preview.MaskWallOpening);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        LayerId? currentLayerId = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            currentLayerId: currentLayerId,
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance);
    }
}
