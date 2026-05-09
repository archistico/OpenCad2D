using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadDocumentLockedLayerTests
{
    [Fact]
    public void IsEntityVisible_WhenLayerIsLocked_ShouldReturnTrue()
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

        Assert.True(document.IsEntityVisible(line));
    }

    [Fact]
    public void IsEntitySelectable_WhenLayerIsLocked_ShouldReturnFalse()
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

        Assert.False(document.IsEntitySelectable(line));
    }

    [Fact]
    public void GetVisibleEntities_WhenLayerIsLocked_ShouldReturnEntity()
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

        IReadOnlyList<CadEntity> result = document.GetVisibleEntities().ToList();

        Assert.Contains(line, result);
    }

    [Fact]
    public void GetSelectableEntities_WhenLayerIsLocked_ShouldNotReturnEntity()
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

        IReadOnlyList<CadEntity> result = document.GetSelectableEntities().ToList();

        Assert.DoesNotContain(line, result);
    }

    [Fact]
    public void RemoveEntity_WhenLayerIsLocked_ShouldThrow()
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

        Assert.Throws<InvalidOperationException>(() =>
            document.RemoveEntity(line.Id));

        Assert.True(document.Entities.Contains(line.Id));
    }

    [Fact]
    public void ReplaceEntity_WhenLayerIsLocked_ShouldThrow()
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

        var moved = new LineEntity(
            new Point2D(5, 0),
            new Point2D(15, 0),
            id: line.Id,
            layerId: layerId);

        Assert.Throws<InvalidOperationException>(() =>
            document.ReplaceEntity(moved));

        var result = (LineEntity)document.Entities.GetRequired(line.Id);

        Assert.Equal(new Point2D(0, 0), result.Start);
        Assert.Equal(new Point2D(10, 0), result.End);
    }

    [Fact]
    public void RemoveEntities_WhenOneEntityIsOnLockedLayer_ShouldThrowAndKeepLockedEntity()
    {
        var document = new CadDocument();

        var lockedLayerId = new LayerId("Reference");

        document.Layers.Add(new Layer(
            lockedLayerId,
            "Reference",
            isLocked: true));

        var editable = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var locked = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1),
            layerId: lockedLayerId);

        document.AddEntity(editable);
        document.AddEntity(locked);

        Assert.Throws<InvalidOperationException>(() =>
            document.RemoveEntities(new[] { editable.Id, locked.Id }));

        Assert.True(document.Entities.Contains(locked.Id));
    }
}