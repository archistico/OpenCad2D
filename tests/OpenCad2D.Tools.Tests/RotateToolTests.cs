using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

namespace OpenCad2D.Tools.Tests;

[Collection(TransformToolDefaultsCollection.Name)]
public sealed class RotateToolTests : IDisposable
{
    public RotateToolTests()
    {
        RotateTool.ResetDefaultsForTests();
        ScaleTool.ResetDefaultsForTests();
    }

    public void Dispose()
    {
        RotateTool.ResetDefaultsForTests();
        ScaleTool.ResetDefaultsForTests();
    }

    [Fact]
    public void Constructor_ShouldStartWaitingForBasePoint()
    {
        var tool = new RotateTool();

        Assert.Equal("Rotate", tool.Name);
        Assert.Equal(RotateToolState.WaitingForBasePoint, tool.State);
        Assert.Null(tool.BasePoint);
        Assert.Null(tool.ReferencePoint);
        Assert.Null(tool.CurrentDestinationPoint);
    }

    [Fact]
    public void FirstPointerPress_WithoutSelection_ShouldEnterEntitySelection()
    {
        var context = CreateContext();
        var tool = new RotateTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RotateToolState.WaitingForEntitySelection, tool.State);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void FirstPointerPress_WithoutInitialSelection_ShouldSelectEntityFirst()
    {
        var context = CreateContextWithLine();
        var tool = new RotateTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(RotateToolState.WaitingForEntitySelection, tool.State);
        Assert.True(context.Selection.HasSelection);
    }

    [Fact]
    public void ConfirmEntitySelection_AfterSelectingEntity_ShouldAskForBasePoint()
    {
        var context = CreateContextWithLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.ConfirmEntitySelection(context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(RotateToolState.WaitingForBasePoint, tool.State);
        Assert.Contains("base point", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetActiveSnapKind_WhenSelectingEntities_ShouldUseEntityOnlySnap()
    {
        var context = CreateContext();
        context.EnabledSnaps = SnapKind.Endpoint | SnapKind.Entity;
        var tool = new RotateTool();

        SnapKind snapKind = tool.GetActiveSnapKind(context);

        Assert.Equal(SnapKind.EntityOnly, snapKind);
    }

    [Fact]
    public void FirstPointerPress_WithSelection_ShouldStoreBasePointAndAskForRotationAngle()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(RotateToolState.WaitingForRotationAngle, tool.State);
        Assert.Equal(new Point2D(5, 5), tool.BasePoint);
        Assert.Equal(new Point2D(5, 5), context.CurrentBasePoint);

        CommandPromptState prompt = tool.GetPromptState(context);
        Assert.Equal(CommandInputKind.PointOrAngleOrOption, prompt.ExpectedInput);
        Assert.True(prompt.AcceptsEmptyEnter);
        Assert.Contains(prompt.Options, option => option.Keyword == "Copy");
        Assert.Contains(prompt.Options, option => option.Keyword == "Reference");
    }

    [Fact]
    public void TypedAngleAfterBasePoint_ShouldRotateImmediately()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Angle("90", 90), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Contains("90", result.Message);
        Assert.Equal(RotateToolState.WaitingForBasePoint, tool.State);
        Assert.Null(context.CurrentBasePoint);
        AssertSingleLineEnd(context, 0, 10);
        Assert.Equal(90, RotateTool.LastRotationDegrees, precision: 9);
    }

    [Fact]
    public void PointerMove_InRotationAnglePhase_ShouldPreviewAngleFromBasePoint()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(90, tool.CurrentAngle.Degrees, precision: 6);
        Assert.Equal(new Point2D(0, 10), tool.CurrentDestinationPoint);

        var line = Assert.Single(tool.GetPreviewEntities(context).OfType<LineEntity>());
        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(10, line.End.Y, precision: 6);
    }

    [Fact]
    public void ClickInRotationAnglePhase_ShouldRotateByDirectionAngleFromBasePoint()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(-5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.True(context.CommandHistory.CanUndo);
        AssertSingleLineEnd(context, -10, 0);
    }

