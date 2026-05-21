using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class BreakAtPointToolTests
{
    [Fact]
    public void Constructor_ShouldStartWaitingForTargetEntity()
    {
        var tool = new BreakAtPointTool();

        Assert.Equal("Break Point", tool.Name);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(tool.TargetEntityId);
        Assert.Null(tool.CurrentBreakPoint);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void FirstPointerPress_WithoutLine_ShouldNotStartTool()
    {
        var context = CreateContext();
        var tool = new BreakAtPointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(100, 100)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(tool.TargetEntityId);
    }

    [Fact]
    public void FirstPointerPress_OnLine_ShouldSelectTargetLine()
    {
        var context = CreateContextWithLine(out LineEntity line);
        var tool = new BreakAtPointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0.1)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(BreakAtPointToolState.WaitingForBreakPoint, tool.State);
        Assert.Equal(line.Id, tool.TargetEntityId);
        Assert.Equal(new Point2D(5, 0), context.CurrentBasePoint);
    }

    [Fact]
    public void PointerMove_AfterTargetLine_ShouldUpdatePreview()
    {
        var context = CreateContextWithLine(out _);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(4, 2)));

        Assert.Equal(ToolResultKind.Updated, result.Kind);
        Assert.True(tool.HasPreview);
        Assert.Equal(new Point2D(4, 0), tool.CurrentBreakPoint);

        IReadOnlyList<CadEntity> preview = tool.GetPreviewEntities();
        Assert.Equal(2, preview.Count);
    }

    [Fact]
    public void PointerMove_AtLineEndpoint_ShouldReturnGranularEndpointMessage()
    {
        var context = CreateContextWithLine(out _);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(
            "Break point is too close to an endpoint or intersection tolerance. Pick an interior point on the entity.",
            result.Message);
        Assert.False(tool.HasPreview);
    }

    [Fact]
    public void GetPreviewDescriptor_AfterTargetSelection_ShouldHighlightSelectedTarget()
    {
        var context = CreateContextWithLine(out LineEntity line);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolPreviewDescriptor descriptor = tool.GetPreviewDescriptor(context);
        ToolPreviewEntityOverlay overlay = Assert.Single(descriptor.EntityOverlays);

        Assert.Equal(ToolPreviewHighlightKind.Emphasis, overlay.Kind);
        Assert.Same(line, Assert.Single(overlay.Entities));
        Assert.Empty(descriptor.Markers);
    }

    [Fact]
    public void GetPreviewDescriptor_AfterPointerMove_ShouldShowBreakPointMarker()
    {
        var context = CreateContextWithLine(out LineEntity line);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerMoved(
            context,
            new PointerInfo(new Point2D(4, 2)));

        ToolPreviewDescriptor descriptor = tool.GetPreviewDescriptor(context);

        Assert.Equal(2, descriptor.Entities.Count);
        ToolPreviewMarker marker = Assert.Single(descriptor.Markers);
        Assert.Equal(new Point2D(4, 0), marker.Position);
        Assert.Equal(ToolPreviewMarkerKind.Hot, marker.Kind);
        Assert.Equal(ToolPreviewMarkerShape.Circle, marker.Shape);

        ToolPreviewEntityOverlay overlay = Assert.Single(descriptor.EntityOverlays);
        Assert.Same(line, Assert.Single(overlay.Entities));
    }

    [Fact]
    public void SecondPointerPress_InsideLine_ShouldBreakLineIntoTwoLines()
    {
        var context = CreateContextWithLine(out LineEntity originalLine);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(4, 2)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(context.CurrentBasePoint);
        Assert.False(context.Document.Entities.Contains(originalLine.Id));

        IReadOnlyList<LineEntity> lines = context.Document.Entities.All
            .OfType<LineEntity>()
            .OrderBy(line => line.Start.X)
            .ToList();

        Assert.Equal(2, lines.Count);
        Assert.Equal(new Point2D(0, 0), lines[0].Start);
        Assert.Equal(new Point2D(4, 0), lines[0].End);
        Assert.Equal(new Point2D(4, 0), lines[1].Start);
        Assert.Equal(new Point2D(10, 0), lines[1].End);
    }

    [Fact]
    public void SecondPointerPress_NearEndpoint_ShouldNotBreakLine()
    {
        var context = CreateContextWithLine(out LineEntity originalLine);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakAtPointToolState.WaitingForBreakPoint, tool.State);
        Assert.True(context.Document.Entities.Contains(originalLine.Id));
        Assert.Single(context.Document.Entities.All.OfType<LineEntity>());
    }

    [Fact]
    public void BreakAtPoint_ShouldBeUndoable()
    {
        var context = CreateContextWithLine(out LineEntity originalLine);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(4, 0)));

        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        LineEntity line = Assert.Single(context.Document.Entities.All.OfType<LineEntity>());

        Assert.Equal(originalLine.Id, line.Id);
        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 0), line.End);
    }

    [Fact]
    public void Cancel_ShouldResetTool()
    {
        var context = CreateContextWithLine(out _);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.Cancel(context);

        Assert.Equal(ToolResultKind.Cancelled, result.Kind);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
        Assert.Null(tool.TargetEntityId);
        Assert.Null(context.CurrentBasePoint);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 0.5);
    }

    private static ToolContext CreateContextWithLine(out LineEntity line)
    {
        var document = new CadDocument();
        var commandHistory = new CommandHistory();
        var context = new ToolContext(
            document,
            commandHistory,
            new SnapService(),
            selectionTolerance: 0.5);

        line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        return context;
    }
}

