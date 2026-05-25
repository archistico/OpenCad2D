using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class ImageReferenceSnapTests
{
    [Fact]
    public void Snap_WithEndpointEnabled_ShouldReturnImageCorner()
    {
        var document = new CadDocument();
        var image = CreateImageReference();
        document.AddEntity(image);

        var service = new SnapService();
        var request = new SnapRequest(
            document,
            new Point2D(10.2, 5.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Endpoint);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Endpoint, result.Kind);
        Assert.Equal(image.Id, result.EntityId);
        Assert.Equal(new Point2D(10, 5), result.Point);
    }

    [Fact]
    public void Snap_WithMidpointEnabled_ShouldReturnImageEdgeMidpoint()
    {
        var document = new CadDocument();
        var image = CreateImageReference();
        document.AddEntity(image);

        var service = new SnapService();
        var request = new SnapRequest(
            document,
            new Point2D(15.1, 5.2),
            tolerance: 1,
            enabledSnaps: SnapKind.Midpoint);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Midpoint, result.Kind);
        Assert.Equal(image.Id, result.EntityId);
        Assert.Equal(new Point2D(15, 5), result.Point);
    }

    [Fact]
    public void Snap_WithNearestEnabled_ShouldReturnClosestPointOnImageEdge()
    {
        var document = new CadDocument();
        var image = CreateImageReference();
        document.AddEntity(image);

        var service = new SnapService();
        var request = new SnapRequest(
            document,
            new Point2D(13, 5.3),
            tolerance: 1,
            enabledSnaps: SnapKind.Nearest);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Nearest, result.Kind);
        Assert.Equal(image.Id, result.EntityId);
        Assert.Equal(new Point2D(13, 5), result.Point);
    }

    [Fact]
    public void Snap_WithCenterEnabled_ShouldReturnImageCenter()
    {
        var document = new CadDocument();
        var image = CreateImageReference();
        document.AddEntity(image);

        var service = new SnapService();
        var request = new SnapRequest(
            document,
            new Point2D(15.2, 7.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Center);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Center, result.Kind);
        Assert.Equal(image.Id, result.EntityId);
        Assert.Equal(new Point2D(15, 7), result.Point);
    }

    private static ImageReferenceEntity CreateImageReference()
    {
        return new ImageReferenceEntity(
            "plan.png",
            new Point2D(10, 5),
            new Vector2D(10, 0),
            new Vector2D(0, 4),
            pixelWidth: 100,
            pixelHeight: 40);
    }
}
