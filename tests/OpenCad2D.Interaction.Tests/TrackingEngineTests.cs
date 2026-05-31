using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;

namespace OpenCad2D.Interaction.Tests;

public sealed class TrackingEngineTests
{
    [Fact]
    public void BuildAxisLines_ShouldCreateHorizontalAndVerticalLineForEachSmartPoint()
    {
        var engine = new TrackingEngine();
        SmartPoint smartPoint = CreateSmartPoint(10, 20);

        IReadOnlyList<TrackingLine> lines = engine.BuildAxisLines(new[] { smartPoint });

        Assert.Equal(2, lines.Count);
        Assert.Contains(lines, line =>
            line.Kind == TrackingLineKind.Horizontal &&
            line.Origin == smartPoint.Position &&
            line.Direction == new Vector2D(1, 0));
        Assert.Contains(lines, line =>
            line.Kind == TrackingLineKind.Vertical &&
            line.Origin == smartPoint.Position &&
            line.Direction == new Vector2D(0, 1));
    }

    [Fact]
    public void FindNearestTrackingCandidate_ShouldProjectPointOnHorizontalLine()
    {
        var engine = new TrackingEngine();
        SmartPoint smartPoint = CreateSmartPoint(10, 20);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { smartPoint },
            new Point2D(35, 21),
            tolerance: 2);

