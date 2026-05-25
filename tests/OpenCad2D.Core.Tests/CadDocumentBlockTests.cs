using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using Xunit;

namespace OpenCad2D.Core.Tests;

public sealed class CadDocumentBlockTests
{
    [Fact]
    public void AddEntity_ShouldAcceptBlockReferenceWhenDefinitionExists()
    {
        var document = new CadDocument();
        var definition = new BlockDefinition(
            new BlockDefinitionId("North"),
            "North",
            new[] { new LineEntity(new Point2D(0, 0), new Point2D(0, 1)) });
        document.BlockDefinitions.Add(definition);

        document.AddEntity(new BlockReferenceEntity(
            definition.Id,
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            definition.GetBoundingBox()));

        Assert.Single(document.Entities.All);
    }

    [Fact]
    public void AddEntity_ShouldRejectBlockReferenceWhenDefinitionDoesNotExist()
    {
        var document = new CadDocument();

        Assert.Throws<InvalidOperationException>(() => document.AddEntity(new BlockReferenceEntity(
            new BlockDefinitionId("Missing"),
            Point2D.Origin,
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            new BoundingBox2D(0, 0, 1, 1))));
    }
}
