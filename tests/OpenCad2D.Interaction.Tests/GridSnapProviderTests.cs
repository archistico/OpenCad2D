using OpenCad2D.Core.Documents;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class GridSnapProviderTests
{
    [Fact]
    public void Snap_WithGridEnabled_ShouldReturnNearestGridPoint()
    {
        var document = new CadDocument();
        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(23.2, 46.8),
            tolerance: 10,
            enabledSnaps: SnapKind.Grid,
            gridSettings: new GridSettings(step: 10));

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(SnapKind.Grid, result.Kind);
        Assert.Equal(new Point2D(20, 50), result.Point);
        Assert.Null(result.EntityId);
    }

    [Fact]
    public void Snap_WithGridEnabledButOutsideTolerance_ShouldReturnNull()
    {
        var document = new CadDocument();
        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(23.2, 46.8),
            tolerance: 1,
            enabledSnaps: SnapKind.Grid,
            gridSettings: new GridSettings(step: 10));

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithGridDisabled_ShouldReturnNull()
    {
        var document = new CadDocument();
        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(23.2, 46.8),
            tolerance: 10,
            enabledSnaps: SnapKind.Endpoint,
            gridSettings: new GridSettings(step: 10));

        SnapCandidate? result = service.Snap(request);

        Assert.Null(result);
    }

    [Fact]
    public void Snap_WithCustomGridOrigin_ShouldUseOrigin()
    {
        var document = new CadDocument();
        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(13.2, 13.2),
            tolerance: 10,
            enabledSnaps: SnapKind.Grid,
            gridSettings: new GridSettings(
                step: 10,
                originX: 5,
                originY: 5));

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(new Point2D(15, 15), result.Point);
    }

    [Fact]
    public void Snap_WithNegativeCoordinates_ShouldReturnNearestGridPoint()
    {
        var document = new CadDocument();
        var service = new SnapService();

        var request = new SnapRequest(
            document,
            cursorPoint: new Point2D(-23.2, -46.8),
            tolerance: 10,
            enabledSnaps: SnapKind.Grid,
            gridSettings: new GridSettings(step: 10));

        SnapCandidate? result = service.Snap(request);

        Assert.NotNull(result);
        Assert.Equal(new Point2D(-20, -50), result.Point);
    }

    [Fact]
    public void GridSettings_WithInvalidStep_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new GridSettings(step: 0));
    }
}