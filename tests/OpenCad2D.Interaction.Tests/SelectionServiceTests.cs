using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;

namespace OpenCad2D.Interaction.Tests;

public sealed class SelectionServiceTests
{
    [Fact]
    public void SelectByPoint_WhenPointHitsEntity_ShouldReturnEntityId()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SelectionService();

        EntityId? result = service.SelectByPoint(
            document,
            new Point2D(5, 0.5),
            tolerance: 1);

        Assert.Equal(line.Id, result);
    }

    [Fact]
    public void SelectByPoint_WhenPointHitsNothing_ShouldReturnNull()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new SelectionService();

        EntityId? result = service.SelectByPoint(
            document,
            new Point2D(5, 5),
            tolerance: 1);

        Assert.Null(result);
    }

    [Fact]
    public void SelectByWindow_Inside_ShouldReturnOnlyEntitiesCompletelyInsideWindow()
    {
        var document = new CadDocument();

        var inside = new LineEntity(
            new Point2D(2, 2),
            new Point2D(8, 2));

        var crossing = new LineEntity(
            new Point2D(-5, 5),
            new Point2D(5, 5));

        var outside = new LineEntity(
            new Point2D(20, 20),
            new Point2D(30, 20));

        document.AddEntity(inside);
        document.AddEntity(crossing);
        document.AddEntity(outside);

        var service = new SelectionService();

        IReadOnlyList<EntityId> result = service.SelectByWindow(
            document,
            new BoundingBox2D(0, 0, 10, 10),
            WindowSelectionMode.Inside);

        Assert.Single(result);
        Assert.Contains(inside.Id, result);
    }

    [Fact]
    public void SelectByWindow_Crossing_ShouldReturnEntitiesInsideOrIntersectingWindow()
    {
        var document = new CadDocument();

        var inside = new LineEntity(
            new Point2D(2, 2),
            new Point2D(8, 2));

        var crossing = new LineEntity(
            new Point2D(-5, 5),
            new Point2D(5, 5));

        var outside = new LineEntity(
            new Point2D(20, 20),
            new Point2D(30, 20));

        document.AddEntity(inside);
        document.AddEntity(crossing);
        document.AddEntity(outside);

        var service = new SelectionService();

        IReadOnlyList<EntityId> result = service.SelectByWindow(
            document,
            new BoundingBox2D(0, 0, 10, 10),
            WindowSelectionMode.Crossing);

        Assert.Equal(2, result.Count);
        Assert.Contains(inside.Id, result);
        Assert.Contains(crossing.Id, result);
        Assert.DoesNotContain(outside.Id, result);
    }

    [Fact]
    public void SelectByWindow_ShouldIgnoreInvisibleEntities()
    {
        var document = new CadDocument();

        var invisible = new LineEntity(
            new Point2D(2, 2),
            new Point2D(8, 2),
            isVisible: false);

        document.AddEntity(invisible);

        var service = new SelectionService();

        IReadOnlyList<EntityId> result = service.SelectByWindow(
            document,
            new BoundingBox2D(0, 0, 10, 10),
            WindowSelectionMode.Crossing);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectByWindow_Inside_WithCircleFullyInside_ShouldSelectCircle()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(5, 5),
            2);

        document.AddEntity(circle);

        var service = new SelectionService();

        IReadOnlyList<EntityId> result = service.SelectByWindow(
            document,
            new BoundingBox2D(0, 0, 10, 10),
            WindowSelectionMode.Inside);

        Assert.Single(result);
        Assert.Contains(circle.Id, result);
    }

    [Fact]
    public void SelectByWindow_Inside_WithCirclePartiallyOutside_ShouldNotSelectCircle()
    {
        var document = new CadDocument();

        var circle = new CircleEntity(
            new Point2D(9, 5),
            2);

        document.AddEntity(circle);

        var service = new SelectionService();

        IReadOnlyList<EntityId> result = service.SelectByWindow(
            document,
            new BoundingBox2D(0, 0, 10, 10),
            WindowSelectionMode.Inside);

        Assert.Empty(result);
    }
}