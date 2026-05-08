using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class CopyToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForFirstPoint()
    {
        var tool = new CopyTool();

        Assert.Equal("Copy", tool.Name);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_WithNoSelection_ShouldReturnNoneAndReset()
    {
        var context = CreateContext();
        var tool = new CopyTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Null(tool.FirstPoint);
        Assert.Null(tool.CurrentPoint);
    }

    [Fact]
    public void FirstPointerPress_WithSelection_ShouldStoreBasePoint()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new CopyTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForSecondPoint, tool.State);
        Assert.Equal(new Point2D(1, 2), tool.FirstPoint);
        Assert.Equal(new Point2D(1, 2), tool.CurrentPoint);
    }

    [Fact]
    public void PointerMove_AfterBasePoint_ShouldUpdatePreview()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new CopyTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(5, 2), tool.CurrentPoint);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities(context);

        Assert.Single(preview);

        var previewLine = Assert.IsType<LineEntity>(preview[0]);

        Assert.NotEqual(line.Id, previewLine.Id);
        Assert.Equal(new Point2D(5, 2), previewLine.Start);
        Assert.Equal(new Point2D(15, 2), previewLine.End);

        // Preview must not modify the real document.
        Assert.Equal(1, document.Entities.Count);

        var originalLine = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), originalLine.Start);
        Assert.Equal(new Point2D(10, 0), originalLine.End);
    }

    [Fact]
    public void SecondPointerPress_ShouldCopySelectedEntity()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new CopyTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Equal(2, document.Entities.Count);

        var lines = document.Entities.All
            .OfType<LineEntity>()
            .ToList();

        Assert.Equal(2, lines.Count);

        var original = lines.Single(entity => entity.Id == line.Id);
        var copied = lines.Single(entity => entity.Id != line.Id);

        Assert.Equal(new Point2D(0, 0), original.Start);
        Assert.Equal(new Point2D(10, 0), original.End);

        Assert.Equal(new Point2D(5, 2), copied.Start);
        Assert.Equal(new Point2D(15, 2), copied.End);
    }

    [Fact]
    public void SecondPointerPress_ShouldCopyMultipleSelectedEntities()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var circle = new CircleEntity(
            new Point2D(0, 0),
            5);

        document.AddEntity(line);
        document.AddEntity(circle);

        selection.Select(line.Id);
        selection.Select(circle.Id);

        var context = CreateContext(document, selection);
        var tool = new CopyTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 3)));

        Assert.Equal(4, document.Entities.Count);

        var copiedLine = document.Entities.All
            .OfType<LineEntity>()
            .Single(entity => entity.Id != line.Id);

        var copiedCircle = document.Entities.All
            .OfType<CircleEntity>()
            .Single(entity => entity.Id != circle.Id);

        Assert.Equal(new Point2D(10, 3), copiedLine.Start);
        Assert.Equal(new Point2D(20, 3), copiedLine.End);
        Assert.Equal(new Point2D(10, 3), copiedCircle.Center);
        Assert.Equal(5, copiedCircle.Radius, precision: 10);
    }

    [Fact]
    public void Copy_ShouldBeUndoable()
    {
        CadDocument document = new();
        SelectionSet selection = new();
        CommandHistory history = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection, history);
        var tool = new CopyTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(2, document.Entities.Count);
        Assert.True(history.CanUndo);

        history.Undo(document);

        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void Copy_ShouldKeepOriginalSelection()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new CopyTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.True(selection.Contains(line.Id));
        Assert.Equal(1, selection.Count);
    }

    [Fact]
    public void SecondPointerPress_WithEndpointSnap_ShouldUseSnappedDestination()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var selectedLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var snapLine = new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100));

        document.AddEntity(selectedLine);
        document.AddEntity(snapLine);

        selection.Select(selectedLine.Id);

        var context = CreateContext(
            document,
            selection,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new CopyTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        Assert.Equal(3, document.Entities.Count);

        var copied = document.Entities.All
            .OfType<LineEntity>()
            .Single(entity =>
                entity.Id != selectedLine.Id &&
                entity.Id != snapLine.Id);

        Assert.Equal(new Point2D(100, 100), copied.Start);
        Assert.Equal(new Point2D(110, 100), copied.End);
    }

    [Fact]
    public void Cancel_ShouldResetWithoutCopyingEntity()
    {
        CadDocument document = new();
        SelectionSet selection = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(document, selection);
        var tool = new CopyTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(TwoPointToolState.WaitingForFirstPoint, tool.State);
        Assert.Equal(1, document.Entities.Count);
        Assert.True(document.Entities.Contains(line.Id));
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SelectionSet? selectionSet = null,
        CommandHistory? history = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            history ?? new CommandHistory(),
            new SnapService(),
            selectionSet: selectionSet,
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance);
    }
}