using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;
using System.Reflection;

namespace OpenCad2D.Tools.Tests;

public sealed class OffsetToolTests
{
    [Fact]
    public void HandleCommandInput_WithDistance_ShouldPromptForEntity()
    {
        var context = CreateContext();
        var tool = new OffsetTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromDistance("2", 2),
            context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(2, tool.Distance);
    }

    [Fact]
    public void ConfirmDistancePrompt_WithoutPreviousDistance_ShouldStayWaitingForDistance()
    {
        OffsetTool.ResetLastDistanceForTests();
        var context = CreateContext();
        var tool = new OffsetTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForDistance, tool.State);
        Assert.Contains("No previous offset distance", result.Message);
    }

    [Fact]
    public void ConfirmDistancePrompt_WithPreviousDistance_ShouldUseLastDistance()
    {
        OffsetTool.ResetLastDistanceForTests();
        var firstContext = CreateContext();
        var firstTool = new OffsetTool();
        firstTool.HandleCommandInput(CommandInputSubmission.FromDistance("7", 7), firstContext);

        var secondContext = CreateContext();
        var secondTool = new OffsetTool();

        ToolResult result = secondTool.HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            secondContext);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, secondTool.State);
        Assert.Equal(7, secondTool.Distance);
        Assert.Contains("remains 7", result.Message);
    }

    [Fact]
    public void PointerDistanceInput_WithTwoClicks_ShouldStoreMeasuredDistance()
    {
        OffsetTool.ResetLastDistanceForTests();
        var context = CreateContext();
        var tool = new OffsetTool();

        ToolResult first = tool.OnPointerPressed(context, new PointerInfo(new Point2D(1, 2)));
        ToolResult second = tool.OnPointerPressed(context, new PointerInfo(new Point2D(4, 6)));

        Assert.Equal(ToolResultKind.Started, first.Kind);
        Assert.Equal(ToolResultKind.Started, second.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(5, tool.Distance);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void GetActiveSnapKind_WhenSelectingEntity_ShouldUseEntityOnlySnap()
    {
        var context = CreateContext(enabledSnaps: SnapKind.All | SnapKind.Entity);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);

        Assert.Equal(SnapKind.EntityOnly, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void GetActiveSnapKind_WhenChoosingSide_ShouldExcludeEntitySnap()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);
        var context = CreateContext(document, enabledSnaps: SnapKind.All | SnapKind.Entity);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(SnapKind.All, tool.GetActiveSnapKind(context));
    }

    [Fact]
    public void OffsetLine_ShouldCreateParallelLineOnPickedSide()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        LineEntity offset = Assert.IsType<LineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(line.Id))));
        Assert.Equal(new Point2D(0, 2), offset.Start);
        Assert.Equal(new Point2D(10, 2), offset.End);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
    }


    [Fact]
    public void OffsetPolyline_OpenLShape_ShouldCreateMiteredPolylineOnPickedSide()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10)
            });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.False(offset.IsClosed);
        Assert.Equal(3, offset.Vertices.Count);
        AssertPointNear(new Point2D(0, 2), offset.Vertices[0]);
        AssertPointNear(new Point2D(8, 2), offset.Vertices[1]);
        AssertPointNear(new Point2D(8, 10), offset.Vertices[2]);
    }

    [Fact]
    public void OffsetPolyline_ClosedRectangle_ClickOutside_ShouldCreateLargerRectangle()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5)
            },
            isClosed: true);
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, -2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.True(offset.IsClosed);
        Assert.Equal(4, offset.Vertices.Count);
        AssertPointNear(new Point2D(-2, -2), offset.Vertices[0]);
        AssertPointNear(new Point2D(12, -2), offset.Vertices[1]);
        AssertPointNear(new Point2D(12, 7), offset.Vertices[2]);
        AssertPointNear(new Point2D(-2, 7), offset.Vertices[3]);
    }

    [Fact]
    public void OffsetPolyline_ClosedRectangle_ClickInside_ShouldCreateSmallerRectangle()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5)
            },
            isClosed: true);
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 1)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.True(offset.IsClosed);
        Assert.Equal(4, offset.Vertices.Count);
        AssertPointNear(new Point2D(2, 2), offset.Vertices[0]);
        AssertPointNear(new Point2D(8, 2), offset.Vertices[1]);
        AssertPointNear(new Point2D(8, 3), offset.Vertices[2]);
        AssertPointNear(new Point2D(2, 3), offset.Vertices[3]);
    }



    [Fact]
    public void HandleCommandInput_WithZeroDistance_ShouldStayWaitingForDistance()
    {
        var context = CreateContext();
        var tool = new OffsetTool();

        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromDistance("0", 0),
            context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForDistance, tool.State);
        Assert.Contains("greater than zero", result.Message);
    }

    [Fact]
    public void OffsetLine_ZeroLength_ShouldReturnWarningAndNotCreateEntity()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(0, 0));
        document.AddEntity(line);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Single(document.Entities.All);
        Assert.Contains("zero-length line", result.Message);
    }

    [Fact]
    public void OffsetCircle_ClickInsideWithTooLargeDistance_ShouldReturnWarningAndNotCreateEntity()
    {
        CadDocument document = new();
        var circle = new CircleEntity(new Point2D(0, 0), 5);
        document.AddEntity(circle);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("10", 10), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(1, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Single(document.Entities.All);
        Assert.Contains("circle radius", result.Message);
    }

    [Fact]
    public void OffsetArc_ClickInsideWithTooLargeDistance_ShouldReturnWarningAndNotCreateEntity()
    {
        CadDocument document = new();
        var arc = new ArcEntity(new Point2D(0, 0), 5, Angle.FromDegrees(0), Angle.FromDegrees(90));
        document.AddEntity(arc);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("10", 10), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(1, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Single(document.Entities.All);
        Assert.Contains("arc radius", result.Message);
    }

    [Fact]
    public void OffsetPolyline_WithZeroLengthSegment_ShouldReturnWarningAndNotCreateEntity()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(0, 0) });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Single(document.Entities.All);
        Assert.Contains("zero-length segments", result.Message);
    }

    [Fact]
    public void OffsetPolyline_OpenCollinearSegments_ShouldCreateStraightOffsetPolyline()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(5, 0),
                new Point2D(10, 0)
            });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.False(offset.IsClosed);
        Assert.Equal(3, offset.Vertices.Count);
        AssertPointNear(new Point2D(0, 2), offset.Vertices[0]);
        AssertPointNear(new Point2D(5, 2), offset.Vertices[1]);
        AssertPointNear(new Point2D(10, 2), offset.Vertices[2]);
    }


    [Fact]
    public void OffsetPolyline_OpenSharpTurn_ShouldUseBevelFallbackWhenMiterWouldBeTooLong()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(0.1, 0.1)
            });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("1", 1), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 1)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.False(offset.IsClosed);
        Assert.Equal(4, offset.Vertices.Count);
        Assert.All(offset.Vertices, vertex =>
        {
            Assert.True(
                vertex.DistanceTo(new Point2D(10, 0)) < 20,
                $"Offset vertex {vertex} should not be a long miter spike.");
        });
    }

    [Fact]
    public void OnPointerMoved_WhenWaitingForSidePoint_ShouldExposePreviewWithoutCreatingEntity()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        LineEntity preview = Assert.IsType<LineEntity>(tool.GetPreviewEntity());
        AssertPointNear(new Point2D(0, 2), preview.Start);
        AssertPointNear(new Point2D(10, 2), preview.End);
        Assert.Single(document.Entities.All);
    }



    [Fact]
    public void OffsetPolyline_WithArcSegments_ShouldCreateApproximateLinearOffsetPolyline()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0)
            },
            segmentBulges: new[] { 0.0, 1.0 });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.False(offset.HasArcSegments);
        Assert.False(offset.IsClosed);
        Assert.True(offset.Vertices.Count > polyline.Vertices.Count);
    }


    [Fact]
    public void OnPointerMoved_WithMixedPolyline_ShouldExposeApproximateOffsetPreview()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(20, 0)
            },
            segmentBulges: new[] { 0.0, 1.0 });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(context, new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        PolylineEntity preview = Assert.IsType<PolylineEntity>(tool.GetPreviewEntity());
        Assert.False(preview.HasArcSegments);
        Assert.True(preview.Vertices.Count > polyline.Vertices.Count);
        Assert.Single(document.Entities.All);
    }

    [Fact]
    public void OffsetBezierSpline_ShouldReturnDeferredMessageAndNotCreatePolylineApproximation()
    {
        CadDocument document = new();
        var spline = new BezierSplineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 10),
            new Point2D(10, 0)
        });
        document.AddEntity(spline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("1", 1), context);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 4)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Single(document.Entities.All);
        Assert.Contains("Spline offsets are deferred", result.Message);
    }

    [Fact]
    public void OffsetEllipse_ShouldReturnDeferredMessageAndNotCreateEntity()
    {
        CadDocument document = new();
        var ellipse = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(5, 0),
            3);
        document.AddEntity(ellipse);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("1", 1), context);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Single(document.Entities.All);
        Assert.Contains("Ellipse and elliptical arc offsets are deferred", result.Message);
    }

    [Fact]
    public void OffsetEllipticalArc_ShouldReturnDeferredMessageAndNotCreateEntity()
    {
        CadDocument document = new();
        var arc = new EllipticalArcEntity(
            new Point2D(0, 0),
            new Vector2D(5, 0),
            3,
            0,
            Math.PI);
        document.AddEntity(arc);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("1", 1), context);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Single(document.Entities.All);
        Assert.Contains("Ellipse and elliptical arc offsets are deferred", result.Message);
    }


    [Fact]
    public void DeterminePolylineOffsetSide_WithVeryShortSegmentAndClearSidePoint_ShouldUseNormalizedSignedDistance()
    {
        int side = InvokeDeterminePolylineOffsetSide(
            new[]
            {
                new LineSegment2D(new Point2D(0, 0), new Point2D(0.01, 0))
            },
            new Point2D(0.005, -0.1),
            new GeometryTolerance(
                distance: 0.001,
                angle: 1e-10,
                parameter: 1e-12,
                vectorLength: 1e-12));

        Assert.Equal(-1, side);
    }

    [Fact]
    public void DeterminePolylineOffsetSide_WithLongSegmentAndNearLinePoint_ShouldUseDistanceTolerance()
    {
        int side = InvokeDeterminePolylineOffsetSide(
            new[]
            {
                new LineSegment2D(new Point2D(0, 0), new Point2D(100, 0))
            },
            new Point2D(50, 0.0005),
            new GeometryTolerance(
                distance: 0.001,
                angle: 1e-10,
                parameter: 1e-12,
                vectorLength: 1e-12));

        Assert.Equal(1, side);
    }

    [Fact]
    public void GetPreviewDescriptor_AfterEntitySelection_ShouldHighlightSelectedTarget()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolPreviewDescriptor descriptor = tool.GetPreviewDescriptor(context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        ToolPreviewEntityOverlay overlay = Assert.Single(descriptor.EntityOverlays);
        Assert.Equal(ToolPreviewHighlightKind.Emphasis, overlay.Kind);
        Assert.Same(line, Assert.Single(overlay.Entities));
        Assert.Empty(descriptor.HighlightedEntities);
    }

    [Fact]
    public void GetPreviewDescriptor_AfterPointerMove_ShouldHighlightOffsetPreviewAsAddition()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult move = tool.OnPointerMoved(context, new PointerInfo(new Point2D(5, 2)));

        ToolPreviewDescriptor descriptor = tool.GetPreviewDescriptor(context);

        Assert.Equal(ToolResultKind.Updated, move.Kind);
        Assert.Contains("Highlighted preview", move.Message);
        Assert.Equal(ToolPreviewHighlightKind.Addition, descriptor.HighlightedEntityKind);
        LineEntity preview = Assert.IsType<LineEntity>(Assert.Single(descriptor.HighlightedEntities));
        AssertPointNear(new Point2D(0, 2), preview.Start);
        AssertPointNear(new Point2D(10, 2), preview.End);
        ToolPreviewEntityOverlay overlay = Assert.Single(descriptor.EntityOverlays);
        Assert.Same(line, Assert.Single(overlay.Entities));
    }

    [Fact]
    public void CreateOffset_AfterSideSelection_ShouldReturnToEntitySelectionWithSameDistance()
    {
        CadDocument document = new();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(0, 10), new Point2D(10, 10));
        document.AddEntity(first);
        document.AddEntity(second);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        ToolResult created = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.Completed, created.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(2, tool.Distance);
    }

    [Fact]
    public void HandleCommandInput_WithTypedPointWhileWaitingForDistance_ShouldStartDistanceMeasurement()
    {
        OffsetTool.ResetLastDistanceForTests();
        var context = CreateContext();
        var tool = new OffsetTool();

        ToolResult first = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("1,2", new Point2D(1, 2)),
            context);
        ToolResult second = tool.HandleCommandInput(
            CommandInputSubmission.FromPoint("4,6", new Point2D(4, 6)),
            context);

        Assert.Equal(ToolResultKind.Started, first.Kind);
        Assert.Equal(ToolResultKind.Started, second.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(5, tool.Distance);
    }

    [Fact]
    public void HandleCommandInput_WithNumberWhileWaitingForSecondPoint_ShouldUseTypedDistance()
    {
        var context = CreateContext();
        var tool = new OffsetTool();

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(1, 2)));
        ToolResult result = tool.HandleCommandInput(
            CommandInputSubmission.FromNumber("12.5", 12.5),
            context);

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
        Assert.Equal(12.5, tool.Distance);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void GetPromptState_DistancePhases_ShouldAcceptPointsNumbersAndEmptyEnter()
    {
        var context = CreateContext();
        var tool = new OffsetTool();

        CommandPromptState distancePrompt = tool.GetPromptState(context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 0)));
        CommandPromptState secondPointPrompt = tool.GetPromptState(context);

        Assert.Equal(CommandInputKind.PointOrNumber, distancePrompt.ExpectedInput);
        Assert.True(distancePrompt.AcceptsEmptyEnter);
        Assert.Equal(CommandInputKind.PointOrNumber, secondPointPrompt.ExpectedInput);
    }

    [Fact]
    public void ConfirmWhileSelectingEntity_ShouldFinishCommand()
    {
        CadDocument document = new();
        document.AddEntity(new LineEntity(new Point2D(0, 0), new Point2D(10, 0)));
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();
        var controller = new ToolController(context, tool);

        controller.SubmitCommandInput(CommandInputSubmission.FromDistance("2", 2));
        Assert.True(tool.GetPromptState(context).AcceptsEmptyEnter);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsNotType<OffsetTool>(controller.ActiveTool);
        Assert.Single(document.Entities.All);
    }

    [Fact]
    public void ConfirmWhileChoosingSide_ShouldFinishCommandWithoutCreatingEntity()
    {
        CadDocument document = new();
        document.AddEntity(new LineEntity(new Point2D(0, 0), new Point2D(10, 0)));
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();
        var controller = new ToolController(context, tool);

        controller.SubmitCommandInput(CommandInputSubmission.FromDistance("2", 2));
        controller.OnPointerPressed(new PointerInfo(new Point2D(5, 0)));
        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsNotType<OffsetTool>(controller.ActiveTool);
        Assert.Single(document.Entities.All);
        Assert.Null(context.CurrentBasePoint);
    }

    [Fact]
    public void OffsetPolyline_ClosedRectangle_SidePointOnEdgeExtension_ShouldOffsetOutside()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5)
            },
            isClosed: true);
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("1", 1), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        // (12, 0) lies on the extension of the bottom edge, outside the rectangle.
        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(12, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        AssertPointNear(new Point2D(-1, -1), offset.Vertices[0]);
        AssertPointNear(new Point2D(11, -1), offset.Vertices[1]);
        AssertPointNear(new Point2D(11, 6), offset.Vertices[2]);
        AssertPointNear(new Point2D(-1, 6), offset.Vertices[3]);
    }

    [Fact]
    public void OffsetPolyline_OpenSharpInnerCorner_ShouldTrimAtIntersectionWithoutLoop()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 10),
                new Point2D(0, 0),
                new Point2D(1.5, 10)
            });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("0.1", 0.1), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 5)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(0.3, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.Equal(3, offset.Vertices.Count);
        AssertPointNear(new Point2D(0.1, 10), offset.Vertices[0]);
        Assert.True(offset.Vertices[1].Y > 1.0, $"Inner tip {offset.Vertices[1]} should be the trimmed V tip, not a loop near the source vertex.");
        AssertAllVerticesAtLeastDistanceFromSource(polyline, offset, 0.1);
    }

    [Fact]
    public void OffsetPolyline_ClosedRectangleInwardTooFar_ShouldRejectWithoutCreatingInvertedGeometry()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5)
            },
            isClosed: true);
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("3", 3), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 1)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Contains("too large", result.Message);
        Assert.Single(document.Entities.All);
        Assert.Equal(OffsetToolState.WaitingForSidePoint, tool.State);
    }

    [Fact]
    public void OffsetPolyline_RoundedRectangleInwardBeyondCornerRadius_ShouldDropConsumedArcSegments()
    {
        CadDocument document = new();
        // Quarter-circle corners of radius 1. In PolylineEntity a negative bulge turns
        // counter-clockwise, so these corners are convex for this CCW outline.
        double bulge = -Math.Tan(Math.PI / 8.0);
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(1, 0),
                new Point2D(19, 0),
                new Point2D(20, 1),
                new Point2D(20, 9),
                new Point2D(19, 10),
                new Point2D(1, 10),
                new Point2D(0, 9),
                new Point2D(0, 1)
            },
            isClosed: true,
            segmentBulges: new[] { 0.0, bulge, 0.0, bulge, 0.0, bulge, 0.0, bulge });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.True(offset.IsClosed);
        Assert.Equal(4, offset.Vertices.Count);
        AssertPointNear(new Point2D(2, 2), offset.Vertices[0]);
        AssertPointNear(new Point2D(18, 2), offset.Vertices[1]);
        AssertPointNear(new Point2D(18, 8), offset.Vertices[2]);
        AssertPointNear(new Point2D(2, 8), offset.Vertices[3]);
    }

    [Fact]
    public void OffsetPolyline_ClosedWithRepeatedClosingVertex_ShouldIgnoreZeroLengthClosingSegment()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 5),
                new Point2D(0, 0)
            },
            isClosed: true);
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, -2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(polyline.Id))));
        Assert.True(offset.IsClosed);
        Assert.Equal(4, offset.Vertices.Count);
        AssertPointNear(new Point2D(-2, -2), offset.Vertices[0]);
        AssertPointNear(new Point2D(12, -2), offset.Vertices[1]);
        AssertPointNear(new Point2D(12, 7), offset.Vertices[2]);
        AssertPointNear(new Point2D(-2, 7), offset.Vertices[3]);
    }

    [Fact]
    public void CreateOffset_WhenTargetWasRemovedWhileChoosingSide_ShouldNotCreateEntity()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        document.AddEntity(line);
        ToolContext context = CreateContext(document);
        var tool = new OffsetTool();

        tool.HandleCommandInput(CommandInputSubmission.FromDistance("2", 2), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        document.RemoveEntity(line.Id);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(document.Entities.All);
        Assert.Equal(OffsetToolState.WaitingForEntity, tool.State);
    }

    private static void AssertAllVerticesAtLeastDistanceFromSource(
        PolylineEntity source,
        PolylineEntity offset,
        double distance)
    {
        Assert.All(offset.Vertices, vertex =>
        {
            double actual = source.DistanceTo(vertex);
            Assert.True(
                actual >= distance - 1e-6,
                $"Offset vertex {vertex} is {actual} from the source, closer than the offset distance {distance}.");
        });
    }

    private static int InvokeDeterminePolylineOffsetSide(
        IReadOnlyList<LineSegment2D> segments,
        Point2D sidePoint,
        GeometryTolerance tolerance)
    {
        MethodInfo method = typeof(OffsetTool).GetMethod(
            "DeterminePolylineOffsetSide",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("DeterminePolylineOffsetSide was not found.");

        return (int)method.Invoke(
            null,
            new object[] { segments, sidePoint, tolerance })!;
    }

    private static void AssertPointNear(Point2D expected, Point2D actual)
    {
        Assert.True(
            expected.DistanceTo(actual) < 1e-6,
            $"Expected {expected}, actual {actual}.");
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SnapKind enabledSnaps = SnapKind.None)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: enabledSnaps,
            selectionTolerance: 5);
    }
}
