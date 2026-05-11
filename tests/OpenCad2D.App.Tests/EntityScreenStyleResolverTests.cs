using OpenCad2D.App.Rendering;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class EntityScreenStyleResolverTests
{
    [Fact]
    public void Resolve_ShouldUseLayerLineWeightOnly_WhenEntityHasOwnLineWeight()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Walls");

        document.Layers.Add(new Layer(
            layerId,
            "Walls",
            CadColor.FromRgb(200, 200, 200),
            LineWeight.FromMillimeters(3.5)));

        var entity = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId,
            style: new EntityStyle
            {
                LineWeight = LineWeight.FromMillimeters(99)
            });

        EntityScreenStyle style = EntityScreenStyleResolver.Resolve(
            document,
            entity,
            isSelected: false);

        Assert.Equal(3.5, style.LineWeight);
    }

    [Fact]
    public void Resolve_ShouldKeepLayerLineWeight_WhenEntityIsSelected()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Furniture");

        document.Layers.Add(new Layer(
            layerId,
            "Furniture",
            CadColor.FromRgb(120, 80, 40),
            LineWeight.FromMillimeters(2.25)));

        var entity = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId,
            style: new EntityStyle
            {
                LineWeight = LineWeight.FromMillimeters(0.1)
            });

        EntityScreenStyle style = EntityScreenStyleResolver.Resolve(
            document,
            entity,
            isSelected: true);

        Assert.Equal(2.25, style.LineWeight);
        Assert.Equal(CadColor.FromRgb(0, 191, 255), style.Color);
    }
}
