using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class SnapLockedLayerTests
{
    [Fact]
    public void Snap_WhenEntityIsOnLockedLayer_ShouldStillReturnSnapCandidate()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Reference");

        document.Layers.Add(new Layer(
            layerId,
            "Reference",
            isLocked: true));

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId);

        document.AddEntity(line);

        var service = new SnapService();

        var request = new SnapRequest(
            document,
            new Point2D(0.2, 0.1),
            tolerance: 1,
            enabledSnaps: SnapKind.Endpoint);

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Endpoint, result.Kind);
        Assert.Equal(new Point2D(0, 0), result.Point);
        Assert.Equal(line.Id, result.EntityId);
    }
}