        Assert.NotNull(candidate);
        Assert.Equal(SnapKind.Tracking, candidate.Kind);
        Assert.Equal(new Point2D(35, 20), candidate.Point);
    }

    [Fact]
    public void FindNearestTrackingCandidate_ShouldProjectPointOnVerticalLine()
    {
        var engine = new TrackingEngine();
        SmartPoint smartPoint = CreateSmartPoint(10, 20);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { smartPoint },
            new Point2D(11, 55),
            tolerance: 2);

        Assert.NotNull(candidate);
        Assert.Equal(SnapKind.Tracking, candidate.Kind);
        Assert.Equal(new Point2D(10, 55), candidate.Point);
    }


    [Fact]
    public void FindNearestTrackingCandidate_ShouldStoreOriginAndSignedPositiveDirection()
    {
        var engine = new TrackingEngine();
        SmartPoint smartPoint = CreateSmartPoint(10, 20);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { smartPoint },
            new Point2D(35, 21),
            tolerance: 2);

        Assert.NotNull(candidate);
        Assert.Equal(smartPoint.Position, candidate.TrackingOrigin);
        Assert.Equal(new Vector2D(1, 0), candidate.TrackingDirection);
    }

    [Fact]
    public void FindNearestTrackingCandidate_ShouldStoreSignedNegativeDirection()
    {
        var engine = new TrackingEngine();
        SmartPoint smartPoint = CreateSmartPoint(10, 20);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { smartPoint },
            new Point2D(5, 21),
            tolerance: 2);

        Assert.NotNull(candidate);
        Assert.Equal(smartPoint.Position, candidate.TrackingOrigin);
        Assert.Equal(new Vector2D(-1, 0), candidate.TrackingDirection);
    }


    [Fact]
    public void FindNearestTrackingCandidate_ShouldReturnTrackingIntersectionForCrossingLines()
    {
        var engine = new TrackingEngine();
        SmartPoint first = CreateSmartPoint(10, 20);
        SmartPoint second = CreateSmartPoint(40, 50);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { first, second },
            new Point2D(40.5, 20.5),
            tolerance: 2);

        Assert.NotNull(candidate);
        Assert.Equal(SnapKind.TrackingIntersection, candidate.Kind);
        Assert.Equal(new Point2D(40, 20), candidate.Point);
        Assert.Null(candidate.TrackingOrigin);
        Assert.Null(candidate.TrackingDirection);
    }

    [Fact]
    public void FindNearestTrackingCandidate_ShouldIgnoreTrackingIntersectionOutsideTolerance()
    {
        var engine = new TrackingEngine();
        SmartPoint first = CreateSmartPoint(10, 20);
        SmartPoint second = CreateSmartPoint(40, 50);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { first, second },
            new Point2D(43, 20.5),
            tolerance: 2);

        Assert.NotNull(candidate);
        Assert.Equal(SnapKind.Tracking, candidate.Kind);
    }

    [Fact]
    public void FindNearestTrackingCandidate_ShouldReturnNullWhenOutsideTolerance()
    {
        var engine = new TrackingEngine();
        SmartPoint smartPoint = CreateSmartPoint(10, 20);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { smartPoint },
            new Point2D(35, 25),
            tolerance: 2);

        Assert.Null(candidate);
    }

    [Fact]
    public void FindNearestTrackingCandidate_ShouldReturnNullForNonPositiveTolerance()
    {
        var engine = new TrackingEngine();
        SmartPoint smartPoint = CreateSmartPoint(10, 20);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { smartPoint },
            new Point2D(35, 20),
            tolerance: 0);

        Assert.Null(candidate);
    }


    [Fact]
    public void BuildLines_ShouldCreateEntityExtensionForLineSmartPoint()
    {
        var engine = new TrackingEngine();
        var document = new CadDocument();
        var line = new LineEntity(
            new Point2D(10, 20),
            new Point2D(30, 40));
        document.AddEntity(line);

        SmartPoint smartPoint = CreateSmartPoint(
            line.Start,
            line.Id);

        IReadOnlyList<TrackingLine> lines = engine.BuildLines(
            new[] { smartPoint },
            document);

        Assert.Contains(lines, trackingLine =>
            trackingLine.Kind == TrackingLineKind.EntityExtension &&
            trackingLine.Origin == line.Start &&
            Math.Abs(trackingLine.Direction.Cross(new Vector2D(20, 20).Normalize())) <= 1e-9);
    }

    [Fact]
    public void FindNearestTrackingCandidate_ShouldProjectPointOnLineEntityExtension()
    {
        var engine = new TrackingEngine();
        var document = new CadDocument();
        var line = new LineEntity(
            new Point2D(10, 20),
            new Point2D(30, 40));
        document.AddEntity(line);

        SmartPoint smartPoint = CreateSmartPoint(
            line.Start,
            line.Id);

        SnapCandidate? candidate = engine.FindNearestTrackingCandidate(
            new[] { smartPoint },
            new Point2D(39, 51),
            tolerance: 2,
            document: document);

        Assert.NotNull(candidate);
        Assert.Equal(SnapKind.Extension, candidate.Kind);
        Assert.Equal(line.Id, candidate.EntityId);
        Assert.Equal(smartPoint.Position, candidate.TrackingOrigin);
        Assert.NotNull(candidate.TrackingDirection);
    }

    [Fact]
    public void BuildLines_ShouldCreateEntityExtensionForStraightPolylineSegment()
    {
        var engine = new TrackingEngine();
        var document = new CadDocument();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 10),
            new Point2D(20, 10)
        });
        document.AddEntity(polyline);

        SmartPoint smartPoint = CreateSmartPoint(
            new Point2D(5, 5),
            polyline.Id,
            SnapKind.Midpoint);

        IReadOnlyList<TrackingLine> lines = engine.BuildLines(
            new[] { smartPoint },
            document);

        Assert.Contains(lines, trackingLine =>
            trackingLine.Kind == TrackingLineKind.EntityExtension &&
            trackingLine.Origin == smartPoint.Position &&
            Math.Abs(trackingLine.Direction.Cross(new Vector2D(10, 10).Normalize())) <= 1e-9);
    }

    private static SmartPoint CreateSmartPoint(double x, double y)
    {
        return CreateSmartPoint(
            new Point2D(x, y),
            EntityId.New());
    }

    private static SmartPoint CreateSmartPoint(
        Point2D position,
        EntityId entityId,
        SnapKind sourceSnapKind = SnapKind.Endpoint)
    {
        return new SmartPoint(
            position,
            sourceSnapKind,
            entityId,
            DateTimeOffset.UtcNow);
    }
}
