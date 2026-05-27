using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class PointSnapProviderTests
{
    [Fact]
    public void EndpointSnapProvider_ShouldReturnPointPositionAsEndpointCandidate()
    {
        var document = new CadDocument();
        var point = new PointEntity(new Point2D(10, 20));
        document.AddEntity(point);

        var provider = new EndpointSnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(11, 21),
            3,
            SnapKind.Endpoint);

        SnapCandidate candidate = Assert.Single(provider.GetCandidates(request));

        Assert.Equal(SnapKind.Endpoint, candidate.Kind);
        Assert.Equal(point.Id, candidate.EntityId);
        Assert.Equal(point.Position, candidate.Point);
    }

    [Fact]
    public void NearestSnapProvider_ShouldReturnPointPositionAsClosestPoint()
    {
        var document = new CadDocument();
        var point = new PointEntity(new Point2D(5, 6));
        document.AddEntity(point);

        var provider = new NearestSnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(6, 6),
            2,
            SnapKind.Nearest);

        SnapCandidate candidate = Assert.Single(provider.GetCandidates(request));

        Assert.Equal(SnapKind.Nearest, candidate.Kind);
        Assert.Equal(point.Id, candidate.EntityId);
        Assert.Equal(point.Position, candidate.Point);
    }
}

public sealed class BlockReferenceSnapProviderTests
{
    [Fact]
    public void EndpointSnapProvider_ShouldReturnBlockInternalEndpointAsBlockCandidate()
    {
        var document = new CadDocument();
        var blockDefinitionId = OpenCad2D.Core.Identifiers.BlockDefinitionId.New();
        var internalLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var definition = new OpenCad2D.Core.Blocks.BlockDefinition(
            blockDefinitionId,
            "Door",
            new[] { internalLine });

        document.BlockDefinitions.Add(definition);

        var blockReference = new BlockReferenceEntity(
            blockDefinitionId,
            new Point2D(100, 50),
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            definition.GetBoundingBox());

        document.AddEntity(blockReference);

        var provider = new EndpointSnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(100.25, 50.25),
            1,
            SnapKind.Endpoint);

        SnapCandidate candidate = Assert.Single(provider.GetCandidates(request));

        Assert.Equal(SnapKind.Endpoint, candidate.Kind);
        Assert.Equal(blockReference.Id, candidate.EntityId);
        Assert.Equal(new Point2D(100, 50), candidate.Point);
    }
}
