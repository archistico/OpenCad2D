using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class EntitySnapProviderTests
{
    [Fact]
    public void GetCandidates_WhenCursorIsNearSelectableEntity_ShouldReturnEntityCandidate()
    {
        CadDocument document = new();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var provider = new EntitySnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(5, 0.25),
            1,
            SnapKind.EntityOnly);

        SnapCandidate candidate = Assert.Single(provider.GetCandidates(request));

        Assert.Equal(SnapKind.Entity, candidate.Kind);
        Assert.Equal(line.Id, candidate.EntityId);
        Assert.Equal(new Point2D(5, 0), candidate.Point);
    }

    [Fact]
    public void SnapService_WhenEntitySnapIsEnabled_ShouldReturnEntityCandidate()
    {
        CadDocument document = new();
        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SnapService();
        var request = new SnapRequest(
            document,
            new Point2D(5, 0.25),
            1,
            SnapKind.EntityOnly);

        SnapCandidate? candidate = service.Snap(request);

        Assert.NotNull(candidate);
        Assert.Equal(SnapKind.Entity, candidate.Kind);
        Assert.Equal(line.Id, candidate.EntityId);
    }

    [Fact]
    public void GetCandidates_WhenEntityIsOnLockedLayer_ShouldNotReturnCandidate()
    {
        CadDocument document = new();
        Layer lockedLayer = new(
            new OpenCad2D.Core.Identifiers.LayerId("locked"),
            "Locked",
            isVisible: true,
            isLocked: true);

        document.Layers.Add(lockedLayer);

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: lockedLayer.Id);

        document.AddEntity(line);

        var provider = new EntitySnapProvider();
        var request = new SnapRequest(
            document,
            new Point2D(5, 0),
            1,
            SnapKind.EntityOnly);

        Assert.Empty(provider.GetCandidates(request));
    }
}
