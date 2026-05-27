using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using Xunit;

namespace OpenCad2D.Core.Tests;

public sealed class UpdateBlockDefinitionsCommandTests
{
    [Fact]
    public void Execute_ShouldRenameDefinitionAndUndoShouldRestoreOriginalName()
    {
        var document = new CadDocument();
        var definition = CreateDefinition("door", "Door");
        document.BlockDefinitions.Add(definition);

        var command = new UpdateBlockDefinitionsCommand(
            new[] { definition },
            new[] { definition.WithName("Door Renamed") });

        command.Execute(document);

        Assert.Equal("Door Renamed", document.BlockDefinitions.GetRequired(definition.Id).Name);

        command.Undo(document);

        Assert.Equal("Door", document.BlockDefinitions.GetRequired(definition.Id).Name);
    }

    [Fact]
    public void Execute_ShouldRejectRemovingDefinitionStillUsedByBlockReference()
    {
        var document = new CadDocument();
        var definition = CreateDefinition("door", "Door");
        document.BlockDefinitions.Add(definition);
        document.AddEntity(new BlockReferenceEntity(
            definition.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            definition.GetBoundingBox()));

        var command = new UpdateBlockDefinitionsCommand(
            new[] { definition },
            Array.Empty<BlockDefinition>());

        Assert.Throws<InvalidOperationException>(() => command.Execute(document));
        Assert.True(document.BlockDefinitions.Contains(definition.Id));
    }

    private static BlockDefinition CreateDefinition(
        string id,
        string name)
    {
        return new BlockDefinition(
            new BlockDefinitionId(id),
            name,
            new[]
            {
                new LineEntity(Point2D.Origin, new Point2D(1, 0))
            });
    }
}
