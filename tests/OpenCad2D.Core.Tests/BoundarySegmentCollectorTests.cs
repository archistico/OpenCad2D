using OpenCad2D.Core.Editing;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class BoundarySegmentCollectorTests
{
    [Fact]
    public void Collect_LineEntity_ShouldCreateSegmentWithSourceMetadata()
    {
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var collector = new BoundarySegmentCollector();

        BoundarySegmentCollection collection = collector.Collect(
            new[] { line },
            new BoundaryFillOptions());

        BoundarySegment segment = Assert.Single(collection.Segments);
        Assert.Equal(line.Id, segment.SourceEntityId);
        Assert.Equal(BoundarySegmentSourceKind.Line, segment.SourceKind);
        Assert.False(segment.IsSampledCurve);
        Assert.Equal(0, collection.IgnoredEntityCount);
    }

    [Fact]
    public void Collect_ClosedStraightPolyline_ShouldIncludeClosingSegment()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5)
            },
            isClosed: true);
        var collector = new BoundarySegmentCollector();

        BoundarySegmentCollection collection = collector.Collect(
            new[] { polyline },
            new BoundaryFillOptions());

        Assert.Equal(4, collection.Segments.Count);
        Assert.All(collection.Segments, segment =>
        {
            Assert.Equal(polyline.Id, segment.SourceEntityId);
            Assert.Equal(BoundarySegmentSourceKind.Polyline, segment.SourceKind);
            Assert.False(segment.IsSampledCurve);
        });
    }

    [Fact]
    public void Collect_OpenStraightPolyline_ShouldNotIncludeClosingSegment()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5)
            });
        var collector = new BoundarySegmentCollector();

        BoundarySegmentCollection collection = collector.Collect(
            new[] { polyline },
            new BoundaryFillOptions());

        Assert.Equal(2, collection.Segments.Count);
    }

    [Fact]
    public void Collect_Circle_WhenCurveBoundariesDisabled_ShouldIgnoreCircle()
    {
        var collector = new BoundarySegmentCollector();
        var circle = new CircleEntity(new Point2D(0, 0), 10);

        BoundarySegmentCollection collection = collector.Collect(
            new[] { circle },
            new BoundaryFillOptions(includeCurveBoundaries: false));

        Assert.Empty(collection.Segments);
        Assert.Equal(1, collection.IgnoredEntityCount);
        Assert.Equal(0, collection.SampledCurveSegmentCount);
    }

    [Fact]
    public void Collect_Circle_WhenCurveBoundariesEnabled_ShouldSampleCircle()
    {
        var collector = new BoundarySegmentCollector();
        var circle = new CircleEntity(new Point2D(0, 0), 10);

        BoundarySegmentCollection collection = collector.Collect(
            new[] { circle },
            new BoundaryFillOptions(
                includeCurveBoundaries: true,
                curveSampleCount: 16));

        Assert.Equal(16, collection.Segments.Count);
        Assert.Equal(16, collection.SampledCurveSegmentCount);
        Assert.All(collection.Segments, segment =>
        {
            Assert.Equal(circle.Id, segment.SourceEntityId);
            Assert.Equal(BoundarySegmentSourceKind.Circle, segment.SourceKind);
            Assert.True(segment.IsSampledCurve);
        });
    }

    [Fact]
    public void Collect_Arc_WhenCurveBoundariesEnabled_ShouldSampleArcIncludingEndpoints()
    {
        var collector = new BoundarySegmentCollector();
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(0),
            Angle.FromDegrees(180));

        BoundarySegmentCollection collection = collector.Collect(
            new[] { arc },
            new BoundaryFillOptions(
                includeCurveBoundaries: true,
                curveSampleCount: 16));

        Assert.NotEmpty(collection.Segments);
        Assert.Equal(arc.Geometry.StartPoint, collection.Segments[0].Start);
        Assert.Equal(arc.Geometry.EndPoint, collection.Segments[^1].End);
        Assert.All(collection.Segments, segment =>
        {
            Assert.Equal(arc.Id, segment.SourceEntityId);
            Assert.Equal(BoundarySegmentSourceKind.Arc, segment.SourceKind);
            Assert.True(segment.IsSampledCurve);
        });
    }

    [Fact]
    public void Collect_ClockwiseArcAcrossZero_WhenCurveBoundariesEnabled_ShouldSampleArcIncludingEndpoints()
    {
        var collector = new BoundarySegmentCollector();
        var arc = new ArcEntity(
            new Point2D(0, 0),
            10,
            Angle.FromDegrees(10),
            Angle.FromDegrees(350),
            isCounterClockwise: false);

        BoundarySegmentCollection collection = collector.Collect(
            new[] { arc },
            new BoundaryFillOptions(
                includeCurveBoundaries: true,
                curveSampleCount: 64));

        Assert.NotEmpty(collection.Segments);
        Assert.Equal(arc.Geometry.StartPoint, collection.Segments[0].Start);
        Assert.Equal(arc.Geometry.EndPoint, collection.Segments[^1].End);
        Assert.All(collection.Segments, segment => Assert.True(segment.IsSampledCurve));
    }

    [Fact]
    public void Collect_BulgedPolyline_WhenCurveBoundariesEnabled_ShouldSamplePolylineArcSegments()
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0)
            },
            segmentBulges: new[] { 1.0 });
        var collector = new BoundarySegmentCollector();

        BoundarySegmentCollection collection = collector.Collect(
            new[] { polyline },
            new BoundaryFillOptions(
                includeCurveBoundaries: true,
                curveSampleCount: 16));

        Assert.True(collection.Segments.Count > 1);
        Assert.Equal(collection.Segments.Count, collection.SampledCurveSegmentCount);
        Assert.All(collection.Segments, segment => Assert.True(segment.IsSampledCurve));
    }
    [Fact]
    public void Collect_WithSmallEndpointGapWithinTolerance_ShouldBridgeEndpointGap()
    {
        var collector = new BoundarySegmentCollector();
        var lines = new[]
        {
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)),
            new LineEntity(new Point2D(0, 0.2), new Point2D(0, 5))
        };

        BoundarySegmentCollection collection = collector.Collect(
            lines,
            new BoundaryFillOptions(gapTolerance: 0.25));

        Assert.Equal(3, collection.Segments.Count);
        Assert.Equal(1, collection.BridgedGapCount);
        BoundarySegment bridge = Assert.Single(collection.Segments, segment =>
            segment.SourceKind == BoundarySegmentSourceKind.GapBridge);
        Assert.Equal(new Point2D(0, 0), bridge.Start);
        Assert.Equal(new Point2D(0, 0.2), bridge.End);
        Assert.Equal(new Point2D(0, 0), collection.Segments[0].Start);
        Assert.Equal(new Point2D(0, 0.2), collection.Segments[1].Start);
    }

    [Fact]
    public void Collect_WithEndpointGapAboveTolerance_ShouldKeepEndpointsSeparate()
    {
        var collector = new BoundarySegmentCollector();
        var lines = new[]
        {
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)),
            new LineEntity(new Point2D(0, 0.5), new Point2D(0, 5))
        };

        BoundarySegmentCollection collection = collector.Collect(
            lines,
            new BoundaryFillOptions(gapTolerance: 0.25));

        Assert.Equal(2, collection.Segments.Count);
        Assert.Equal(0, collection.BridgedGapCount);
        Assert.NotEqual(collection.Segments[0].Start, collection.Segments[1].Start);
    }


    [Fact]
    public void Collect_WithSmallEndpointToSegmentGapWithinTolerance_ShouldBridgeToProjectedPoint()
    {
        var collector = new BoundarySegmentCollector();
        var lines = new[]
        {
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)),
            new LineEntity(new Point2D(5, 0.25), new Point2D(5, 5))
        };

        BoundarySegmentCollection collection = collector.Collect(
            lines,
            new BoundaryFillOptions(gapTolerance: 0.5));

        Assert.Equal(3, collection.Segments.Count);
        Assert.Equal(1, collection.BridgedGapCount);
        BoundarySegment bridge = Assert.Single(collection.Segments, segment =>
            segment.SourceKind == BoundarySegmentSourceKind.GapBridge);
        Assert.Equal(new Point2D(5, 0.25), bridge.Start);
        Assert.Equal(new Point2D(5, 0), bridge.End);
        Assert.Equal(new Point2D(0, 0), collection.Segments[0].Start);
        Assert.Equal(new Point2D(10, 0), collection.Segments[0].End);
    }

    [Fact]
    public void Collect_WithEndpointToSegmentGapAboveTolerance_ShouldNotBridgeToProjectedPoint()
    {
        var collector = new BoundarySegmentCollector();
        var lines = new[]
        {
            new LineEntity(new Point2D(0, 0), new Point2D(10, 0)),
            new LineEntity(new Point2D(5, 0.75), new Point2D(5, 5))
        };

        BoundarySegmentCollection collection = collector.Collect(
            lines,
            new BoundaryFillOptions(gapTolerance: 0.5));

        Assert.Equal(2, collection.Segments.Count);
        Assert.Equal(0, collection.BridgedGapCount);
        Assert.DoesNotContain(collection.Segments, segment =>
            segment.SourceKind == BoundarySegmentSourceKind.GapBridge);
    }

}
