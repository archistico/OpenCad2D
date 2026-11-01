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
public sealed class ScaleToolTests : IDisposable
{
    public ScaleToolTests()
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
        var tool = new ScaleTool();

        Assert.Equal("Scale", tool.Name);
        Assert.Equal(ScaleToolState.WaitingForBasePoint, tool.State);
        Assert.Null(tool.BasePoint);
        Assert.Null(tool.ReferencePoint);
        Assert.Null(tool.CurrentDestinationPoint);
        Assert.Equal(1.0, tool.CurrentFactor);
    }

    [Fact]
    public void FirstPointerPress_WithoutSelection_ShouldEnterEntitySelection()
    {
        var context = CreateContext();
        var tool = new ScaleTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForEntitySelection, tool.State);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void FirstPointerPress_WithoutInitialSelection_ShouldSelectEntityFirst()
    {
        var context = CreateContextWithLine();
        var tool = new ScaleTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForEntitySelection, tool.State);
        Assert.True(context.Selection.HasSelection);
    }

    [Fact]
    public void ConfirmEntitySelection_AfterSelectingEntity_ShouldAskForBasePoint()
    {
        var context = CreateContextWithLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.ConfirmEntitySelection(context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForBasePoint, tool.State);
        Assert.Contains("base point", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetActiveSnapKind_WhenSelectingEntities_ShouldUseEntityOnlySnap()
    {
        var context = CreateContext();
        context.EnabledSnaps = SnapKind.Endpoint | SnapKind.Entity;
        var tool = new ScaleTool();

        SnapKind snapKind = tool.GetActiveSnapKind(context);

        Assert.Equal(SnapKind.EntityOnly, snapKind);
    }

    [Fact]
    public void FirstPointerPress_WithSelection_ShouldStoreBasePointAndAskForScaleFactor()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForScaleFactor, tool.State);
        Assert.Equal(new Point2D(5, 5), tool.BasePoint);
        Assert.Equal(new Point2D(5, 5), context.CurrentBasePoint);

        CommandPromptState prompt = tool.GetPromptState(context);
        Assert.Equal(CommandInputKind.PointOrNumberOrOption, prompt.ExpectedInput);
        Assert.True(prompt.AcceptsEmptyEnter);
        Assert.Contains(prompt.Options, option => option.Keyword == "Copy");
        Assert.Contains(prompt.Options, option => option.Keyword == "Reference");
    }

    [Fact]
    public void TypedFactorAfterBasePoint_ShouldScaleImmediately()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Contains("2", result.Message);
        Assert.Equal(ScaleToolState.WaitingForBasePoint, tool.State);
        Assert.Null(context.CurrentBasePoint);
        AssertSingleLineEnd(context, 20);
        Assert.Equal(2, ScaleTool.LastScaleFactor, precision: 9);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void TypedNonPositiveFactor_ShouldBeRejected(double factor)
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromNumber("x", factor), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForScaleFactor, tool.State);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void ClickInScaleFactorPhase_ShouldUseDistanceFromBasePointAsFactor()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(0, 3)));
        Assert.True(tool.HasPreview);
        Assert.Equal(3, tool.CurrentFactor, precision: 9);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 3)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        AssertSingleLineEnd(context, 30);
    }

    [Fact]
    public void ClickOnBasePointInScaleFactorPhase_ShouldBeRejected()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForScaleFactor, tool.State);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void EnterInScaleFactorPhase_ShouldReuseLastFactor()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        Assert.Contains("<2>", tool.GetPromptState(context).Prompt);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.Confirm(string.Empty), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        AssertSingleLineEnd(context, 40);
    }

    [Fact]
    public void ReferenceOptionWithPoints_ShouldScaleReferenceLengthToNewLength()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Reference"), context);
        Assert.Equal(ScaleToolState.WaitingForReferenceLength, tool.State);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        Assert.Equal(ScaleToolState.WaitingForReferenceSecondPoint, tool.State);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));
        Assert.Equal(ScaleToolState.WaitingForNewLength, tool.State);
        Assert.Equal(10, tool.ReferenceLength!.Value, precision: 9);

        tool.OnPointerMoved(context, new PointerInfo(new Point2D(0, 25)));
        Assert.Equal(2.5, tool.CurrentFactor, precision: 9);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 25)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        AssertSingleLineEnd(context, 25);
    }

    [Fact]
    public void ReferenceOptionWithTypedLengths_ShouldScaleByRatio()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Reference"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("4", 4), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        AssertSingleLineEnd(context, 5);
    }

    [Fact]
    public void NewLengthPointsOption_ShouldUseTwoPointDistance()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Reference"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("10", 10), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("P", "Points"), context);
        Assert.Equal(ScaleToolState.WaitingForNewLengthFirstPoint, tool.State);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(100, 100)));
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(100, 115)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        AssertSingleLineEnd(context, 15);
    }

    [Fact]
    public void ZeroReferenceLength_ShouldBeRejected()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Reference"), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromNumber("0", 0), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ScaleToolState.WaitingForReferenceLength, tool.State);
    }

    [Fact]
    public void CopyOption_ShouldAddScaledCopyAndKeepOriginal()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();
        LineEntity original = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("C", "Copy"), context);
        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromNumber("3", 3), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Same(original, context.Document.Entities.GetRequired(original.Id));
        LineEntity copy = Assert.Single(
            context.Document.Entities.All.OfType<LineEntity>(),
            line => !line.Id.Equals(original.Id));
        Assert.Equal(30, copy.End.X, precision: 6);
    }

    [Fact]
    public void Scale_ShouldBeUndoable()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new ScaleTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("2", 2), context);

        context.CommandHistory.Undo(context.Document);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    private static void AssertSingleLineEnd(ToolContext context, double x)
    {
        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(0, line.Start.X, precision: 6);
        Assert.Equal(0, line.Start.Y, precision: 6);
        Assert.Equal(x, line.End.X, precision: 6);
        Assert.Equal(0, line.End.Y, precision: 6);
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
