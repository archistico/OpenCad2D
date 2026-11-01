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

/// <summary>
/// Regression coverage shared by Move, Copy, Rotate and Scale: non-editable or stale entities
/// in the selection, no-op transformations, exact typed input and command completion.
/// </summary>
[Collection(TransformToolDefaultsCollection.Name)]
public sealed class TransformToolsRegressionTests
{
    private static readonly LayerId LockedLayerId = new("Locked reference");
    private static readonly LayerId HiddenLayerId = new("Hidden reference");

    public static TheoryData<string> TransformToolNames => new()
    {
        "Move",
        "Copy",
        "Rotate",
        "Scale"
    };

    [Theory]
    [MemberData(nameof(TransformToolNames))]
    public void Transform_WithSelectedEntityOnLockedLayer_ShouldChangeOnlyEditableEntitiesAndStayUndoable(string toolName)
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity editable);
        LineEntity locked = AddLineOnLayer(document, LockedLayerId, isLocked: true, isVisible: true);
        context.Selection.Set.Select(locked.Id);
        int initialCount = document.Entities.Count;

        ICadTool tool = CreateTool(toolName);
        ToolResult result = RunDefaultTransform(tool, context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Contains("skipped", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(locked, document.Entities.GetRequired(locked.Id));
        Assert.DoesNotContain(
            document.Entities.All,
            entity => entity.LayerId.Equals(LockedLayerId) && !entity.Id.Equals(locked.Id));
        Assert.Equal(1, context.CommandHistory.UndoCount);

        if (toolName == "Copy")
        {
            Assert.Equal(initialCount + 1, document.Entities.Count);
        }
        else
        {
            Assert.NotSame(editable, document.Entities.GetRequired(editable.Id));
        }

        context.CommandHistory.Undo(document);

        Assert.Same(editable, document.Entities.GetRequired(editable.Id));
        Assert.Equal(initialCount, document.Entities.Count);
    }

    [Theory]
    [MemberData(nameof(TransformToolNames))]
    public void Transform_WithSelectedEntityOnHiddenLayer_ShouldLeaveHiddenEntityUntouched(string toolName)
    {
        ToolContext context = CreateContext(out CadDocument document, out _);
        LineEntity hidden = AddLineOnLayer(document, HiddenLayerId, isLocked: false, isVisible: false);
        context.Selection.Set.Select(hidden.Id);

        ICadTool tool = CreateTool(toolName);
        ToolResult result = RunDefaultTransform(tool, context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Same(hidden, document.Entities.GetRequired(hidden.Id));
        Assert.Single(document.Entities.All, entity => entity.LayerId.Equals(HiddenLayerId));
    }

    [Theory]
    [MemberData(nameof(TransformToolNames))]
    public void Transform_WithDeletedEntityStillSelected_ShouldIgnoreStaleIdInPreviewAndCommand(string toolName)
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity first);
        var second = new LineEntity(new Point2D(0, 5), new Point2D(10, 5));
        document.AddEntity(second);
        context.Selection.Set.Select(second.Id);
        document.RemoveEntity(first.Id);

        ICadTool tool = CreateTool(toolName);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(0, 3)));
        IReadOnlyList<CadEntity> preview = ((IToolPreviewEntityProvider)tool).GetPreviewEntities(context);
        ToolResult result = RunRemainingDefaultTransform(tool, context);

        Assert.True(preview.Count <= 1);
        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, context.CommandHistory.UndoCount);
    }

    [Theory]
    [MemberData(nameof(TransformToolNames))]
    public void Transform_WhenOnlyLockedEntitiesAreSelected_ShouldAskForEditableSelectionWithoutThrowing(string toolName)
    {
        CadDocument document = new();
        LineEntity locked = AddLineOnLayer(document, LockedLayerId, isLocked: true, isVisible: true);
        ToolContext context = CreateContext(document);
        context.Selection.Set.Select(locked.Id);

        ICadTool tool = CreateTool(toolName);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        Assert.NotEqual(ToolResultKind.Completed, result.Kind);
        Assert.Equal(0, context.CommandHistory.UndoCount);
        Assert.Equal(SnapKind.EntityOnly, ((ISnapModeProvider)tool).GetActiveSnapKind(context));
        Assert.Same(locked, document.Entities.GetRequired(locked.Id));
    }

    [Theory]
    [MemberData(nameof(TransformToolNames))]
    public void ConfirmAtBasePoint_ShouldFinishCommand(string toolName)
    {
        ToolContext context = CreateContext(out CadDocument document, out _);
        ICadTool tool = CreateTool(toolName);
        var controller = new ToolController(context, tool);

        Assert.True(((ICommandDrivenTool)tool).GetPromptState(context).AcceptsEmptyEnter);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsType<OpenCad2D.Tools.Selection.SelectionTool>(controller.ActiveTool);
        Assert.Equal(0, context.CommandHistory.UndoCount);
        Assert.Single(document.Entities.All);
    }

    [Fact]
    public void Rotate_ByPointer_ShouldReportAppliedAngle()
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Contains("90", result.Message);
    }

    [Fact]
    public void Rotate_TypedDestinationPointWithOrthoEnabled_ShouldUseExactPoint()
    {
        ToolContext context = CreateContext(out CadDocument document, out LineEntity line);
        context.IsOrthoEnabled = true;
        var tool = new RotateTool();
        double radians = 30.0 * Math.PI / 180.0;

        tool.HandleCommandInput(CommandInputSubmission.FromPoint("0,0", new Point2D(0, 0)), context);
        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("@10<30", new Point2D(10 * Math.Cos(radians), 10 * Math.Sin(radians))),
            context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Contains("30", result.Message);
        LineEntity rotated = Assert.IsType<LineEntity>(document.Entities.GetRequired(line.Id));
        Assert.Equal(10 * Math.Cos(radians), rotated.End.X, precision: 9);
        Assert.Equal(10 * Math.Sin(radians), rotated.End.Y, precision: 9);
    }

    [Theory]
    [InlineData(20, 0)]
    [InlineData(0, 0)]
    public void Rotate_DestinationWithoutRotation_ShouldBeRejectedWithoutUndoEntry(double x, double y)
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(x, y)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RotateToolState.WaitingForRotationAngle, tool.State);
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(360)]
    public void Rotate_TypedZeroAngle_ShouldBeRejectedWithoutUndoEntry(double angle)
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Angle(angle.ToString(System.Globalization.CultureInfo.InvariantCulture), angle),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RotateToolState.WaitingForRotationAngle, tool.State);
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Scale_PointerFactorOne_ShouldBeRejectedWithoutUndoEntry()
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 1)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForScaleFactor, tool.State);
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Scale_TypedFactorOne_ShouldBeRejectedWithoutUndoEntry()
    {
        ToolContext context = CreateContext(out _, out _);
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromNumber("1", 1), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForScaleFactor, tool.State);
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    private static ICadTool CreateTool(string toolName)
    {
        return toolName switch
        {
            "Move" => new MoveTool(),
            "Copy" => new CopyTool(),
            "Rotate" => new RotateTool(),
            "Scale" => new ScaleTool(),
            _ => throw new ArgumentOutOfRangeException(nameof(toolName))
        };
    }

    private static ToolResult RunDefaultTransform(ICadTool tool, ToolContext context)
    {
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        return RunRemainingDefaultTransform(tool, context);
    }

    private static ToolResult RunRemainingDefaultTransform(ICadTool tool, ToolContext context)
    {
        if (tool is RotateTool or ScaleTool)
        {
            // Rotate: direction straight up (90 degrees). Scale: distance 20 used as the factor.
            return tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 20)));
        }

        return tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 3)));
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
