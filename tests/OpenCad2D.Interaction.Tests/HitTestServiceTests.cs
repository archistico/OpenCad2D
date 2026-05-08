using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.HitTesting;

namespace OpenCad2D.Interaction.Tests;

public sealed class HitTestServiceTests
{
    [Fact]
    public void HitTest_WhenPointIsNearLine_ShouldReturnLine()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new HitTestService();

        HitTestResult? result = service.HitTest(
            document,
            new Point2D(5, 0.5),
            tolerance: 1);

        Assert.NotNull(result);
        Assert.Equal(line.Id, result.Entity.Id);
    }

    [Fact]
    public void HitTest_WhenPointIsTooFar_ShouldReturnNull()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        document.AddEntity(line);

        var service = new HitTestService();

        HitTestResult? result = service.HitTest(
            document,
            new Point2D(5, 5),
            tolerance: 1);

        Assert.Null(result);
    }

    [Fact]
    public void HitTest_ShouldIgnoreInvisibleEntities()
    {
        var document = new CadDocument();

        var line = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            isVisible: false);

        document.AddEntity(line);

        var service = new HitTestService();

        HitTestResult? result = service.HitTest(
            document,
            new Point2D(5, 0),
            tolerance: 1);

        Assert.Null(result);
    }

    [Fact]
    public void HitTest_WhenMultipleEntitiesMatch_ShouldReturnNearestEntity()
    {
        var document = new CadDocument();

        var nearLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var farLine = new LineEntity(
            new Point2D(0, 3),
            new Point2D(10, 3));

        document.AddEntity(farLine);
        document.AddEntity(nearLine);

        var service = new HitTestService();

        HitTestResult? result = service.HitTest(
            document,
            new Point2D(5, 0.2),
            tolerance: 5);

        Assert.NotNull(result);
        Assert.Equal(nearLine.Id, result.Entity.Id);
    }

    [Fact]
    public void HitTest_WhenDistancesAreEqual_ShouldPreferHigherDrawOrder()
    {
        var document = new CadDocument();

        var lower = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            drawOrder: 1);

        var higher = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            drawOrder: 10);

        document.AddEntity(lower);
        document.AddEntity(higher);

        var service = new HitTestService();

        HitTestResult? result = service.HitTest(
            document,
            new Point2D(5, 0),
            tolerance: 1);

        Assert.NotNull(result);
        Assert.Equal(higher.Id, result.Entity.Id);
    }

    [Fact]
    public void HitTestAll_ShouldReturnAllMatchingEntities()
    {
        var document = new CadDocument();

        var first = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var second = new LineEntity(
            new Point2D(0, 0.5),
            new Point2D(10, 0.5));

        document.AddEntity(first);
        document.AddEntity(second);

        var service = new HitTestService();

        IReadOnlyList<HitTestResult> results = service.HitTestAll(
            document,
            new Point2D(5, 0),
            tolerance: 1);

        Assert.Equal(2, results.Count);
    }
}