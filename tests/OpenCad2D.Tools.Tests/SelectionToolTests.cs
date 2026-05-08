using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class SelectionToolTests
{
    [Fact]
    public void Constructor_ShouldHaveName()
    {
        var tool = new SelectionTool();

        Assert.Equal("Selection", tool.Name);
    }

    [Fact]
    public void PointerPressed_OnEntity_ShouldSelectEntity()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var context = CreateContext(
            document,
            selectionSet,
            selectionTolerance: 1);

        var tool = new SelectionTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0.2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(selectionSet.Contains(line.Id));
        Assert.Equal(1, selectionSet.Count);
    }

    [Fact]
    public void PointerPressed_OnEmptySpace_ShouldClearSelection()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var selectedCircle = new CircleEntity(
            new Point2D(100, 100),
            5);

        document.AddEntity(line);
        document.AddEntity(selectedCircle);

        selectionSet.Select(selectedCircle.Id);

        var context = CreateContext(
            document,
            selectionSet,
            selectionTolerance: 1);

        var tool = new SelectionTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(50, 50)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(selectionSet.IsEmpty);
    }

    [Fact]
    public void PointerPressed_OnEntityWithoutShift_ShouldReplaceExistingSelection()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineEntity(
            new Point2D(20, 0),
            new Point2D(30, 0));

        document.AddEntity(first);
        document.AddEntity(second);

        selectionSet.Select(first.Id);

        var context = CreateContext(
            document,
            selectionSet,
            selectionTolerance: 1);

        var tool = new SelectionTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(25, 0.2)));

        Assert.False(selectionSet.Contains(first.Id));
        Assert.True(selectionSet.Contains(second.Id));
        Assert.Equal(1, selectionSet.Count);
    }

    [Fact]
    public void PointerPressed_OnEntityWithShift_ShouldAddEntityToSelection()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineEntity(
            new Point2D(20, 0),
            new Point2D(30, 0));

        document.AddEntity(first);
        document.AddEntity(second);

        selectionSet.Select(first.Id);

        var context = CreateContext(
            document,
            selectionSet,
            selectionTolerance: 1);

        var tool = new SelectionTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(
                new Point2D(25, 0.2),
                PointerModifiers.Shift));

        Assert.True(selectionSet.Contains(first.Id));
        Assert.True(selectionSet.Contains(second.Id));
        Assert.Equal(2, selectionSet.Count);
    }

    [Fact]
    public void PointerPressed_OnSelectedEntityWithShift_ShouldRemoveEntityFromSelection()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        var context = CreateContext(
            document,
            selectionSet,
            selectionTolerance: 1);

        var tool = new SelectionTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(
                new Point2D(5, 0.2),
                PointerModifiers.Shift));

        Assert.False(selectionSet.Contains(line.Id));
        Assert.True(selectionSet.IsEmpty);
    }

    [Fact]
    public void PointerPressed_OnEmptySpaceWithShift_ShouldKeepSelection()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        var context = CreateContext(
            document,
            selectionSet,
            selectionTolerance: 1);

        var tool = new SelectionTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(
                new Point2D(50, 50),
                PointerModifiers.Shift));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.True(selectionSet.Contains(line.Id));
        Assert.Equal(1, selectionSet.Count);
    }

    [Fact]
    public void PointerPressed_ShouldIgnoreInvisibleEntities()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var invisible = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            isVisible: false);

        document.AddEntity(invisible);

        var context = CreateContext(
            document,
            selectionSet,
            selectionTolerance: 1);

        var tool = new SelectionTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.True(selectionSet.IsEmpty);
    }

    [Fact]
    public void PointerPressed_WhenMultipleEntitiesUnderCursor_ShouldSelectTopByDrawOrder()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var bottom = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            drawOrder: 1);

        var top = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            drawOrder: 10);

        document.AddEntity(bottom);
        document.AddEntity(top);

        var context = CreateContext(
            document,
            selectionSet,
            selectionTolerance: 1);

        var tool = new SelectionTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.False(selectionSet.Contains(bottom.Id));
        Assert.True(selectionSet.Contains(top.Id));
        Assert.Equal(1, selectionSet.Count);
    }

    [Fact]
    public void PointerMoved_ShouldReturnNone()
    {
        var context = CreateContext();
        var tool = new SelectionTool();

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
    }

    [Fact]
    public void Cancel_ShouldClearSelection()
    {
        CadDocument document = new();
        SelectionSet selectionSet = new();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selectionSet.Select(line.Id);

        var context = CreateContext(document, selectionSet);
        var tool = new SelectionTool();

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.True(selectionSet.IsEmpty);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SelectionSet? selectionSet = null,
        double selectionTolerance = 5)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionSet: selectionSet,
            enabledSnaps: SnapKind.None,
            snapTolerance: 0,
            selectionTolerance: selectionTolerance);
    }
}