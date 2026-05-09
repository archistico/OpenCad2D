using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;

namespace OpenCad2D.Interaction.Tests;

public sealed class SelectionLockedLayerTests
{
    [Fact]
    public void SelectByPoint_WhenEntityIsOnLockedLayer_ShouldReturnNull()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Reference");

        document.Layers.Add(new Layer(
            layerId,
            "Reference",
            isLocked: true));

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId);

        document.AddEntity(line);

        var service = new SelectionService();

        EntityId? result = service.SelectByPoint(
            document,
            new Point2D(5, 0.5),
            tolerance: 1);

        Assert.Null(result);
    }

    [Fact]
    public void SelectByWindow_Inside_WhenEntityIsOnLockedLayer_ShouldNotSelectEntity()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Reference");

        document.Layers.Add(new Layer(
            layerId,
            "Reference",
            isLocked: true));

        var line = new LineEntity(
            new Point2D(2, 2),
            new Point2D(8, 2),
            layerId: layerId);

        document.AddEntity(line);

        var service = new SelectionService();

        IReadOnlyList<EntityId> result = service.SelectByWindow(
            document,
            new BoundingBox2D(0, 0, 10, 10),
            WindowSelectionMode.Inside);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectByWindow_Crossing_WhenEntityIsOnLockedLayer_ShouldNotSelectEntity()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Reference");

        document.Layers.Add(new Layer(
            layerId,
            "Reference",
            isLocked: true));

        var line = new LineEntity(
            new Point2D(-5, 5),
            new Point2D(5, 5),
            layerId: layerId);

        document.AddEntity(line);

        var service = new SelectionService();

        IReadOnlyList<EntityId> result = service.SelectByWindow(
            document,
            new BoundingBox2D(0, 0, 10, 10),
            WindowSelectionMode.Crossing);

        Assert.Empty(result);
    }
}