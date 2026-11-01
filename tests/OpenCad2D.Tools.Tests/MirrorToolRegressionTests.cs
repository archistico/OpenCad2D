using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

public sealed class MirrorToolRegressionTests
{
    private static readonly LayerId LockedLayerId = new("Locked reference");
    private static readonly LayerId HiddenLayerId = new("Hidden reference");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mirror_WithSelectedEntityOnLockedLayer_ShouldMirrorOnlyEditableEntities(bool deleteSource)
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity editable);
        LineEntity locked = AddLineOnLayer(document, LockedLayerId, isLocked: true, isVisible: true);
        context.Selection.Set.Select(locked.Id);
        int initialCount = document.Entities.Count;
        var tool = new MirrorTool();

        ToolResult result = RunMirrorAcrossYAxis(tool, context, deleteSource);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Contains("skipped", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(locked, document.Entities.GetRequired(locked.Id));
        Assert.Single(document.Entities.All, entity => entity.LayerId.Equals(LockedLayerId));
        Assert.Equal(1, context.CommandHistory.UndoCount);
        Assert.Equal(deleteSource ? initialCount : initialCount + 1, document.Entities.Count);

        context.CommandHistory.Undo(document);

        Assert.Same(editable, document.Entities.GetRequired(editable.Id));
        Assert.Equal(initialCount, document.Entities.Count);
    }

    [Fact]
    public void Mirror_WithSelectedEntityOnHiddenLayer_ShouldNotCopyHiddenEntity()
    {
        ToolContext context = CreateContext(out CadDocument document, out _);
        LineEntity hidden = AddLineOnLayer(document, HiddenLayerId, isLocked: false, isVisible: false);
        context.Selection.Set.Select(hidden.Id);
        var tool = new MirrorTool();

        ToolResult result = RunMirrorAcrossYAxis(tool, context, deleteSource: false);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Single(document.Entities.All, entity => entity.LayerId.Equals(HiddenLayerId));
    }

    [Fact]
    public void Mirror_WithDeletedEntityStillSelected_ShouldIgnoreStaleIdInPreviewAndCommand()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity first);
        var second = new LineEntity(new Point2D(0, 5), new Point2D(10, 5));
        document.AddEntity(second);
        context.Selection.Set.Select(second.Id);
        document.RemoveEntity(first.Id);
        var tool = new MirrorTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(-1, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(-1, 10)));
        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities(context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(-1, 10)));
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Confirm(string.Empty), context);

        Assert.Single(preview);
        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Mirror_WhenOnlyLockedEntitiesAreSelected_ShouldAskForEditableSelectionWithoutThrowing()
    {
        CadDocument document = new();
        LineEntity locked = AddLineOnLayer(document, LockedLayerId, isLocked: true, isVisible: true);
        ToolContext context = CreateContext(document);
        context.Selection.Set.Select(locked.Id);
        var tool = new MirrorTool();

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        Assert.NotEqual(ToolResultKind.Completed, result.Kind);
        Assert.Equal(MirrorToolState.WaitingForEntitySelection, tool.State);
        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Mirror_TypedSecondAxisPointWithOrthoEnabled_ShouldUseExactAxis()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        context.IsOrthoEnabled = true;
        var tool = new MirrorTool();

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);
        tool.HandleCommandInput(CommandInputSubmission.FromPoint("10,10", new Point2D(10, 10)), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Option("Y", "Yes"), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        LineEntity mirrored = Assert.IsType<LineEntity>(document.Entities.GetRequired(line.Id));
        Assert.Equal(0, mirrored.End.X, precision: 9);
        Assert.Equal(10, mirrored.End.Y, precision: 9);
    }

    [Fact]
    public void GetActiveSnapKind_InAxisPointPhases_ShouldExcludeEntitySnap()
    {
        ToolContext context = CreateContext(out _, out _);
        context.EnabledSnaps = SnapKind.All | SnapKind.Entity;
        var tool = new MirrorTool();

        Assert.Equal(SnapKind.All, tool.GetActiveSnapKind(context));

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(SnapKind.All, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void ConfirmAtFirstAxisPoint_ShouldFinishCommand()
    {
        ToolContext context = CreateContext(out CadDocument document, out _);
        var tool = new MirrorTool();
        var controller = new ToolController(context, tool);

        Assert.True(tool.GetPromptState(context).AcceptsEmptyEnter);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsType<OpenCad2D.Tools.Selection.SelectionTool>(controller.ActiveTool);
        Assert.Single(document.Entities.All);
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    private static ToolResult RunMirrorAcrossYAxis(MirrorTool tool, ToolContext context, bool deleteSource)
    {
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(-1, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(-1, 10)));
        return deleteSource
            ? tool.HandleCommandInput(CommandInputSubmission.Option("Y", "Yes"), context)
            : tool.HandleCommandInput(CommandInputSubmission.Confirm(string.Empty), context);
    }

    private static LineEntity AddLineOnLayer(
        CadDocument document,
        LayerId layerId,
        bool isLocked,
        bool isVisible)
    {
        document.Layers.Add(new Layer(layerId, layerId.Value));
        var line = new LineEntity(new Point2D(0, 7), new Point2D(10, 7), layerId: layerId);
        document.AddEntity(line);
        document.Layers.SetLocked(layerId, isLocked);
        document.Layers.SetVisibility(layerId, isVisible);
        return line;
    }

    private static ToolContext CreateContext(out CadDocument document, out LineEntity line)
    {
        document = new CadDocument();
        line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);
        ToolContext context = CreateContext(document);
        context.Selection.Set.Select(line.Id);
        return context;
    }

    private static ToolContext CreateContext(CadDocument document)
    {
        return new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: SnapKind.None,
            selectionTolerance: 1);
    }
}
