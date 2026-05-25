using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using Xunit;

namespace OpenCad2D.Core.Tests;

public sealed class AddBlockDefinitionCommandTests
{
    [Fact]
    public void Execute_ShouldAddBlockDefinitionAndUndoShouldRemoveIt()
    {
        var document = new CadDocument();
        var definition = new BlockDefinition(
            new BlockDefinitionId("Door"),
            "Door",
            new[]
            {
                new LineEntity(Point2D.Origin, new Point2D(1, 0))
            });
        var command = new AddBlockDefinitionCommand(definition);

        command.Execute(document);

        Assert.True(document.BlockDefinitions.Contains(definition.Id));

        command.Undo(document);

        Assert.False(document.BlockDefinitions.Contains(definition.Id));
    }
}
