using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadDocumentAtomicAddTests
{
    [Fact]
    public void AddEntities_WhenOneEntityUsesMissingLayer_ShouldNotAddAnyEntity()
    {
        var document = new CadDocument();
        var valid = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var invalid = new LineEntity(new Point2D(0, 5), new Point2D(10, 5), layerId: new LayerId("Missing"));

        Assert.Throws<InvalidOperationException>(() => document.AddEntities(new CadEntity[] { valid, invalid }));

        Assert.Empty(document.Entities.All);
    }

    [Fact]
    public void AddEntities_WithDuplicateIdsInBatch_ShouldNotAddAnyEntity()
    {
        var document = new CadDocument();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        CadEntity duplicate = new LineEntity(new Point2D(0, 5), new Point2D(10, 5)).WithId(first.Id);

        Assert.Throws<InvalidOperationException>(() => document.AddEntities(new[] { first, duplicate }));

        Assert.Empty(document.Entities.All);
    }

    [Fact]
    public void ModifyEntitiesCommand_WhenAddedEntitiesAreInvalid_ShouldRestoreRemovedEntities()
    {
        var document = new CadDocument();
        var removed = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(removed);
        var invalid = new LineEntity(new Point2D(0, 5), new Point2D(10, 5), layerId: new LayerId("Missing"));
        var history = new CommandHistory();

        Assert.Throws<InvalidOperationException>(() => history.Execute(
            document,
            new ModifyEntitiesCommand(new CadEntity[] { removed }, new CadEntity[] { invalid })));

        Assert.Same(removed, Assert.Single(document.Entities.All));
        Assert.False(history.CanUndo);
    }
}
