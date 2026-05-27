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

public sealed class ScaleBarToolTests
{
    [Fact]
    public void Constructor_ShouldExposeToolName()
    {
        var tool = new ScaleBarTool();

        Assert.Equal("Metric Scale Bar", tool.Name);
        Assert.Null(tool.LastInsertionPoint);
    }

    [Fact]
    public void PointerPress_ShouldInsertScaleBarOnCurrentLayer()
    {
        LayerId layerId = new("Annotations");
        var document = new CadDocument();
        document.Layers.Add(new Layer(layerId, "Annotations"));

        ToolContext context = CreateContext(
            document,
            currentLayerId: layerId);

        var tool = new ScaleBarTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 20)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(new Point2D(10, 20), tool.LastInsertionPoint);
        Assert.Equal(new Point2D(10, 20), context.CurrentBasePoint);
        Assert.Equal(20, document.Entities.Count);
        Assert.All(document.Entities.All, entity => Assert.Equal(layerId, entity.LayerId));
        Assert.Equal(6, document.Entities.All.OfType<PolylineEntity>().Count());
        Assert.Equal(7, document.Entities.All.OfType<LineEntity>().Count());
        Assert.Equal(7, document.Entities.All.OfType<TextEntity>().Count());
    }

    [Fact]
    public void PointerPress_ShouldCreateUndoableCompositeCommand()
    {
        ToolContext context = CreateContext();
        var tool = new ScaleBarTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(20, context.Document.Entities.Count);
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

        var tool = new ScaleBarTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(101, 101)));

        Assert.Equal(new Point2D(100, 100), tool.LastInsertionPoint);
        Assert.Equal(21, document.Entities.Count);
    }

    [Fact]
    public void CreateEntities_ShouldUseRequestedMetricScaleBarGeometry()
    {
        IReadOnlyList<CadEntity> entities = ScaleBarTool.CreateEntities(
            new Point2D(0, 0),
            LayerId.Default,
            TextFormatId.Standard);

        Assert.Equal(20, entities.Count);

        IReadOnlyList<PolylineEntity> rectangles = entities
            .OfType<PolylineEntity>()
            .ToArray();

        Assert.Equal(6, rectangles.Count);
        Assert.All(rectangles, rectangle => Assert.True(rectangle.IsClosed));

        Assert.True(rectangles[0].IsFilled);
        Assert.False(rectangles[1].IsFilled);
        Assert.True(rectangles[2].IsFilled);
        Assert.False(rectangles[3].IsFilled);
        Assert.True(rectangles[4].IsFilled);
        Assert.False(rectangles[5].IsFilled);

        Assert.Equal(new Point2D(0, 20), rectangles[0].Vertices[0]);
        Assert.Equal(new Point2D(100, 20), rectangles[0].Vertices[1]);
        Assert.Equal(new Point2D(100, 0), rectangles[0].Vertices[2]);
        Assert.Equal(new Point2D(0, 0), rectangles[0].Vertices[3]);

        Assert.Equal(new Point2D(500, 0), rectangles[5].Vertices[0]);
        Assert.Equal(new Point2D(1000, 0), rectangles[5].Vertices[1]);
        Assert.Equal(new Point2D(1000, -20), rectangles[5].Vertices[2]);
        Assert.Equal(new Point2D(500, -20), rectangles[5].Vertices[3]);

        IReadOnlyList<LineEntity> ticks = entities
            .OfType<LineEntity>()
            .OrderBy(line => line.Start.X)
            .ToArray();

        Assert.Equal(7, ticks.Count);
        Assert.Equal(new Point2D(0, 30), ticks[0].Start);
        Assert.Equal(new Point2D(0, -30), ticks[0].End);
        Assert.Equal(new Point2D(1000, 30), ticks[^1].Start);
        Assert.Equal(new Point2D(1000, -30), ticks[^1].End);

        string[] labels = entities.OfType<TextEntity>()
            .Select(text => text.Text)
            .ToArray();

        Assert.Equal(
            new[] { "0", "100", "200", "300", "400", "500", "1000" },
            labels);
    }

    [Fact]
    public void CreateEntities_ShouldUseInsertionPointAsLocalOrigin()
    {
        IReadOnlyList<CadEntity> entities = ScaleBarTool.CreateEntities(
            new Point2D(10, 20),
            LayerId.Default,
            TextFormatId.Standard);

        PolylineEntity firstRectangle = entities.OfType<PolylineEntity>().First();
        LineEntity lastTick = entities.OfType<LineEntity>().Last();
        TextEntity lastLabel = entities.OfType<TextEntity>().Last();

        Assert.Equal(new Point2D(10, 40), firstRectangle.Vertices[0]);
        Assert.Equal(new Point2D(1010, 50), lastTick.Start);
        Assert.Equal(new Point2D(1010, -10), lastTick.End);
        Assert.Equal(new Point2D(1010, -30), lastLabel.InsertionPoint);
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
