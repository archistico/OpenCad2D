using OpenCad2D.Core.Anchors;
using OpenCad2D.Core.Architecture.Doors;
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

public sealed class DoorToolTests
{
    [Fact]
    public void Constructor_ShouldExposeToolName()
    {
        var tool = new DoorTool();

        Assert.Equal("Door", tool.Name);
        Assert.Null(tool.LastInsertionPoint);
        Assert.Equal(DoorSwingDirection.Left, tool.CurrentSwingDirection);
        Assert.Equal(AnchorPoint.MiddleLeft, tool.CurrentAnchor);
        Assert.True(tool.CurrentMaskWallOpening);
    }

    [Fact]
    public void Defaults_ShouldUseArchitecturalCentimeterValues()
    {
        Assert.Equal(90.0, DoorTool.DefaultWidth);
        Assert.Equal(20.0, DoorTool.DefaultWallThickness);
        Assert.Equal(90.0, DoorTool.DefaultOpeningAngleDegrees);
        Assert.Equal(DoorSwingDirection.Left, DoorTool.DefaultSwingDirection);
        Assert.Equal(AnchorPoint.MiddleLeft, DoorTool.DefaultAnchor);
        Assert.True(DoorTool.DefaultMaskWallOpening);
    }

    [Fact]
    public void GetPromptState_ShouldExposeCurrentDoorDefaultsInInsertionPrompt()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        tool.HandleCommandInput(
            CommandInputSubmission.Option("R", "Right"),
            context);
        tool.SetAnchor(AnchorPoint.BottomCenter);
        tool.HandleCommandInput(
            CommandInputSubmission.Option("M", "Mask"),
            context);

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Contains("W=90", prompt.Prompt);
        Assert.Contains("T=20", prompt.Prompt);
        Assert.Contains("O=90", prompt.Prompt);
        Assert.Contains("S=Right", prompt.Prompt);
        Assert.Contains("A=Bottom center", prompt.Prompt);
        Assert.Contains("M=Off", prompt.Prompt);
    }

    [Fact]
    public void PointerPress_ShouldInsertDoorOnCurrentLayer()
    {
        LayerId layerId = new("Architecture");
        var document = new CadDocument();
        document.Layers.Add(new Layer(layerId, "Architecture"));

        ToolContext context = CreateContext(
            document,
            currentLayerId: layerId);

        var tool = new DoorTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Door inserted.", result.Message);
        Assert.Equal(new Point2D(10, 20), tool.LastInsertionPoint);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);

        DoorEntity door = Assert.Single(document.Entities.All.OfType<DoorEntity>());
        Assert.Equal(layerId, door.LayerId);
        Assert.Equal(new Point2D(10, 20), door.InsertionPoint);
        Assert.Equal(DoorTool.DefaultWidth, door.Width);
        Assert.Equal(DoorTool.DefaultWallThickness, door.WallThickness);
        Assert.Equal(DoorTool.DefaultOpeningAngleDegrees, door.OpeningAngleDegrees);
        Assert.Equal(DoorSwingDirection.Left, door.SwingDirection);
        Assert.Equal(AnchorPoint.MiddleLeft, door.Anchor);
        Assert.True(door.MaskWallOpening);
    }

    [Fact]
    public void PointerPress_ShouldCreateUndoableCommand()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Single(context.Document.Entities.All.OfType<DoorEntity>());
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Empty(context.Document.Entities.All);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void HandleCommandInput_WithRightOption_ShouldInsertRightSwingDoor()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("R", "Right"),
            context);

        Assert.Equal(ToolResultKind.Updated, optionResult.Kind);
        Assert.Equal(DoorSwingDirection.Right, tool.CurrentSwingDirection);

        ToolResult insertResult = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint(
                "30,40",
                new Point2D(30, 40)),
            context);

        Assert.Equal(ToolResultKind.Completed, insertResult.Kind);

        DoorEntity door = Assert.Single(context.Document.Entities.All.OfType<DoorEntity>());
        Assert.Equal(DoorSwingDirection.Right, door.SwingDirection);
        Assert.Equal(new Point2D(30, 40), door.InsertionPoint);
    }


    [Fact]
    public void HandleCommandInput_WithMaskOption_ShouldToggleInsertedDoorMask()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("M", "Mask"),
            context);

        Assert.Equal(ToolResultKind.Updated, optionResult.Kind);
        Assert.False(tool.CurrentMaskWallOpening);

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        DoorEntity door = Assert.Single(context.Document.Entities.All.OfType<DoorEntity>());
        Assert.False(door.MaskWallOpening);
    }

    [Fact]
    public void SetAnchor_ShouldUpdateInsertedDoorAnchor()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        ToolResult anchorResult = tool.SetAnchor(AnchorPoint.BottomCenter);

        Assert.Equal(ToolResultKind.Updated, anchorResult.Kind);
        Assert.Equal(AnchorPoint.BottomCenter, tool.CurrentAnchor);

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        DoorEntity door = Assert.Single(context.Document.Entities.All.OfType<DoorEntity>());
        Assert.Equal(AnchorPoint.BottomCenter, door.Anchor);
    }

    [Fact]
    public void PointerMove_ShouldExposePreviewEntity()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(15, 25)));

        DoorEntity preview = Assert.Single(tool.GetPreviewEntities(context).OfType<DoorEntity>());
        Assert.Equal(new Point2D(15, 25), preview.InsertionPoint);
        Assert.Equal(DoorSwingDirection.Left, preview.SwingDirection);
        Assert.True(preview.MaskWallOpening);
    }

    [Fact]
    public void PointerPress_WithEndpointSnap_ShouldInsertAtSnappedLocation()
    {
        var document = new CadDocument();

        document.AddEntity(new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100)));

        ToolContext context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new DoorTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        Assert.Equal(new Point2D(100, 100), tool.LastInsertionPoint);

        DoorEntity door = Assert.Single(document.Entities.All.OfType<DoorEntity>());
        Assert.Equal(new Point2D(100, 100), door.InsertionPoint);
    }

    [Fact]
    public void Cancel_ShouldClearState()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(10, 20)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Null(tool.LastInsertionPoint);
        Assert.Null(context.CurrentBasePoint);
        Assert.Empty(tool.GetPreviewEntities(context));
    }

    [Fact]
    public void HandleCommandInput_WithWidthOption_ShouldUseCustomWidthForNextDoor()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("W", "Width"),
            context);

        Assert.Equal(ToolResultKind.Started, optionResult.Kind);
        Assert.Equal(DoorToolState.WaitingForWidth, tool.State);

        ToolResult widthResult = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("105", 105),
            context);

        Assert.Equal(ToolResultKind.Started, widthResult.Kind);
        Assert.Equal(DoorToolState.WaitingForInsertionPoint, tool.State);
        Assert.Equal(105, tool.CurrentWidth);

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        DoorEntity door = Assert.Single(context.Document.Entities.All.OfType<DoorEntity>());
        Assert.Equal(105, door.Width);
    }

    [Fact]
    public void HandleCommandInput_WithOpeningOption_ShouldRejectInvalidAngle()
    {
        ToolContext context = CreateContext();
        var tool = new DoorTool();

        tool.HandleCommandInput(
            CommandInputSubmission.Option("O", "Opening"),
            context);

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("200", 200),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(DoorToolState.WaitingForOpeningAngle, tool.State);
        Assert.Equal(DoorTool.DefaultOpeningAngleDegrees, tool.CurrentOpeningAngleDegrees);
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
