using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class ExtendToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForBoundaryEntity()
    {
        var tool = new ExtendTool();

        Assert.Equal("Extend", tool.Name);
        Assert.Equal(ExtendToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void FirstPointerPress_WithoutLine_ShouldNotStartTool()
    {
        var context = CreateContext();
        var tool = new ExtendTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
    }

    [Fact]
    public void FirstPointerPress_OnLine_ShouldSelectBoundaryLine()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out LineEntity boundary,
            out _);
        var tool = new ExtendTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForTargetEntity, tool.State);
        Assert.Equal(boundary.Id, tool.BoundaryEntityId);
        Assert.NotNull(context.CurrentBasePoint);
    }

    [Fact]
    public void PointerMove_AfterBoundary_ShouldUpdatePreview()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        LineEntity line = Assert.IsType<LineEntity>(Assert.Single(preview));

        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }


    [Fact]
    public void PointerMove_AfterBoundary_ShouldExposeHighlightedExtensionSegment()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 0)));

        IReadOnlyList<CadEntity> highlighted = tool.GetHighlightedPreviewEntities();
        LineEntity extension = Assert.IsType<LineEntity>(Assert.Single(highlighted));

        Assert.Equal(new Point2D(5, 0), extension.Start);
        Assert.Equal(new Point2D(10, 0), extension.End);
    }


    [Fact]
    public void GetPreviewDescriptor_AfterBoundary_ShouldMarkExtensionSegmentAsAddition()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolPreviewDescriptor descriptor = tool.GetPreviewDescriptor(context);

        Assert.Equal(ToolPreviewHighlightKind.Addition, descriptor.HighlightedEntityKind);
        LineEntity extension = Assert.IsType<LineEntity>(Assert.Single(descriptor.HighlightedEntities));
        Assert.Equal(new Point2D(5, 0), extension.Start);
        Assert.Equal(new Point2D(10, 0), extension.End);
    }

    [Fact]
    public void SecondPointerPress_NearEnd_ShouldExtendLineToBoundary()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out LineEntity target);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForTargetEntity, tool.State);
        Assert.True(context.Document.Entities.Contains(target.Id));

        LineEntity extended = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), extended.Start);
        Assert.Equal(new Point2D(10, 0), extended.End);
    }

    [Fact]
    public void SecondPointerPress_NearStart_ShouldExtendStartToBoundary()
    {
        var context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(-5, -5),
            new Point2D(-5, 5));

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(-5, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);

        LineEntity extended = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(-5, 0), extended.Start);
        Assert.Equal(new Point2D(5, 0), extended.End);
    }

    [Fact]
    public void SecondPointerPress_WhenIntersectionIsInsideTargetSegment_ShouldNotExtend()
    {
        var context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);

        LineEntity unchanged = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), unchanged.Start);
        Assert.Equal(new Point2D(10, 0), unchanged.End);
    }

    [Fact]
    public void Extend_ShouldBeUndoable()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out LineEntity target);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        LineEntity restored = (LineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), restored.Start);
        Assert.Equal(new Point2D(5, 0), restored.End);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out _,
            out _);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Null(tool.BoundaryEntityId);
        Assert.Null(context.CurrentBasePoint);
    }


    [Fact]
    public void PointerMove_AfterBoundary_OnArcTarget_ShouldExposeHighlightedExtensionArc()
    {
        ToolContext context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(-10, 0),
            new Point2D(10, 0));
        var target = new ArcEntity(
            new Point2D(0, 0),
            5,
            Angle.FromDegrees(0),
            Angle.FromDegrees(90));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(-5, 0)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(target.Geometry.EndPoint));

        IReadOnlyList<CadEntity> highlighted = tool.GetHighlightedPreviewEntities();
        ArcEntity arc = Assert.IsType<ArcEntity>(Assert.Single(highlighted));

        Assert.Equal(target.EndAngle, arc.StartAngle);
        Assert.True(arc.EndAngle.NormalizePositive().Degrees > 170);
    }

    [Fact]
    public void PointerMove_AfterBoundary_OnPolylineTarget_ShouldExposeHighlightedExtensionLine()
    {
        ToolContext context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0)
        });

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 0)));

        IReadOnlyList<CadEntity> highlighted = tool.GetHighlightedPreviewEntities();
        LineEntity extension = Assert.IsType<LineEntity>(Assert.Single(highlighted));

        Assert.Equal(new Point2D(5, 0), extension.Start);
        Assert.Equal(new Point2D(10, 0), extension.End);
    }


    [Fact]
    public void FirstPointerPress_OnEllipseBoundary_ShouldSelectBoundaryEllipse()
    {
        ToolContext context = CreateContext();
        var boundary = new EllipseEntity(
            new Point2D(0, 0),
            new Vector2D(10, 0),
            5);

        context.Document.AddEntity(boundary);
        var tool = new ExtendTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForTargetEntity, tool.State);
        Assert.Equal(boundary.Id, tool.BoundaryEntityId);
        Assert.Equal(
            "Select the endpoint side to extend. Highlighted preview shows the portion that will be added.",
            result.Message);
    }

    [Fact]
    public void PointerMove_WithPreview_ShouldExplainHighlightedAddition()
    {
        ToolContext context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.Equal(
            "Extend preview updated. Highlighted portion will be added.",
            result.Message);
    }

    [Fact]
    public void SecondPointerPress_OnClosedPolylineTarget_ShouldReturnClearMessage()
    {
        ToolContext context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0),
            new Point2D(5, 5)
        }, isClosed: true);

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(
            "Extend supports lines, arcs, elliptical arcs and open polylines as targets. Closed curves cannot be extended.",
            result.Message);
    }

    [Fact]
    public void SecondPointerPress_OnCircleTarget_ShouldReturnClearMessage()
    {
        ToolContext context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));
        var target = new CircleEntity(new Point2D(0, 0), 5);

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(
            "Extend supports lines, arcs, elliptical arcs and open polylines as targets. Closed curves cannot be extended.",
            result.Message);
    }

    [Fact]
    public void ExtendOpenPolyline_ShouldBeUndoable()
    {
        ToolContext context = CreateContext();

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));
        var target = new PolylineEntity(new[]
        {
            new Point2D(0, 0),
            new Point2D(5, 0)
        });

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        context.CommandHistory.Undo(context.Document);

        PolylineEntity restored =
            (PolylineEntity)context.Document.Entities.GetRequired(target.Id);

        Assert.Equal(new Point2D(0, 0), restored.Vertices[0]);
        Assert.Equal(new Point2D(5, 0), restored.Vertices[^1]);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 0.5);
    }


    [Fact]
    public void GetPreviewDescriptor_AfterBoundarySelection_ShouldHighlightSelectedBoundary()
    {
        var context = CreateContextWithBoundaryAndTarget(
            out LineEntity boundary,
            out _);
        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        ToolPreviewDescriptor descriptor = tool.GetPreviewDescriptor(context);
        ToolPreviewEntityOverlay overlay = Assert.Single(descriptor.EntityOverlays);

        Assert.Equal(ToolPreviewHighlightKind.Emphasis, overlay.Kind);
        Assert.Same(boundary, Assert.Single(overlay.Entities));
    }

    private static ToolContext CreateContextWithBoundaryAndTarget(
        out LineEntity boundary,
        out LineEntity target)
    {
        ToolContext context = CreateContext();

        boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));

        target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        return context;
    }
}
