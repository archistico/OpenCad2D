using OpenCad2D.Core.Architecture.Stairs;
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

public sealed class StairToolTests
{
    [Fact]
    public void Constructor_ShouldExposeToolName()
    {
        var tool = new StairTool();

        Assert.Equal("Stair", tool.Name);
        Assert.Null(tool.LastInsertionPoint);
    }

    [Fact]
    public void Defaults_ShouldUseArchitecturalCentimeterValues()
    {
        Assert.Equal(100.0, StairTool.DefaultWidth);
        Assert.Equal(18, StairTool.DefaultTreadCount);
        Assert.Equal(28.0, StairTool.DefaultTreadDepth);
        Assert.Equal(17.0, StairTool.DefaultRiserHeight);
        Assert.False(StairTool.DefaultShowStructure);
        Assert.Equal(3.0, StairTool.DefaultSlabThickness);
        Assert.Equal(StairPlanArrowMode.FirstToLast, StairTool.DefaultPlanArrowMode);
        Assert.False(StairTool.DefaultShowPlanSectionMarker);
    }

    [Fact]
    public void PointerPress_ShouldInsertStairOnCurrentLayer()
    {
        LayerId layerId = new("Architecture");
        var document = new CadDocument();
        document.Layers.Add(new Layer(layerId, "Architecture"));

        ToolContext context = CreateContext(
            document,
            currentLayerId: layerId);

        var tool = new StairTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal("Plan stair inserted.", result.Message);
        Assert.Equal(new Point2D(10, 20), tool.LastInsertionPoint);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);

        StairEntity stair = Assert.Single(document.Entities.All.OfType<StairEntity>());
        Assert.Equal(layerId, stair.LayerId);
        Assert.Equal(new Point2D(10, 20), stair.InsertionPoint);
        Assert.Equal(StairViewKind.Plan, stair.ViewKind);
        Assert.Equal(StairTool.DefaultWidth, stair.Width);
        Assert.Equal(StairTool.DefaultTreadCount, stair.TreadCount);
        Assert.Equal(StairTool.DefaultTreadDepth, stair.TreadDepth);
        Assert.Equal(StairTool.DefaultRiserHeight, stair.RiserHeight);
        Assert.False(stair.ShowStructure);
        Assert.Equal(StairTool.DefaultSlabThickness, stair.SlabThickness);
        Assert.Equal(StairTool.DefaultPlanArrowMode, stair.PlanArrowMode);
        Assert.Equal(StairTool.DefaultShowPlanSectionMarker, stair.ShowPlanSectionMarker);
    }

    [Fact]
    public void PointerPress_ShouldCreateUndoableCommand()
    {
        ToolContext context = CreateContext();
        var tool = new StairTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Single(context.Document.Entities.All.OfType<StairEntity>());
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Empty(context.Document.Entities.All);
        Assert.True(context.CommandHistory.CanRedo);
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

        var tool = new StairTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        Assert.Equal(new Point2D(100, 100), tool.LastInsertionPoint);

        StairEntity stair = Assert.Single(document.Entities.All.OfType<StairEntity>());
        Assert.Equal(new Point2D(100, 100), stair.InsertionPoint);
    }

    [Fact]
    public void PromptState_ShouldExposeViewOptions()
    {
        ToolContext context = CreateContext();
        var tool = new StairTool();

        CommandPromptState prompt = tool.GetPromptState(context);

        Assert.Equal(CommandInputKind.PointOrOption, prompt.ExpectedInput);
        Assert.Contains(prompt.Options, option => option.Keyword == "Plan");
        Assert.Contains(prompt.Options, option => option.Keyword == "Side");
        Assert.Contains(prompt.Options, option => option.Keyword == "Front");
    }

    [Fact]
    public void HandleCommandInput_WithPoint_ShouldInsertStair()
    {
        ToolContext context = CreateContext();
        var tool = new StairTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint(
                "30,40",
                new Point2D(30, 40)),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        StairEntity stair = Assert.Single(context.Document.Entities.All.OfType<StairEntity>());
        Assert.Equal(new Point2D(30, 40), stair.InsertionPoint);
    }

    [Fact]
    public void HandleCommandInput_WithSideOption_ShouldInsertSideElevationStair()
    {
        ToolContext context = CreateContext();
        var tool = new StairTool();

        ToolResult optionResult = tool.HandleCommandInput(
            CommandInputSubmission.Option("S", "Side"),
            context);

        Assert.Equal(ToolResultKind.Updated, optionResult.Kind);
        Assert.Equal(StairViewKind.SideElevation, tool.CurrentViewKind);

        ToolResult insertResult = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint(
                "30,40",
                new Point2D(30, 40)),
            context);

        Assert.Equal(ToolResultKind.Completed, insertResult.Kind);

        StairEntity stair = Assert.Single(context.Document.Entities.All.OfType<StairEntity>());
        Assert.Equal(StairViewKind.SideElevation, stair.ViewKind);
        Assert.Equal(new Point2D(30, 40), stair.InsertionPoint);
    }

    [Fact]
    public void HandleCommandInput_WithFrontOption_ShouldUpdatePreviewAndInsertedStair()
    {
        ToolContext context = CreateContext();
        var tool = new StairTool();

        tool.HandleCommandInput(
            CommandInputSubmission.Option("F", "Front"),
            context);

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(15, 25)));

        StairEntity preview = Assert.Single(tool.GetPreviewEntities(context).OfType<StairEntity>());
        Assert.Equal(StairViewKind.FrontElevation, preview.ViewKind);

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(15, 25)));

        StairEntity stair = Assert.Single(context.Document.Entities.All.OfType<StairEntity>());
        Assert.Equal(StairViewKind.FrontElevation, stair.ViewKind);
    }

    [Fact]
    public void PointerMove_ShouldExposePreviewEntity()
    {
        ToolContext context = CreateContext();
        var tool = new StairTool();

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(15, 25)));

        StairEntity preview = Assert.Single(tool.GetPreviewEntities(context).OfType<StairEntity>());
        Assert.Equal(new Point2D(15, 25), preview.InsertionPoint);
        Assert.Equal(StairViewKind.Plan, preview.ViewKind);
    }

    [Fact]
    public void Cancel_ShouldClearState()
    {
        ToolContext context = CreateContext();
        var tool = new StairTool();

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
