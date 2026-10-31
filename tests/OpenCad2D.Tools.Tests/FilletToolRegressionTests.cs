using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;
using OpenCad2D.Tools.Input;
using OpenCad2D.Tools.Selection;

namespace OpenCad2D.Tools.Tests;

public sealed class FilletToolRegressionTests
{
    [Theory]
    [InlineData("Radius")]
    [InlineData("Trim")]
    public void ParameterOption_WithActivePreview_ShouldClearStaleFilletGeometry(string option)
    {
        ToolContext context = CreateLines(out LineEntity first, out _);
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));
        Assert.NotEmpty(tool.GetPreviewEntities());

        tool.HandleCommandInput(CommandInputSubmission.Option(option, option), context);

        Assert.Empty(tool.GetPreviewEntities());
        Assert.Equal(first.Id, tool.FirstEntityId);
    }

    [Fact]
    public void CurvedPolyline_FirstPick_ShouldReportUnsupportedGeometryImmediately()
    {
        ToolContext context = CreateContext();
        context.Document.AddEntity(new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 10) },
            segmentBulges: new[] { -0.25, 0.0 }));
        FilletTool tool = WithRadius(context, 2);

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Contains("linear polylines only", result.Message);
        Assert.Null(tool.FirstEntityId);
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
    }

    [Fact]
    public void PolylineFirstPick_ShouldEmphasizeOnlySelectedSegment()
    {
        ToolContext context = CreateContext();
        context.Document.AddEntity(new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 10) }));
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        LineEntity emphasized = Assert.IsType<LineEntity>(Assert.Single(Assert.Single(tool.GetPreviewDescriptor(context).EntityOverlays).Entities));

        Assert.Equal(new Point2D(0, 0), emphasized.Start);
        Assert.Equal(new Point2D(10, 0), emphasized.End);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void LineDirections_Reversed_ShouldKeepExactRadiusTangencyAndUndoRedo(bool reverseFirst, bool reverseSecond)
    {
        ToolContext context = CreateContext();
        var first = new LineEntity(reverseFirst ? new Point2D(10, 0) : new Point2D(0, 0),
            reverseFirst ? new Point2D(0, 0) : new Point2D(10, 0));
        var second = new LineEntity(reverseSecond ? new Point2D(10, 10) : new Point2D(10, 0),
            reverseSecond ? new Point2D(10, 0) : new Point2D(10, 10));
        context.Document.AddEntity(first);
        context.Document.AddEntity(second);
        const double radius = 2.12345678901234;
        FilletTool tool = WithRadius(context, radius);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        ArcEntity arc = Assert.Single(context.Document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(radius, arc.Radius);
        Assert.Equal(10 - radius, arc.Center.X, 12);
        Assert.Equal(radius, arc.Center.Y, 12);
        Assert.Equal(10 - radius, arc.Geometry.StartPoint.X, 12);
        Assert.Equal(0, arc.Geometry.StartPoint.Y, 12);
        Assert.Equal(10, arc.Geometry.EndPoint.X, 12);
        Assert.Equal(radius, arc.Geometry.EndPoint.Y, 12);
        Assert.All(context.Document.Entities.All.OfType<LineEntity>(), line => Assert.True(line.Geometry.Length > 0));
        context.CommandHistory.Undo(context.Document);
        Assert.Same(first, context.Document.Entities.GetRequired(first.Id));
        Assert.Same(second, context.Document.Entities.GetRequired(second.Id));
        context.CommandHistory.Redo(context.Document);
        Assert.Same(arc, Assert.Single(context.Document.Entities.All.OfType<ArcEntity>()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ClosedPolyline_EachCorner_ShouldKeepOneNativeArcOfRequestedRadius(int corner)
    {
        ToolContext context = CreateContext();
        Point2D[] vertices = { new(0, 0), new(10, 0), new(10, 10), new(0, 10) };
        var source = new PolylineEntity(vertices, isClosed: true);
        context.Document.AddEntity(source);
        FilletTool tool = WithRadius(context, 2);
        Point2D before = vertices[(corner + 3) % 4];
        Point2D common = vertices[corner];
        Point2D after = vertices[(corner + 1) % 4];
        tool.OnPointerPressed(context, new PointerInfo(new Point2D((before.X + common.X) / 2, (before.Y + common.Y) / 2)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D((after.X + common.X) / 2, (after.Y + common.Y) / 2)));

        PolylineEntity result = Assert.IsType<PolylineEntity>(Assert.Single(context.Document.Entities.All));
        Assert.Equal(5, result.Vertices.Count);
        int arcIndex = Assert.Single(Enumerable.Range(0, result.SegmentCount).Where(i => result.SegmentBulges[i] != 0));
        double chord = result.Vertices[arcIndex].DistanceTo(result.Vertices[(arcIndex + 1) % result.Vertices.Count]);
        double sweep = Math.Abs(4 * Math.Atan(result.SegmentBulges[arcIndex]));
        Assert.Equal(2, chord / (2 * Math.Sin(sweep / 2)), 12);
        context.CommandHistory.Undo(context.Document);
        Assert.Same(source, Assert.Single(context.Document.Entities.All));
    }

    [Fact]
    public void TerminalIntersectionPick_OnReversedLine_ShouldChooseExistingRay()
    {
        ToolContext context = CreateContext();
        context.Document.AddEntity(new LineEntity(new Point2D(0, 0), new Point2D(10, 0)));
        context.Document.AddEntity(new LineEntity(new Point2D(10, 10), new Point2D(10, 0)));
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        ArcEntity arc = Assert.Single(context.Document.Entities.All.OfType<ArcEntity>());
        Assert.Equal(8, arc.Center.X, 12);
        Assert.Equal(2, arc.Center.Y, 12);
    }

    [Fact]
    public void InteriorIntersectionPick_ShouldRejectUnspecifiedSide()
    {
        ToolContext context = CreateContext();
        context.Document.AddEntity(new LineEntity(new Point2D(-10, 0), new Point2D(10, 0)));
        context.Document.AddEntity(new LineEntity(new Point2D(0, -10), new Point2D(0, 10)));
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(-5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(Point2D.Origin));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Contains("side to KEEP", result.Message);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstSource_WhenReplacedOrLayerLocked_ShouldClearSelectionAndPreview(bool locked)
    {
        ToolContext context = CreateLines(out LineEntity first, out _);
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));
        Assert.NotEmpty(tool.GetPreviewEntities());
        if (locked)
            context.Document.Layers.Replace(context.Document.Layers.GetRequired(LayerId.Default).WithLocked(true));
        else
            context.Document.ReplaceEntity(new LineEntity(new Point2D(-10, 0), first.End, id: first.Id));

        tool.OnPointerMoved(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Null(tool.FirstEntityId);
        Assert.Empty(tool.GetPreviewEntities());
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
        Assert.Equal(2, tool.Radius);
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void TrimMode_AfterFirstSelection_ShouldKeepSelectionAndAddOnlyArc()
    {
        ToolContext context = CreateLines(out LineEntity first, out LineEntity second);
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.HandleCommandInput(CommandInputSubmission.Option("T", "Trim"), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("N", "NoTrim"), context);
        Assert.Equal(first.Id, tool.FirstEntityId);
        Assert.Equal(FilletToolState.WaitingForSecondEntity, tool.State);

        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Same(first, context.Document.Entities.GetRequired(first.Id));
        Assert.Same(second, context.Document.Entities.GetRequired(second.Id));
        Assert.Single(context.Document.Entities.All.OfType<ArcEntity>());
    }

    [Fact]
    public void NoTrim_ExcessiveRadius_ShouldNotAddDisconnectedArc()
    {
        ToolContext context = CreateLines(out _, out _);
        FilletTool tool = WithRadius(context, 20);
        tool.HandleCommandInput(CommandInputSubmission.Option("T", "Trim"), context);
        tool.HandleCommandInput(CommandInputSubmission.Option("N", "NoTrim"), context);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(context.Document.Entities.All.OfType<ArcEntity>());
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(20.0)]
    public void Radius_ConsumesOrPassesRetainedEndpoint_ShouldRejectWithoutChangingSources(double radius)
    {
        ToolContext context = CreateLines(out LineEntity first, out LineEntity second);
        FilletTool tool = WithRadius(context, radius);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Contains("radius", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(first, context.Document.Entities.GetRequired(first.Id));
        Assert.Same(second, context.Document.Entities.GetRequired(second.Id));
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Fact]
    public void ClosedPolyline_ClosingCorner_ShouldInsertOneArcWithoutDuplicatingPath()
    {
        ToolContext context = CreateContext();
        var polyline = new PolylineEntity(new[]
        {
            new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 10), new Point2D(0, 10)
        }, isClosed: true, isFilled: true);
        context.Document.AddEntity(polyline);
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(0, 5)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity filleted = Assert.IsType<PolylineEntity>(Assert.Single(context.Document.Entities.All));
        Assert.Equal(5, filleted.Vertices.Count);
        Assert.Single(filleted.SegmentBulges.Where(bulge => bulge != 0));
        Assert.True(filleted.IsClosed);
        Assert.True(filleted.IsFilled);
    }

    [Fact]
    public void Polyline_SecondClickOnSameDistantSegment_ShouldNotChooseAnotherSegment()
    {
        ToolContext context = CreateContext();
        var polyline = new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(100, 0), new Point2D(100, 100) });
        context.Document.AddEntity(polyline);
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(context, new PointerInfo(new Point2D(20, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Same(polyline, Assert.Single(context.Document.Entities.All));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FirstSource_WhenRemovedOrHidden_ShouldResetSelectionWithoutUsingSnapshot(bool hide)
    {
        ToolContext context = CreateLines(out LineEntity first, out _);
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        if (hide)
            context.Document.ReplaceEntity(new LineEntity(first.Start, first.End, id: first.Id, isVisible: false));
        else
            context.Document.RemoveEntity(first.Id);

        Exception? exception = Record.Exception(() => tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5))));

        Assert.Null(exception);
        Assert.Equal(FilletToolState.WaitingForFirstEntityOrRadius, tool.State);
        Assert.Null(tool.FirstEntityId);
        Assert.Empty(context.Document.Entities.All.OfType<ArcEntity>());
        Assert.False(context.CommandHistory.CanUndo);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Radius_NonFiniteSubmission_ShouldRejectAndKeepRadiusPrompt(double radius)
    {
        ToolContext context = CreateContext();
        var tool = new FilletTool();
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);

        ToolResult result = tool.HandleCommandInput(CommandInputSubmission.FromNumber("invalid", radius), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(0, tool.Radius);
        Assert.Equal(FilletToolState.WaitingForRadius, tool.State);
    }

    [Fact]
    public void RadiusOption_AfterFirstSelection_ShouldKeepFirstObjectAndReturnToSecondPick()
    {
        ToolContext context = CreateLines(out LineEntity first, out _);
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        Assert.Equal(FilletToolState.WaitingForRadius, tool.State);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber("3", 3), context);

        Assert.Equal(3, tool.Radius);
        Assert.Equal(first.Id, tool.FirstEntityId);
        Assert.Equal(FilletToolState.WaitingForSecondEntity, tool.State);
    }

    [Fact]
    public void InvalidHover_ParallelLines_ShouldExplainFailureInPromptFeedback()
    {
        ToolContext context = CreateContext();
        context.Document.AddEntity(new LineEntity(new Point2D(0, 0), new Point2D(10, 0)));
        context.Document.AddEntity(new LineEntity(new Point2D(0, 5), new Point2D(10, 5)));
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));

        tool.OnPointerMoved(context, new PointerInfo(new Point2D(5, 5)));

        Assert.Empty(tool.GetPreviewEntities());
        Assert.Contains("parallel", tool.GetPromptState(context).Feedback, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Confirm_AtEntityPrompt_ShouldFinishToSelection()
    {
        ToolContext context = CreateContext();
        var controller = new ToolController(context, new FilletTool());

        controller.ConfirmActiveToolCommand();

        Assert.IsType<SelectionTool>(controller.ActiveTool);
    }

    [Fact]
    public void Polyline_RightAngle_ShouldKeepNativeTangencyAndBulgeConvention()
    {
        ToolContext context = CreateContext();
        context.Document.AddEntity(new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 10) }));
        FilletTool tool = WithRadius(context, 2);
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 5)));

        PolylineEntity result = Assert.IsType<PolylineEntity>(Assert.Single(context.Document.Entities.All));
        Point2D arcMidpoint = result.GetSegmentMidpoints()[1];
        Assert.Equal(8 + Math.Sqrt(2), arcMidpoint.X, 8);
        Assert.Equal(2 - Math.Sqrt(2), arcMidpoint.Y, 8);
    }

    private static ToolContext CreateContext() => new(new CadDocument(), new CommandHistory(), new SnapService(), selectionTolerance: 0.5);

    private static ToolContext CreateLines(out LineEntity first, out LineEntity second)
    {
        ToolContext context = CreateContext();
        first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        second = new LineEntity(new Point2D(10, 0), new Point2D(10, 10));
        context.Document.AddEntity(first);
        context.Document.AddEntity(second);
        return context;
    }

    private static FilletTool WithRadius(ToolContext context, double radius)
    {
        var tool = new FilletTool();
        tool.HandleCommandInput(CommandInputSubmission.Option("R", "Radius"), context);
        tool.HandleCommandInput(CommandInputSubmission.FromNumber(radius.ToString(System.Globalization.CultureInfo.InvariantCulture), radius), context);
        return tool;
    }
}
