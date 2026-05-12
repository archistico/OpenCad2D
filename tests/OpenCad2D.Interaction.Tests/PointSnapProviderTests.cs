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
