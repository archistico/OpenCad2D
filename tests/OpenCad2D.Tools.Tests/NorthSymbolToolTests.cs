using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Architectural;
using OpenCad2D.Tools.Common;

namespace OpenCad2D.Tools.Tests;

public sealed class NorthSymbolToolTests
{
    [Fact]
    public void Constructor_ShouldExposeToolName()
    {
        var tool = new NorthSymbolTool();

        Assert.Equal("North Symbol", tool.Name);
        Assert.Null(tool.LastInsertionPoint);
    }

    [Fact]
    public void PointerPress_ShouldInsertNorthSymbolOnCurrentLayer()
    {
        LayerId layerId = new("Annotations");
        var document = new CadDocument();
        document.Layers.Add(new Layer(layerId, "Annotations"));

        ToolContext context = CreateContext(
            document,
            currentLayerId: layerId);

        var tool = new NorthSymbolTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(new Point2D(10, 20), tool.LastInsertionPoint);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);
        Assert.Equal(5, document.Entities.Count);
        Assert.All(document.Entities.All, entity => Assert.Equal(layerId, entity.LayerId));
        Assert.Equal(3, document.Entities.All.OfType<LineEntity>().Count());
        Assert.Single(document.Entities.All.OfType<CircleEntity>());

        TextEntity text = Assert.Single(document.Entities.All.OfType<TextEntity>());
        Assert.Equal("N", text.Text);
    }

    [Fact]
    public void PointerPress_ShouldCreateUndoableCompositeCommand()
    {
        ToolContext context = CreateContext();
        var tool = new NorthSymbolTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(5, context.Document.Entities.Count);
        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        Assert.Equal(0, context.Document.Entities.Count);
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

        var tool = new NorthSymbolTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        Assert.Equal(new Point2D(100, 100), tool.LastInsertionPoint);
        Assert.Equal(6, document.Entities.Count);
    }

    [Fact]
    public void CreateEntities_ShouldUseRequestedNorthSymbolGeometry()
    {
        IReadOnlyList<CadEntity> entities = NorthSymbolTool.CreateEntities(
            new Point2D(0, 0),
            NorthSymbolTool.DefaultSize,
            LayerId.Default,
            TextFormatId.Standard);

        Assert.Equal(5, entities.Count);

        LineEntity shaft = entities.OfType<LineEntity>()
            .Single(line =>
                Math.Abs(line.Start.X) < 0.000001 &&
                Math.Abs(line.End.X) < 0.000001);

        Assert.Equal(0, shaft.Start.X, 6);
        Assert.Equal(11.35533905932737, shaft.Start.Y, 6);
        Assert.Equal(0, shaft.End.X, 6);
        Assert.Equal(-16, shaft.End.Y, 6);

        LineEntity rightArrowSide = entities.OfType<LineEntity>()
            .Single(line => line.End.X > 0);

        Assert.Equal(0, rightArrowSide.Start.X, 6);
        Assert.Equal(-24, rightArrowSide.Start.Y, 6);
        Assert.Equal(17.67766952966369, rightArrowSide.End.X, 6);
        Assert.Equal(-6.322330470336311, rightArrowSide.End.Y, 6);

        LineEntity leftArrowSide = entities.OfType<LineEntity>()
            .Single(line => line.End.X < 0);

        Assert.Equal(0, leftArrowSide.Start.X, 6);
        Assert.Equal(-24, leftArrowSide.Start.Y, 6);
        Assert.Equal(-17.677669529663685, leftArrowSide.End.X, 6);
        Assert.Equal(-6.322330470336311, leftArrowSide.End.Y, 6);

        CircleEntity circle = Assert.Single(entities.OfType<CircleEntity>());
        Assert.Equal(0, circle.Center.X, 6);
        Assert.Equal(-6.322330470336316, circle.Center.Y, 6);
        Assert.Equal(17.677669529663685, circle.Radius, 6);

        TextEntity text = Assert.Single(entities.OfType<TextEntity>());
        Assert.Equal("N", text.Text);
        Assert.Equal(-3.554439814609392, text.InsertionPoint.X, 6);
        Assert.Equal(-35.916367382398974, text.InsertionPoint.Y, 6);
        Assert.True(text.InsertionPoint.Y < rightArrowSide.Start.Y);
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
