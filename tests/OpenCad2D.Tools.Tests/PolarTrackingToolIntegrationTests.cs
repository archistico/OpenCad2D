using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Common;
using OpenCad2D.Tools.Drawing;
using OpenCad2D.Tools.Editing;

namespace OpenCad2D.Tools.Tests;

public sealed class PolarTrackingToolIntegrationTests
{
    [Fact]
    public void TwoPointToolPreview_WhenPolarTrackingIsEnabled_ShouldUseConstrainedCurrentPoint()
    {
        var context = CreateContext(
            angleConstraintSettings: AngleConstraintSettings.FromStep(45));

        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        tool.OnPointerMoved(
            context,
            new PointerInfo(PointFromPolar(10, 38)));

        LineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        AssertPointNear(PointFromPolar(10, 45), preview.End);
        AssertPointNear(PointFromPolar(10, 45), tool.CurrentPoint!.Value);
    }

    [Fact]
    public void TwoPointToolSecondPoint_WhenPolarTrackingIsEnabled_ShouldCreateConstrainedLine()
    {
        var context = CreateContext(
            angleConstraintSettings: AngleConstraintSettings.FromStep(45));

        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        tool.OnPointerPressed(
            context,
            new PointerInfo(PointFromPolar(10, 38)));

        LineEntity line = Assert.Single(
            context.Document.Entities.All.OfType<LineEntity>());

        AssertPointNear(Point2D.Origin, line.Start);
        AssertPointNear(PointFromPolar(10, 45), line.End);
    }

    [Fact]
    public void TwoPointToolSecondPoint_ShouldApplySnapBeforePolarTracking()
    {
        var document = new CadDocument();

        var snapSource = new LineEntity(
            new Point2D(10, 10),
            new Point2D(20, 20));

        document.AddEntity(snapSource);

        var context = CreateContext(
            document,
            enabledSnaps: SnapKind.Endpoint,
            snapTolerance: 5,
            angleConstraintSettings: AngleConstraintSettings.FromStep(90));

        var tool = new LineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(11, 9)));

        LineEntity createdLine = context.Document.Entities.All
            .OfType<LineEntity>()
            .Single(entity => entity.Id != snapSource.Id);

        AssertPointNear(Point2D.Origin, createdLine.Start);
        AssertPointNear(new Point2D(0, Math.Sqrt(200)), createdLine.End);
    }

    [Fact]
    public void MoveToolPreview_WhenPolarTrackingIsEnabled_ShouldMovePreviewAlongConstrainedDirection()
    {
        var document = new CadDocument();
        var selection = new SelectionSet();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(
            document,
            selection,
            angleConstraintSettings: AngleConstraintSettings.FromStep(45));

        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        tool.OnPointerMoved(
            context,
            new PointerInfo(PointFromPolar(10, 38)));

        LineEntity preview = Assert.Single(
            tool.GetPreviewEntities(context).OfType<LineEntity>());

        Point2D expectedDisplacement = PointFromPolar(10, 45);

        AssertPointNear(expectedDisplacement, preview.Start);
        AssertPointNear(
            new Point2D(10 + expectedDisplacement.X, expectedDisplacement.Y),
            preview.End);
    }

    [Fact]
    public void MoveToolDestination_WhenPolarTrackingIsEnabled_ShouldMoveSelectionAlongConstrainedDirection()
    {
        var document = new CadDocument();
        var selection = new SelectionSet();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);
        selection.Select(line.Id);

        var context = CreateContext(
            document,
            selection,
            angleConstraintSettings: AngleConstraintSettings.FromStep(45));

        var tool = new MoveTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        tool.OnPointerPressed(
            context,
            new PointerInfo(PointFromPolar(10, 38)));

        LineEntity moved = Assert.Single(
            context.Document.Entities.All.OfType<LineEntity>());

        Point2D expectedDisplacement = PointFromPolar(10, 45);

        AssertPointNear(expectedDisplacement, moved.Start);
        AssertPointNear(
            new Point2D(10 + expectedDisplacement.X, expectedDisplacement.Y),
            moved.End);
    }


    [Fact]
    public void PolylineToolPreview_WhenPolarTrackingIsEnabled_ShouldConstrainFromLastVertex()
    {
        var context = CreateContext(
            angleConstraintSettings: AngleConstraintSettings.FromStep(45));

        var tool = new PolylineTool();

        tool.OnPointerPressed(
            context,
            new PointerInfo(Point2D.Origin));

        tool.OnPointerPressed(
            context,
            new PointerInfo(new Point2D(10, 0)));

        tool.OnPointerMoved(
            context,
            new PointerInfo(Add(new Point2D(10, 0), PointFromPolar(10, 38))));

        Point2D expected = Add(new Point2D(10, 0), PointFromPolar(10, 45));

        AssertPointNear(expected, tool.CurrentPoint!.Value);

        PolylineEntity? preview = tool.GetPreviewEntity();

        Assert.NotNull(preview);
        AssertPointNear(expected, preview.Vertices[^1]);
    }

    private static ToolContext CreateContext(
        CadDocument? document = null,
        SelectionSet? selectionSet = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0,
        AngleConstraintSettings? angleConstraintSettings = null)
    {
        return new ToolContext(
            document ?? new CadDocument(),
            new CommandHistory(),
            new SnapService(),
            selectionSet: selectionSet,
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance,
            angleConstraintSettings: angleConstraintSettings);
    }

    private static Point2D Add(
        Point2D point,
        Point2D vector)
    {
        return new Point2D(
            point.X + vector.X,
            point.Y + vector.Y);
    }

    private static Point2D PointFromPolar(
        double distance,
        double angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;

        return new Point2D(
            Math.Cos(radians) * distance,
            Math.Sin(radians) * distance);
    }

    private static void AssertPointNear(
        Point2D expected,
        Point2D actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 10);
        Assert.Equal(expected.Y, actual.Y, precision: 10);
    }
}