public sealed class BreakAtPointToolAdvancedTests
{
    [Fact]
    public void BreakAtPoint_WithArc_ShouldReplaceArcWithTwoArcs()
    {
        var context = CreateContextWithEntity(
            new ArcEntity(
                new Point2D(0, 0),
                10,
                Angle.FromDegrees(0),
                Angle.FromDegrees(180)),
            out CadEntity original);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 10)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(context.Document.Entities.Contains(original.Id));
        Assert.Equal(2, context.Document.Entities.All.OfType<ArcEntity>().Count());
    }

    [Fact]
    public void BreakAtPoint_WithOpenPolyline_ShouldReplacePolylineWithTwoPolylines()
    {
        var context = CreateContextWithEntity(
            new PolylineEntity(
                new[]
                {
                    new Point2D(0, 0),
                    new Point2D(10, 0),
                    new Point2D(10, 10)
                }),
            out CadEntity original);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(4, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(context.Document.Entities.Contains(original.Id));
        Assert.Equal(2, context.Document.Entities.All.OfType<PolylineEntity>().Count());
    }

    [Fact]
    public void BreakAtPoint_WithClosedPolyline_ShouldReplacePolylineWithOneOpenPolyline()
    {
        var context = CreateContextWithEntity(
            new PolylineEntity(
                new[]
                {
                    new Point2D(0, 0),
                    new Point2D(10, 0),
                    new Point2D(10, 10),
                    new Point2D(0, 10)
                },
                isClosed: true),
            out CadEntity original);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(4, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(context.Document.Entities.Contains(original.Id));

        PolylineEntity opened = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.False(opened.IsClosed);
    }

    [Fact]
    public void BreakAtPoint_WithFullEllipse_ShouldReturnClearNotApplicableMessage()
    {
        var context = CreateContextWithEntity(
            new EllipseEntity(
                new Point2D(0, 0),
                new Vector2D(10, 0),
                5),
            out _);
        var tool = new BreakAtPointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Contains("not applicable to full ellipses", result.Message!);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
    }

    [Fact]
    public void BreakAtPoint_WithEllipticalArc_ShouldAcceptTarget()
    {
        var context = CreateContextWithEntity(
            new EllipticalArcEntity(
                new Point2D(0, 0),
                new Vector2D(10, 0),
                5,
                0,
                Math.PI),
            out CadEntity original);
        var tool = new BreakAtPointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.Started, result.Kind);
        Assert.Equal(original.Id, tool.TargetEntityId);
        Assert.Equal(BreakAtPointToolState.WaitingForBreakPoint, tool.State);
    }

    [Fact]
    public void BreakAtPoint_WithCircle_ShouldReturnClearNotApplicableMessage()
    {
        var context = CreateContextWithEntity(
            new CircleEntity(
                new Point2D(0, 0),
                10),
            out _);
        var tool = new BreakAtPointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Contains("not applicable to circles", result.Message!);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
    }

    [Fact]
    public void BreakAtPoint_WithPolyline_ShouldBeUndoable()
    {
        var context = CreateContextWithEntity(
            new PolylineEntity(
                new[]
                {
                    new Point2D(0, 0),
                    new Point2D(10, 0),
                    new Point2D(10, 10)
                }),
            out CadEntity original);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(4, 0)));
        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.True(context.CommandHistory.CanUndo);

        context.CommandHistory.Undo(context.Document);

        PolylineEntity restored = Assert.Single(context.Document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(3, restored.Vertices.Count);
    }


    [Fact]
    public void BreakAtPoint_AtLineEndpoint_ShouldReturnGranularEndpointMessage()
    {
        var context = CreateContextWithEntity(
            new LineEntity(
                new Point2D(0, 0),
                new Point2D(10, 0)),
            out _);
        var tool = new BreakAtPointTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(0, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(
            "Break point is too close to an endpoint or intersection tolerance. Pick an interior point on the entity.",
            result.Message);
    }

    private static ToolContext CreateContextWithEntity(
        CadEntity entity,
        out CadEntity addedEntity)
    {
        var document = new CadDocument();
        var commandHistory = new CommandHistory();
        var context = new ToolContext(
            document,
            commandHistory,
            new SnapService(),
            selectionTolerance: 0.5);

        document.AddEntity(entity);
        addedEntity = entity;

        return context;
    }
}
