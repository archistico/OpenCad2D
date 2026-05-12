using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class ModifyToolLayerRuleTests
{
    [Fact]
    public void BreakPoint_ShouldIgnoreHiddenTarget()
    {
        ToolContext context = CreateContext();
        LayerId hiddenLayerId = AddLayer(
            context.Document,
            "Hidden",
            isVisible: false,
            isLocked: false);

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: hiddenLayerId);

        context.Document.AddEntity(target);

        var tool = new BreakAtPointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
        Assert.True(context.Document.Entities.Contains(target.Id));
    }

    [Fact]
    public void BreakPoint_ShouldIgnoreLockedTarget()
    {
        ToolContext context = CreateContext();
        LayerId lockedLayerId = AddLayer(
            context.Document,
            "Locked",
            isVisible: true,
            isLocked: true);

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: lockedLayerId);

        context.Document.AddEntity(target);

        var tool = new BreakAtPointTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakAtPointToolState.WaitingForTargetEntity, tool.State);
        Assert.True(context.Document.Entities.Contains(target.Id));
    }

    [Fact]
    public void BreakSegment_ShouldIgnoreHiddenTarget()
    {
        ToolContext context = CreateContext();
        LayerId hiddenLayerId = AddLayer(
            context.Document,
            "Hidden",
            isVisible: false,
            isLocked: false);

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: hiddenLayerId);

        context.Document.AddEntity(target);

        var tool = new BreakBetweenPointsTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForTargetEntity, tool.State);
        Assert.True(context.Document.Entities.Contains(target.Id));
    }

    [Fact]
    public void BreakSegment_ShouldIgnoreLockedTarget()
    {
        ToolContext context = CreateContext();
        LayerId lockedLayerId = AddLayer(
            context.Document,
            "Locked",
            isVisible: true,
            isLocked: true);

        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: lockedLayerId);

        context.Document.AddEntity(target);

        var tool = new BreakBetweenPointsTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(BreakBetweenPointsToolState.WaitingForTargetEntity, tool.State);
        Assert.True(context.Document.Entities.Contains(target.Id));
    }

    [Fact]
    public void Trim_ShouldUseLockedBoundaryAsReference()
    {
        ToolContext context = CreateContext();
        LayerId lockedLayerId = AddLayer(
            context.Document,
            "LockedBoundary",
            isVisible: true,
            isLocked: true);

        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5),
            layerId: lockedLayerId);
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new TrimTool();

        ToolResult start = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));
        ToolResult complete = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(8, 0)));

        Assert.Equal(ToolResultKind.Started, start.Kind);
        Assert.Equal(boundary.Id, tool.BoundaryEntityId);
        Assert.Equal(ToolResultKind.Completed, complete.Kind);
        Assert.True(context.Document.Entities.Contains(boundary.Id));
        Assert.False(context.Document.Entities.Contains(target.Id));
    }

    [Fact]
    public void Trim_ShouldNotUseHiddenBoundaryAsReference()
    {
        ToolContext context = CreateContext();
        LayerId hiddenLayerId = AddLayer(
            context.Document,
            "HiddenBoundary",
            isVisible: false,
            isLocked: false);

        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5),
            layerId: hiddenLayerId);
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new TrimTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(TrimToolState.WaitingForBoundaryEntity, tool.State);
        Assert.True(context.Document.Entities.Contains(target.Id));
    }

    [Fact]
    public void Trim_ShouldNotModifyLockedTarget()
    {
        ToolContext context = CreateContext();
        LayerId lockedLayerId = AddLayer(
            context.Document,
            "LockedTarget",
            isVisible: true,
            isLocked: true);

        var boundary = new LineEntity(
            new Point2D(5, -5),
            new Point2D(5, 5));
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: lockedLayerId);

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new TrimTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 2)));
        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(8, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.True(context.Document.Entities.Contains(target.Id));
        Assert.Single(context.Document.Entities.All.OfType<LineEntity>(), line => line.Id.Equals(target.Id));
    }

    [Fact]
    public void Extend_ShouldUseLockedBoundaryAsReference()
    {
        ToolContext context = CreateContext();
        LayerId lockedLayerId = AddLayer(
            context.Document,
            "LockedBoundary",
            isVisible: true,
            isLocked: true);

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5),
            layerId: lockedLayerId);
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        ToolResult start = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));
        ToolResult complete = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.Started, start.Kind);
        Assert.Equal(boundary.Id, tool.BoundaryEntityId);
        Assert.Equal(ToolResultKind.Completed, complete.Kind);

        LineEntity extended = Assert.IsType<LineEntity>(
            context.Document.Entities.GetRequired(target.Id));

        Assert.Equal(new Point2D(10, 0), extended.End);
        Assert.True(context.Document.Entities.Contains(boundary.Id));
    }

    [Fact]
    public void Extend_ShouldNotUseHiddenBoundaryAsReference()
    {
        ToolContext context = CreateContext();
        LayerId hiddenLayerId = AddLayer(
            context.Document,
            "HiddenBoundary",
            isVisible: false,
            isLocked: false);

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5),
            layerId: hiddenLayerId);
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0));

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Equal(ExtendToolState.WaitingForBoundaryEntity, tool.State);
        Assert.Equal(new Point2D(5, 0), target.End);
    }

    [Fact]
    public void Extend_ShouldNotModifyLockedTarget()
    {
        ToolContext context = CreateContext();
        LayerId lockedLayerId = AddLayer(
            context.Document,
            "LockedTarget",
            isVisible: true,
            isLocked: true);

        var boundary = new LineEntity(
            new Point2D(10, -5),
            new Point2D(10, 5));
        var target = new LineEntity(
            new Point2D(0, 0),
            new Point2D(5, 0),
            layerId: lockedLayerId);

        context.Document.AddEntity(boundary);
        context.Document.AddEntity(target);

        var tool = new ExtendTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 2)));
        ToolResult result = tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(5, 0)));

        Assert.Equal(ToolResultKind.None, result.Kind);

        LineEntity unchanged = Assert.IsType<LineEntity>(
            context.Document.Entities.GetRequired(target.Id));

        Assert.Equal(new Point2D(5, 0), unchanged.End);
    }

    private static ToolContext CreateContext()
    {
        return new ToolContext(
            new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionTolerance: 0.5);
    }

    private static LayerId AddLayer(
        CadDocument document,
        string name,
        bool isVisible,
        bool isLocked)
    {
        LayerId layerId = new(name);

        document.Layers.Add(
            new Layer(
                layerId,
                name,
                color: null,
                lineWeight: null,
                isVisible: isVisible,
                isLocked: isLocked));

        return layerId;
    }
}
