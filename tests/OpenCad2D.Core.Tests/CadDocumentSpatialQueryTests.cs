using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.Core.Tests;

public sealed class CadDocumentSpatialQueryTests
{
    [Fact]
    public void GetVisibleEntities_WithArea_ShouldExcludeHiddenLayerEntities()
    {
        var document = new CadDocument();

        var hiddenLayer = new Layer(
            new LayerId("Hidden"),
            "Hidden",
            isVisible: false);

        document.Layers.Add(hiddenLayer);

        var visibleLine = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0));

        var hiddenLine = new LineEntity(
            new Point2D(0, 1),
            new Point2D(10, 1),
            layerId: hiddenLayer.Id);

        document.AddEntity(visibleLine);
        document.AddEntity(hiddenLine);

        IReadOnlyList<CadEntity> result = document
            .GetVisibleEntities(new BoundingBox2D(-1, -1, 11, 2))
            .ToList();

        Assert.Single(result);
        Assert.Equal(visibleLine.Id, result[0].Id);
    }
}