    [Fact]
    public void ClickOnBasePointInRotationAnglePhase_ShouldBeRejected()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RotateToolState.WaitingForRotationAngle, tool.State);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void EnterInRotationAnglePhase_ShouldReuseLastAngle()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Angle("90", 90), context);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        Assert.Contains("<90>", tool.GetPromptState(context).Prompt);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Confirm(string.Empty), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        AssertSingleLineEnd(context, -10, 0);
    }

    [Fact]
    public void ReferenceOptionWithTypedAngles_ShouldRotateByNewMinusReference()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Reference"), context);
        Assert.Equal(RotateToolState.WaitingForReferenceAngle, tool.State);

        tool.HandleCommandInput(CommandInputSubmission.Angle("30", 30), context);
        Assert.Equal(RotateToolState.WaitingForNewAngle, tool.State);

        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Angle("120", 120), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        AssertSingleLineEnd(context, 0, 10);
    }

    [Fact]
    public void ReferenceOptionWithPoints_ShouldAlignReferenceLineToClickedDirection()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Reference"), context);

        // Reference line from (0,0) to (10,10): 45 degrees.
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        Assert.Equal(RotateToolState.WaitingForReferenceSecondPoint, tool.State);
        Assert.Equal(new Point2D(0, 0), tool.ReferencePoint);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 10)));
        Assert.Equal(RotateToolState.WaitingForNewAngle, tool.State);
        Assert.Equal(45, tool.ReferenceAngleDegrees!.Value, precision: 9);

        tool.OnPointerMoved(context, new PointerInfo(new Point2D(0, 10)));
        Assert.Equal(45, tool.CurrentAngle.Degrees, precision: 6);

        // New direction straight up: 90 degrees, so the rotation is 45 degrees.
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        double expected = 10 * Math.Sqrt(0.5);
        AssertSingleLineEnd(context, expected, expected);
    }

    [Fact]
    public void NewAnglePointsOption_ShouldUseTwoPointDirection()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Reference"), context);
        tool.HandleCommandInput(CommandInputSubmission.Angle("0", 0), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("P", "Points"), context);
        Assert.Equal(RotateToolState.WaitingForNewAngleFirstPoint, tool.State);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(50, 50)));
        Assert.Equal(new Point2D(50, 50), context.CurrentBasePoint);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(50, 60)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        AssertSingleLineEnd(context, 0, 10);
    }

    [Fact]
    public void NewAngleEqualToReference_ShouldBeRejectedAndKeepAskingForNewAngle()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Reference"), context);
        tool.HandleCommandInput(CommandInputSubmission.Angle("30", 30), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Angle("30", 30), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(RotateToolState.WaitingForNewAngle, tool.State);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void CopyOption_ShouldAddRotatedCopyAndKeepOriginal()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();
        LineEntity original = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("C", "Copy"), context);
        Assert.True(tool.IsCopyMode);
        Assert.Contains("(copy)", tool.GetPromptState(context).Prompt);

        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Angle("90", 90), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(tool.IsCopyMode);
        LineEntity[] lines = context.Document.Entities.All.OfType<LineEntity>().ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Same(original, context.Document.Entities.GetRequired(original.Id));
        LineEntity copy = Assert.Single(lines, line => !line.Id.Equals(original.Id));
        Assert.Equal(0, copy.End.X, precision: 6);
        Assert.Equal(10, copy.End.Y, precision: 6);

        context.CommandHistory.Undo(context.Document);
        Assert.Single(context.Document.Entities.All);
    }

    [Fact]
    public void Rotate_ShouldBeUndoable()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 10)));

        context.CommandHistory.Undo(context.Document);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    [Fact]
    public void PointerMove_WithOrthoEnabled_ShouldSnapDirectionToNearestRightAngle()
    {
        var context = CreateContextWithSelectedLine();
        context.IsOrthoEnabled = true;
        var tool = new RotateTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(6, 8)));

        Assert.Equal(90, tool.CurrentAngle.Degrees, precision: 6);

        var line = Assert.Single(tool.GetPreviewEntities(context).OfType<LineEntity>());
        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(10, line.End.Y, precision: 6);
    }

    private static void AssertSingleLineEnd(ToolContext context, double x, double y)
    {
        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(0, line.Start.X, precision: 6);
        Assert.Equal(0, line.Start.Y, precision: 6);
        Assert.Equal(x, line.End.X, precision: 6);
        Assert.Equal(y, line.End.Y, precision: 6);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService());
    }

    private static ToolContext CreateContextWithLine()
    {
        var document = new CadDocument();
        var commandHistory = new CommandHistory();
        var context = new ToolContext(
            document,
            commandHistory,
            new SnapService());

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        return context;
    }

    private static ToolContext CreateContextWithSelectedLine()
    {
        var document = new CadDocument();
        var commandHistory = new CommandHistory();
        var context = new ToolContext(
            document,
            commandHistory,
            new SnapService());

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        context.Selection.Set.Select(line.Id);

        return context;
    }
}
