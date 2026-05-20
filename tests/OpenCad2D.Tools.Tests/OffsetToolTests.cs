using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Geometry;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;

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
    public void OffsetBezierSpline_ShouldCreatePolylineApproximationOffset()
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
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 4)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 8)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity offset = Assert.IsType<PolylineEntity>(Assert.Single(document.Entities.All.Where(entity => !entity.Id.Equals(spline.Id))));
        Assert.False(offset.IsClosed);
        Assert.True(offset.Vertices.Count > 10);
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
