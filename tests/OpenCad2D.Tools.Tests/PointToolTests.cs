using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;

namespace OpenCad2D.Tools.Tests;

public sealed class PointToolTests
{
    [Fact]
    public void Constructor_ShouldExposeToolName()
    {
        var tool = new PointTool();

        Assert.Equal("Point", tool.Name);
        Assert.Null(tool.LastCreatedPosition);
    }

    [Fact]
    public void PointerPress_ShouldCreatePointOnCurrentLayer()
    {
        LayerId detailLayerId = new("Details");
        var document = new CadDocument();
        document.Layers.Add(new Layer(detailLayerId, "Details"));

        ToolContext context = CreateContext(
            document,
            currentLayerId: detailLayerId);

        var tool = new PointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(1, context.Document.Entities.Count);
        Assert.Equal(new Point2D(10, 20), tool.LastCreatedPosition);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);

        var point = Assert.Single(context.Document.Entities.All.OfType<PointEntity>());
        Assert.Equal(new Point2D(10, 20), point.Position);
        Assert.Equal(detailLayerId, point.LayerId);
    }

    [Fact]
    public void PointerPress_ShouldCreateUndoablePoint()
    {
        ToolContext context = CreateContext();
        var tool = new PointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        Assert.Equal(1, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanRedo);
    }

    [Fact]
    public void PointerPress_WithEndpointSnap_ShouldCreatePointAtSnappedLocation()
    {
        var document = new CadDocument();

        document.AddEntity(new LineEntity(
            new Point2D(100, 100),
            new Point2D(200, 100)));

        ToolContext context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5);

        var tool = new PointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        var point = Assert.Single(context.Document.Entities.All.OfType<PointEntity>());
        Assert.Equal(new Point2D(100, 100), point.Position);
    }

    [Fact]
    public void Cancel_ShouldClearLastCreatedPositionAndBasePoint()
    {
        ToolContext context = CreateContext();
        var tool = new PointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(1, 2)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Null(tool.LastCreatedPosition);
        Assert.Null(context.CurrentBasePoint);
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
