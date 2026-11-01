using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadDocumentAtomicMutationTests
{
    [Fact]
    public void ReplaceEntities_WhenOneEntityIsOnLockedLayer_ShouldNotModifyAnyEntity()
    {
        CadDocument document = CreateDocument(out LineEntity editable, out LineEntity locked);
        var movedEditable = new LineEntity(new Point2D(0, 1), new Point2D(10, 1), id: editable.Id);
        var movedLocked = new LineEntity(new Point2D(0, 6), new Point2D(10, 6), id: locked.Id, layerId: locked.LayerId);

        Assert.Throws<InvalidOperationException>(() => document.ReplaceEntities(new CadEntity[] { movedEditable, movedLocked }));

        Assert.Same(editable, document.Entities.GetRequired(editable.Id));
        Assert.Same(locked, document.Entities.GetRequired(locked.Id));
    }

    [Fact]
    public void RemoveEntities_WhenOneEntityIsOnLockedLayer_ShouldNotRemoveAnyEntity()
    {
        CadDocument document = CreateDocument(out LineEntity editable, out LineEntity locked);

        Assert.Throws<InvalidOperationException>(() => document.RemoveEntities(new[] { editable.Id, locked.Id }));

        Assert.True(document.Entities.Contains(editable.Id));
        Assert.True(document.Entities.Contains(locked.Id));
    }

    [Fact]
    public void RemoveEntities_WithDuplicateIds_ShouldRemoveEntityOnce()
    {
        CadDocument document = CreateDocument(out LineEntity editable, out LineEntity locked);

        document.RemoveEntities(new[] { editable.Id, editable.Id });

        Assert.False(document.Entities.Contains(editable.Id));
        Assert.True(document.Entities.Contains(locked.Id));
    }

    private static CadDocument CreateDocument(out LineEntity editable, out LineEntity locked)
    {
        var document = new CadDocument();
        var layerId = new LayerId("Reference");
        document.Layers.Add(new Layer(layerId, "Reference"));
        editable = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        locked = new LineEntity(new Point2D(0, 5), new Point2D(10, 5), layerId: layerId);
        document.AddEntity(editable);
        document.AddEntity(locked);
        document.Layers.SetLocked(layerId, true);
        return document;
    }
}
