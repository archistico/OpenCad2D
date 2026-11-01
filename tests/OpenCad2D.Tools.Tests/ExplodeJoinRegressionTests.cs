using OpenCad2D.Core.Blocks;
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

namespace OpenCad2D.Tools.Tests;

public sealed class ExplodeJoinRegressionTests
{
    private static readonly LayerId LockedLayerId = new("Locked reference");
    private static readonly LayerId HiddenLayerId = new("Hidden reference");

    [Fact]
    public void Explode_WithDeletedEntityStillSelected_ShouldIgnoreStaleId()
    {
        CadDocument document = new();
        PolylineEntity polyline = AddLShapePolyline(document);
        PolylineEntity removed = AddLShapePolyline(document, offsetY: 20);
        ToolContext context = CreateContext(document, polyline.Id, removed.Id);
        document.RemoveEntity(removed.Id);

        ToolResult result = Confirm(new ExplodeTool(), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.OfType<LineEntity>().Count());
    }

    [Fact]
    public void Explode_WithPolylineOnLockedLayer_ShouldSkipItAndExplodeEditableOnes()
    {
        CadDocument document = new();
        PolylineEntity editable = AddLShapePolyline(document);
        PolylineEntity locked = AddLShapePolyline(document, offsetY: 20, layerId: AddLayer(document, LockedLayerId));
        document.Layers.SetLocked(LockedLayerId, true);
        ToolContext context = CreateContext(document, editable.Id, locked.Id);

        ToolResult result = Confirm(new ExplodeTool(), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Contains("skipped", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(locked, document.Entities.GetRequired(locked.Id));
        Assert.False(document.Entities.Contains(editable.Id));
        Assert.Equal(1, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Explode_WithPolylineOnHiddenLayer_ShouldLeaveItUntouched()
    {
        CadDocument document = new();
        PolylineEntity hidden = AddLShapePolyline(document, layerId: AddLayer(document, HiddenLayerId));
        document.Layers.SetVisibility(HiddenLayerId, false);
        ToolContext context = CreateContext(document, hidden.Id);

        ToolResult result = Confirm(new ExplodeTool(), context);

        Assert.NotEqual(ToolResultKind.Completed, result.Kind);
        Assert.Same(hidden, document.Entities.GetRequired(hidden.Id));
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Explode_PolylineWithRepeatedVertices_ShouldNotCreateZeroLengthEntities()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 0),
                new Point2D(10, 5),
                new Point2D(0, 0)
            },
            isClosed: true,
            segmentBulges: new[] { 0.0, 0.5, 0.0, 0.0, 0.0 });
        document.AddEntity(polyline);
        ToolContext context = CreateContext(document, polyline.Id);

        ToolResult result = Confirm(new ExplodeTool(), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(3, document.Entities.Count);
        Assert.All(document.Entities.All.OfType<LineEntity>(), line => Assert.True(line.Start.DistanceTo(line.End) > 1e-6));
        Assert.Empty(document.Entities.All.OfType<ArcEntity>());
    }

    [Fact]
    public void Explode_BlockWithEntityOnMissingLayer_ShouldUseBlockReferenceLayerAndStayUndoable()
    {
        CadDocument document = new();
        LayerId referenceLayer = AddLayer(document, new LayerId("Furniture"));
        var definition = new BlockDefinition(
            BlockDefinitionId.New(),
            "Chair",
            new CadEntity[]
            {
                new LineEntity(new Point2D(0, 0), new Point2D(1, 0)),
                new LineEntity(new Point2D(0, 0), new Point2D(0, 1), layerId: new LayerId("Deleted layer"))
            });
        document.BlockDefinitions.Add(definition);
        var reference = new BlockReferenceEntity(
            definition.Id,
            new Point2D(5, 5),
            new Vector2D(1, 0),
            new Vector2D(0, 1),
            definition.GetBoundingBox(),
            layerId: referenceLayer);
        document.AddEntity(reference);
        ToolContext context = CreateContext(document, reference.Id);

        ToolResult result = Confirm(new ExplodeTool(), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.False(document.Entities.Contains(reference.Id));
        LineEntity[] lines = document.Entities.All.OfType<LineEntity>().ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Contains(lines, line => line.LayerId.Equals(LayerId.Default));
        Assert.Contains(lines, line => line.LayerId.Equals(referenceLayer));

        context.CommandHistory.Undo(document);

        Assert.Same(reference, Assert.Single(document.Entities.All));
    }

    [Fact]
    public void Explode_ClickOnAnotherPolylineWithPreselection_ShouldAddItInsteadOfExploding()
    {
        CadDocument document = new();
        PolylineEntity first = AddLShapePolyline(document);
        PolylineEntity second = AddLShapePolyline(document, offsetY: 20);
        ToolContext context = CreateContext(document, first.Id);
        var tool = new ExplodeTool();

        ToolResult click = tool.OnPointerPressed(context, new PointerInfo(new Point2D(5, 20)));

        Assert.NotEqual(ToolResultKind.Completed, click.Kind);
        Assert.Equal(2, context.Selection.SelectedIds.Count);
        Assert.Equal(0, context.CommandHistory.UndoCount);

        ToolResult result = Confirm(tool, context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Equal(4, document.Entities.All.OfType<LineEntity>().Count());
    }

    [Fact]
    public void Join_WithDeletedEntityStillSelected_ShouldIgnoreStaleId()
    {
        CadDocument document = new();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 5));
        var removed = new LineEntity(new Point2D(50, 0), new Point2D(60, 0));
        document.AddEntity(first);
        document.AddEntity(second);
        document.AddEntity(removed);
        ToolContext context = CreateContext(document, first.Id, second.Id, removed.Id);
        document.RemoveEntity(removed.Id);

        ToolResult result = Confirm(new JoinTool(), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Single(document.Entities.All.OfType<PolylineEntity>());
    }

    [Fact]
    public void Join_WithEntitiesOnLockedLayer_ShouldNotThrowOrModifyThem()
    {
        CadDocument document = new();
        LayerId layer = AddLayer(document, LockedLayerId);
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0), layerId: layer);
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 5), layerId: layer);
        document.AddEntity(first);
        document.AddEntity(second);
        document.Layers.SetLocked(LockedLayerId, true);
        ToolContext context = CreateContext(document, first.Id, second.Id);

