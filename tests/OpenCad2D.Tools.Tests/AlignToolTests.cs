using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class AlignToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForSourcePoint1()
    {
        var tool = new AlignTool();

        Assert.Equal("Align", tool.Name);
        Assert.Equal(AlignToolState.WaitingForSourcePoint1, tool.State);
        Assert.Null(tool.SourcePoint1);
        Assert.Null(tool.DestinationPoint1);
        Assert.Null(tool.SourcePoint2);
        Assert.Null(tool.DestinationPoint2);
    }

    [Fact]
    public void FirstPointerPress_WithoutSelection_ShouldNotStartTool()
    {
        var context = CreateContext();
        var tool = new AlignTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(AlignToolState.WaitingForSourcePoint1, tool.State);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void FirstPointerPress_WithSelection_ShouldStoreSourcePoint1()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new AlignTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(AlignToolState.WaitingForDestinationPoint1, tool.State);
        Assert.Equal(new Point2D(0, 0), tool.SourcePoint1);
        Assert.Equal(new Point2D(0, 0), context.CurrentBasePoint);
    }

    [Fact]
    public void SecondPointerPress_ShouldStoreDestinationPoint1()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new AlignTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(AlignToolState.WaitingForSourcePoint2, tool.State);
        Assert.Equal(new Point2D(5, 5), tool.DestinationPoint1);
        Assert.Equal(new Point2D(5, 5), context.CurrentBasePoint);
    }

    [Fact]
    public void SourcePoint2EqualToSourcePoint1_ShouldBeRejected()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new AlignTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(AlignToolState.WaitingForSourcePoint2, tool.State);
        Assert.Null(tool.SourcePoint2);
    }

    [Fact]
    public void ThirdPointerPress_ShouldStoreSourcePoint2()
    {
        var context = CreateContextWithSelectedLine();
        var tool = new AlignTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(AlignToolState.WaitingForDestinationPoint2, tool.State);
        Assert.Equal(new Point2D(10, 0), tool.SourcePoint2);
        Assert.Equal(new Point2D(0, 0), context.CurrentBasePoint);
    }

    [Fact]
    public void PointerMove_AfterSourcePoint2_ShouldUpdatePreviewWithoutScale()
    {
        var context = CreateContextWithSelectedLine();
        var tool = CreateToolWaitingForDestinationPoint2(context);

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.NotNull(tool.CurrentTransform);
        Assert.Equal(90, tool.CurrentTransform.RotationDegrees, precision: 6);
        Assert.False(tool.CurrentTransform.ScaleApplied);
        Assert.Equal(1, tool.CurrentTransform.ScaleFactor, precision: 6);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities(context);
        var line = Assert.Single(preview.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(10, line.End.Y, precision: 6);
    }

    [Fact]
    public void DestinationPoint2EqualToDestinationPoint1_ShouldBeRejected()
    {
        var context = CreateContextWithSelectedLine();
        var tool = CreateToolWaitingForDestinationPoint2(context);

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(AlignToolState.WaitingForDestinationPoint2, tool.State);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void FourthPointerPress_ShouldWaitForScaleConfirmation()
    {
        var context = CreateContextWithSelectedLine();
        var tool = CreateToolWaitingForDestinationPoint2(context);

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(AlignToolState.WaitingForScaleConfirmation, tool.State);
        Assert.True(tool.HasPreview);
        Assert.False(context.CommandHistory.CanUndo);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void ConfirmWithoutScale_ShouldAlignSelectedEntities()
    {
        var context = CreateContextWithSelectedLine();
        var tool = CreateToolWaitingForScaleConfirmation(context);

        ToolResult result = tool.ConfirmWithoutScale(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(AlignToolState.WaitingForSourcePoint1, tool.State);
        Assert.Null(context.CurrentBasePoint);
        Assert.True(context.CommandHistory.CanUndo);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(10, line.End.Y, precision: 6);
    }

    [Fact]
    public void ConfirmWithScale_ShouldAlignAndScaleSelectedEntities()
    {
        var context = CreateContextWithSelectedLine();
        var tool = CreateToolWaitingForScaleConfirmation(
            context,
            destinationPoint2: new Point2D(0, 20));

        ToolResult result = tool.ConfirmWithScale(context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(AlignToolState.WaitingForSourcePoint1, tool.State);
        Assert.True(context.CommandHistory.CanUndo);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(20, line.End.Y, precision: 6);
    }

    [Fact]
    public void ConfirmWithoutScale_ShouldIgnoreDifferentDestinationLength()
    {
        var context = CreateContextWithSelectedLine();
        var tool = CreateToolWaitingForScaleConfirmation(
            context,
            destinationPoint2: new Point2D(0, 20));

        tool.ConfirmWithoutScale(context);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(0, line.End.X, precision: 6);
        Assert.Equal(10, line.End.Y, precision: 6);
    }

    [Fact]
    public void Align_ShouldBeUndoable()
    {
        var context = CreateContextWithSelectedLine();
        var tool = CreateToolWaitingForScaleConfirmation(context);

        tool.ConfirmWithoutScale(context);

        context.CommandHistory.Undo(context.Document);

        var line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    private static AlignTool CreateToolWaitingForDestinationPoint2(ToolContext context)
    {
        var tool = new AlignTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        return tool;
    }

    private static AlignTool CreateToolWaitingForScaleConfirmation(
        ToolContext context,
        Point2D? destinationPoint2 = null)
    {
        AlignTool tool = CreateToolWaitingForDestinationPoint2(context);

        tool.OnPointerPressed(
            context,
            new PointerInfo(destinationPoint2 ?? new Point2D(0, 10)));

        return tool;
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService());
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
