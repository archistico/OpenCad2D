using OpenCad2D.Core.Blocks;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry.Transformations;
using Xunit;

namespace OpenCad2D.Core.Tests;

public sealed class BlockDefinitionTests
{
    [Fact]
    public void GetBoundingBox_ShouldContainAllDefinitionEntities()
    {
        var definition = new BlockDefinition(
            new BlockDefinitionId("Door"),
            "Door",
            new CadEntity[]
            {
                new LineEntity(new Point2D(0, 0), new Point2D(1, 0)),
                new LineEntity(new Point2D(1, 0), new Point2D(1, 2))
            });

        BoundingBox2D box = definition.GetBoundingBox();

        Assert.Equal(0, box.MinX);
        Assert.Equal(0, box.MinY);
        Assert.Equal(1, box.MaxX);
        Assert.Equal(2, box.MaxY);
    }

    [Fact]
    public void TransformContainedEntity_ShouldApplyReferenceTransform()
    {
        var reference = new BlockReferenceEntity(
            new BlockDefinitionId("Door"),
            new Point2D(10, 20),
            new Vector2D(2, 0),
            new Vector2D(0, 2),
            new BoundingBox2D(0, 0, 1, 1));

        var localLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(1, 1));

        var transformed = Assert.IsType<LineEntity>(reference.TransformContainedEntity(localLine));

        Assert.Equal(new Point2D(10, 20), transformed.Start);
        Assert.Equal(new Point2D(12, 22), transformed.End);
    }

    [Fact]
    public void Transform_ShouldMoveBlockReferenceAxesAndInsertionPoint()
    {
        var reference = new BlockReferenceEntity(
            new BlockDefinitionId("Door"),
            new Point2D(1, 1),
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            new BoundingBox2D(0, 0, 1, 1));

        var transformed = Assert.IsType<BlockReferenceEntity>(reference.Transform(
            Matrix2D.Translation(5, 7)));

        Assert.Equal(new Point2D(6, 8), transformed.InsertionPoint);
        Assert.Equal(new Vector2D(1, 0), transformed.XAxis);
        Assert.Equal(new Vector2D(0, 1), transformed.YAxis);
    }
}