        ToolResult result = Confirm(new JoinTool(), context);

        Assert.NotEqual(ToolResultKind.Completed, result.Kind);
        Assert.Same(first, document.Entities.GetRequired(first.Id));
        Assert.Same(second, document.Entities.GetRequired(second.Id));
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Join_WithEntitiesOnHiddenLayer_ShouldNotJoinThem()
    {
        CadDocument document = new();
        LayerId layer = AddLayer(document, HiddenLayerId);
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0), layerId: layer);
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 5), layerId: layer);
        document.AddEntity(first);
        document.AddEntity(second);
        document.Layers.SetVisibility(HiddenLayerId, false);
        ToolContext context = CreateContext(document, first.Id, second.Id);

        ToolResult result = Confirm(new JoinTool(), context);

        Assert.NotEqual(ToolResultKind.Completed, result.Kind);
        Assert.Equal(2, document.Entities.All.OfType<LineEntity>().Count());
    }

    [Fact]
    public void Join_WithZeroLengthLineAtJunction_ShouldNotReportBranchingAndShouldIgnoreIt()
    {
        CadDocument document = new();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 5));
        var degenerate = new LineEntity(new Point2D(10, 0), new Point2D(10, 0));
        document.AddEntity(first);
        document.AddEntity(second);
        document.AddEntity(degenerate);
        ToolContext context = CreateContext(document, first.Id, second.Id, degenerate.Id);

        ToolResult result = Confirm(new JoinTool(), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity joined = Assert.Single(document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(3, joined.Vertices.Count);
    }

    [Fact]
    public void Join_PolylineWithRepeatedVertex_ShouldNotReportBranchingOrKeepDuplicateVertex()
    {
        CadDocument document = new();
        var polyline = new PolylineEntity(new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 0) });
        var line = new LineEntity(new Point2D(10, 0), new Point2D(10, 5));
        document.AddEntity(polyline);
        document.AddEntity(line);
        ToolContext context = CreateContext(document, polyline.Id, line.Id);

        ToolResult result = Confirm(new JoinTool(), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity joined = Assert.Single(document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(
            new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 5) },
            joined.Vertices);
    }

    [Fact]
    public void Join_TwoOverlappingDuplicateLines_ShouldNotCreateDegenerateClosedPolyline()
    {
        CadDocument document = new();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(0, 0));
        document.AddEntity(first);
        document.AddEntity(second);
        ToolContext context = CreateContext(document, first.Id, second.Id);

        ToolResult result = Confirm(new JoinTool(), context);

        Assert.Equal(ToolResultKind.None, result.Kind);
        Assert.Empty(document.Entities.All.OfType<PolylineEntity>());
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void Join_LineAndArcClosingADShape_ShouldStillCreateClosedMixedPolyline()
    {
        CadDocument document = new();
        var line = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var arc = new ArcEntity(new Point2D(5, 0), 5, Angle.FromDegrees(0), Angle.FromDegrees(180));
        document.AddEntity(line);
        document.AddEntity(arc);
        ToolContext context = CreateContext(document, line.Id, arc.Id);

        ToolResult result = Confirm(new JoinTool(), context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        PolylineEntity joined = Assert.Single(document.Entities.All.OfType<PolylineEntity>());
        Assert.True(joined.IsClosed);
        Assert.True(joined.HasArcSegments);
    }

    [Fact]
    public void Join_ClickOnAnotherLineWithPreselection_ShouldAddItInsteadOfJoining()
    {
        CadDocument document = new();
        var first = new LineEntity(new Point2D(0, 0), new Point2D(10, 0));
        var second = new LineEntity(new Point2D(10, 0), new Point2D(10, 5));
        document.AddEntity(first);
        document.AddEntity(second);
        ToolContext context = CreateContext(document, first.Id);
        var tool = new JoinTool();

        ToolResult click = tool.OnPointerPressed(context, new PointerInfo(new Point2D(10, 3)));

        Assert.NotEqual(ToolResultKind.Completed, click.Kind);
        Assert.Equal(2, context.Selection.SelectedIds.Count);

        ToolResult result = Confirm(tool, context);

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.Single(document.Entities.All.OfType<PolylineEntity>());
    }

    [Theory]
    [InlineData("Explode")]
    [InlineData("Join")]
    public void ConfirmWithoutSelection_ShouldFinishCommand(string toolName)
    {
        CadDocument document = new();
        AddLShapePolyline(document);
        ToolContext context = CreateContext(document);
        ICadTool tool = toolName == "Explode" ? new ExplodeTool() : new JoinTool();
        var controller = new ToolController(context, tool);

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsType<OpenCad2D.Tools.Selection.SelectionTool>(controller.ActiveTool);
        Assert.Equal(0, context.CommandHistory.UndoCount);
    }

    [Fact]
    public void ConfirmAfterSuccessfulExplode_ShouldFinishCommand()
    {
        CadDocument document = new();
        PolylineEntity polyline = AddLShapePolyline(document);
        ToolContext context = CreateContext(document, polyline.Id);
        var controller = new ToolController(context, new ExplodeTool());

        ToolResult result = controller.ConfirmActiveToolCommand();

        Assert.Equal(ToolResultKind.Completed, result.Kind);
        Assert.IsType<OpenCad2D.Tools.Selection.SelectionTool>(controller.ActiveTool);
        Assert.Equal(1, context.CommandHistory.UndoCount);
    }

    private static ToolResult Confirm(ICadTool tool, ToolContext context)
    {
        return ((ICommandDrivenTool)tool).HandleCommandInput(
            CommandInputSubmission.Confirm(string.Empty),
            context);
    }

    private static PolylineEntity AddLShapePolyline(
        CadDocument document,
        double offsetY = 0,
        LayerId? layerId = null)
    {
        var polyline = new PolylineEntity(
            new[]
            {
                new Point2D(0, offsetY),
                new Point2D(10, offsetY),
                new Point2D(10, offsetY + 5)
            },
            layerId: layerId);
        document.AddEntity(polyline);
        return polyline;
    }

    private static LayerId AddLayer(CadDocument document, LayerId layerId)
    {
        document.Layers.Add(new Layer(layerId, layerId.Value));
        return layerId;
    }

    private static ToolContext CreateContext(CadDocument document, params EntityId[] selectedIds)
    {
        var context = new ToolContext(
            document,
            new CommandHistory(),
            new SnapService(),
            enabledSnaps: SnapKind.None,
            selectionTolerance: 1);

        foreach (EntityId id in selectedIds)
        {
            context.Selection.Set.Select(id);
        }

        return context;
    }
}
