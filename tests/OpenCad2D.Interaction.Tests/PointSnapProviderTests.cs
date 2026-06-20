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

public sealed class StairSnapProviderTests
{
    [Fact]
    public void EndpointSnapProvider_ShouldReturnGeneratedStairEndpoint()
    {
        var document = new CadDocument();
        var stair = CreatePlanStair();
        document.AddEntity(stair);

        var provider = new EndpointSnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(3.05, 1.05),
            0.2,
            SnapKind.Endpoint);

        SnapCandidate candidate = Assert.Single(provider.GetCandidates(request));

        Assert.Equal(SnapKind.Endpoint, candidate.Kind);
        Assert.Equal(stair.Id, candidate.EntityId);
        Assert.Equal(new Point2D(3, 1), candidate.Point);
    }


    [Fact]
    public void EndpointSnapProvider_ShouldIgnoreStairPlanAnnotationEndpoints()
    {
        var document = new CadDocument();
        var stair = CreatePlanStair();
        document.AddEntity(stair);

        var provider = new EndpointSnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(0, 0.5),
            0.05,
            SnapKind.Endpoint);

        Assert.Empty(provider.GetCandidates(request));
    }

    [Fact]
    public void MidpointSnapProvider_ShouldReturnGeneratedStairSegmentMidpoint()
    {
        var document = new CadDocument();
        var stair = CreatePlanStair();
        document.AddEntity(stair);

        var provider = new MidpointSnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(1.5, 0.05),
            0.2,
            SnapKind.Midpoint);

        SnapCandidate candidate = Assert.Single(provider.GetCandidates(request));

        Assert.Equal(SnapKind.Midpoint, candidate.Kind);
        Assert.Equal(stair.Id, candidate.EntityId);
        Assert.Equal(new Point2D(1.5, 0), candidate.Point);
    }

    [Fact]
    public void CenterSnapProvider_ShouldReturnStairBoundingBoxCenter()
    {
        var document = new CadDocument();
        var stair = CreatePlanStair();
        document.AddEntity(stair);

        var provider = new CenterSnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(1.5, 0.5),
            0.2,
            SnapKind.Center);

        SnapCandidate candidate = Assert.Single(provider.GetCandidates(request));

        Assert.Equal(SnapKind.Center, candidate.Kind);
        Assert.Equal(stair.Id, candidate.EntityId);
        Assert.Equal(new Point2D(1.5, 0.5), candidate.Point);
    }

    [Fact]
    public void NearestSnapProvider_ShouldReturnClosestPointOnGeneratedStairLinework()
    {
        var document = new CadDocument();
        var stair = CreatePlanStair();
        document.AddEntity(stair);

        var provider = new NearestSnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(2.4, 0.2),
            0.3,
            SnapKind.Nearest);

        SnapCandidate candidate = Assert.Single(provider.GetCandidates(request));

        Assert.Equal(SnapKind.Nearest, candidate.Kind);
        Assert.Equal(stair.Id, candidate.EntityId);
        Assert.Equal(2.4, candidate.Point.X, precision: 10);
        Assert.Equal(0, candidate.Point.Y, precision: 10);
    }

    private static StairEntity CreatePlanStair()
    {
        return new StairEntity(
            new Point2D(0, 0),
            OpenCad2D.Core.Architecture.Stairs.StairViewKind.Plan,
            width: 1,
            treadCount: 3,
            treadDepth: 1,
            riserHeight: 0.2);
    }
}
