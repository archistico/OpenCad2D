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
    public void Resolve_ShouldUseLineFormatAppearance_WhenEntityHasOwnStyle()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Walls");
        var formatId = new LineFormatId("WallsFormat");

        document.ReplaceLineFormats(new LineFormatCollection(new[]
        {
            new LineFormat(
                formatId,
                "Walls format",
                CadColor.FromRgb(12, 34, 56),
                LineWeight.FromMillimeters(3.5),
                LineStyle.Dashed)
        }));

        document.Layers.Add(new Layer(
            layerId,
            "Walls",
            formatId));

        var entity = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId,
            style: new EntityStyle
            {
                Color = CadColor.FromRgb(200, 200, 200),
                LineWeight = LineWeight.FromMillimeters(99)
            });

        EntityScreenStyle style = EntityScreenStyleResolver.Resolve(
            document,
            entity,
            isSelected: false);

        Assert.Equal(CadColor.FromRgb(12, 34, 56), style.Color);
        Assert.Equal(3.5, style.LineWeight);
        Assert.Equal(LineStyle.Dashed, style.LineStyle);
    }

    [Fact]
    public void Resolve_ShouldKeepLineFormatWeightAndStyle_WhenEntityIsSelected()
    {
        var document = new CadDocument();
        var layerId = new LayerId("Furniture");
        var formatId = new LineFormatId("FurnitureFormat");

        document.ReplaceLineFormats(new LineFormatCollection(new[]
        {
            new LineFormat(
                formatId,
                "Furniture format",
                CadColor.FromRgb(120, 80, 40),
                LineWeight.FromMillimeters(2.25),
                LineStyle.DashDot)
        }));

        document.Layers.Add(new Layer(
            layerId,
            "Furniture",
            formatId));

        var entity = new LineEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            layerId: layerId,
            style: new EntityStyle
            {
                Color = CadColor.FromRgb(1, 2, 3),
                LineWeight = LineWeight.FromMillimeters(0.1)
            });

        EntityScreenStyle style = EntityScreenStyleResolver.Resolve(
            document,
            entity,
            isSelected: true);

        Assert.Equal(CadColor.FromRgb(0, 191, 255), style.Color);
        Assert.Equal(2.25, style.LineWeight);
        Assert.Equal(LineStyle.DashDot, style.LineStyle);
    }
}
